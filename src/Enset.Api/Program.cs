using System.Text.Json.Serialization;
using Enset.Api.Logging;
using Enset.Application.Imports.Abstractions;
using Enset.Application.Imports.Coordination;
using Enset.Application.Imports.Resolution;
using Enset.Application.Imports.WriteGate;
using Enset.Infrastructure.Imports.Analysis;
using Enset.Infrastructure.Imports.Database;
using Enset.Infrastructure.Imports.Excel;
using Enset.Infrastructure.Imports.Persistence;
using Enset.Infrastructure.Imports.RawZone;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

var appDataPath = Path.Combine(builder.Environment.ContentRootPath, "App_Data");
var reportPath = Path.Combine(appDataPath, "import-reports");
var stagingPath = Path.Combine(appDataPath, "staging");
var rawZonePath = Path.Combine(appDataPath, "raw-zone");
var outputPath = Path.Combine(appDataPath, "outputs");

builder.Services.AddSingleton<IImportLogger, ApiImportLogger>();
builder.Services.AddSingleton<IImportReportRepository>(
    new JsonImportReportRepository(reportPath));
builder.Services.AddSingleton<IImportAnalysisService>(services =>
    new ExcelImportAnalysisService(
        stagingPath,
        services.GetRequiredService<IImportReportRepository>(),
        services.GetRequiredService<IImportLogger>()));
builder.Services.AddSingleton<IApplyResolutionService, ApplyResolutionService>();
builder.Services.AddSingleton<IImportWriteGate, ImportWriteGate>();
builder.Services.AddSingleton<IImportWriter>(new ExcelImportWriter(outputPath));
builder.Services.AddSingleton<IImportWriter, DatabaseImportWriter>();
builder.Services.AddSingleton<IRawZoneWriter>(
    new FileSystemRawZoneWriter(rawZonePath));
builder.Services.AddSingleton<IImportCommitService, ImportCommitService>();

var app = builder.Build();

app.MapControllers();
app.Run();

public partial class Program;
