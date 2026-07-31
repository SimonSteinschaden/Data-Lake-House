using System.Diagnostics;
using Enset.Api.Mapping;
using Enset.Application.Imports.Enums;
using Enset.Application.Imports.Issues;
using Enset.Application.Imports.Models;
using Enset.Application.Imports.Reports;
using Enset.Application.Imports.Resolution;
using Enset.Application.Imports.Validation;
using Xunit;
using Xunit.Abstractions;

namespace Enset.Import.Tests;

public sealed class ScalableGroupResolutionTests
{
    private readonly ITestOutputHelper _output;

    public ScalableGroupResolutionTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void TenCompatibleNumbersAreResolvedByOneRule()
    {
        var report = Report(Enumerable.Range(0, 10)
            .Select(index => NumberIssue("AnnualTotal", $"{index + 1},25"))
            .ToArray());

        var result = ResolveNumbers(report, report.Issues[0]);

        Assert.Equal(10, result.MatchedIssueCount);
        Assert.Equal(10, result.ResolvedIssueCount);
        Assert.Equal(0, result.FailedIssueCount);
    }

    [Theory]
    [InlineData(10)]
    [InlineData(1_000)]
    [InlineData(100_000)]
    public void LargeNumberGroupIsLinearAndCompletes(int issueCount)
    {
        var report = Report(Enumerable.Range(0, issueCount)
            .Select(index => NumberIssue(
                "AnnualTotal",
                $"{(index % 9000) + 1},25"))
            .ToArray());
        var stopwatch = Stopwatch.StartNew();

        var result = ResolveNumbers(report, report.Issues[0]);

        stopwatch.Stop();
        _output.WriteLine(
            "Issues={0}; DurationMs={1}",
            issueCount,
            stopwatch.ElapsedMilliseconds);
        Assert.Equal(issueCount, result.MatchedIssueCount);
        Assert.Equal(issueCount, result.ResolvedIssueCount);
        Assert.Equal(0, result.FailedIssueCount);
        Assert.True(
            stopwatch.Elapsed < TimeSpan.FromSeconds(30),
            $"Resolution took {stopwatch.Elapsed}.");
    }

    [Fact]
    public void MissingValuesGroupByField()
    {
        var firstName = MissingValue("CustomerName");
        var secondName = MissingValue("CustomerName");
        var postalCode = MissingValue("PostalCode");
        var report = Report(firstName, secondName, postalCode);

        var result = new ApplyResolutionService().ApplyRule(
            report,
            Command(
                firstName,
                ResolutionScope.MatchingIssuesInCurrentImport,
                ImportResolutionAction.IgnoreMissingValue),
            "tester",
            DateTime.UtcNow);

        Assert.Equal(3, result.MatchedIssueCount);
        Assert.True(firstName.IsResolved);
        Assert.True(secondName.IsResolved);
        Assert.True(postalCode.IsResolved);
    }

    [Fact]
    public void RdwReferencesGroupOnlyWhenSuggestedTargetMatches()
    {
        var first = ReferenceIssue("CUSTOMER-1", 2);
        var sameTarget = ReferenceIssue("CUSTOMER-1", 3);
        var otherTarget = ReferenceIssue("CUSTOMER-2", 4);

        Assert.True(ImportIssueCompatibility.MatchesCurrentGroup(
            first,
            sameTarget));
        Assert.False(ImportIssueCompatibility.MatchesCurrentGroup(
            first,
            otherTarget));
        Assert.False(ImportIssueCompatibility.MatchesIssueType(
            first,
            otherTarget));
    }

