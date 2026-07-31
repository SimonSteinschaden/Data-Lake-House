using System.Globalization;
using Enset.Domain.Curation;
using Enset.Domain.Energy;

namespace Enset.Application.CanonicalSnapshots;

public enum EnergySystemGoldFieldState
{
    Missing,
    PresentUnconfirmed,
    Confirmed
}

public sealed record EnergySystemGoldFieldAssessment(
    string FieldName,
    string Label,
    string? Value,
    bool HasValue,
    bool IsConfirmed,
    bool IsConfirmable,
    EnergySystemGoldFieldState State,
    string? UnfulfilledReason);

public sealed record EnergySystemGoldAssessment(
    bool TechnicallyComplete,
    int GoldRequiredFieldCount,
    int GoldConfirmedFieldCount,
    DataMaturityLevel MaturityLevel,
    bool IsGoldReady,
    IReadOnlyList<EnergySystemGoldFieldAssessment> GoldFieldStates)
{
    public IReadOnlyList<string> MissingReasons =>
        GoldFieldStates
            .Where(item => item.State == EnergySystemGoldFieldState.Missing &&
                           item.UnfulfilledReason is not null)
            .Select(item => item.UnfulfilledReason!)
            .ToArray();

    public IReadOnlyList<string> ConfirmationReasons =>
        GoldFieldStates
            .Where(item => item.IsConfirmable &&
                           item.State == EnergySystemGoldFieldState.PresentUnconfirmed &&
                           item.UnfulfilledReason is not null)
            .Select(item => item.UnfulfilledReason!)
            .ToArray();
}

/// <summary>
/// Zentrale, einzige Definition der Gold-relevanten Anlagenfelder. Die
/// Anlagennummer und die Gebäudezuordnung sind technische
/// Vollständigkeitsvoraussetzungen (systemseitig erzeugt beziehungsweise eine
/// Beziehung über <see cref="Enset.Domain.Energy.EnergySystemBuildingAssignment"/>)
/// und daher nicht fachlich bestätigbar. Nur Anlagentyp und, je nach Typ,
/// Leistung sind kuratierbare Gold-Werte.
/// </summary>
public static class EnergySystemGoldDefinition
{
    public static EnergySystemGoldAssessment Evaluate(
        string? energySystemNumber,
        EnergySystemType type,
        decimal? ratedPowerKw,
        bool buildingAssigned,
        IReadOnlyDictionary<string, bool>? confirmations = null)
    {
        var numberField = TechnicalField(
            "EnergySystemNumber", "Anlagennummer",
            !string.IsNullOrWhiteSpace(energySystemNumber));
        var assignmentField = TechnicalField(
            "BuildingAssignment", "Gebäudezuordnung", buildingAssigned);

        var confirmableFields = new List<EnergySystemGoldFieldAssessment>
        {
            ConfirmableField("Type", "Anlagentyp",
                type == EnergySystemType.Unknown ? null : type.ToString(),
                type != EnergySystemType.Unknown, confirmations)
        };
        if (RequiresRatedPower(type))
            confirmableFields.Add(ConfirmableField("RatedPowerKw", "Leistung",
                Invariant(ratedPowerKw), ratedPowerKw.HasValue, confirmations));

        var allFields = new[] { numberField, assignmentField }
            .Concat(confirmableFields).ToArray();

        var technicallyComplete = numberField.HasValue && assignmentField.HasValue;
        var allPresent = technicallyComplete && confirmableFields.All(f => f.HasValue);
        var confirmedCount = confirmableFields.Count(f => f.IsConfirmed);
        var allConfirmed = allPresent && confirmedCount == confirmableFields.Count;

        var maturity = !allPresent
            ? DataMaturityLevel.Bronze
            : allConfirmed
                ? DataMaturityLevel.Gold
                : DataMaturityLevel.Silver;

        return new EnergySystemGoldAssessment(
            technicallyComplete,
            confirmableFields.Count,
            confirmedCount,
            maturity,
            allConfirmed,
            allFields);
    }

    /// <summary>
    /// Bestimmt, ob Leistung für den Anlagentyp ein zwingendes Gold-Kriterium
    /// ist. Fernwärmeanschlüsse (vertragliche Anschlussleistung statt
    /// installierter Leistung), Lüftungsanlagen (primär über Luftmenge statt
    /// kW charakterisiert) sowie unbekannte/sonstige Typen verlangen keine
    /// verpflichtende Leistungsangabe für Gold.
    /// </summary>
    public static bool RequiresRatedPower(EnergySystemType type) => type switch
    {
        EnergySystemType.Photovoltaic => true,
        EnergySystemType.HeatPump => true,
        EnergySystemType.Boiler => true,
        EnergySystemType.BatteryStorage => true,
        EnergySystemType.ChargingInfrastructure => true,
        EnergySystemType.Cooling => true,
        EnergySystemType.DistrictHeating => false,
        EnergySystemType.Ventilation => false,
        EnergySystemType.Other => false,
        _ => false
    };

    private static EnergySystemGoldFieldAssessment TechnicalField(
        string fieldName, string label, bool present) =>
        new(fieldName, label, present ? "Vorhanden" : null, present,
            present, false,
            present ? EnergySystemGoldFieldState.Confirmed : EnergySystemGoldFieldState.Missing,
            present ? null : $"{label} fehlt.");

    private static EnergySystemGoldFieldAssessment ConfirmableField(
        string fieldName, string label, string? value, bool hasValue,
        IReadOnlyDictionary<string, bool>? confirmations)
    {
        var isConfirmed = hasValue && confirmations?.GetValueOrDefault(fieldName) == true;
        var state = !hasValue
            ? EnergySystemGoldFieldState.Missing
            : isConfirmed
                ? EnergySystemGoldFieldState.Confirmed
                : EnergySystemGoldFieldState.PresentUnconfirmed;
        var reason = !hasValue
            ? $"{label} fehlt."
            : !isConfirmed
                ? $"{label} ist vorhanden, aber noch nicht fachlich bestätigt. Aktueller Wert: {value}."
                : null;
        return new(fieldName, label, value, hasValue, isConfirmed, true, state, reason);
    }

    private static string? Invariant(decimal? value) =>
        value?.ToString(CultureInfo.InvariantCulture);
}
