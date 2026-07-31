using Enset.Application.Imports.Abstractions;
using Enset.Application.Imports.Coordination;
using Enset.Application.Imports.Enums;
using Enset.Application.Imports.Issues;
using Enset.Application.Imports.Models;
using Enset.Application.Imports.Reports;
using Enset.Application.Imports.Resolution;
using Enset.Application.Imports.Validation;
using Enset.Application.Imports.WriteGate;
using Enset.Api.Mapping;
using Enset.Infrastructure.Imports.Persistence;
using Enset.Infrastructure.Imports.Persistence.Mappings;
using Xunit;

namespace Enset.Import.Tests;

public sealed class ImportCommitReadinessTests
{
    [Fact]
    public void NoIssues_IsReadyToCommit()
    {
        var report = new ImportReport();

        report.RecalculateCommitReadiness();

        Assert.Equal(ImportStatus.ReadyToCommit, report.Status);
        Assert.Empty(report.Issues);
        Assert.Equal(0, report.UnresolvedIssueCount);
    }

    [Fact]
    public void StaleAwaitingResolution_IsRecalculatedByEmptyResolutionBatch()
    {
        var report = CreateReport(CreateIssue(
            ImportIssueSeverity.Warning,
            requiresUserDecision: false,
            isResolved: false));
        report.Status = ImportStatus.AwaitingResolution;

        new ApplyResolutionService().Apply(
            report,
            [],
            "test-user",
            DateTime.UtcNow);

        Assert.Equal(ImportStatus.ReadyToCommit, report.Status);
        Assert.Equal(0, report.UnresolvedIssueCount);
    }

    [Fact]
    public void OnlyAutomaticallyResolvedIssues_IsReadyToCommit()
    {
        var report = CreateReport(CreateIssue(
            ImportIssueSeverity.Error,
            requiresUserDecision: false,
            isResolved: true));

        report.RecalculateCommitReadiness();

        Assert.Equal(ImportStatus.ReadyToCommit, report.Status);
        var issue = Assert.Single(report.Issues);
        Assert.True(issue.IsResolved);
        Assert.Equal(ImportResolutionSource.Automatic, issue.ResolutionSource);
        Assert.NotNull(issue.ResolvedAt);
    }

    [Fact]
    public void AutomaticallyAndManuallyResolvedIssues_AreReadyToCommit()
    {
        var report = CreateReport(
            CreateIssue(ImportIssueSeverity.Error, false, true),
            CreateIssue(ImportIssueSeverity.Warning, true, true));

        report.RecalculateCommitReadiness();

        Assert.Equal(ImportStatus.ReadyToCommit, report.Status);
        Assert.Equal(ImportResolutionSource.Automatic, report.Issues[0].ResolutionSource);
        Assert.Equal(ImportResolutionSource.Manual, report.Issues[1].ResolutionSource);
    }

    [Fact]
    public void OpenBlockingIssue_IsAwaitingResolution()
    {
        var report = CreateReport(CreateIssue(
            ImportIssueSeverity.Error,
            requiresUserDecision: false,
            isResolved: false));

        report.RecalculateCommitReadiness();

        Assert.Equal(ImportStatus.AwaitingResolution, report.Status);
        Assert.Equal(1, report.UnresolvedIssueCount);
        var response = report.ToResponse();
        Assert.Equal(1, response.UnresolvedIssueCount);
        Assert.Equal("1 Issue noch ungelöst.", response.ReadinessMessage);
    }

    [Fact]
    public void MultipleOpenBlockingIssues_UsePluralReadinessMessage()
    {
        var report = CreateReport(
            CreateIssue(ImportIssueSeverity.Error, false, false),
            CreateIssue(ImportIssueSeverity.Warning, true, false));

        report.RecalculateCommitReadiness();
        var response = report.ToResponse();

        Assert.Equal(ImportStatus.AwaitingResolution, response.Status);
        Assert.Equal(2, response.UnresolvedIssueCount);
        Assert.Equal("2 Issues noch ungelöst.", response.ReadinessMessage);
    }

