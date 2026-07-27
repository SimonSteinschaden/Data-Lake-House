using Enset.Application.Imports.Abstractions;
using Enset.Application.Imports.Coordination;
using Enset.Application.Imports.DuplicationCheck.Services;
using Enset.Application.Imports.Enums;
using Enset.Application.Imports.Issues;
using Enset.Application.Imports.Mapping;
using Enset.Application.Imports.Reports;
using Enset.Application.Imports.Resolution;
using Enset.Application.Imports.Validation;
using Enset.Application.Imports.WriteGate;
using Enset.Infrastructure.Imports.Database;
using Enset.Infrastructure.Imports.Excel;
using Enset.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Xunit;

namespace Enset.Import.Tests;

public sealed class CRM_ExcelIntegrationTests
{
    [Fact]
    public void MapReferenceGroup_UpdatesAllCompatibleBuildingReferences()
    {
        var first = MissingBuildingCustomerIssue(2, "CUSTOMER-1");
        var second = MissingBuildingCustomerIssue(3, "CUSTOMER-1");
        var other = MissingBuildingCustomerIssue(4, "CUSTOMER-2");
        var report = new ImportReport
        {
            SourceType = ImportSourceType.CRM_Excel,
            Customers =
            [
                new()
                {
                    ExternalCustomerId = "CUSTOMER-1",
                    CompanyName = "First"
                },
                new()
                {
                    ExternalCustomerId = "CUSTOMER-2",
                    CompanyName = "Second"
                }
            ],
            Buildings =
            [
                new() { SourceRowNumber = 2, ExternalBuildingId = "B-1" },
                new() { SourceRowNumber = 3, ExternalBuildingId = "B-2" },
                new() { SourceRowNumber = 4, ExternalBuildingId = "B-3" }
            ],
            Issues = [first, second, other]
        };
        report.RecalculateCommitReadiness();

        var result = new ApplyResolutionService().ApplyRule(
            report,
            new ApplyResolutionRuleCommand
            {
                SeedIssueId = first.IssueId,
                Scope = ResolutionScope.MatchingIssuesInCurrentImport,
                ResolutionType = ImportResolutionType.ExistingAction,
                ResolutionAction = ImportResolutionAction.MapReference,
                ResolutionPayload = "CUSTOMER-1"
            },
            "tester",
            DateTime.UtcNow);

        Assert.Equal(2, result.MatchedIssueCount);
        Assert.Equal(2, result.ResolvedIssueCount);
        Assert.Equal(
            ["CUSTOMER-1", "CUSTOMER-1", null],
            report.Buildings.Select(building => building.ExternalCustomerId));
        Assert.False(other.IsResolved);
    }

