using Enset.Application.Imports.Issues;
using Enset.Application.Imports.Enums;

namespace Enset.Application.Imports.Resolution;

public static class ImportIssueCompatibility
{
    public static bool MatchesCurrentGroup(
        ImportIssue candidate,
        ImportIssue representative,
        ImportSourceType sourceType = ImportSourceType.Excel) =>
        string.Equals(
            CurrentGroupKey(candidate, sourceType),
            CurrentGroupKey(representative, sourceType),
            StringComparison.Ordinal);

    public static bool MatchesIssueType(
        ImportIssue candidate,
        ImportIssue representative,
        ImportSourceType sourceType = ImportSourceType.Excel) =>
        string.Equals(
            IssueTypeCompatibilityKey(candidate, sourceType),
            IssueTypeCompatibilityKey(representative, sourceType),
            StringComparison.Ordinal);

    public static string CurrentGroupKey(
        ImportIssue issue,
        ImportSourceType sourceType = ImportSourceType.Excel)
    {
        ArgumentNullException.ThrowIfNull(issue);

        var field = Normalize(issue.FieldName);
        var options = ResolutionOptionsSignature(issue);
        if (sourceType == ImportSourceType.CRM_Excel &&
            IsMissingWorkbookId(issue.Type))
            return Join(issue.Type);
        if (sourceType == ImportSourceType.Landesenergiebuchhaltung &&
            IsLebAggregateIssue(issue.Type))
            return Join(issue.Type);

        return issue.Type switch
        {
            ImportIssueType.InvalidNumberFormat => Join(
                issue.Type,
                field,
                issue.TargetDataType,
                issue.NumberFormatPattern,
                options),
            ImportIssueType.MissingData => Join(
                issue.Type,
                field,
                issue.TargetDataType,
                options),
            ImportIssueType.SourceColumnMappingRequired => Join(
                issue.Type,
                field,
                ReferenceOrValueSignature(issue),
                options),
            ImportIssueType.DuplicateCustomer or
                ImportIssueType.DuplicateBuilding or
                ImportIssueType.DuplicateMeter => Join(
                    issue.Type,
                    field,
                    DuplicateSignature(issue),
                    options),
            ImportIssueType.MissingCustomer or
                ImportIssueType.MissingBuilding or
                ImportIssueType.MissingMeter when IsRdwReferenceIssue(issue) =>
                    Join(
                        issue.Type,
                        field,
                        ReferenceOrValueSignature(issue),
                        options),
            ImportIssueType.MissingCustomer or
                ImportIssueType.MissingBuilding or
                ImportIssueType.MissingMeter => Join(
                    issue.Type,
                    field,
                    options),
            _ => Join(
                issue.Type,
                field,
                EffectivePattern(issue),
                ExactValueWhenRequired(issue),
                issue.TargetDataType,
                issue.NumberFormatPattern,
                options)
        };
    }

    public static string IssueTypeCompatibilityKey(
        ImportIssue issue,
        ImportSourceType sourceType = ImportSourceType.Excel)
    {
        ArgumentNullException.ThrowIfNull(issue);

        var field = Normalize(issue.FieldName);
        var options = ResolutionOptionsSignature(issue);
        if (sourceType == ImportSourceType.CRM_Excel &&
            IsMissingWorkbookId(issue.Type))
            return Join(issue.Type);
        if (sourceType == ImportSourceType.Landesenergiebuchhaltung &&
            IsLebAggregateIssue(issue.Type))
            return Join(issue.Type);

        return issue.Type switch
        {
            // A type-wide number action intentionally spans decimal fields when
            // they share the same parsing rule. The narrower current-group key
            // still keeps FieldName-specific display groups.
            ImportIssueType.InvalidNumberFormat => Join(
                issue.Type,
                issue.TargetDataType,
                issue.NumberFormatPattern,
                options),
            ImportIssueType.MissingData => Join(
                issue.Type,
                field,
                issue.TargetDataType,
                options),
            ImportIssueType.SourceColumnMappingRequired => Join(
                issue.Type,
                field,
                ReferenceOrValueSignature(issue),
                options),
            ImportIssueType.DuplicateCustomer or
                ImportIssueType.DuplicateBuilding or
                ImportIssueType.DuplicateMeter => Join(
                    issue.Type,
                    field,
                    options),
            ImportIssueType.MissingCustomer or
                ImportIssueType.MissingBuilding or
                ImportIssueType.MissingMeter when IsRdwReferenceIssue(issue) =>
                    Join(
                        issue.Type,
                        field,
                        ReferenceOrValueSignature(issue),
                        options),
            _ => Join(
                issue.Type,
                field,
                issue.TargetDataType,
                EffectivePattern(issue),
                issue.NumberFormatPattern,
                options)
        };
    }

    public static bool HasIdenticalResolutionOptions(
        ImportIssue first,
        ImportIssue second) =>
        string.Equals(
            ResolutionOptionsSignature(first),
            ResolutionOptionsSignature(second),
            StringComparison.Ordinal);

    public static string ResolutionOptionsSignature(ImportIssue issue) =>
        string.Join(
            "|",
            ImportResolutionOptionsProvider.GetOptions(issue).Select(option =>
                $"{option.Action}:{option.InputType}:{option.SupportsBatch}"));

    public static string? GroupValue(ImportIssue issue) =>
        ReferenceOrValueSignature(issue);

    private static string? ReferenceOrValueSignature(ImportIssue issue) =>
        Normalize(issue.SecondValue) ?? Normalize(issue.FirstValue);

    private static string DuplicateSignature(ImportIssue issue)
    {
        var values = new[]
            {
                Normalize(issue.FirstValue),
                Normalize(issue.SecondValue)
            }
            .Where(value => value is not null)
            .OrderBy(value => value, StringComparer.Ordinal);
        return string.Join("|", values);
    }

    private static string? ExactValueWhenRequired(ImportIssue issue) =>
        EffectivePattern(issue) == ImportIssueValuePattern.ExactValue
            ? Normalize(issue.FirstValue)
            : null;

    private static ImportIssueValuePattern EffectivePattern(ImportIssue issue) =>
        issue.ValuePattern == ImportIssueValuePattern.None
            ? ImportIssueValuePattern.ExactValue
            : issue.ValuePattern;

    private static bool IsRdwReferenceIssue(ImportIssue issue) =>
        issue.FieldName is
            "Customer.InternalCustomerId" or
            "Building.InternalCustomerId" or
            "Building.InternalBuildingId";

    private static bool IsMissingWorkbookId(ImportIssueType type) =>
        type is ImportIssueType.MissingCustomer or
            ImportIssueType.MissingBuilding or
            ImportIssueType.MissingMeter;

    private static bool IsLebAggregateIssue(ImportIssueType type) =>
        type is ImportIssueType.InvalidNumberFormat or
            ImportIssueType.MissingData;

    private static string Join(params object?[] values) =>
        string.Join("::", values.Select(value =>
            value?.ToString()?.Trim().ToUpperInvariant() ?? string.Empty));

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim().ToUpperInvariant();
}
