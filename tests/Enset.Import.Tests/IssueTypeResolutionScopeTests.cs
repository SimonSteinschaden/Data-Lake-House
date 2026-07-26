using Enset.Application.Imports.Enums;
using Enset.Application.Imports.Issues;
using Enset.Application.Imports.Reports;
using Enset.Application.Imports.Resolution;
using Enset.Infrastructure.Imports.Persistence;
using Enset.Api.Mapping;
using Xunit;

namespace Enset.Import.Tests;

public sealed class IssueTypeResolutionScopeTests
{
    [Fact]
    public void EnsetWorkbookIssueTypeScopeResolvesAllMissingCustomers()
    {
        var first = Issue(
            ImportIssueType.MissingCustomer,
            "CustomerReference",
            "C-1");
        var second = Issue(
            ImportIssueType.MissingCustomer,
            "CustomerReference",
            "C-2");
        var differentContext = Issue(
            ImportIssueType.MissingCustomer,
            "BillingCustomerReference",
            "C-3");
        var building = Issue(
            ImportIssueType.MissingBuilding,
            "BuildingReference",
            "B-1");
        var report = Report(first, second, differentContext, building);

        var result = Apply(
            report,
            first,
            ResolutionScope.MatchingIssueTypeInCurrentImport,
            ImportResolutionAction.SkipRow);

        Assert.Equal(3, result.MatchedIssueCount);
        Assert.Equal(3, result.ResolvedIssueCount);
        Assert.Equal(0, result.SkippedIssueCount);
        Assert.True(first.IsResolved);
        Assert.True(second.IsResolved);
        Assert.True(differentContext.IsResolved);
        Assert.False(building.IsResolved);
    }

    [Fact]
    public void CurrentGroupResolvesTheDisplayedMissingCustomerGroup()
    {
        var first = Issue(
            ImportIssueType.MissingCustomer,
            "CustomerReference",
            "C-1");
        var second = Issue(
            ImportIssueType.MissingCustomer,
            "CustomerReference",
            "C-2");
        var report = Report(first, second);

        var result = Apply(
            report,
            first,
            ResolutionScope.MatchingIssuesInCurrentImport,
            ImportResolutionAction.CreateNew);

        Assert.Equal(2, result.MatchedIssueCount);
        Assert.True(first.IsResolved);
        Assert.True(second.IsResolved);
    }

    [Fact]
    public void DuplicateResolutionCannotMatchMissingCustomer()
    {
        var duplicate = Issue(
            ImportIssueType.DuplicateCustomer,
            "Customer",
            "Same");
        var missing = Issue(
            ImportIssueType.MissingCustomer,
            "Customer",
            "Same");
        var report = Report(duplicate, missing);

        var result = Apply(
            report,
            duplicate,
            ResolutionScope.MatchingIssueTypeInCurrentImport,
            ImportResolutionAction.KeepFirst);

        Assert.Equal(1, result.ResolvedIssueCount);
        Assert.True(duplicate.IsResolved);
        Assert.False(missing.IsResolved);
    }

    [Fact]
    public void SameIssueTypeWithDifferentResolutionSetRemainsSeparate()
    {
        var numeric = Issue(
            ImportIssueType.MissingData,
            "AnnualTotal",
            string.Empty);
        numeric.TargetDataType = ResolutionTargetDataType.Decimal;
        var text = Issue(
            ImportIssueType.MissingData,
            "Comment",
            string.Empty);
        var report = Report(numeric, text);

        var response = report.ToResponse();
        var numericResponse = Assert.Single(
            response.Issues,
            issue => issue.IssueId == numeric.IssueId);
        Assert.Equal(1, numericResponse.CompatibleIssueTypeCount);

        var result = Apply(
            report,
            numeric,
            ResolutionScope.MatchingIssueTypeInCurrentImport,
            ImportResolutionAction.SetZero);

        Assert.Equal(1, result.ResolvedIssueCount);
        Assert.True(numeric.IsResolved);
        Assert.False(text.IsResolved);
    }

    [Fact]
    public void MissingCustomerGroupExposesBatchResolution()
    {
        var first = Issue(
            ImportIssueType.MissingCustomer,
            "CustomerReference",
            "C-1");
        var second = Issue(
            ImportIssueType.MissingCustomer,
            "CustomerReference",
            "C-2");

        var response = Report(first, second).ToResponse();
        var group = Assert.Single(response.Issues);

        Assert.Equal(2, group.MatchingIssueCount);
        Assert.True(group.SupportsGroupResolution);
        Assert.Contains(
            group.AllowedResolutions,
            option =>
                option.Type == ImportResolutionAction.CreateNew &&
                option.SupportsBatch);
    }

