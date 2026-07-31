using Enset.Application.Quality;
using Enset.Application.CanonicalSnapshots;
using Enset.Domain.Curation;
using Enset.Domain.Energy;
using Enset.Domain.Quality;
using Enset.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Enset.Infrastructure.Quality;

public sealed class EfHierarchicalQualityAssessmentService(EnsetDbContext db)
    : IHierarchicalQualityAssessmentService
{
    private readonly record struct MeterAnnualInfo(
        Guid Id, MeterDirection Direction, decimal? AnnualValue, string? AnnualValueOrigin);


    public async Task<IReadOnlyDictionary<Guid, MeterQualityAssessment>> AssessMeters(
        IReadOnlyCollection<Guid> ids, CancellationToken ct)
    {
        if (ids.Count == 0) return new Dictionary<Guid, MeterQualityAssessment>();
        var set = ids.ToHashSet();
        var meters = await db.Meters.AsNoTracking().Where(x => set.Contains(x.Id)).Select(x => new
        { x.Id, x.MeterNumber, x.Medium, x.Direction, x.Quantity, x.Unit, x.BuildingId }).ToListAsync(ct);
        var analyses = await db.MeterProfileAnalyses.AsNoTracking()
            .Where(x => set.Contains(x.MeterId) && x.IsCurrent).ToListAsync(ct);
        var analysisIds = analyses.Select(x => x.Id).ToHashSet();
        var issues = await db.MeterProfileIssues.AsNoTracking()
            .Where(x => analysisIds.Contains(x.MeterProfileAnalysisId))
            .GroupBy(x => x.MeterId)
            .Select(g => new
            {
                MeterId = g.Key,
                Open = g.Count(x => x.ResolutionStatus != ProfileIssueResolutionStatus.Resolved),
                Blocking = g.Count(x => x.IsBlocking && x.ResolutionStatus != ProfileIssueResolutionStatus.Resolved),
                Anomalies = g.Count(x => x.ResolutionStatus != ProfileIssueResolutionStatus.Resolved &&
                    (x.Category == ProfileIssueCategory.Outlier ||
                     x.Category == ProfileIssueCategory.SuddenJump ||
                     x.Category == ProfileIssueCategory.ZeroSequence ||
                     x.Category == ProfileIssueCategory.ImplausibleValue ||
                     x.Category == ProfileIssueCategory.NegativeValue))
            }).ToListAsync(ct);

        return meters.ToDictionary(x => x.Id, x =>
        {
            var analysis = analyses.SingleOrDefault(y => y.MeterId == x.Id);
            var counts = issues.SingleOrDefault(y => y.MeterId == x.Id);

            var missing = new List<string>();
            if (string.IsNullOrWhiteSpace(x.MeterNumber)) missing.Add("Zählpunktnummer fehlt.");
            if (x.BuildingId is null) missing.Add("Gebäudezuordnung fehlt.");
            if (x.Medium.ToString() == "Unknown") missing.Add("Energieträger fehlt.");
            if (x.Direction.ToString() == "Unknown") missing.Add("Richtung fehlt.");
            if (x.Unit.ToString() == "Unknown") missing.Add("Einheit fehlt.");

            var profileLevel = MeterProfileQuality.Evaluate(analysis is null ? null : new MeterProfileAnalysisResult(
                analysis.Id, analysis.MeterId, analysis.PeriodFromUtc, analysis.PeriodToUtc,
                analysis.ExpectedReadingCount, analysis.ActualReadingCount, analysis.CompletenessPercentage,
                analysis.GapCount, analysis.AnomalyCount, analysis.BlockingIssueCount, analysis.WarningCount,
                analysis.AnalysisVersion, analysis.ExecutedAtUtc, analysis.ExecutedByUserId,
                Map(analysis.AnalysisStatus), analysis.Summary ?? "", []));

            var level = missing.Count > 0 ? DataMaturityLevel.Bronze : profileLevel;
            var blocking = missing
                .Concat(counts?.Blocking > 0 ? [$"{counts.Blocking} blockierende Profilprobleme sind offen."] : Array.Empty<string>())
                .ToArray();
            if (blocking.Length > 0 && level == DataMaturityLevel.Gold) level = DataMaturityLevel.Silver;

            var warnings = new List<string>();
            if (analysis?.WarningCount > 0)
                warnings.Add($"{analysis.WarningCount} nicht blockierende Profilhinweise sind offen.");
            if (level == DataMaturityLevel.Silver && analysis?.AnalysisStatus is
                ProfileAnalysisStatus.Completed or ProfileAnalysisStatus.Curated)
                warnings.Add("Profilanalyse liegt vor, ist aber noch nicht fachlich bestätigt.");

            var nextActions = new List<string>();
            if (analysis is null)
                nextActions.Add("Profilanalyse starten.");
            else if (counts?.Blocking > 0)
                nextActions.Add("Blockierende Profilprobleme bearbeiten.");
            else if (analysis.AnalysisStatus != ProfileAnalysisStatus.Confirmed)
                nextActions.Add("Profil fachlich bestätigen.");

            return new MeterQualityAssessment(
                x.Id, level, analysis?.AnalysisStatus ?? ProfileAnalysisStatus.NotAnalyzed,
                analysis?.Id, analysis?.AnalysisVersion, analysis?.CompletenessPercentage,
                counts?.Open ?? 0, counts?.Blocking ?? 0, analysis?.ExecutedAtUtc,
                analysis?.AnalysisStatus == ProfileAnalysisStatus.Confirmed ? "Bestätigt" : "Unbestätigt",
                blocking, warnings, nextActions)
            {
                OpenAnomalyCount = counts?.Anomalies ?? 0
            };
        });
    }

    public async Task<IReadOnlyDictionary<Guid, EnergySystemQualityAssessment>> AssessEnergySystems(
        IReadOnlyCollection<Guid> ids, CancellationToken ct)
    {
        if (ids.Count == 0) return new Dictionary<Guid, EnergySystemQualityAssessment>();
        var set = ids.ToHashSet();
        var rows = await db.EnergySystems.AsNoTracking().Where(x => set.Contains(x.Id)).Select(x => new
        {
            x.Id, x.EnergySystemNumber, x.Type, x.RatedPowerKw,
            Assigned = x.BuildingAssignments.Any(a => a.ValidTo == null)
        }).ToListAsync(ct);
        var curatedRows = await db.CuratedFieldValues.AsNoTracking()
            .Where(x => x.EntityType == "EnergySystem" && set.Contains(x.EntityId) && x.ValidToUtc == null)
            .ToListAsync(ct);
        var curatedByEntity = curatedRows.GroupBy(x => x.EntityId)
            .ToDictionary(g => g.Key, g => (IReadOnlyDictionary<string, bool>)g.ToDictionary(x => x.FieldName, x => x.Confirmed));

        return rows.ToDictionary(x => x.Id, x =>
        {
            var confirmations = curatedByEntity.GetValueOrDefault(x.Id, EmptyConfirmations);
            var assessment = EnergySystemGoldDefinition.Evaluate(
                x.EnergySystemNumber, x.Type, x.RatedPowerKw, x.Assigned, confirmations);

            var missing = assessment.MissingReasons;
            var warnings = assessment.ConfirmationReasons;

            var nextActions = new List<string>();
            if (missing.Count > 0) nextActions.AddRange(missing.Select(m => $"{m.TrimEnd('.')} ergänzen."));
            else if (warnings.Count > 0) nextActions.Add("Anlagendaten fachlich bestätigen.");

            return new EnergySystemQualityAssessment(
                x.Id, assessment.MaturityLevel, assessment.IsGoldReady ? "Bestätigt" : "Unbestätigt",
                missing, missing, warnings, nextActions);
        });
    }

    private static readonly IReadOnlyDictionary<string, bool> EmptyConfirmations =
        new Dictionary<string, bool>();

    public async Task<IReadOnlyDictionary<Guid, OperationalBuildingQualityAssessment>> AssessBuildings(
        IReadOnlyCollection<Guid> ids, CancellationToken ct)
    {
        if (ids.Count == 0) return new Dictionary<Guid, OperationalBuildingQualityAssessment>();
        var set = ids.ToHashSet();
        var rows = await db.Buildings.AsNoTracking().Where(x => set.Contains(x.Id)).Select(x => new
        {
            x.Id,
            Version = x.Versions.Where(v => v.ValidTo == null)
                .OrderByDescending(v => v.VersionNumber)
                .Select(v => new
                {
                    Category = v.BuildingCategory,
                    Use = v.PrimaryUseType,
                    Postal = v.Address != null && v.Address.PostalCodeArea != null
                        ? v.Address.PostalCodeArea.Code : null
                }).FirstOrDefault(),
            Meters = x.Meters.Select(m =>
                new MeterAnnualInfo(m.Id, m.Direction, m.AnnualValue, m.AnnualValueOrigin)).ToList(),
            SystemIds = x.EnergySystemAssignments.Where(a => a.ValidTo == null)
                .Select(a => a.EnergySystemId).ToList()
        }).ToListAsync(ct);

        var meter = await AssessMeters(rows.SelectMany(x => x.Meters.Select(m => m.Id)).Distinct().ToArray(), ct);
        var systems = await AssessEnergySystems(rows.SelectMany(x => x.SystemIds).Distinct().ToArray(), ct);
        var declarations = await db.BuildingInventoryDeclarations.AsNoTracking()
            .Where(x => set.Contains(x.BuildingId) && x.IsCurrent).ToListAsync(ct);
        var curated = await db.CuratedFieldValues.AsNoTracking()
            .Where(x => x.EntityType == "Building" && set.Contains(x.EntityId) && x.ValidToUtc == null)
            .ToListAsync(ct);

        return rows.ToDictionary(x => x.Id, x =>
        {
            var fields = curated.Where(c => c.EntityId == x.Id).ToDictionary(c => c.FieldName, c => c);
            string? state = fields.GetValueOrDefault("BuildingState")?.CuratedValue;
            var core = BuildingGoldDefinition.Evaluate(
                x.Version?.Category.ToString(), x.Version?.Use.ToString(), state, x.Version?.Postal,
                fields.ToDictionary(k => k.Key, v => v.Value.Confirmed));
            var req = core.GoldFieldStates.Select(f => new QualityRequirement(
                f.FieldName, f.Label, f.State switch
                {
                    BuildingGoldFieldState.Confirmed => QualityRequirementState.Confirmed,
                    BuildingGoldFieldState.PresentUnconfirmed => QualityRequirementState.Complete,
                    _ => QualityRequirementState.Missing
                }, f.UnfulfilledReason)).ToArray();

            var declaration = declarations.SingleOrDefault(d => d.BuildingId == x.Id);
            var meterScopes = x.Meters.Select(m =>
                new ScopeQuality(m.Id, "Zählpunkt", meter[m.Id].QualityLevel, meter[m.Id].BlockingReasons)).ToArray();
            var systemScopes = x.SystemIds.Select(id =>
                new ScopeQuality(id, "Anlage", systems[id].QualityLevel, systems[id].BlockingReasons)).ToArray();

            var consumptionMeters = x.Meters.Where(m =>
                m.Direction is MeterDirection.Consumption or MeterDirection.Bidirectional).ToArray();
            var productionMeters = x.Meters.Where(m =>
                m.Direction is MeterDirection.Production or MeterDirection.Bidirectional).ToArray();
            var consumption = AnnualStatus(consumptionMeters, meter);
            var production = declaration?.NoRelevantEnergySystemsConfirmed == true
                ? AnnualEnergyStatus.Confirmed
                : AnnualStatus(productionMeters, meter);

            var assessment = HierarchicalQualityAssessment.Evaluate(new(
                req, meterScopes, systemScopes,
                declaration?.MeterInventoryComplete == true,
                declaration?.EnergySystemInventoryComplete == true,
                declaration?.NoRelevantEnergySystemsConfirmed == true,
                consumption, production,
                meterScopes.Any(m => m.BlockingReasons.Count > 0) ||
                systemScopes.Any(s => s.BlockingReasons.Count > 0)));

            var nextActions = new List<string>();
            if (declaration is null) nextActions.Add("Inventarerklärung abgeben.");
            if (meterScopes.Any(m => m.Level == DataMaturityLevel.Bronze) ||
                systemScopes.Any(s => s.Level == DataMaturityLevel.Bronze))
                nextActions.Add("Zählpunkte beziehungsweise Anlagen mit Bronze-Status vervollständigen.");
            if (consumption is AnnualEnergyStatus.NotAvailable or AnnualEnergyStatus.IncompleteYear)
                nextActions.Add("Jahresverbrauch vervollständigen.");
            if (nextActions.Count == 0 && assessment.OverallQualityLevel != DataMaturityLevel.Gold)
                nextActions.Add("Offene Qualitätsanforderungen bearbeiten.");

            return new OperationalBuildingQualityAssessment(
                x.Id, assessment, declaration is null ? "Nicht bestätigt" : "Bestätigt", nextActions)
            {
                MeterAssessments = x.Meters.Select(m => meter[m.Id]).ToArray(),
                EnergySystemAssessments = x.SystemIds.Select(id => systems[id]).ToArray(),
                NoRelevantEnergySystemsConfirmed = declaration?.NoRelevantEnergySystemsConfirmed == true
            };
        });
    }

    private static AnnualEnergyStatus AnnualStatus(
        IReadOnlyCollection<MeterAnnualInfo> meters,
        IReadOnlyDictionary<Guid, MeterQualityAssessment> assessments)
    {
        if (meters.Count == 0) return AnnualEnergyStatus.NotAvailable;
        var withValue = meters.Where(m => m.AnnualValue != null).ToArray();
        if (withValue.Length == 0) return AnnualEnergyStatus.NotAvailable;
        if (withValue.Length < meters.Count) return AnnualEnergyStatus.IncompleteYear;
        if (withValue.All(m => assessments[m.Id].QualityLevel == DataMaturityLevel.Gold))
            return AnnualEnergyStatus.Confirmed;
        if (withValue.Any(m => m.AnnualValueOrigin == "Manual"))
            return AnnualEnergyStatus.Estimated;
        return AnnualEnergyStatus.CompleteYear;
    }

    private static MeterProfileAnalysisStatus Map(ProfileAnalysisStatus status) => status switch
    {
        ProfileAnalysisStatus.Completed => MeterProfileAnalysisStatus.AnalysisCompleted,
        ProfileAnalysisStatus.RequiresReview => MeterProfileAnalysisStatus.RequiresReview,
        ProfileAnalysisStatus.Curated => MeterProfileAnalysisStatus.Curated,
        ProfileAnalysisStatus.Confirmed => MeterProfileAnalysisStatus.Confirmed,
        _ => MeterProfileAnalysisStatus.NotAnalyzed
    };
}
