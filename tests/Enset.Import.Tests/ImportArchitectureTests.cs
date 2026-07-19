using Enset.Api.Controllers;
using Enset.Application.Imports.Abstractions;
using Enset.Application.Imports.Coordination;
using Enset.Application.Imports.Decisions;
using Enset.Application.Imports.DTOs;
using Enset.Application.Imports.DuplicationCheck.Abstractions;
using Enset.Application.Imports.Enums;
using Enset.Application.Imports.Issues;
using Enset.Application.Imports.Models;
using Enset.Application.Imports.Reports;
using Enset.Application.Imports.Resolution;
using Enset.Application.Imports.WriteGate;
using Enset.Infrastructure.Imports.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Enset.Import.Tests;

public sealed class ImportArchitectureTests
{
    [Fact]
    public void Infrastructure_ContainsSingleCanonicalDbContext()
    {
        var infrastructureAssembly =
            typeof(Enset.Infrastructure.Persistence.EnsetDbContext).Assembly;

        var contextTypes = infrastructureAssembly
            .GetTypes()
            .Where(type =>
                !type.IsAbstract &&
                typeof(DbContext).IsAssignableFrom(type))
            .ToList();

        var contextType = Assert.Single(contextTypes);
        Assert.Equal(
            typeof(Enset.Infrastructure.Persistence.EnsetDbContext),
            contextType);
    }

    [Fact]
    public async Task ImportCoordinator_RemainsAnalyzeOnly()
    {
        var constructorParameterTypes = typeof(ImportCoordinator)
            .GetConstructors()
            .Single()
            .GetParameters()
            .Select(parameter => parameter.ParameterType)
            .ToList();

        Assert.DoesNotContain(typeof(IImportWriter), constructorParameterTypes);
        Assert.DoesNotContain(typeof(IImportWriteGate), constructorParameterTypes);

        var coordinator = new ImportCoordinator(
            new StubReader(),
            new StubMapper(),
            new StubValidator(),
            new StubDuplicationCheck(),
            new StubLogger());

        var report = await coordinator.RunAsync();

        Assert.Single(report.Customers);
        Assert.Equal(ImportStatus.ReadyToCommit, report.Status);
    }

    [Fact]
    public async Task ImportReport_CanBeSavedAndLoaded()
    {
        var rootPath = Path.Combine(Path.GetTempPath(), $"enset-tests-{Guid.NewGuid():N}");
        try
        {
            var repository = new JsonImportReportRepository(rootPath);
            var report = CreateReport(ImportStatus.AwaitingResolution, resolved: false);
            report.SourceFile = new ImportSourceFileMetadata
            {
                FileName = "source.xlsx",
                ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                Length = 42,
                Sha256 = "ABC",
                StagedPath = "staging/source.xlsx"
            };
            report.AuditTrail.Add(new ImportAuditEntry
            {
                UserId = "tester",
                Action = "AnalysisCompleted"
            });

            await repository.SaveAsync(report);
            var loaded = await repository.GetAsync(report.ImportId);

            Assert.NotNull(loaded);
            Assert.Equal(report.ImportId, loaded.ImportId);
            Assert.Equal(ImportStatus.AwaitingResolution, loaded.Status);
            Assert.Single(loaded.Issues);
            Assert.Single(loaded.AuditTrail);
            Assert.Equal("source.xlsx", loaded.SourceFile!.FileName);
        }
        finally
        {
            if (Directory.Exists(rootPath))
                Directory.Delete(rootPath, recursive: true);
        }
    }

    [Fact]
    public void ApplyResolutionService_AllowsDecisionChangesAndAuditsEachChange()
    {
        var report = CreateReport(ImportStatus.AwaitingResolution, resolved: false);
        var issue = Assert.Single(report.Issues);
        var service = new ApplyResolutionService();

        service.Apply(
            report,
            [new ImportIssueResolution
            {
                IssueId = issue.IssueId,
                ResolutionAction = ImportResolutionAction.KeepFirst
            }],
            "user-1",
            DateTime.UtcNow);

        service.Apply(
            report,
            [new ImportIssueResolution
            {
                IssueId = issue.IssueId,
                ResolutionAction = ImportResolutionAction.UseCustomValue,
                CustomResolvedValue = "Merged customer"
            }],
            "user-2",
            DateTime.UtcNow.AddMinutes(1));

        Assert.True(issue.IsResolved);
        Assert.Equal(ImportResolutionAction.UseCustomValue, issue.ResolutionAction);
        Assert.Equal("Merged customer", issue.CustomResolvedValue);
        Assert.Equal(2, report.AuditTrail.Count);
        Assert.Equal(ImportResolutionAction.KeepFirst,
            report.AuditTrail[1].PreviousResolutionAction);
        Assert.Equal(ImportStatus.ReadyToCommit, report.Status);
    }