    [Fact]
    public void SummaryAndReadinessUseTheSameOpenIssueDefinition()
    {
        var blocking = Enumerable.Range(0, 5)
            .Select(_ => CreateIssue(
                ImportIssueSeverity.Error,
                requiresUserDecision: false,
                isResolved: false));
        var report = CreateReport(
            blocking
                .Append(CreateIssue(
                    ImportIssueSeverity.Warning,
                    requiresUserDecision: false,
                    isResolved: false))
                .Append(CreateIssue(
                    ImportIssueSeverity.Error,
                    requiresUserDecision: false,
                    isResolved: true))
                .Append(CreateIssue(
                    ImportIssueSeverity.Warning,
                    requiresUserDecision: true,
                    isResolved: true))
                .ToArray());

        report.RecalculateCommitReadiness();
        var response = report.ToResponse();

        Assert.Equal(ImportStatus.AwaitingResolution, response.Status);
        Assert.Equal(8, response.IssueCount);
        Assert.Equal(6, response.OpenIssueCount);
        Assert.Equal(5, response.BlockingOpenIssueCount);
        Assert.Equal(response.BlockingOpenIssueCount, response.UnresolvedIssueCount);
        Assert.Equal(1, response.AutomaticallyResolvedIssueCount);
        Assert.Equal(1, response.ManuallyResolvedIssueCount);
        Assert.Equal("5 Issues noch ungelöst.", response.ReadinessMessage);
    }

    [Fact]
    public void ResolvingAllBlockingIssuesUpdatesSummaryToReady()
    {
        var issues = Enumerable.Range(0, 5)
            .Select(_ => CreateIssue(
                ImportIssueSeverity.Warning,
                requiresUserDecision: true,
                isResolved: false))
            .ToArray();
        var report = CreateReport(issues);

        new ApplyResolutionService().Apply(
            report,
            issues.Select(issue => new ImportIssueResolution
            {
                IssueId = issue.IssueId,
                ResolutionAction = ImportResolutionAction.KeepFirst
            }).ToArray(),
            "test-user",
            DateTime.UtcNow);
        var response = report.ToResponse();

        Assert.Equal(ImportStatus.ReadyToCommit, response.Status);
        Assert.Equal(0, response.OpenIssueCount);
        Assert.Equal(0, response.BlockingOpenIssueCount);
        Assert.Equal(5, response.ManuallyResolvedIssueCount);
        Assert.Null(response.ReadinessMessage);
    }

    [Fact]
    public void OnlyOpenNonBlockingInformation_IsReadyToCommit()
    {
        var report = CreateReport(CreateIssue(
            ImportIssueSeverity.Info,
            requiresUserDecision: false,
            isResolved: false));

        report.RecalculateCommitReadiness();

        Assert.Equal(ImportStatus.ReadyToCommit, report.Status);
    }

    [Fact]
    public void OpenWarningWithoutDecision_IsReadyToCommit()
    {
        var report = CreateReport(CreateIssue(
            ImportIssueSeverity.Warning,
            requiresUserDecision: false,
            isResolved: false));

        report.RecalculateCommitReadiness();

        Assert.Equal(ImportStatus.ReadyToCommit, report.Status);
        Assert.Equal(0, report.UnresolvedIssueCount);
    }

    [Fact]
    public void LastManualResolution_ChangesStatusToReadyToCommit()
    {
        var issue = CreateIssue(
            ImportIssueSeverity.Warning,
            requiresUserDecision: true,
            isResolved: false);
        var report = CreateReport(issue);
        report.RecalculateCommitReadiness();

        new ApplyResolutionService().Apply(
            report,
            [new ImportIssueResolution
            {
                IssueId = issue.IssueId,
                ResolutionAction = ImportResolutionAction.KeepFirst
            }],
            "test-user",
            DateTime.UtcNow);

        Assert.Equal(ImportStatus.ReadyToCommit, report.Status);
        Assert.Equal(ImportResolutionSource.Manual, issue.ResolutionSource);
        Assert.Equal("test-user", issue.ResolvedBy);
    }

