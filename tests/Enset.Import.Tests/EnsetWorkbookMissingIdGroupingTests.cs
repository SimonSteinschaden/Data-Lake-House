using Enset.Api.Mapping;
using Enset.Application.Imports.Enums;
using Enset.Application.Imports.Issues;
using Enset.Application.Imports.Reports;
using Enset.Application.Imports.Resolution;
using Xunit;

namespace Enset.Import.Tests;

public sealed class EnsetWorkbookMissingIdGroupingTests
{
    [Theory]
    [InlineData(ImportIssueType.MissingCustomer, 10)]
    [InlineData(ImportIssueType.MissingBuilding, 1_000)]
    [InlineData(ImportIssueType.MissingMeter, 100_000)]
    public void MissingIdsProduceOneRepresentativeAndResolveCompletely(
        ImportIssueType issueType,
        int issueCount)
    {
        var report = Report(Enumerable.Range(0, issueCount)
            .Select(index => Issue(issueType, index))
            .ToArray());

        var response = report.ToResponse();
        var representative = Assert.Single(response.Issues);

        Assert.Equal(issueCount, representative.MatchingIssueCount);
        Assert.Equal(issueCount, representative.CompatibleIssueTypeCount);
        Assert.Contains(
            ResolutionScope.MatchingIssueTypeInCurrentImport,
            representative.SupportedScopes);

        var result = Resolve(report, report.Issues[0]);

        Assert.Equal(issueCount, result.MatchedIssueCount);
        Assert.Equal(issueCount, result.ResolvedIssueCount);
        Assert.Equal(0, result.FailedIssueCount);
        Assert.Empty(report.ToResponse().Issues);
    }

    [Fact]
    public void MixedMissingIdsProduceExactlyThreeRepresentatives()
    {
        var report = Report(
            Enumerable.Range(0, 10)
                .Select(index => Issue(ImportIssueType.MissingCustomer, index))
                .Concat(Enumerable.Range(0, 20)
                    .Select(index => Issue(ImportIssueType.MissingBuilding, index)))
                .Concat(Enumerable.Range(0, 30)
                    .Select(index => Issue(ImportIssueType.MissingMeter, index)))
                .ToArray());

        var response = report.ToResponse();

        Assert.Equal(3, response.ReturnedIssueCount);
        Assert.Equal(
            10,
            Assert.Single(response.Issues,
                issue => issue.Type == ImportIssueType.MissingCustomer)
                .MatchingIssueCount);
        Assert.Equal(
            20,
            Assert.Single(response.Issues,
                issue => issue.Type == ImportIssueType.MissingBuilding)
                .MatchingIssueCount);
        Assert.Equal(
            30,
            Assert.Single(response.Issues,
                issue => issue.Type == ImportIssueType.MissingMeter)
                .MatchingIssueCount);
    }

    [Fact]
    public void RowWorksheetFieldAndRawValueDoNotSplitWorkbookIdGroup()
    {
        var issues = Enumerable.Range(0, 10)
            .Select(index =>
            {
                var issue = Issue(ImportIssueType.MissingCustomer, index);
                issue.FieldName = index % 2 == 0
                    ? "Customer.InternalCustomerId"
                    : "Building.InternalCustomerId";
                issue.FirstValue = $"raw-{index}";
                issue.SecondValue = $"target-{index}";
                return issue;
            })
            .ToArray();
        var report = Report(issues);

        var representative = Assert.Single(report.ToResponse().Issues);
        var result = Resolve(report, issues[0]);

        Assert.Equal(10, representative.MatchingIssueCount);
        Assert.Equal(10, result.MatchedIssueCount);
        Assert.Equal(10, result.ResolvedIssueCount);
    }

    [Fact]
    public void FiveHundredRepresentativeLimitDoesNotLimitMissingIdMatch()
    {
        var missing = Enumerable.Range(0, 1_000)
            .Select(index => Issue(ImportIssueType.MissingBuilding, index))
            .ToList();
        var unrelated = Enumerable.Range(0, 501)
            .Select(index => new ImportIssue
            {
                Type = ImportIssueType.InvalidValue,
                Severity = ImportIssueSeverity.Error,
                FieldName = $"Other-{index}",
                FirstValue = index.ToString(),
                Message = "Unrelated"
            });
        var report = Report(missing.Concat(unrelated).ToArray());

        var response = report.ToResponse();
        var group = Assert.Single(
            response.Issues,
            issue => issue.Type == ImportIssueType.MissingBuilding);
        var result = Resolve(report, missing[0]);

        Assert.Equal(500, response.ReturnedIssueCount);
        Assert.True(response.HasMoreIssues);
        Assert.Equal(1_000, group.MatchingIssueCount);
        Assert.Equal(1_000, result.ResolvedIssueCount);
    }

    private static ApplyResolutionRuleResult Resolve(
        ImportReport report,
        ImportIssue seed) =>
        new ApplyResolutionService().ApplyRule(
            report,
            new ApplyResolutionRuleCommand
            {
                SeedIssueId = seed.IssueId,
                Scope = ResolutionScope.MatchingIssueTypeInCurrentImport,
                ResolutionType = ImportResolutionType.ExistingAction,
                ResolutionAction = ImportResolutionAction.SkipRow
            },
            "test-user",
            DateTime.UtcNow);

    private static ImportReport Report(params ImportIssue[] issues)
    {
        var report = new ImportReport
        {
            SourceType = ImportSourceType.EnsetWorkbook
        };
        report.Issues.AddRange(issues);
        report.RecalculateCommitReadiness();
        return report;
    }

    private static ImportIssue Issue(ImportIssueType type, int index) => new()
    {
        Type = type,
        Severity = ImportIssueSeverity.Error,
        RequiresUserDecision = true,
        FieldName = $"Field-{index % 3}",
        SourceRowNumber = index + 2,
        FirstValue = $"raw-{index}",
        SecondValue = $"target-{index}",
        Message = $"Worksheet-{index % 4}: missing id"
    };
}
