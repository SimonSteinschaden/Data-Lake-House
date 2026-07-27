using System.Text.Json;
using Enset.Application.Imports.Enums;
using Enset.Application.Imports.Issues;
using Enset.Application.Imports.Reports;
using Enset.Application.Imports.Resolution;
using Enset.Infrastructure.Imports.Persistence;
using Enset.Api.Mapping;
using Xunit;

namespace Enset.Import.Tests;

public sealed class ImportResolutionRuleTests
{
    [Fact]
    public void GroupRule_ResolvesMatchingIssuesOnlyAndRecalculatesStatus()
    {
        var matching = Enumerable.Range(0, 3)
            .Select(_ => Issue("AnnualTotal", ImportIssueValuePattern.GermanDecimal))
            .ToList();
        var otherField = Issue("Jan", ImportIssueValuePattern.GermanDecimal);
        var otherPattern = Issue(
            "AnnualTotal", ImportIssueValuePattern.MissingAnnualTotalWithMonthlyValues);
        var report = Report([.. matching, otherField, otherPattern]);

        var result = Apply(report, matching[0],
            ResolutionScope.MatchingIssuesInCurrentImport);

        Assert.Equal(3, result.MatchedIssueCount);
        Assert.Equal(3, result.ResolvedIssueCount);
        Assert.All(matching, issue => Assert.True(issue.IsResolved));
        Assert.False(otherField.IsResolved);
        Assert.False(otherPattern.IsResolved);
        Assert.Equal(2, result.RemainingBlockingIssueCount);
        Assert.Equal(ImportStatus.AwaitingResolution, result.Status);
    }

    [Fact]
    public void SingleIssue_ResolvesOnlySeedIssue()
    {
        var first = Issue("AnnualTotal", ImportIssueValuePattern.GermanDecimal);
        var second = Issue("AnnualTotal", ImportIssueValuePattern.GermanDecimal);
        var report = Report([first, second]);

        var result = Apply(report, first, ResolutionScope.SingleIssue);

        Assert.Equal(1, result.MatchedIssueCount);
        Assert.True(first.IsResolved);
        Assert.False(second.IsResolved);
    }

    [Fact]
    public void ReapplyingSameRule_IsIdempotent()
    {
        var issues = Enumerable.Range(0, 5)
            .Select(_ => Issue("AnnualTotal", ImportIssueValuePattern.GermanDecimal))
            .ToList();
        var report = Report(issues);
        var ruleId = Guid.NewGuid();

        var first = Apply(report, issues[0],
            ResolutionScope.MatchingIssuesInCurrentImport, ruleId);
        var second = Apply(report, issues[0],
            ResolutionScope.MatchingIssuesInCurrentImport, ruleId);

        Assert.Equal(5, first.ResolvedIssueCount);
        Assert.Equal(0, second.ResolvedIssueCount);
        Assert.Single(report.ResolutionRules);
        Assert.Single(report.AuditTrail);
    }

    [Fact]
    public void RuleSkipsAlreadyResolvedMatchingIssue()
    {
        var resolved = Issue("AnnualTotal", ImportIssueValuePattern.GermanDecimal);
        resolved.ResolveAutomatically(
            ImportResolutionAction.KeepFirst, DateTime.UtcNow);
        var open = Issue("AnnualTotal", ImportIssueValuePattern.GermanDecimal);
        var report = Report([resolved, open]);

        var result = Apply(report, open,
            ResolutionScope.MatchingIssuesInCurrentImport);

        Assert.Equal(2, result.MatchedIssueCount);
        Assert.Equal(1, result.ResolvedIssueCount);
        Assert.Equal(ImportStatus.ReadyToCommit, report.Status);
    }

