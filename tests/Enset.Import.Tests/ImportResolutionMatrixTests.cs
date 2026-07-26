using Enset.Api.Mapping;
using Enset.Application.Imports.Enums;
using Enset.Application.Imports.Issues;
using Enset.Application.Imports.Leb;
using Enset.Application.Imports.Leb.DTOs;
using Enset.Application.Imports.Reports;
using Enset.Application.Imports.Resolution;
using Enset.Infrastructure.Imports.Persistence;
using Xunit;

namespace Enset.Import.Tests;

public sealed class ImportResolutionMatrixTests
{
    [Fact]
    public void Duplicate_OffersOnlyDuplicateSpecificOptions()
    {
        var options = Options(ImportIssueType.DuplicateBuilding);

        Assert.Contains(ImportResolutionAction.KeepFirst, options);
        Assert.Contains(ImportResolutionAction.KeepSecond, options);
        Assert.Contains(ImportResolutionAction.Merge, options);
        Assert.DoesNotContain(ImportResolutionAction.ParseDeAt, options);
        Assert.DoesNotContain(ImportResolutionAction.EnterValue, options);
    }

    [Fact]
    public void MissingData_OffersEnterIgnoreAndZeroOnlyForNumericFields()
    {
        var numeric = Options(ImportIssueType.MissingData, "AnnualTotal");
        var text = Options(ImportIssueType.MissingData, "Comment");

        Assert.Contains(ImportResolutionAction.EnterValue, numeric);
        Assert.Contains(ImportResolutionAction.IgnoreMissingValue, numeric);
        Assert.Contains(ImportResolutionAction.SetZero, numeric);
        Assert.DoesNotContain(ImportResolutionAction.SetZero, text);
        Assert.DoesNotContain(ImportResolutionAction.KeepFirst, numeric);
    }

    [Fact]
    public void InvalidNumberFormat_OffersOnlyNumericResolutions()
    {
        var options = ImportResolutionOptionsProvider.GetOptions(new ImportIssue
        {
            Type = ImportIssueType.InvalidNumberFormat,
            FieldName = "AnnualTotal",
            TargetDataType = ResolutionTargetDataType.Decimal,
            NumberFormatPattern = NumberFormatPattern.AustrianDecimal
        }).Select(x => x.Action).ToList();

        Assert.Contains(ImportResolutionAction.ParseDeAt, options);
        Assert.DoesNotContain(ImportResolutionAction.ParseInvariant, options);
        Assert.DoesNotContain(ImportResolutionAction.EnterValue, options);
        Assert.DoesNotContain(ImportResolutionAction.KeepFirst, options);
        Assert.DoesNotContain(ImportResolutionAction.KeepSecond, options);
    }

    [Fact]
    public void EnterValueValidatesInputAndSetZeroPersistsExplicitZero()
    {
        var issue = MissingDataIssue("AnnualTotal");
        var report = Report(issue);
        issue.SourceRowNumber = 2;
        report.SourceColumns =
        [
            new LebSourceColumn
            {
                Index = 32,
                EffectiveHeader = "AnnualTotal"
            }
        ];
        var service = new ApplyResolutionService();

        Assert.Throws<ArgumentException>(() => service.Apply(
            report,
            [Resolution(issue, ImportResolutionAction.EnterValue, "not-a-number")],
            "user",
            DateTime.UtcNow));

        service.Apply(
            report,
            [Resolution(issue, ImportResolutionAction.SetZero)],
            "user",
            DateTime.UtcNow);

        Assert.True(issue.IsResolved);
        Assert.Equal("0", issue.CustomResolvedValue);
        Assert.Empty(Assert.Single(report.SourceColumns).Values);
    }

    [Fact]
    public void IgnoreKeepsMissingValueNullAndRecordsAcceptance()
    {
        var issue = MissingDataIssue("AnnualTotal");
        var report = Report(issue);

        new ApplyResolutionService().Apply(
            report,
            [Resolution(issue, ImportResolutionAction.IgnoreMissingValue)],
            "user",
            DateTime.UtcNow);

        Assert.True(issue.IsResolved);
        Assert.Null(issue.CustomResolvedValue);
        Assert.Equal(ImportResolutionAction.IgnoreMissingValue, issue.ResolutionAction);
        Assert.Equal(ImportResolutionSource.Manual, issue.ResolutionSource);
    }