    [Fact]
    public async Task RealRdwWorkbook_ResolveCommitAndReimport_IsConsistent()
    {
        var databaseName = $"rdw-workbook-{Guid.NewGuid():N}";
        var options = new DbContextOptionsBuilder<EnsetDbContext>()
            .UseInMemoryDatabase(databaseName)
            .ConfigureWarnings(warnings =>
                warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        await using var db = new EnsetDbContext(options);
        await db.Database.EnsureCreatedAsync();

        var firstReport = await AnalyzeWorkbookAsync(
            "Datenbasis Grundlage RDW.xlsm");
        var firstClassification = ResolveRdwReport(firstReport);

        Assert.Equal(564, firstClassification.SuggestedMappings);
        Assert.True(firstClassification.ManualCreations > 0);
        Assert.True(firstClassification.SkippedBuildings > 0);
        Assert.Equal(0, firstReport.BlockingOpenIssueCount);
        Assert.Equal(ImportStatus.ReadyToCommit, firstReport.Status);
        Assert.All(
            firstReport.Buildings,
            building => Assert.Contains(
                firstReport.Customers,
                customer => string.Equals(
                    customer.ExternalCustomerId,
                    building.ExternalCustomerId,
                    StringComparison.OrdinalIgnoreCase)));

        var firstResult = await CommitAsync(firstReport, db);
        Assert.True(firstResult.Succeeded);
        Assert.Equal(ImportStatus.Committed, firstResult.Report!.Status);
        Assert.Equal(
            await db.Buildings.CountAsync(),
            await db.CustomerBuildingAssignments.CountAsync());
        Assert.All(
            await db.CustomerBuildingAssignments.ToListAsync(),
            assignment =>
            {
                Assert.Contains(
                    db.Customers,
                    customer => customer.Id == assignment.CustomerId);
                Assert.Contains(
                    db.Buildings,
                    building => building.Id == assignment.BuildingId);
            });

        var countsAfterFirstCommit = await CountsAsync(db);
        var secondReport = await AnalyzeWorkbookAsync(
            "Datenbasis Grundlage RDW.xlsm");
        var secondClassification = ResolveRdwReport(secondReport);
        var secondResult = await CommitAsync(secondReport, db);

        Assert.Equal(firstClassification, secondClassification);
        Assert.True(secondResult.Succeeded);
        Assert.Equal(ImportStatus.Committed, secondResult.Report!.Status);
        Assert.Equal(countsAfterFirstCommit, await CountsAsync(db));
    }

    [Fact]
    public async Task RealCRM_Excel_AnalyzeCommitAndReimport_IsRelationallyConsistent()
    {
        var databaseName = $"crm-excel-{Guid.NewGuid():N}";
        var options = new DbContextOptionsBuilder<EnsetDbContext>()
            .UseInMemoryDatabase(databaseName)
            .ConfigureWarnings(warnings =>
                warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        await using var db = new EnsetDbContext(options);
        await db.Database.EnsureCreatedAsync();

        var firstReport = await AnalyzeWorkbookAsync(
            "ENSET_Testimport_MVP.xlsx");
        Assert.Equal(ImportSourceType.CRM_Excel, firstReport.SourceType);
        Assert.Equal(ImportStatus.ReadyToCommit, firstReport.Status);
        Assert.Equal(1, firstReport.CustomerCount);
        Assert.Equal(1, firstReport.BuildingCount);
        Assert.Equal(1, firstReport.MeterCount);
        Assert.Equal(5, firstReport.MeterReadingCount);
        Assert.Empty(firstReport.Issues);

        var firstResult = await CommitAsync(firstReport, db);
        Assert.True(firstResult.Succeeded);
        Assert.Equal(ImportStatus.Committed, firstResult.Report!.Status);

        var customer = await db.Customers.SingleAsync(
            item => item.CustomerNumber == "CUST-0001");
        var building = await db.Buildings.SingleAsync(
            item => item.BuildingNumber == "BLD-0001");
        var assignment = await db.CustomerBuildingAssignments.SingleAsync();
        var meter = await db.Meters.SingleAsync(
            item => item.MeterNumber == "AT001000000000000001");

        Assert.Equal(customer.Id, assignment.CustomerId);
        Assert.Equal(building.Id, assignment.BuildingId);
        Assert.Equal(building.Id, meter.BuildingId);
        Assert.Equal(
            5,
            await db.MeterReadings.CountAsync(
                reading => reading.MeterId == meter.Id));

        var countsAfterFirstCommit = await CountsAsync(db);
        var secondReport = await AnalyzeWorkbookAsync(
            "ENSET_Testimport_MVP.xlsx");
        var secondResult = await CommitAsync(secondReport, db);

        Assert.True(secondResult.Succeeded);
        Assert.Equal(ImportStatus.Committed, secondResult.Report!.Status);
        Assert.Equal(countsAfterFirstCommit, await CountsAsync(db));
        Assert.Equal(
            5,
            await db.MeterReadings
                .Where(reading => reading.MeterId == meter.Id)
                .Select(reading => reading.Timestamp)
                .Distinct()
                .CountAsync());
    }

    private static async Task<ImportReport> AnalyzeWorkbookAsync(
        string fileName)
    {
        var workbookPath = Path.Combine(
            AppContext.BaseDirectory,
            "TestData",
            fileName);
        Assert.True(File.Exists(workbookPath), workbookPath);

        return await new ImportCoordinator(
                new ExcelImportReader(
                    new ExcelWorkbookReader(),
                    workbookPath),
                new CustomerImportMapper(),
                new ExcelImportValidator(),
                new DuplicationCheckService(),
                new NullImportLogger())
            .RunAsync();
    }

    private static RdwResolutionClassification ResolveRdwReport(
        ImportReport report)
    {
        var suggestedMappings = 0;
        var manualCreations = new HashSet<string>(
            StringComparer.OrdinalIgnoreCase);
        var skippedRows = new HashSet<int>();
        var resolutions = new List<ImportIssueResolution>();

        foreach (var issue in report.Issues)
        {
            var action = ImportResolutionAction.KeepSeparate;
            string? payload = null;

            if (issue.Type == ImportIssueType.MissingCustomer &&
                issue.FieldName == "Building.InternalCustomerId")
            {
                if (!string.IsNullOrWhiteSpace(issue.SecondValue))
                {
                    action = ImportResolutionAction.MapReference;
                    payload = issue.SecondValue;
                    suggestedMappings++;
                }
                else if (string.IsNullOrWhiteSpace(
                             issue.FirstValue?.Replace("|", string.Empty)) ||
                         issue.FirstValue.StartsWith(
                             "PRIVAT|",
                             StringComparison.OrdinalIgnoreCase))
                {
                    action = ImportResolutionAction.SkipRow;
                    skippedRows.Add(issue.SourceRowNumber!.Value);
                }
                else
                {
                    action = ImportResolutionAction.CreateNew;
                    manualCreations.Add(issue.FirstValue);
                }
            }
            else if (issue.Type is ImportIssueType.MissingCustomer or
                     ImportIssueType.MissingBuilding)
            {
                action = ImportResolutionAction.CreateNew;
                if (issue.Type == ImportIssueType.MissingCustomer)
                    manualCreations.Add(issue.FirstValue ?? string.Empty);
            }

            resolutions.Add(new ImportIssueResolution
            {
                IssueId = issue.IssueId,
                ResolutionAction = action,
                CustomResolvedValue = payload
            });
        }

        new ApplyResolutionService().Apply(
            report,
            resolutions,
            "rdw-integration-test",
            DateTime.UtcNow);

        return new RdwResolutionClassification(
            suggestedMappings,
            manualCreations.Count,
            skippedRows.Count);
    }

    private static async Task<ImportCommitResult> CommitAsync(
        ImportReport report,
        EnsetDbContext db)
    {
        var repository = new InMemoryReportRepository(report);
        var service = new ImportCommitService(
            repository,
            new ImportWriteGate(),
            [new DatabaseImportWriter(db)]);

        return await service.CommitAsync(new ImportCommitCommand
        {
            ImportId = report.ImportId,
            UserId = "integration-test",
            Timestamp = DateTime.UtcNow,
            TargetMode = ImportTargetMode.Upsert,
            TargetWriter = ImportWriterType.Database,
            ArchiveRawSource = false
        });
    }

    private static async Task<(int Customers, int Buildings, int Assignments,
        int Meters, int Readings)> CountsAsync(EnsetDbContext db) => (
        await db.Customers.CountAsync(),
        await db.Buildings.CountAsync(),
        await db.CustomerBuildingAssignments.CountAsync(),
        await db.Meters.CountAsync(),
        await db.MeterReadings.CountAsync());

    private sealed class InMemoryReportRepository : IImportReportRepository
    {
        private ImportReport _report;

        public InMemoryReportRepository(ImportReport report)
        {
            _report = report;
        }

        public Task SaveAsync(
            ImportReport report,
            CancellationToken cancellationToken = default)
        {
            _report = report;
            return Task.CompletedTask;
        }

        public Task<ImportReport?> GetAsync(
            Guid importId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<ImportReport?>(
                _report.ImportId == importId ? _report : null);
    }

    private sealed class NullImportLogger : IImportLogger
    {
        public void Info(string message) { }
        public void Warning(string message) { }
        public void Error(string message) { }
    }

    private static ImportIssue MissingBuildingCustomerIssue(
        int sourceRowNumber,
        string suggestedCustomerId) => new()
    {
        Type = ImportIssueType.MissingCustomer,
        Severity = ImportIssueSeverity.Error,
        RequiresUserDecision = true,
        FieldName = "Building.InternalCustomerId",
        SourceRowNumber = sourceRowNumber,
        FirstValue = $"GROUP-{sourceRowNumber}",
        SecondValue = suggestedCustomerId,
        Message = "Customer reference requires resolution."
    };

    private sealed record RdwResolutionClassification(
        int SuggestedMappings,
        int ManualCreations,
        int SkippedBuildings);
}
