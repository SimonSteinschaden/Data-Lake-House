using Enset.Domain.Curation;

namespace Enset.Application.Quality;

public enum DataProductReleaseStatus { Draft, Released, Archived }
public enum QualityRequirementState { Missing, Complete, Confirmed }
public enum AnnualEnergyStatus
{
    NotAvailable, IncompleteYear, CompleteYear, Estimated, Confirmed
}

public sealed record QualityRequirement(
    string Code, string Label, QualityRequirementState State,
    string? Reason = null);

public sealed record ScopeQuality(
    Guid ScopeId, string ScopeType, DataMaturityLevel Level,
    IReadOnlyList<string> BlockingReasons);

public sealed record QualityProgress(
    int Percentage, int GoldCount, int SilverCount, int BronzeCount);

public sealed record BuildingQualityAssessment(
    DataMaturityLevel BuildingCoreQuality,
    IReadOnlyList<ScopeQuality> MeterQualities,
    IReadOnlyList<ScopeQuality> EnergySystemQualities,
    bool MeterInventoryComplete,
    bool EnergySystemInventoryComplete,
    AnnualEnergyStatus AnnualConsumptionStatus,
    AnnualEnergyStatus AnnualProductionStatus,
    IReadOnlyList<string> BlockingReasons,
    IReadOnlyList<string> Warnings,
    DataMaturityLevel OverallQualityLevel,
    QualityProgress GoldProgress);

public sealed record BuildingQualityInput(
    IReadOnlyList<QualityRequirement> CoreRequirements,
    IReadOnlyList<ScopeQuality> Meters,
    IReadOnlyList<ScopeQuality> EnergySystems,
    bool MeterInventoryComplete,
    bool EnergySystemInventoryComplete,
    bool NoRelevantEnergySystemsConfirmed,
    AnnualEnergyStatus AnnualConsumptionStatus,
    AnnualEnergyStatus AnnualProductionStatus,
    bool HasOpenBlockingIssues);

public static class HierarchicalQualityAssessment
{
    public static BuildingQualityAssessment Evaluate(BuildingQualityInput input)
    {
        var requirements = input.CoreRequirements.Concat(
        [
            Requirement("METER_INVENTORY", "Zählpunktinventar",
                input.MeterInventoryComplete),
            Requirement("ENERGY_SYSTEM_INVENTORY", "Anlageninventar",
                input.EnergySystemInventoryComplete),
            Energy("ANNUAL_CONSUMPTION", "Jahresenergieverbrauch",
                input.AnnualConsumptionStatus),
            input.NoRelevantEnergySystemsConfirmed
                ? new("ANNUAL_PRODUCTION", "Jahresenergieproduktion",
                    QualityRequirementState.Confirmed)
                : Energy("ANNUAL_PRODUCTION", "Jahresenergieproduktion",
                    input.AnnualProductionStatus)
        ]).ToArray();

        var blockers = requirements
            .Where(x => x.State == QualityRequirementState.Missing)
            .Select(x => x.Reason ?? $"{x.Label} fehlt.")
            .Concat(input.Meters.Where(x => x.Level == DataMaturityLevel.Bronze)
                .Select(x => $"Zählpunkt {x.ScopeId} besitzt Bronze-Status."))
            .Concat(input.EnergySystems.Where(x => x.Level == DataMaturityLevel.Bronze)
                .Select(x => $"Anlage {x.ScopeId} besitzt Bronze-Status."))
            .ToList();
        if (input.Meters.Count == 0)
            blockers.Add("Mindestens ein relevanter Zählpunkt ist erforderlich.");
        if (input.HasOpenBlockingIssues)
            blockers.Add("Es bestehen offene blockierende Qualitätsprobleme.");

        var allRequirementsConfirmed = requirements.All(
            x => x.State == QualityRequirementState.Confirmed);
        var allChildrenGold = input.Meters.Count > 0 &&
            input.Meters.All(x => x.Level == DataMaturityLevel.Gold) &&
            input.EnergySystems.All(x => x.Level == DataMaturityLevel.Gold);
        var anyBronze = blockers.Count > 0;
        var overall = anyBronze
            ? DataMaturityLevel.Bronze
            : allRequirementsConfirmed && allChildrenGold
                ? DataMaturityLevel.Gold
                : DataMaturityLevel.Silver;
        var core = Level(input.CoreRequirements);
        var progressRequirements = requirements
            .Concat(input.Meters.Select(ChildRequirement))
            .Concat(input.EnergySystems.Select(ChildRequirement))
            .ToArray();
        return new(core, input.Meters, input.EnergySystems,
            input.MeterInventoryComplete, input.EnergySystemInventoryComplete,
            input.AnnualConsumptionStatus, input.AnnualProductionStatus,
            blockers, [], overall, Progress(progressRequirements));
    }

    public static QualityProgress Progress(
        IReadOnlyCollection<QualityRequirement> requirements)
    {
        if (requirements.Count == 0) return new(0, 0, 0, 0);
        var points = requirements.Sum(x => (int)x.State);
        return new(points * 100 / (requirements.Count * 2),
            requirements.Count(x => x.State == QualityRequirementState.Confirmed),
            requirements.Count(x => x.State == QualityRequirementState.Complete),
            requirements.Count(x => x.State == QualityRequirementState.Missing));
    }

    private static DataMaturityLevel Level(
        IReadOnlyCollection<QualityRequirement> requirements) =>
        requirements.Any(x => x.State == QualityRequirementState.Missing)
            ? DataMaturityLevel.Bronze
            : requirements.All(x => x.State == QualityRequirementState.Confirmed)
                ? DataMaturityLevel.Gold
                : DataMaturityLevel.Silver;

    private static QualityRequirement Requirement(
        string code, string label, bool confirmed) =>
        new(code, label, confirmed ? QualityRequirementState.Confirmed :
            QualityRequirementState.Missing,
            confirmed ? null : $"{label} wurde noch nicht bestätigt.");

    private static QualityRequirement Energy(
        string code, string label, AnnualEnergyStatus status) =>
        new(code, label, status switch
        {
            AnnualEnergyStatus.Confirmed => QualityRequirementState.Confirmed,
            AnnualEnergyStatus.CompleteYear => QualityRequirementState.Complete,
            _ => QualityRequirementState.Missing
        }, status is AnnualEnergyStatus.Confirmed or AnnualEnergyStatus.CompleteYear
            ? null : $"{label} ist nicht belastbar ableitbar.");

    private static QualityRequirement ChildRequirement(ScopeQuality scope) =>
        new($"{scope.ScopeType}:{scope.ScopeId}", scope.ScopeType,
            scope.Level switch
            {
                DataMaturityLevel.Gold => QualityRequirementState.Confirmed,
                DataMaturityLevel.Silver => QualityRequirementState.Complete,
                _ => QualityRequirementState.Missing
            });
}
