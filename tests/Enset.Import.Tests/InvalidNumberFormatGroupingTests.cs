using Enset.Application.Imports.Enums;
using Enset.Application.Imports.Issues;
using Enset.Application.Imports.Reports;
using Enset.Application.Imports.Resolution;
using Enset.Api.Mapping;
using Enset.Infrastructure.Imports.Persistence;
using Xunit;

namespace Enset.Import.Tests;

public sealed class InvalidNumberFormatGroupingTests
{
    [Theory]
    [InlineData("2.465,17", NumberFormatPattern.AustrianDecimal)]
    [InlineData("1202,48", NumberFormatPattern.AustrianDecimal)]
    [InlineData("1 202,48", NumberFormatPattern.AustrianDecimal)]
    [InlineData("1,202.48", NumberFormatPattern.InvariantDecimal)]
    [InlineData("12xx,48", NumberFormatPattern.Invalid)]
    [InlineData("01.02.2026", NumberFormatPattern.Invalid)]
    public void DetectorClassifiesStructuredSourceValue(
        string value,
        NumberFormatPattern expected)
    {
        Assert.Equal(expected, NumberFormatPatternDetector.Detect(value));
    }

    [Fact]
    public void LebNumberGroupSpansFieldsAndDetectedPatterns()
    {
        var annual = NumberIssue("AnnualTotal", "2.465,17");
        var january = NumberIssue("Jan", "1.202,48");
        var march = NumberIssue("Mrz", "3.141,06");
        var invariant = NumberIssue("Feb", "1,202.48");
        var invalid = NumberIssue("Apr", "12xx,48");
        var report = Report(annual, january, march, invariant, invalid);

        var result = new ApplyResolutionService().ApplyRule(
            report,
            new ApplyResolutionRuleCommand
            {
                SeedIssueId = annual.IssueId,
                Scope = ResolutionScope.MatchingIssueTypeInCurrentImport,
                ResolutionType = ImportResolutionType.ExistingAction,
                ResolutionAction = ImportResolutionAction.IgnoreInvalidValue
            },
            "user",
            DateTime.UtcNow);

        Assert.Equal(5, result.MatchedIssueCount);
        Assert.Equal(5, result.ResolvedIssueCount);
        Assert.Equal(0, result.FailedIssueCount);
        Assert.All(report.Issues, issue => Assert.True(issue.IsResolved));
        Assert.Equal(0, result.RemainingBlockingIssueCount);
    }

    [Fact]
    public void MisclassifiedUnparseableValueFailsWithoutAbortingGroup()
    {
        var valid = NumberIssue("AnnualTotal", "2.465,17");
        var invalid = NumberIssue("AnnualTotal", "12xx,48");
        invalid.NumberFormatPattern = NumberFormatPattern.AustrianDecimal;
        invalid.ValuePattern = ImportIssueValuePattern.GermanDecimal;
        var report = Report(valid, invalid);

        var result = ApplyGroup(report, valid);

        Assert.Equal(2, result.MatchedIssueCount);
        Assert.Equal(1, result.ResolvedIssueCount);
        Assert.Equal(1, result.FailedIssueCount);
        Assert.True(valid.IsResolved);
        Assert.False(invalid.IsResolved);
        Assert.Equal("12xx,48", invalid.FirstValue);
        Assert.Equal(ImportStatus.AwaitingResolution, report.Status);

        var response = report.ToResponse();
        var openGroup = Assert.Single(
            response.Issues,
            issue => !issue.IsResolved);
        Assert.Equal(1, openGroup.MatchingIssueCount);
        Assert.Equal(1, response.OpenIssueCount);
        Assert.Equal(1, response.BlockingOpenIssueCount);
    }

    [Fact]
    public async Task GroupDecisionSurvivesReloadAndSingleModeStillWorks()
    {
        var first = NumberIssue("AnnualTotal", "2.465,17");
        var second = NumberIssue("Jan", "1.202,48");
        var report = Report(first, second);

        var single = new ApplyResolutionService().ApplyRule(
            report,
            Command(first, ResolutionScope.SingleIssue),
            "user",
            DateTime.UtcNow);
        Assert.Equal(1, single.ResolvedIssueCount);
        Assert.False(second.IsResolved);

        var group = ApplyGroup(report, second);
        Assert.Equal(1, group.ResolvedIssueCount);
        Assert.Equal(ImportStatus.ReadyToCommit, report.Status);

        var root = Path.Combine(Path.GetTempPath(), $"enset-number-rule-{Guid.NewGuid():N}");
        try
        {
            var repository = new JsonImportReportRepository(root);
            await repository.SaveAsync(report);
            var loaded = await repository.GetAsync(report.ImportId);
            Assert.Equal(2, loaded!.ResolutionRules.Count);
            Assert.All(loaded.Issues, issue => Assert.True(issue.IsResolved));
            Assert.Equal(ImportStatus.ReadyToCommit, loaded.Status);
        }
        finally
        {
            if (Directory.Exists(root))
                Directory.Delete(root, recursive: true);
        }
    }

    private static ApplyResolutionRuleResult ApplyGroup(
        ImportReport report,
        ImportIssue seed) =>
        new ApplyResolutionService().ApplyRule(
            report,
            Command(seed, ResolutionScope.MatchingIssuesInCurrentImport),
            "user",
            DateTime.UtcNow);

    private static ApplyResolutionRuleResult ApplyTypeGroup(
        ImportReport report,
        ImportIssue seed) =>
        new ApplyResolutionService().ApplyRule(
            report,
            Command(seed, ResolutionScope.MatchingIssueTypeInCurrentImport),
            "user",
            DateTime.UtcNow);

    private static ApplyResolutionRuleCommand Command(
        ImportIssue seed,
        ResolutionScope scope) => new()
    {
        SeedIssueId = seed.IssueId,
        Scope = scope,
        ResolutionType = ImportResolutionType.ParseWithCulture,
        ResolutionAction = ImportResolutionAction.ParseDeAt
    };

    private static ImportReport Report(params ImportIssue[] issues)
    {
        var report = new ImportReport
        {
            SourceType = ImportSourceType.Landesenergiebuchhaltung
        };
        report.Issues.AddRange(issues);
        report.RecalculateCommitReadiness();
        return report;
    }

    private static ImportIssue NumberIssue(string field, string value)
    {
        var pattern = NumberFormatPatternDetector.Detect(value);
        return new ImportIssue
        {
            Type = ImportIssueType.InvalidNumberFormat,
            Severity = ImportIssueSeverity.Error,
            FieldName = field,
            FirstValue = value,
            TargetDataType = ResolutionTargetDataType.Decimal,
            NumberFormatPattern = pattern,
            ValuePattern = pattern == NumberFormatPattern.AustrianDecimal
                ? ImportIssueValuePattern.GermanDecimal
                : ImportIssueValuePattern.ExactValue,
            Message = "Structured number issue."
        };
    }
}
