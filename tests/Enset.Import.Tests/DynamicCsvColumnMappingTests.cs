using System.Text;
using Enset.Application.Imports.Enums;
using Enset.Application.Imports.Abstractions;
using Enset.Application.Imports.Coordination;
using Enset.Application.Imports.DuplicationCheck.Services;
using Enset.Application.Imports.Issues;
using Enset.Application.Imports.Mapping;
using Enset.Application.Imports.Reports;
using Enset.Application.Imports.Resolution;
using Enset.Infrastructure.Imports.Readers;
using Enset.Application.Imports.Validation;
using Enset.Application.Imports.WriteGate;
using Enset.Domain.Energy;
using Enset.Infrastructure.Imports.Database;
using Enset.Infrastructure.Imports.Persistence;
using Enset.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Enset.Import.Tests;

public sealed class DynamicCsvColumnMappingTests
{
    [Fact]
    public void SynonymsAndUnitsAreDetectedAndMapped()
    {
        using var stream = Csv(
            "Meter;Datum/Uhrzeit;Verbrauch kWh;Einheit\n" +
            "M-1;2026-01-01T00:00:00Z;12,5;kWh\n");

        var reader = new CsvMeterReadingReader();
        var mapping = reader.ReadMapping(stream);
        var dto = Assert.Single(CsvMeterReadingMappingService
            .Map(mapping, null)
            .Select(MeterReadingExcelRowMapper.ToDto));

        Assert.Equal("Datum/Uhrzeit", mapping.TimestampColumn);
        Assert.Equal("Verbrauch kWh", mapping.ValueColumn);
        Assert.Equal(ImportFieldSource.FileColumn, mapping.TimestampSource);
        Assert.Equal(ImportFieldSource.FileColumn, mapping.ValueSource);
        Assert.Equal(12.5m, dto.Value);
    }

    [Fact]
    public void MultipleTimestampCandidatesRequireSelection()
    {
        using var stream = Csv(
            "Meter;Datum;Zeit;Value\nM-1;2026-01-01;00:00;1\n");

        var mapping = new CsvMeterReadingReader().ReadMapping(stream);

        Assert.Null(mapping.TimestampColumn);
        Assert.Equal(ImportFieldSource.Missing, mapping.TimestampSource);
    }

    [Fact]
    public void MultipleValueCandidatesRequireSelection()
    {
        using var stream = Csv(
            "Meter;Timestamp;Wert;Verbrauch\n" +
            "M-1;2026-01-01;1;2\n");

        var mapping = new CsvMeterReadingReader().ReadMapping(stream);

        Assert.Null(mapping.ValueColumn);
        Assert.Equal(ImportFieldSource.Missing, mapping.ValueSource);
    }