    [Fact]
    public async Task ReadyStatus_SurvivesRepositoryRoundtrip()
    {
        var rootPath = Path.Combine(
            Path.GetTempPath(),
            $"enset-readiness-{Guid.NewGuid():N}");

        try
        {
            var repository = new JsonImportReportRepository(rootPath);
            var report = CreateReport(CreateIssue(
                ImportIssueSeverity.Error,
                requiresUserDecision: false,
                isResolved: true));
            report.RecalculateCommitReadiness();

            await repository.SaveAsync(report);
            var loaded = await repository.GetAsync(report.ImportId);

            Assert.NotNull(loaded);
            Assert.Equal(ImportStatus.ReadyToCommit, loaded.Status);
            Assert.False(loaded.HasOpenCommitBlockingIssues);
            Assert.Equal(0, loaded.UnresolvedIssueCount);
            var loadedIssue = Assert.Single(loaded.Issues);
            Assert.Equal(ImportResolutionSource.Automatic, loadedIssue.ResolutionSource);
            Assert.NotNull(loadedIssue.ResolvedAt);
        }
        finally
        {
            if (Directory.Exists(rootPath))
                Directory.Delete(rootPath, recursive: true);
        }
    }

    [Fact]
    public void DuplicateMeterReading_IsExplicitlyResolvedAutomatically()
    {
        var timestamp = "2026-07-21T10:00:00Z";
        var readings = new[]
        {
            new MeterReadingExcelRow
            {
                RowNumber = 2,
                MeterNumber = "M-1",
                Timestamp = timestamp,
                Value = "1.0"
            },
            new MeterReadingExcelRow
            {
                RowNumber = 3,
                MeterNumber = "M-1",
                Timestamp = timestamp,
                Value = "1.0"
            }
        };

        var report = new ExcelImportValidator().Validate(
            [],
            [],
            [],
            readings,
            ImportSourceType.Csv);

        var issue = Assert.Single(report.Issues);
        Assert.True(issue.IsResolved);
        Assert.False(issue.RequiresUserDecision);
        Assert.Equal(ImportResolutionSource.Automatic, issue.ResolutionSource);
        Assert.Equal(ImportResolutionAction.KeepFirst, issue.ResolutionAction);
    }

    [Fact]
    public async Task SixHundredTwentyEightAutomaticAndEightManualIssues_CanBeCommitted()
    {
        var automaticIssues = Enumerable.Range(0, 628)
            .Select(_ => CreateIssue(ImportIssueSeverity.Error, false, true));
        var manualIssues = Enumerable.Range(0, 8)
            .Select(_ => CreateIssue(ImportIssueSeverity.Warning, true, true));
        var report = CreateReport(automaticIssues.Concat(manualIssues).ToArray());

        report.RecalculateCommitReadiness();

        Assert.Equal(636, report.IssueCount);
        Assert.Equal(628, report.Issues.Count(issue =>
            issue.ResolutionSource == ImportResolutionSource.Automatic));
        Assert.Equal(8, report.Issues.Count(issue =>
            issue.ResolutionSource == ImportResolutionSource.Manual));
        Assert.DoesNotContain(report.Issues, issue => !issue.IsResolved);
        Assert.Equal(ImportStatus.ReadyToCommit, report.Status);

        var writer = new RecordingWriter();
        var service = new ImportCommitService(
            new InMemoryReportRepository(report),
            new ImportWriteGate(),
            [writer]);
        var result = await service.CommitAsync(CreateCommitCommand(report.ImportId));

        Assert.True(result.Succeeded);
        Assert.Equal(1, writer.CallCount);
        Assert.Equal(ImportStatus.Committed, result.Report!.Status);
    }

    [Fact]
    public void AutomaticResolution_IsExposedByApiDto()
    {
        var report = CreateReport(CreateIssue(
            ImportIssueSeverity.Error,
            requiresUserDecision: false,
            isResolved: true));

        var issue = Assert.Single(report.ToResponse().Issues);

        Assert.True(issue.IsResolved);
        Assert.False(issue.RequiresUserDecision);
        Assert.Equal(ImportResolutionSource.Automatic, issue.ResolutionSource);
        Assert.NotNull(issue.ResolvedAt);
    }

