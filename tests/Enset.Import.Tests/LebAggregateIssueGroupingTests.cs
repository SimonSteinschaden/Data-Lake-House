using Enset.Api.Mapping;
using Enset.Application.Imports.Enums;
using Enset.Application.Imports.Issues;
using Enset.Application.Imports.Reports;
using Enset.Application.Imports.Resolution;
using Xunit;

namespace Enset.Import.Tests;

public sealed class LebAggregateIssueGroupingTests
{
    [Fact]
    public void MixedNumberFormatsProduceOneLebGroupAndResolveTogether()
    {
        var issues = new[]
        {
            Number("Jän", "1,23", NumberFormatPattern.AustrianDecimal),
            Number("Feb", "1.23", NumberFormatPattern.InvariantDecimal),
            Number("Mär", "1 234,56", NumberFormatPattern.AustrianDecimal),
            Number("AnnualTotal", "1234.56",
                NumberFormatPattern.InvariantDecimal),
            Number("Other", "12xx", NumberFormatPattern.Invalid)
        };
        var report = Report(issues);

        var response = report.ToResponse();
        var group = Assert.Single(response.Issues);

        Assert.Equal(5, group.MatchingIssueCount);
        Assert.Equal(5, group.CompatibleIssueTypeCount);
        Assert.Contains(
            ResolutionScope.MatchingIssueTypeInCurrentImport,
            group.SupportedScopes);
        Assert.Contains(
            group.AllowedResolutions,
            option => option.Type == ImportResolutionAction.IgnoreInvalidValue);
        Assert.DoesNotContain(
            group.AllowedResolutions,
            option => option.Type is ImportResolutionAction.ParseDeAt or
                ImportResolutionAction.ParseInvariant);

        var result = Resolve(
            report,
            issues[0],
            ImportResolutionAction.IgnoreInvalidValue);

        Assert.Equal(5, result.MatchedIssueCount);
        Assert.Equal(5, result.ResolvedIssueCount);
        Assert.Equal(0, result.FailedIssueCount);
    }

    [Fact]
    public void MissingDataAcrossFieldsProducesOneNonBlockingLebGroup()
    {
        var issues = Enumerable.Range(0, 1_000)
            .Select(index => Missing(
                index % 2 == 0 ? "Baujahr" : $"Field-{index}",
                index))
            .ToArray();
        var report = Report(issues);

        var response = report.ToResponse();
        var group = Assert.Single(response.Issues);

        Assert.Equal(1_000, group.MatchingIssueCount);
        Assert.Equal(0, report.BlockingOpenIssueCount);
        var ignore = Assert.Single(
            group.AllowedResolutions,
            option => option.Type ==
                ImportResolutionAction.IgnoreMissingValue);
        Assert.Equal(
            "Fehlende Werte bewusst leer übernehmen",
            ignore.Label);

        var result = Resolve(
            report,
            issues[0],
            ImportResolutionAction.IgnoreMissingValue);

        Assert.Equal(1_000, result.MatchedIssueCount);
        Assert.Equal(1_000, result.ResolvedIssueCount);
    }

    [Fact]
    public void MixedLebReportProducesExactlyTwoAggregateGroups()
    {
        var report = Report(
            Enumerable.Range(0, 18)
                .Select(index => Number(
                    $"Number-{index}",
                    index % 2 == 0 ? "1,23" : "1.23",
                    index % 2 == 0
                        ? NumberFormatPattern.AustrianDecimal
                        : NumberFormatPattern.InvariantDecimal))
                .Concat(Enumerable.Range(0, 27)
                    .Select(index => Missing($"Missing-{index}", index)))
                .ToArray());

        var response = report.ToResponse();

        Assert.Equal(2, response.ReturnedIssueCount);
        Assert.Equal(
            18,
            Assert.Single(response.Issues,
                issue => issue.Type == ImportIssueType.InvalidNumberFormat)
                .MatchingIssueCount);
        Assert.Equal(
            27,
            Assert.Single(response.Issues,
                issue => issue.Type == ImportIssueType.MissingData)
                .MatchingIssueCount);
    }

    private static ApplyResolutionRuleResult Resolve(
        ImportReport report,
        ImportIssue seed,
        ImportResolutionAction action) =>
        new ApplyResolutionService().ApplyRule(
            report,
            new ApplyResolutionRuleCommand
            {
                SeedIssueId = seed.IssueId,
                Scope = ResolutionScope.MatchingIssueTypeInCurrentImport,
                ResolutionType = ImportResolutionType.ExistingAction,
                ResolutionAction = action
            },
            "test-user",
            DateTime.UtcNow);

    private static ImportReport Report(IEnumerable<ImportIssue> issues)
    {
        var report = new ImportReport
        {
            SourceType = ImportSourceType.Landesenergiebuchhaltung
        };
        report.Issues.AddRange(issues);
        report.RecalculateCommitReadiness();
        return report;
    }

    private static ImportIssue Number(
        string field,
        string value,
        NumberFormatPattern pattern) => new()
    {
        Type = ImportIssueType.InvalidNumberFormat,
        Severity = ImportIssueSeverity.Error,
        FieldName = field,
        FirstValue = value,
        TargetDataType = ResolutionTargetDataType.Decimal,
        NumberFormatPattern = pattern,
        SourceRowNumber = value.Length,
        Message = "LEB number format"
    };

    private static ImportIssue Missing(string field, int row) => new()
    {
        Type = ImportIssueType.MissingData,
        Severity = ImportIssueSeverity.Warning,
        FieldName = field,
        FirstValue = $"raw-{row}",
        SourceRowNumber = row,
        Message = "LEB missing value"
    };
}
