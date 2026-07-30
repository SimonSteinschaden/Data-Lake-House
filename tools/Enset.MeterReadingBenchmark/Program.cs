using System.Diagnostics;
using System.Text.Json;
using Enset.Application.Imports.Enums;
using Enset.Application.Imports.Reports;
using Enset.Infrastructure.Imports.MassImport;
using Enset.Infrastructure.Imports.Persistence;
using Enset.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

if (args.Length != 2)
    throw new ArgumentException(
        "Usage: <connection-string> <csv-path>");

var connectionString = args[0];
var csvPath = Path.GetFullPath(args[1]);
var file = new FileInfo(csvPath);
if (!file.Exists)
    throw new FileNotFoundException("Benchmark CSV not found.", csvPath);

var dbOptions = new DbContextOptionsBuilder<EnsetDbContext>()
    .UseNpgsql(connectionString)
    .Options;
await using var db = new EnsetDbContext(dbOptions);
await db.Database.ExecuteSqlRawAsync(
    """
    TRUNCATE TABLE
      "ImportStagingMeterReadings",
      "MeterReadingImportAudits",
      "MeterReadingImportJobs",
      "MeterReadings",
      "Meters" CASCADE;
    """);
var meterId = Guid.NewGuid();
await db.Database.ExecuteSqlInterpolatedAsync($"""
    INSERT INTO "Meters"
      ("Id","MeterNumber","Name","Medium","Quantity","Unit","Direction",
       "Type","IsActive","CreatedAtUtc","DataOrigin","IsDeleted",
       "LastModifiedSource")
    VALUES
      ({meterId}, 'AT001000000000000001', 'Benchmark meter',
       'Electricity', 'Energy', 'KWh', 'Import', 'SmartMeter', TRUE,
       NOW(), 'Imported', FALSE, 'Import');
    """);

var reader = new MeterReadingStreamingReader();
var analyzer = new MeterReadingStreamingAnalyzer(reader);
var samples = new List<long>();
var process = Process.GetCurrentProcess();
var cpuStart = process.TotalProcessorTime;
using var samplingCancellation = new CancellationTokenSource();
var sampler = Task.Run(async () =>
{
    while (!samplingCancellation.IsCancellationRequested)
    {
        process.Refresh();
        samples.Add(process.WorkingSet64);
        await Task.Delay(100);
    }
});

var analysisStarted = DateTime.UtcNow;
var analysis = await analyzer.Analyze(
    csvPath, "AT001000000000000001", 10_000, default);
var analysisCompleted = DateTime.UtcNow;

var reportRoot = Path.Combine(
    Path.GetTempPath(), "enset-benchmark-reports",
    Guid.NewGuid().ToString("N"));
var reports = new JsonImportReportRepository(reportRoot);
var importId = Guid.NewGuid();
var report = new ImportReport
{
    ImportId = importId,
    SourceType = ImportSourceType.Csv,
    AssignedMeterId = meterId,
    DefaultMeterNumber = "AT001000000000000001",
    SourceFile = new ImportSourceFileMetadata
    {
        FileName = file.Name,
        Length = file.Length,
        StagedPath = csvPath,
        Sha256 = "BENCHMARK"
    },
    MeterReadingAnalysis = analysis.Summary,
    MeterReadings = analysis.Samples,
    MeterReadingCount = checked((int)analysis.Summary.ReadRows),
    Issues = analysis.Issues.ToList()
};
await reports.SaveAsync(report);
var job = new MeterReadingImportJobEntity
{
    JobId = Guid.NewGuid(),
    ImportId = importId,
    TargetMode = ImportTargetMode.Upsert.ToString(),
    Status = "Queued",
    Phase = "Queued",
    TotalBytes = file.Length,
    AcceptedAtUtc = DateTime.UtcNow,
    UpdatedAtUtc = DateTime.UtcNow
};
db.MeterReadingImportJobs.Add(job);
await db.SaveChangesAsync();

var commitStarted = DateTime.UtcNow;
var processor = new MeterReadingMassImportProcessor(
    db,
    reports,
    reader,
    Options.Create(new MeterReadingMassImportOptions
    {
        ChunkSize = 10_000
    }),
    NullLogger<MeterReadingMassImportProcessor>.Instance);
await processor.Process(job.JobId, default);
var commitCompleted = DateTime.UtcNow;

samplingCancellation.Cancel();
await sampler;
process.Refresh();
var cpu = process.TotalProcessorTime - cpuStart;
var persisted = await db.MeterReadingImportJobs
    .AsNoTracking()
    .SingleAsync(x => x.JobId == job.JobId);
var duration = commitCompleted - commitStarted;
var output = new
{
    file = file.Name,
    fileBytes = file.Length,
    rows = persisted.ReadRows,
    chunkSize = 10_000,
    chunks = persisted.CurrentBatch,
    copyOperations = persisted.CurrentBatch,
    startUtc = commitStarted,
    endUtc = commitCompleted,
    durationSeconds = duration.TotalSeconds,
    averageRowsPerSecond = persisted.ReadRows /
        Math.Max(duration.TotalSeconds, 0.001),
    conflictsAndDuplicates = persisted.DuplicateRows,
    rejects = persisted.RejectedRows,
    written = persisted.WrittenRows,
    analysisSeconds = (analysisCompleted - analysisStarted).TotalSeconds,
    peakWorkingSetBytes = samples.Count == 0
        ? process.WorkingSet64 : samples.Max(),
    averageWorkingSetBytes = samples.Count == 0
        ? process.WorkingSet64 : samples.Average(),
    cpuSeconds = cpu.TotalSeconds,
    maxEfTrackedEntries = db.ChangeTracker.Entries().Count()
};
Console.WriteLine(JsonSerializer.Serialize(
    output,
    new JsonSerializerOptions { WriteIndented = true }));