    [Fact]
    public async Task AmbiguousColumnsProduceTwoSelectionIssuesWithAllHeaders()
    {
        var path = Path.Combine(
            Path.GetTempPath(),
            $"enset-dynamic-columns-{Guid.NewGuid():N}.csv");
        try
        {
            await File.WriteAllTextAsync(
                path,
                "Meter;Datum;Zeit;Wert;Verbrauch\n" +
                "M-1;2026-01-01;00:00;1;2\n");
            var coordinator = new ImportCoordinator(
                new CsvImportReader(path, new CsvMeterReadingReader()),
                new CustomerImportMapper(),
                new ExcelImportValidator(),
                new DuplicationCheckService(),
                new NullLogger());

            var report = await coordinator.RunAsync();

            var timestamp = Assert.Single(report.Issues, issue =>
                issue.Type ==
                ImportIssueType.TimestampColumnSelectionRequired);
            var value = Assert.Single(report.Issues, issue =>
                issue.Type == ImportIssueType.ValueColumnSelectionRequired);
            Assert.All(
                new[] { timestamp, value },
                issue => Assert.Contains("Verbrauch", issue.SecondValue));
            Assert.Equal(ImportStatus.AwaitingResolution, report.Status);
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }

    [Fact]
    public async Task MissingMeterProducesIssueAndAssignmentEnablesCommit()
    {
        var path = Path.Combine(
            Path.GetTempPath(),
            $"enset-assign-meter-{Guid.NewGuid():N}.csv");
        var reportDirectory = Path.Combine(
            Path.GetTempPath(),
            $"enset-assign-meter-reports-{Guid.NewGuid():N}");
        try
        {
            await File.WriteAllTextAsync(
                path,
                "Timestamp;Value\n2026-01-01T00:00:00Z;9,5\n");
            var coordinator = new ImportCoordinator(
                new CsvImportReader(path, new CsvMeterReadingReader()),
                new CustomerImportMapper(),
                new ExcelImportValidator(),
                new DuplicationCheckService(),
                new NullLogger());
            var report = await coordinator.RunAsync();
            var issue = Assert.Single(report.Issues, candidate =>
                candidate.Type == ImportIssueType.AssignMeterRequired);
            var unresolvedGate = new ImportWriteGate().Evaluate(
                new ImportWriteContext
                {
                    ImportId = report.ImportId,
                    Report = report,
                    TargetMode = ImportTargetMode.Upsert,
                    TargetWriter = ImportWriterType.Database,
                    UserId = "assign-meter-test"
                });
            Assert.DoesNotContain(
                "Every meter reading requires an existing MeterNumber.",
                unresolvedGate.Errors);

            await using var db = CreateDatabase();
            var meter = new Meter
            {
                MeterNumber = "TARGET-METER",
                Name = "Target meter"
            };
            db.Meters.Add(meter);
            await db.SaveChangesAsync();

            Apply(report, issue, ImportResolutionAction.AssignMeter,
                meter.Id.ToString());

            var reports = new JsonImportReportRepository(reportDirectory);
            await reports.SaveAsync(report);
            var reloadedReport = await reports.GetAsync(report.ImportId);
            Assert.NotNull(reloadedReport);
            report = reloadedReport;

            Assert.Equal(meter.Id, report.AssignedMeterId);
            Assert.All(report.MeterReadings, reading =>
                Assert.Equal(meter.Id, reading.MeterId));
            Assert.Equal(ImportStatus.ReadyToCommit, report.Status);

            var writeContext = new ImportWriteContext
            {
                ImportId = report.ImportId,
                Report = report,
                TargetMode = ImportTargetMode.Upsert,
                TargetWriter = ImportWriterType.Database,
                UserId = "assign-meter-test"
            };
            Assert.True(new ImportWriteGate().CanWrite(writeContext));
            await new DatabaseImportWriter(db).WriteAsync(
                writeContext);

            var raw = Assert.Single(
                await db.ImportedMeterReadings.ToListAsync());
            var curated = Assert.Single(
                await db.MeterReadings.ToListAsync());
            Assert.Equal(meter.Id, raw.MeterId);
            Assert.Equal(meter.Id, curated.MeterId);
            Assert.Equal(9.5m, curated.Value);
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
            if (Directory.Exists(reportDirectory))
                Directory.Delete(reportDirectory, recursive: true);
        }
    }

    [Fact]
    public async Task MissingMeterCanCreateMeterFromTextboxAndCommit()
    {
        using var stream = Csv(
            "Timestamp;Value\n2026-01-01T00:00:00Z;9,5\n");
        var mapping = new CsvMeterReadingReader().ReadMapping(stream);
        var report = Report(mapping);
        var issue = new ImportIssue
        {
            Type = ImportIssueType.AssignMeterRequired,
            Severity = ImportIssueSeverity.Error,
            RequiresUserDecision = true,
            Message = "Assign meter"
        };
        report.Issues.Add(issue);

        Apply(
            report,
            issue,
            ImportResolutionAction.CreateMeter,
            " NEW-METER-1 ");

        Assert.Null(report.AssignedMeterId);
        Assert.Equal("NEW-METER-1", report.DefaultMeterNumber);
        Assert.Equal(
            "NEW-METER-1",
            Assert.Single(report.Meters).MeterNumber);
        Assert.All(
            report.MeterReadings,
            reading => Assert.Equal("NEW-METER-1", reading.MeterNumber));
        Assert.Equal(ImportStatus.ReadyToCommit, report.Status);

        await using var db = CreateDatabase();
        var writeContext = new ImportWriteContext
        {
            ImportId = report.ImportId,
            Report = report,
            TargetMode = ImportTargetMode.Upsert,
            TargetWriter = ImportWriterType.Database,
            UserId = "create-meter-test"
        };
        Assert.True(new ImportWriteGate().CanWrite(writeContext));
        await new DatabaseImportWriter(db).WriteAsync(
            writeContext);

        var meter = Assert.Single(await db.Meters.ToListAsync());
        Assert.Equal("NEW-METER-1", meter.MeterNumber);
        Assert.Equal(
            meter.Id,
            Assert.Single(await db.MeterReadings.ToListAsync()).MeterId);
    }

    [Fact]
    public void UnknownColumnsCanBeSelectedAndRemapped()
    {
        using var stream = Csv(
            "Meter;WhenObserved;AmountObserved\n" +
            "M-1;2026-01-01T00:00:00Z;7,25\n");
        var mapping = new CsvMeterReadingReader().ReadMapping(stream);
        var report = Report(mapping);
        var timestampIssue = SelectionIssue(
            ImportIssueType.TimestampColumnSelectionRequired);
        var valueIssue = SelectionIssue(
            ImportIssueType.ValueColumnSelectionRequired);
        report.Issues.AddRange([timestampIssue, valueIssue]);

        Apply(report, timestampIssue,
            ImportResolutionAction.SelectTimestampColumn, "WhenObserved");
        Apply(report, valueIssue,
            ImportResolutionAction.SelectValueColumn, "AmountObserved");

        var dto = Assert.Single(report.MeterReadings);
        Assert.NotNull(dto.Timestamp);
        Assert.Equal(7.25m, dto.Value);
        Assert.Equal(
            ImportFieldSource.UserSelectedColumn,
            dto.TimestampSource);
        Assert.Equal(ImportFieldSource.UserSelectedColumn, dto.ValueSource);
        Assert.Equal(ImportStatus.ReadyToCommit, report.Status);
    }

    [Fact]
    public void TimestampCanBeGeneratedFromStartAndInterval()
    {
        using var stream = Csv(
            "Meter;Value\nM-1;1\nM-1;2\nM-1;3\n");
        var mapping = new CsvMeterReadingReader().ReadMapping(stream);
        var report = Report(mapping);
        var issue = SelectionIssue(
            ImportIssueType.TimestampColumnSelectionRequired);
        report.Issues.Add(issue);

        Apply(
            report,
            issue,
            ImportResolutionAction.GenerateTimestamps,
            """
            {
              "startTimestamp": "2026-01-01T00:00:00Z",
              "samplingInterval": "00:15:00"
            }
            """);

        Assert.Equal(
            new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            report.MeterReadings[0].Timestamp);
        Assert.Equal(
            new DateTime(2026, 1, 1, 0, 30, 0, DateTimeKind.Utc),
            report.MeterReadings[2].Timestamp);
        Assert.All(report.MeterReadings, reading =>
        {
            Assert.Equal(ImportFieldSource.Generated, reading.TimestampSource);
            Assert.Null(reading.TimestampRaw);
        });
    }

    [Fact]
    public void MissingQualityIsGeneratedButExistingQualityWins()
    {
        using var missingStream = Csv(
            "Meter;Timestamp;Value\nM-1;2026-01-01;1\n");
        var missingMapping =
            new CsvMeterReadingReader().ReadMapping(missingStream);
        var missingDto = Assert.Single(CsvMeterReadingMappingService
            .Map(missingMapping, null)
            .Select(MeterReadingExcelRowMapper.ToDto));

        using var presentStream = Csv(
            "Meter;Timestamp;Value;Status\nM-1;2026-01-01;1;2\n");
        var presentMapping =
            new CsvMeterReadingReader().ReadMapping(presentStream);
        var presentDto = Assert.Single(CsvMeterReadingMappingService
            .Map(presentMapping, null)
            .Select(MeterReadingExcelRowMapper.ToDto));

        Assert.Equal(ImportFieldSource.Generated, missingDto.QualitySource);
        Assert.Null(missingDto.QualityRaw);
        Assert.Null(missingDto.QualityFlag);
        Assert.Equal(ImportFieldSource.FileColumn, presentDto.QualitySource);
        Assert.Equal("2", presentDto.QualityRaw);
        Assert.Equal(2, presentDto.QualityFlag);
    }

    [Fact]
    public void ValueCannotBeGenerated()
    {
        var issue = SelectionIssue(
            ImportIssueType.ValueColumnSelectionRequired);

        Assert.DoesNotContain(
            ImportResolutionOptionsProvider.GetOptions(issue),
            option => option.Action ==
                ImportResolutionAction.GenerateTimestamps);
    }

    private static ImportReport Report(
        Enset.Application.Imports.Models.CsvMeterReadingMapping mapping)
    {
        var report = new ImportReport
        {
            SourceType = ImportSourceType.Csv,
            CsvMapping = mapping
        };
        report.MeterReadings = CsvMeterReadingMappingService.Map(mapping, null)
            .Select(MeterReadingExcelRowMapper.ToDto)
            .ToList();
        return report;
    }

    private static ImportIssue SelectionIssue(ImportIssueType type) => new()
    {
        Type = type,
        Severity = ImportIssueSeverity.Error,
        RequiresUserDecision = true,
        Message = "Select CSV column"
    };

    private static void Apply(
        ImportReport report,
        ImportIssue issue,
        ImportResolutionAction action,
        string payload)
    {
        new ApplyResolutionService().Apply(
            report,
            [
                new ImportIssueResolution
                {
                    IssueId = issue.IssueId,
                    ResolutionAction = action,
                    CustomResolvedValue = payload
                }
            ],
            "test-user",
            DateTime.UtcNow);
    }

    private static MemoryStream Csv(string value) =>
        new(Encoding.UTF8.GetBytes(value));

    private static EnsetDbContext CreateDatabase()
    {
        var options = new DbContextOptionsBuilder<EnsetDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(warnings => warnings.Ignore(
                Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId
                    .TransactionIgnoredWarning))
            .Options;
        return new EnsetDbContext(options);
    }

    private sealed class NullLogger : IImportLogger
    {
        public void Info(string message) { }
        public void Warning(string message) { }
        public void Error(string message) { }
    }
}
