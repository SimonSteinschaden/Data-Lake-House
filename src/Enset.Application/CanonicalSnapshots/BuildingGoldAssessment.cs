using Enset.Domain.Curation;

namespace Enset.Application.CanonicalSnapshots;

public enum BuildingGoldFieldState
{
    Missing,
    PresentUnconfirmed,
    Confirmed
}

public sealed record BuildingGoldFieldAssessment(
    string FieldName,
    string Label,
    string? Value,
    bool HasValue,
    bool IsConfirmed,
    bool IsGoldRelevant,
    BuildingGoldFieldState State,
    string? UnfulfilledReason);

public sealed record BuildingGoldAssessment(
    int GoldCompletenessPercentage,
    int GoldConfirmationPercentage,
    int GoldRequiredFieldCount,
    int GoldPresentFieldCount,
    int GoldConfirmedFieldCount,
    DataMaturityLevel MaturityLevel,
    bool IsGoldReady,
    IReadOnlyList<BuildingGoldFieldAssessment> GoldFieldStates)
{
    public IReadOnlyList<string> MissingReasons =>
        GoldFieldStates
            .Where(item => item.State == BuildingGoldFieldState.Missing &&
                           item.UnfulfilledReason is not null)
            .Select(item => item.UnfulfilledReason!)
            .ToArray();

    public IReadOnlyList<string> ConfirmationReasons =>
        GoldFieldStates
            .Where(item => item.State == BuildingGoldFieldState.PresentUnconfirmed &&
                           item.UnfulfilledReason is not null)
            .Select(item => item.UnfulfilledReason!)
            .ToArray();

    public IReadOnlyList<string> UnfulfilledReasons =>
        GoldFieldStates
            .Where(item => !item.IsConfirmed &&
                           item.UnfulfilledReason is not null)
            .Select(item => item.UnfulfilledReason!)
            .ToArray();
}

public static class BuildingGoldDefinition
{
    private sealed record Definition(string FieldName, string Label);

    private static readonly IReadOnlyList<Definition> Fields =
    [
        new("BuildingCategory", "Gebäudetyp"),
        new("PrimaryUseType", "Nutzungstyp"),
        new("BuildingState", "Gebäudezustand"),
        new("PostalCode", "PLZ")
    ];

    public static BuildingGoldAssessment Evaluate(
        string? buildingCategory,
        string? primaryUseType,
        string? buildingState,
        string? postalCode,
        IReadOnlyDictionary<string, bool>? confirmations = null)
    {
        var values = new Dictionary<string, string?>
        {
            ["BuildingCategory"] = buildingCategory,
            ["PrimaryUseType"] = primaryUseType,
            ["BuildingState"] = buildingState,
            ["PostalCode"] = postalCode
        };
        var assessments = Fields.Select(definition =>
        {
            var value = Normalize(values[definition.FieldName]);
            var hasValue = value is not null;
            var isConfirmed = hasValue &&
                confirmations?.GetValueOrDefault(definition.FieldName) == true;
            var state = !hasValue
                ? BuildingGoldFieldState.Missing
                : isConfirmed
                    ? BuildingGoldFieldState.Confirmed
                    : BuildingGoldFieldState.PresentUnconfirmed;
            var reason = !hasValue
                ? $"{definition.Label} fehlt."
                : !isConfirmed
                    ? $"{definition.Label} ist vorhanden, aber noch nicht fachlich bestätigt. Aktueller Wert: {value}."
                    : null;
            return new BuildingGoldFieldAssessment(
                definition.FieldName,
                definition.Label,
                value,
                hasValue,
                isConfirmed,
                true,
                state,
                reason);
        }).ToArray();
        var readiness = assessments.Count(field => field.HasValue) * 100 /
            assessments.Length;
        var confirmation = assessments.Count(field => field.IsConfirmed) * 100 /
            assessments.Length;
        var maturity = readiness < 100
            ? DataMaturityLevel.Bronze
            : confirmation < 100
                ? DataMaturityLevel.Silver
                : DataMaturityLevel.Gold;
        return new BuildingGoldAssessment(
            readiness,
            confirmation,
            assessments.Length,
            assessments.Count(field => field.HasValue),
            assessments.Count(field => field.IsConfirmed),
            maturity,
            confirmation == 100,
            assessments);
    }

    private static string? Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;
        var normalized = value.Trim();
        return normalized.Equals("Unknown", StringComparison.OrdinalIgnoreCase) ||
               normalized.Equals("NotSpecified", StringComparison.OrdinalIgnoreCase)
            ? null
            : normalized;
    }
}