    [Fact]
    public void ImportWriteGate_BlocksOpenIssues()
    {
        var report = CreateReport(ImportStatus.AwaitingResolution, resolved: false);
        var result = new ImportWriteGate().Evaluate(CreateContext(report));

        Assert.False(result.IsAllowed);
        Assert.Contains(result.Errors, error => error.Contains("unresolved", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void ImportWriteGate_AllowsResolvedIssues()
    {
        var report = CreateReport(ImportStatus.ReadyToCommit, resolved: true);
        var result = new ImportWriteGate().Evaluate(CreateContext(report));

        Assert.True(result.IsAllowed);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public async Task Commit_InvokesWriterOnlyAfterSuccessfulGate()
    {
        var blockedReport = CreateReport(ImportStatus.AwaitingResolution, resolved: false);
        var repository = new InMemoryReportRepository(blockedReport);
        var writer = new RecordingWriter();
        var service = new ImportCommitService(
            repository,
            new ImportWriteGate(),
            [writer]);

        var blockedResult = await service.CommitAsync(CreateCommitRequest(blockedReport.ImportId));

        Assert.False(blockedResult.Succeeded);
        Assert.Equal(0, writer.CallCount);

        var allowedReport = CreateReport(ImportStatus.ReadyToCommit, resolved: true);
        repository.Report = allowedReport;

        var allowedResult = await service.CommitAsync(CreateCommitRequest(allowedReport.ImportId));

        Assert.True(allowedResult.Succeeded);
        Assert.Equal(1, writer.CallCount);
        Assert.Equal(ImportStatus.Committed, allowedReport.Status);
    }

    [Fact]
    public void ApiController_HasNoDirectWriterDependency()
    {
        var controllerType = typeof(ImportsController);

        Assert.DoesNotContain(
            controllerType.GetFields(
                System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.NonPublic),
            field => typeof(IImportWriter).IsAssignableFrom(field.FieldType));

        Assert.DoesNotContain(
            controllerType.GetConstructors().SelectMany(constructor => constructor.GetParameters()),
            parameter => typeof(IImportWriter).IsAssignableFrom(parameter.ParameterType));
    }

    private static ImportReport CreateReport(ImportStatus status, bool resolved)
    {
        return new ImportReport
        {
            Status = status,
            Decision = new ImportDecision
            {
                Type = resolved ? ImportDecisionType.Continue : ImportDecisionType.Abort,
                Reason = "Test"
            },
            Customers = [new CustomerImportDto { CompanyName = "Test customer" }],
            Issues =
            [
                new ImportIssue
                {
                    Type = ImportIssueType.DuplicateCustomer,
                    Severity = ImportIssueSeverity.Error,
                    Message = "Duplicate",
                    RequiresUserDecision = true,
                    IsResolved = resolved,
                    ResolutionAction = resolved
                        ? ImportResolutionAction.KeepFirst
                        : ImportResolutionAction.None
                }
            ]
        };
    }

    private static ImportWriteContext CreateContext(ImportReport report)
    {
        return new ImportWriteContext
        {
            ImportId = report.ImportId,
            Report = report,
            TargetMode = ImportTargetMode.Upsert,
            TargetWriter = ImportWriterType.Excel,
            TargetLocation = "output.xlsx",
            UserId = "tester",
            Timestamp = DateTime.UtcNow
        };
    }

    private static ImportCommitCommand CreateCommitRequest(Guid importId)
    {
        return new ImportCommitCommand
        {
            ImportId = importId,
            UserId = "tester",
            TargetMode = ImportTargetMode.Upsert,
            TargetWriter = ImportWriterType.Excel,
            TargetLocation = "output.xlsx",
            ArchiveRawSource = false
        };
    }

    private sealed class StubReader : IImportReader
    {
        public ImportWorkbook Read() => new();
    }

    private sealed class StubMapper : IImportMapper
    {
        public IReadOnlyList<CustomerImportDto> Map(
            IReadOnlyCollection<CustomerExcelRow> rows) =>
            [new CustomerImportDto { CompanyName = "Test" }];
    }

    private sealed class StubValidator : IImportValidator
    {
        public ImportReport Validate(
            IReadOnlyList<CustomerExcelRow> customers,
            IReadOnlyList<BuildingExcelRow> buildings) => new();
    }

    private sealed class StubDuplicationCheck : IDuplicationCheckService
    {
        public IReadOnlyCollection<ImportIssue> DetectCustomers(
            IReadOnlyCollection<CustomerImportDto> customers) => [];
    }

    private sealed class StubLogger : IImportLogger
    {
        public void Info(string message) { }
        public void Warning(string message) { }
        public void Error(string message) { }
    }

    private sealed class InMemoryReportRepository : IImportReportRepository
    {
        public InMemoryReportRepository(ImportReport report) => Report = report;

        public ImportReport Report { get; set; }

        public Task SaveAsync(
            ImportReport report,
            CancellationToken cancellationToken = default)
        {
            Report = report;
            return Task.CompletedTask;
        }

        public Task<ImportReport?> GetAsync(
            Guid importId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(Report.ImportId == importId ? Report : null);
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
