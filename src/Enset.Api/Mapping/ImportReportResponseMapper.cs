using Enset.Api.Contracts.Imports.Responses;
using Enset.Application.Imports.Reports;
using Enset.Application.Imports.Resolution;

namespace Enset.Api.Mapping;

public static class ImportReportResponseMapper
{
    private const int IssueResponseLimit = 500;

    public static ImportReportResponse ToResponse(this ImportReport report)
    {
        var issueGroups = report.Issues
            .GroupBy(issue => IssueGroupKey(issue, report.SourceType))
            .ToList();
        var matchingCounts = issueGroups
            .ToDictionary(group => group.Key, group => group.Count());
        var groupAllowedOptions = issueGroups.ToDictionary(
            group => group.Key,
            group => CommonGroupOptions(group, report.SourceType));
        var groupBatchEligibility = issueGroups.ToDictionary(
            group => group.Key,
            group => groupAllowedOptions[group.Key]
                .Any(option => option.SupportsBatch));
        var groupExamples = issueGroups.ToDictionary(
            group => group.Key,
            group => group
                .Select(issue => issue.FirstValue)
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Distinct(StringComparer.Ordinal)
                .Take(3)
                .Select(value => value!)
                .ToList());
        var compatibleIssueTypeCounts = report.Issues
            .Where(issue => !issue.IsResolved)
            .GroupBy(issue =>
                ImportIssueCompatibility.IssueTypeCompatibilityKey(
                    issue, report.SourceType))
            .ToDictionary(group => group.Key, group => group.Count());
        var groupRepresentatives = issueGroups
            .Select(group => group.First())
            .Where(issue =>
                report.SourceType !=
                    Enset.Application.Imports.Enums.ImportSourceType.CRM_Excel ||
                !issue.IsResolved ||
                !IsReferenceIssue(issue.Type))
            .ToList();

        var visibleIssues = groupRepresentatives
            .OrderBy(issue => issue.IsResolved)
            .ThenByDescending(issue => issue.RequiresUserDecision)
            .Take(IssueResponseLimit)
            .ToList();

        return new ImportReportResponse
        {
            ImportId = report.ImportId,
            Status = report.Status,
            SourceFile = report.SourceFile is null
                ? null
                : new ImportSourceFileResponse
                {
                    FileName = report.SourceFile.FileName,
                    ContentType = report.SourceFile.ContentType,
                    Length = report.SourceFile.Length,
                    Sha256 = report.SourceFile.Sha256,
                    IsRawArchived = !string.IsNullOrWhiteSpace(report.SourceFile.RawStoragePath)
                },
            Customers = report.Customers,
            SourceColumns = report.SourceColumns.Select(column =>
                new ImportSourceColumnResponse
                {
                    Index = column.Index,
                    OriginalHeader = column.OriginalHeader,
                    EffectiveHeader = column.EffectiveHeader,
                    WasHeaderGenerated = column.WasHeaderGenerated,
                    HasData = column.HasData,
                    ValueCount = column.Values.Count
                }).ToList(),
            Issues = visibleIssues.Select(issue => new ImportIssueResponse
            {
                IssueId = issue.IssueId,
                EntityId = issue.EntityId,
                Type = issue.Type,
                Severity = issue.Severity,
                Message = issue.Message,
                SimilarityScore = issue.SimilarityScore,
                RequiresUserDecision = issue.RequiresUserDecision,
                FieldName = issue.FieldName,
                SourceRowNumber = issue.SourceRowNumber,
                FirstValue = issue.FirstValue,
                SecondValue =
                    IsDuplicate(issue.Type) ||
                    IsReferenceIssue(issue.Type) ||
                    IsCsvColumnSelection(issue.Type)
                        ? issue.SecondValue
                        : null,
                ValuePattern = issue.ValuePattern,
                TargetDataType = issue.TargetDataType,
                NumberFormatPattern = issue.NumberFormatPattern,
                ExampleValues = groupExamples.GetValueOrDefault(
                    IssueGroupKey(issue, report.SourceType),
                    []),
                MatchingIssueCount = matchingCounts.GetValueOrDefault(
                    IssueGroupKey(issue, report.SourceType),
                    1),
                CompatibleIssueTypeCount =
                    compatibleIssueTypeCounts.GetValueOrDefault(
                        ImportIssueCompatibility
                            .IssueTypeCompatibilityKey(
                                issue, report.SourceType)),
                SupportsGroupResolution =
                    groupBatchEligibility.GetValueOrDefault(
                        IssueGroupKey(issue, report.SourceType)),
                SupportedScopes = SupportedScopes(
                    issue,
                    matchingCounts.GetValueOrDefault(
                        IssueGroupKey(issue, report.SourceType),
                        1),
                    compatibleIssueTypeCounts.GetValueOrDefault(
                        ImportIssueCompatibility
                            .IssueTypeCompatibilityKey(
                                issue, report.SourceType))),
                AllowedResolutions = groupAllowedOptions[
                        IssueGroupKey(issue, report.SourceType)]
                    .Select(option => new AllowedImportResolutionResponse
                    {
                        Type = option.Action,
                        Label =
                            report.SourceType ==
                                Enset.Application.Imports.Enums.ImportSourceType
                                    .Landesenergiebuchhaltung &&
                            issue.Type ==
                                Enset.Application.Imports.Issues.ImportIssueType
                                    .MissingData &&
                            option.Action ==
                                Enset.Application.Imports.Issues
                                    .ImportResolutionAction.IgnoreMissingValue
                                ? "Fehlende Werte bewusst leer übernehmen"
                                : option.Label,
                        RequiresInput = option.RequiresInput,
                        InputType = option.InputType,
                        SupportsBatch = option.SupportsBatch,
                        Culture = option.Culture
                    }).ToList(),
                ResolutionAction = issue.ResolutionAction,
                CustomResolvedValue = issue.CustomResolvedValue,
                IsResolved = issue.IsResolved,
                ResolutionSource = issue.ResolutionSource,
                ResolvedAt = issue.ResolvedAt,
                ResolvedBy = issue.ResolvedBy,
                ResolutionScope = issue.ResolutionScope,
                ResolutionRuleId = issue.ResolutionRuleId
            }).ToList(),
            AuditTrail = report.AuditTrail.Select(entry => new ImportAuditEntryResponse
            {
                AuditId = entry.AuditId,
                Timestamp = entry.Timestamp,
                UserId = entry.UserId,
                Action = entry.Action,
                IssueId = entry.IssueId,
                PreviousResolutionAction = entry.PreviousResolutionAction,
                ResolutionAction = entry.ResolutionAction,
                PreviousCustomResolvedValue = entry.PreviousCustomResolvedValue,
                CustomResolvedValue = entry.CustomResolvedValue,
                Details = entry.Details
            }).ToList(),
            Decision = report.Decision,
            CreatedAt = report.CreatedAt,
            UpdatedAt = report.UpdatedAt,
            CustomerCount = report.CustomerCount,
            BuildingCount = report.BuildingCount,
            MeterCount = report.MeterCount,
            MeterReadingCount = report.MeterReadingCount,
            IssueCount = report.IssueCount,
            ReturnedIssueCount = visibleIssues.Count,
            HasMoreIssues = groupRepresentatives.Count > visibleIssues.Count,
            UnresolvedIssueCount = report.UnresolvedIssueCount,
            OpenIssueCount = report.OpenIssueCount,
            BlockingOpenIssueCount = report.BlockingOpenIssueCount,
            AutomaticallyResolvedIssueCount =
                report.AutomaticallyResolvedIssueCount,
            ManuallyResolvedIssueCount = report.ManuallyResolvedIssueCount,
            ReadinessMessage =
                CreateReadinessMessage(report.BlockingOpenIssueCount),
            ErrorCount = report.ErrorCount,
            WarningCount = report.WarningCount
        };
    }