    [Fact]
    public void AutomaticResolution_SurvivesEfPersistenceMapping()
    {
        var report = CreateReport(CreateIssue(
            ImportIssueSeverity.Error,
            requiresUserDecision: false,
            isResolved: true));

        var mapped = ImportReportPersistenceMapper.ToModel(
            ImportReportPersistenceMapper.ToEntity(report));
        var issue = Assert.Single(mapped.Issues);

        Assert.True(issue.IsResolved);
        Assert.Equal(ImportResolutionSource.Automatic, issue.ResolutionSource);
        Assert.NotNull(issue.ResolvedAt);
    }

    [Fact]
    public async Task Commit_AfterCompleteResolution_Succeeds()
    {
        var issue = CreateIssue(ImportIssueSeverity.Warning, true, false);
        var report = CreateReport(issue);
        report.RecalculateCommitReadiness();
        new ApplyResolutionService().Apply(
            report,
            [new ImportIssueResolution
            {
                IssueId = issue.IssueId,
                ResolutionAction = ImportResolutionAction.KeepFirst
            }],
            "test-user",
            DateTime.UtcNow);
        var writer = new RecordingWriter();
        var service = new ImportCommitService(
            new InMemoryReportRepository(report),
            new ImportWriteGate(),
            [writer]);

        var result = await service.CommitAsync(CreateCommitCommand(report.ImportId));

        Assert.True(result.Succeeded);
        Assert.Equal(1, writer.CallCount);
        Assert.Equal(ImportStatus.Committed, result.Report!.Status);
    }

    [Fact]
    public async Task Commit_WithOpenBlockingIssue_RemainsBlocked()
    {
        var report = CreateReport(CreateIssue(ImportIssueSeverity.Error, false, false));
        report.RecalculateCommitReadiness();
        var writer = new RecordingWriter();
        var service = new ImportCommitService(
            new InMemoryReportRepository(report),
            new ImportWriteGate(),
            [writer]);

        var result = await service.CommitAsync(CreateCommitCommand(report.ImportId));

        Assert.False(result.Succeeded);
        Assert.Equal(0, writer.CallCount);
    }

    private static ImportReport CreateReport(params ImportIssue[] issues)
    {
        var report = new ImportReport();
        report.Issues.AddRange(issues);
        return report;
    }

    private static ImportIssue CreateIssue(
        ImportIssueSeverity severity,
        bool requiresUserDecision,
        bool isResolved)
    {
        var issue = new ImportIssue
        {
            Type = ImportIssueType.InvalidValue,
            Severity = severity,
            Message = "Test issue",
            RequiresUserDecision = requiresUserDecision
        };

        if (isResolved && requiresUserDecision)
        {
            issue.ResolveManually(
                ImportResolutionAction.KeepFirst,
                null,
                "test-user",
                DateTime.UtcNow);
        }
        else if (isResolved)
        {
            issue.ResolveAutomatically(
                ImportResolutionAction.KeepFirst,
                DateTime.UtcNow);
        }

        return issue;
    }

    private static ImportCommitCommand CreateCommitCommand(Guid importId) => new()
    {
        ImportId = importId,
        UserId = "test-user",
        TargetMode = ImportTargetMode.Upsert,
        TargetWriter = ImportWriterType.Excel,
        TargetLocation = "output.xlsx",
        ArchiveRawSource = false
    };

    private sealed class InMemoryReportRepository : IImportReportRepository
    {
        private ImportReport _report;

        public InMemoryReportRepository(ImportReport report) => _report = report;

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
            Task.FromResult(_report.ImportId == importId ? _report : null);
    }

    private sealed class RecordingWriter : IImportWriter
    {
        public ImportWriterType WriterType => ImportWriterType.Excel;

        public int CallCount { get; private set; }

        public Task WriteAsync(
            ImportWriteContext context,
            CancellationToken cancellationToken = default)
        {
            CallCount++;
            return Task.CompletedTask;
        }
    }
}