    [Fact]
    public void MissingDataGroupsSameFieldButSeparatesDifferentFields()
    {
        var firstAnnual = Issue(
            ImportIssueType.MissingData,
            "AnnualTotal",
            string.Empty);
        var secondAnnual = Issue(
            ImportIssueType.MissingData,
            "AnnualTotal",
            string.Empty);
        var constructionYear = Issue(
            ImportIssueType.MissingData,
            "Baujahr",
            string.Empty);

        var response = Report(
            firstAnnual,
            secondAnnual,
            constructionYear).ToResponse();

        Assert.Equal(2, response.ReturnedIssueCount);
        var annualGroup = Assert.Single(
            response.Issues,
            issue => issue.FieldName == "AnnualTotal");
        var yearGroup = Assert.Single(
            response.Issues,
            issue => issue.FieldName == "Baujahr");
        Assert.Equal(2, annualGroup.MatchingIssueCount);
        Assert.True(annualGroup.SupportsGroupResolution);
        Assert.Equal(1, yearGroup.MatchingIssueCount);
    }

    [Fact]
    public void OpenRepresentativeIsVisibleBeforeMoreThanFiveHundredResolvedGroups()
    {
        var resolved = Enumerable.Range(0, 501)
            .Select(index =>
            {
                var issue = Issue(
                    ImportIssueType.InvalidValue,
                    $"Field-{index}",
                    index.ToString());
                issue.ResolveAutomatically(
                    ImportResolutionAction.KeepFirst,
                    DateTime.UtcNow);
                return issue;
            });
        var openFirst = Issue(
            ImportIssueType.MissingCustomer,
            "CustomerReference",
            "C-1");
        var openSecond = Issue(
            ImportIssueType.MissingCustomer,
            "CustomerReference",
            "C-2");
        var report = Report(resolved.Append(openFirst).Append(openSecond).ToArray());

        var response = report.ToResponse();

        Assert.Equal(500, response.ReturnedIssueCount);
        Assert.Equal(response.Issues.Count, response.ReturnedIssueCount);
        Assert.True(response.HasMoreIssues);
        Assert.False(response.Issues[0].IsResolved);
        Assert.Equal(2, response.Issues[0].MatchingIssueCount);
    }

    [Fact]
    public async Task IssueTypeRuleSurvivesRepositoryRoundtrip()
    {
        var first = Issue(
            ImportIssueType.MissingCustomer,
            "CustomerReference",
            "C-1");
        var second = Issue(
            ImportIssueType.MissingCustomer,
            "CustomerReference",
            "C-2");
        var report = Report(first, second);
        var result = Apply(
            report,
            first,
            ResolutionScope.MatchingIssueTypeInCurrentImport,
            ImportResolutionAction.SkipRow);
        var root = Path.Combine(
            Path.GetTempPath(),
            $"enset-issue-type-rule-{Guid.NewGuid():N}");

        try
        {
            var repository = new JsonImportReportRepository(root);
            await repository.SaveAsync(report);
            var loaded = await repository.GetAsync(report.ImportId);

            var rule = Assert.Single(loaded!.ResolutionRules);
            Assert.Equal(result.RuleId, rule.Id);
            Assert.Equal(
                ResolutionScope.MatchingIssueTypeInCurrentImport,
                rule.Scope);
            Assert.Equal(2, rule.MatchedIssueCount);
            Assert.All(loaded.Issues, issue => Assert.True(issue.IsResolved));
            Assert.Equal(ImportStatus.ReadyToCommit, loaded.Status);
        }
        finally
        {
            if (Directory.Exists(root))
                Directory.Delete(root, recursive: true);
        }
    }

    private static ApplyResolutionRuleResult Apply(
        ImportReport report,
        ImportIssue seed,
        ResolutionScope scope,
        ImportResolutionAction action) =>
        new ApplyResolutionService().ApplyRule(
            report,
            new ApplyResolutionRuleCommand
            {
                SeedIssueId = seed.IssueId,
                Scope = scope,
                ResolutionType = ImportResolutionType.ExistingAction,
                ResolutionAction = action
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

    private static ImportIssue Issue(
        ImportIssueType type,
        string field,
        string value) => new()
    {
        Type = type,
        Severity = ImportIssueSeverity.Error,
        RequiresUserDecision = true,
        FieldName = field,
        FirstValue = value,
        Message = "Structured resolution scope test."
    };
}