    [Fact]
    public async Task RuleSurvivesRepositoryRoundtrip()
    {
        var root = Path.Combine(
            Path.GetTempPath(), $"enset-resolution-rules-{Guid.NewGuid():N}");
        try
        {
            var issue = Issue("AnnualTotal", ImportIssueValuePattern.GermanDecimal);
            var report = Report([issue]);
            var result = Apply(report, issue,
                ResolutionScope.MatchingIssuesInCurrentImport);
            var repository = new JsonImportReportRepository(root);

            await repository.SaveAsync(report);
            var loaded = await repository.GetAsync(report.ImportId);

            var rule = Assert.Single(loaded!.ResolutionRules);
            Assert.Equal(result.RuleId, rule.Id);
            Assert.Equal(1, rule.MatchedIssueCount);
            Assert.Equal(ResolutionScope.MatchingIssuesInCurrentImport, rule.Scope);
            Assert.Equal(result.RuleId, Assert.Single(loaded.Issues).ResolutionRuleId);
            Assert.Equal(ImportStatus.ReadyToCommit, loaded.Status);
        }
        finally
        {
            if (Directory.Exists(root))
                Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void ThirtyFiveThousandIssues_AreResolvedByOneRule()
    {
        var issues = Enumerable.Range(0, 35_000)
            .Select(_ => Issue("AnnualTotal", ImportIssueValuePattern.GermanDecimal))
            .ToList();
        var report = Report(issues);

        var result = Apply(report, issues[0],
            ResolutionScope.MatchingIssuesInCurrentImport);

        Assert.Equal(35_000, result.MatchedIssueCount);
        Assert.Equal(35_000, result.ResolvedIssueCount);
        Assert.Equal(0, result.RemainingBlockingIssueCount);
        Assert.Equal(ImportStatus.ReadyToCommit, result.Status);
        Assert.Single(report.ResolutionRules);
        Assert.Single(report.AuditTrail);
    }

    [Fact]
    public void LargeReport_ReturnsServerGroupedBoundedIssuePayload()
    {
        var issues = Enumerable.Range(0, 35_000)
            .Select(_ => Issue("AnnualTotal", ImportIssueValuePattern.GermanDecimal))
            .ToList();
        var report = Report(issues);

        var response = report.ToResponse();

        Assert.Equal(35_000, response.IssueCount);
        Assert.False(response.HasMoreIssues);
        Assert.Equal(1, response.ReturnedIssueCount);
        Assert.Equal(35_000, response.Issues[0].MatchingIssueCount);
        Assert.True(response.Issues[0].SupportsGroupResolution);
        Assert.Contains(
            response.Issues[0].AllowedResolutions,
            option =>
                option.Type == ImportResolutionAction.ParseDeAt &&
                option.SupportsBatch);
    }

    [Fact]
    public void ServiceHasNoRepositoryDependencyAndFutureScopeIsDisabled()
    {
        Assert.Empty(typeof(ApplyResolutionService).GetConstructors()
            .Single().GetParameters());
        var issue = Issue("AnnualTotal", ImportIssueValuePattern.GermanDecimal);
        var report = Report([issue]);

        Assert.Throws<InvalidOperationException>(() =>
            Apply(report, issue, ResolutionScope.FutureImports));
    }

    private static ApplyResolutionRuleResult Apply(
        ImportReport report,
        ImportIssue seed,
        ResolutionScope scope,
        Guid? ruleId = null) =>
        new ApplyResolutionService().ApplyRule(
            report,
            new ApplyResolutionRuleCommand
            {
                RuleId = ruleId ?? Guid.NewGuid(),
                SeedIssueId = seed.IssueId,
                Scope = scope,
                ResolutionType = ImportResolutionType.ParseWithCulture,
                ResolutionAction = ImportResolutionAction.ParseDeAt
            },
            "test-user",
            DateTime.UtcNow);

    private static ImportReport Report(IEnumerable<ImportIssue> issues)
    {
        var report = new ImportReport
        {
            SourceType = ImportSourceType.CRM_Excel
        };
        report.Issues.AddRange(issues);
        report.RecalculateCommitReadiness();
        return report;
    }

    private static ImportIssue Issue(
        string field,
        ImportIssueValuePattern pattern) => new()
    {
        Type = ImportIssueType.InvalidNumberFormat,
        Severity = ImportIssueSeverity.Error,
        FieldName = field,
        FirstValue = "1.202,48",
        ValuePattern = pattern,
        TargetDataType = ResolutionTargetDataType.Decimal,
        NumberFormatPattern = pattern == ImportIssueValuePattern.GermanDecimal
            ? NumberFormatPattern.AustrianDecimal
            : NumberFormatPattern.Invalid,
        Message = "Structured test issue."
    };
}