    [Fact]
    public async Task MissingLebValuesAreNonBlockingAndSurviveRoundtrip()
    {
        var source = new LebWorkbookDto
        {
            Rows =
            [
                new LebRowDto
                {
                    RowNumber = 2,
                    MunicipalityId = "1",
                    MunicipalityName = "Gemeinde",
                    BuildingId = "2",
                    MeterId = "3",
                    MeterName = "Zähler",
                    Year = "2025",
                    MonthlyValues = ["10"]
                }
            ]
        };
        var report = new LebImportValidator(source).Validate([], [], [], []);
        report.RecalculateCommitReadiness();

        Assert.Equal(ImportStatus.ReadyToCommit, report.Status);
        Assert.Equal(3, report.Issues.Count(x => x.Type == ImportIssueType.MissingData));
        Assert.All(
            report.Issues.Where(x => x.Type == ImportIssueType.MissingData),
            issue =>
            {
                Assert.Equal(ImportIssueSeverity.Warning, issue.Severity);
                Assert.False(issue.RequiresUserDecision);
                Assert.False(issue.IsCommitBlocking);
            });
        Assert.Null(source.Rows[0].AnnualValue);
        Assert.Null(source.Rows[0].FloorArea);
        Assert.Null(source.Rows[0].ConstructionYear);

        var root = Path.Combine(Path.GetTempPath(), $"enset-quality-{Guid.NewGuid():N}");
        try
        {
            var repository = new JsonImportReportRepository(root);
            await repository.SaveAsync(report);
            var loaded = await repository.GetAsync(report.ImportId);
            Assert.Equal(
                3,
                loaded!.Issues.Count(x => x.Type == ImportIssueType.MissingData));
            Assert.Equal(ImportStatus.ReadyToCommit, loaded.Status);
        }
        finally
        {
            if (Directory.Exists(root))
                Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void StructuralErrorRemainsBlockingAndHasNoInteractiveResolution()
    {
        var source = new LebWorkbookDto
        {
            Rows =
            [
                new LebRowDto
                {
                    RowNumber = 2,
                    BuildingId = "2",
                    MeterId = "3",
                    MeterName = "Zähler",
                    Year = "2025",
                    AnnualValue = "10",
                    ConstructionYear = "2000",
                    FloorArea = "100"
                }
            ]
        };
        var report = new LebImportValidator(source).Validate([], [], [], []);
        report.RecalculateCommitReadiness();
        var issue = Assert.Single(
            report.Issues,
            x => x.Type == ImportIssueType.StructuralError);

        Assert.True(issue.IsCommitBlocking);
        Assert.Empty(ImportResolutionOptionsProvider.GetOptions(issue));
        Assert.Equal(ImportStatus.AwaitingResolution, report.Status);
    }

    [Fact]
    public void ApiReturnsServerOptionsAndHidesSecondValueForNonDuplicate()
    {
        var issue = new ImportIssue
        {
            Type = ImportIssueType.InvalidNumberFormat,
            Severity = ImportIssueSeverity.Error,
            FieldName = "AnnualTotal",
            FirstValue = "1.202,48",
            TargetDataType = ResolutionTargetDataType.Decimal,
            NumberFormatPattern = NumberFormatPattern.AustrianDecimal,
            SecondValue = "must-not-leak"
        };
        var response = Report(issue).ToResponse();
        var dto = Assert.Single(response.Issues);

        Assert.Null(dto.SecondValue);
        Assert.Contains(dto.AllowedResolutions,
            option => option.Type == ImportResolutionAction.ParseDeAt);
        Assert.DoesNotContain(dto.AllowedResolutions,
            option => option.Type == ImportResolutionAction.KeepFirst);
    }

    [Fact]
    public void GroupEligibilityRequiresIdenticalResolutionSets()
    {
        var numeric = new ImportIssue
        {
            Type = ImportIssueType.MissingData,
            FieldName = "AnnualTotal"
        };
        var text = new ImportIssue
        {
            Type = ImportIssueType.MissingData,
            FieldName = "Comment"
        };

        Assert.False(
            ImportResolutionOptionsProvider.HaveIdenticalOptions([numeric, text]));
    }

    private static IReadOnlyCollection<ImportResolutionAction> Options(
        ImportIssueType type,
        string? field = null) =>
        ImportResolutionOptionsProvider.GetOptions(new ImportIssue
        {
            Type = type,
            FieldName = field
        }).Select(x => x.Action).ToList();

    private static ImportIssue MissingDataIssue(string field) => new()
    {
        Type = ImportIssueType.MissingData,
        Severity = ImportIssueSeverity.Warning,
        FieldName = field,
        Message = "Missing data."
    };

    private static ImportReport Report(params ImportIssue[] issues)
    {
        var report = new ImportReport();
        report.Issues.AddRange(issues);
        report.RecalculateCommitReadiness();
        return report;
    }

    private static ImportIssueResolution Resolution(
        ImportIssue issue,
        ImportResolutionAction action,
        string? value = null) => new()
    {
        IssueId = issue.IssueId,
        ResolutionAction = action,
        CustomResolvedValue = value
    };
}