    [Fact]
    public void RepresentativeLimitDoesNotLimitResolutionInput()
    {
        var targetIssues = Enumerable.Range(0, 10)
            .Select(index => NumberIssue("AnnualTotal", $"{index + 1},25"))
            .ToList();
        var unrelated = Enumerable.Range(0, 501)
            .Select(index => new ImportIssue
            {
                Type = ImportIssueType.InvalidValue,
                Severity = ImportIssueSeverity.Error,
                FieldName = $"Field-{index}",
                FirstValue = index.ToString(),
                Message = "Unrelated"
            });
        var report = Report(targetIssues.Concat(unrelated).ToArray());

        var response = report.ToResponse();
        var result = ResolveNumbers(report, targetIssues[0]);

        Assert.Equal(500, response.ReturnedIssueCount);
        Assert.True(response.HasMoreIssues);
        Assert.Equal(10, result.MatchedIssueCount);
        Assert.All(targetIssues, issue => Assert.True(issue.IsResolved));
    }

    [Fact]
    public void ResolutionIsIsolatedToSelectedImport()
    {
        var firstImport = Report(
            NumberIssue("AnnualTotal", "1,25"),
            NumberIssue("AnnualTotal", "2,25"));
        var secondImport = Report(
            NumberIssue("AnnualTotal", "1,25"),
            NumberIssue("AnnualTotal", "2,25"));

        ResolveNumbers(firstImport, firstImport.Issues[0]);

        Assert.All(firstImport.Issues, issue => Assert.True(issue.IsResolved));
        Assert.All(secondImport.Issues, issue => Assert.False(issue.IsResolved));
    }

    [Fact]
    public void LebMeterIdentityDoesNotUseMeterName()
    {
        var meters = new[]
        {
            new MeterExcelRow
            {
                RowNumber = 2,
                MeterNumber = "LEB:GEM:1:GEB:10:Z:100",
                ProfileName = "Hauptzähler"
            },
            new MeterExcelRow
            {
                RowNumber = 3,
                MeterNumber = "LEB:GEM:2:GEB:20:Z:100",
                ProfileName = "Hauptzähler"
            }
        };

        var report = new ExcelImportValidator().Validate(
            [],
            [],
            meters,
            [],
            ImportSourceType.CRM_Excel);

        Assert.DoesNotContain(
            report.Issues,
            issue => issue.Type == ImportIssueType.DuplicateMeter);
    }

    private static ApplyResolutionRuleResult ResolveNumbers(
        ImportReport report,
        ImportIssue seed) =>
        new ApplyResolutionService().ApplyRule(
            report,
            Command(
                seed,
                ResolutionScope.MatchingIssuesInCurrentImport,
                ImportResolutionAction.ParseDeAt),
            "tester",
            DateTime.UtcNow);

    private static ApplyResolutionRuleCommand Command(
        ImportIssue seed,
        ResolutionScope scope,
        ImportResolutionAction action) => new()
    {
        SeedIssueId = seed.IssueId,
        Scope = scope,
        ResolutionType = action == ImportResolutionAction.ParseDeAt
            ? ImportResolutionType.ParseWithCulture
            : ImportResolutionType.ExistingAction,
        ResolutionAction = action
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

    private static ImportIssue NumberIssue(string field, string value) => new()
    {
        Type = ImportIssueType.InvalidNumberFormat,
        Severity = ImportIssueSeverity.Error,
        FieldName = field,
        FirstValue = value,
        TargetDataType = ResolutionTargetDataType.Decimal,
        NumberFormatPattern = NumberFormatPattern.AustrianDecimal,
        ValuePattern = ImportIssueValuePattern.GermanDecimal,
        Message = "Number format"
    };

    private static ImportIssue MissingValue(string field) => new()
    {
        Type = ImportIssueType.MissingData,
        Severity = ImportIssueSeverity.Error,
        FieldName = field,
        Message = "Missing value"
    };

    private static ImportIssue ReferenceIssue(
        string suggestedCustomerId,
        int row) => new()
    {
        Type = ImportIssueType.MissingCustomer,
        Severity = ImportIssueSeverity.Error,
        FieldName = "Building.InternalCustomerId",
        SourceRowNumber = row,
        FirstValue = $"Building-{row}",
        SecondValue = suggestedCustomerId,
        Message = "Reference"
    };
}