    private static string? CreateReadinessMessage(int unresolvedIssueCount) =>
        unresolvedIssueCount switch
        {
            0 => null,
            1 => "1 Issue noch ungelöst.",
            _ => $"{unresolvedIssueCount} Issues noch ungelöst."
        };

    private static (
        string CompatibilityKey,
        bool IsResolved)
        IssueGroupKey(
            Enset.Application.Imports.Issues.ImportIssue issue,
            Enset.Application.Imports.Enums.ImportSourceType sourceType)
        => (
            ImportIssueCompatibility.CurrentGroupKey(issue, sourceType),
            issue.IsResolved);

    private static IReadOnlyList<ResolutionScope> SupportedScopes(
        Enset.Application.Imports.Issues.ImportIssue issue,
        int matchingIssueCount,
        int compatibleIssueTypeCount)
    {
        var scopes = new List<ResolutionScope>
        {
            ResolutionScope.SingleIssue
        };
        var supportsBatch = ImportResolutionOptionsProvider
            .GetOptions(issue)
            .Any(option => option.SupportsBatch);
        if (!supportsBatch)
            return scopes;
        if (matchingIssueCount > 1)
            scopes.Add(ResolutionScope.MatchingIssuesInCurrentImport);
        if (compatibleIssueTypeCount > 1)
            scopes.Add(ResolutionScope.MatchingIssueTypeInCurrentImport);
        return scopes;
    }

    private static IReadOnlyList<AllowedImportResolution> CommonGroupOptions(
        IEnumerable<Enset.Application.Imports.Issues.ImportIssue> group,
        Enset.Application.Imports.Enums.ImportSourceType sourceType)
    {
        var issues = group.ToList();
        var firstOptions = ImportResolutionOptionsProvider
            .GetOptions(issues[0]);
        var isLebAggregate =
            sourceType ==
                Enset.Application.Imports.Enums.ImportSourceType
                    .Landesenergiebuchhaltung &&
            issues[0].Type is
                Enset.Application.Imports.Issues.ImportIssueType
                    .InvalidNumberFormat or
                Enset.Application.Imports.Issues.ImportIssueType.MissingData;
        if (!isLebAggregate)
            return firstOptions;

        return firstOptions.Where(option =>
            issues.All(issue =>
                ImportResolutionOptionsProvider.GetOptions(issue).Any(candidate =>
                    candidate.Action == option.Action &&
                    candidate.InputType == option.InputType &&
                    candidate.RequiresInput == option.RequiresInput &&
                    candidate.SupportsBatch == option.SupportsBatch)))
            .ToList();
    }

    private static bool IsDuplicate(
        Enset.Application.Imports.Issues.ImportIssueType type) =>
        type is Enset.Application.Imports.Issues.ImportIssueType.DuplicateCustomer or
            Enset.Application.Imports.Issues.ImportIssueType.DuplicateBuilding or
            Enset.Application.Imports.Issues.ImportIssueType.DuplicateMeter;

    private static bool IsReferenceIssue(
        Enset.Application.Imports.Issues.ImportIssueType type) =>
        type is Enset.Application.Imports.Issues.ImportIssueType.MissingCustomer or
            Enset.Application.Imports.Issues.ImportIssueType.MissingBuilding or
            Enset.Application.Imports.Issues.ImportIssueType.MissingMeter;

    private static bool IsCsvColumnSelection(
        Enset.Application.Imports.Issues.ImportIssueType type) =>
        type is
            Enset.Application.Imports.Issues.ImportIssueType
                .TimestampColumnSelectionRequired or
            Enset.Application.Imports.Issues.ImportIssueType
                .ValueColumnSelectionRequired;
}
