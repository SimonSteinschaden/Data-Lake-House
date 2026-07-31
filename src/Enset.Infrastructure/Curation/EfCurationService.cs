using Enset.Application.Authorization;
using Enset.Application.CanonicalSnapshots;
using Enset.Application.Curation;
using Enset.Domain.Curation;
using Enset.Domain.Energy;
using Enset.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Enset.Infrastructure.Curation;

public sealed class EfCurationService(
    EnsetDbContext db,
    ICurrentUserContext currentUser,
    TimeProvider timeProvider,
    IDataAccessScope scope,
    ICanonicalSnapshotReader snapshots) : ICurationService
{
    public async Task<CurationTaskPage> GetTasksAsync(CurationTaskQuery request, CancellationToken ct)
    {
        await DiscoverTasksAsync(ct);
        var buildingIds = scope.ApplyBuildingScope(db.Buildings).Select(x => x.Id);
        var meterIds = scope.ApplyMeterScope(db.Meters).Select(x => x.Id);
        var energySystemIds = db.EnergySystemBuildingAssignments
            .Where(a => a.ValidTo == null && buildingIds.Contains(a.BuildingId))
            .Select(a => a.EnergySystemId);
        var query = db.CurationTasks.AsNoTracking().Where(x =>
            (x.EntityType == "Building" && buildingIds.Contains(x.EntityId)) ||
            (x.EntityType == "MeteringPoint" && meterIds.Contains(x.EntityId)) ||
            (x.EntityType == "EnergySystem" && energySystemIds.Contains(x.EntityId)));
        if (!string.IsNullOrWhiteSpace(request.EntityType)) query = query.Where(x => x.EntityType == request.EntityType);
        if (!string.IsNullOrWhiteSpace(request.FieldName)) query = query.Where(x => x.FieldName == request.FieldName);
        if (request.Status.HasValue) query = query.Where(x => x.Status == request.Status);
        if (request.MinimumConfidence.HasValue) query = query.Where(x => x.ConfidencePercent >= request.MinimumConfidence);
        if (request.BuildingId.HasValue) query = query.Where(x => x.EntityId == request.BuildingId ||
            (x.EntityType == "MeteringPoint" && db.Meters.Any(m => m.Id == x.EntityId && m.BuildingId == request.BuildingId)));
        if (request.MeteringPointId.HasValue)
            query = query.Where(x => x.EntityType == "MeteringPoint" &&
                x.EntityId == request.MeteringPointId);
        if (request.EntityId.HasValue)
            query = query.Where(x => x.EntityId == request.EntityId);
        if (request.CustomerId.HasValue) query = query.Where(x =>
            (x.EntityType == "Building" && db.CustomerBuildingAssignments.Any(a => a.BuildingId == x.EntityId && a.CustomerId == request.CustomerId)) ||
            (x.EntityType == "MeteringPoint" && db.Meters.Any(m => m.Id == x.EntityId && m.Building!.CustomerAssignments.Any(a => a.CustomerId == request.CustomerId))));
        var page = Math.Max(1, request.Page); var pageSize = Math.Clamp(request.PageSize, 1, 100);
        var total = await query.CountAsync(ct);
        var items = await query
            .OrderBy(x => x.Status).ThenByDescending(x => x.ConfidencePercent)
            .ThenBy(x => x.EntityDisplayName)
            .Skip((page - 1) * pageSize).Take(pageSize).Select(x => Map(x)).ToListAsync(ct);
        return new CurationTaskPage(items, page, pageSize, total,
            Math.Max(1, (int)Math.Ceiling(total / (double)pageSize)));
    }

    public async Task<CurationTaskDetail?> GetTaskAsync(Guid id, CancellationToken ct)
    {
        await DiscoverTasksAsync(ct);
        var task = await db.CurationTasks.AsNoTracking().Include(x => x.Decisions)
            .SingleOrDefaultAsync(x => x.Id == id, ct);
        return task is null || !await IsVisible(task, ct) ? null : MapDetail(task);
    }

    public Task<CurationTaskDetail> AcceptAsync(Guid id, CancellationToken ct) =>
        DecideAsync(id, CurationTaskStatus.Accepted, null,
            "Fachlich bestätigt.", ct);

    public Task<CurationTaskDetail> RejectAsync(Guid id, string? reason, CancellationToken ct) =>
        DecideAsync(id, CurationTaskStatus.Rejected, null, reason, ct);

    public Task<CurationTaskDetail> CustomizeAsync(
        Guid id, string value, string? reason, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new CurationValidationException("Ein kuratierter Wert ist erforderlich.");
        return DecideAsync(id, CurationTaskStatus.Customized, value.Trim(), reason, ct);
    }

    public async Task<CurationStatistics> GetStatisticsAsync(CancellationToken ct)
    {
        await DiscoverTasksAsync(ct);
        var bronze = await db.ImportedMeterReadings.CountAsync(ct);
        var entityCount = await db.Customers.IgnoreQueryFilters().CountAsync(ct)
            + await db.Buildings.IgnoreQueryFilters().CountAsync(ct)
            + await db.Meters.IgnoreQueryFilters().CountAsync(ct)
            + await db.EnergySystems.IgnoreQueryFilters().CountAsync(ct);
        var gold = await db.CurationTasks
            .Where(x => x.Status == CurationTaskStatus.Accepted ||
                        x.Status == CurationTaskStatus.Customized)
            .Select(x => new { x.EntityType, x.EntityId }).Distinct().CountAsync(ct);
        var groups = await db.CurationTasks.AsNoTracking()
            .Where(x => x.Status == CurationTaskStatus.Open)
            .GroupBy(x => new { x.EntityType, x.FieldName })
            .Select(x => new CurationTaskGroup(x.Key.EntityType, x.Key.FieldName, x.Count()))
            .OrderByDescending(x => x.Count).ToListAsync(ct);
        int Open(string entity, string field) => groups
            .Where(x => x.EntityType == entity && x.FieldName == field).Sum(x => x.Count);
        var rejected = await db.CurationTasks.CountAsync(x => x.Status == CurationTaskStatus.Rejected, ct);
        return new CurationStatistics(bronze, Math.Max(0, entityCount - gold), gold,
            groups.Sum(x => x.Count), rejected, Open("Building", "PrimaryUseType"),
            Open("Building", "HeatedAreaSquareMeters"), Open("Building", "PostalCode"),
            Open("MeteringPoint", "UsageType"), Open("MeteringPoint", "Medium"),
            await CountIncompleteMeters(ct), groups);
    }

    private async Task<CurationTaskDetail> DecideAsync(Guid id, CurationTaskStatus status,
        string? customValue, string? reason, CancellationToken ct)
    {
        var task = await db.CurationTasks.Include(x => x.Decisions)
            .SingleOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new CurationNotFoundException("Die Kurationsaufgabe wurde nicht gefunden.");
        if (!await IsVisible(task, ct))
            throw new CurationNotFoundException("Die Kurationsaufgabe wurde nicht gefunden.");
        if (task.Status != CurationTaskStatus.Open)
            throw new CurationConflictException("Über diese Kurationsaufgabe wurde bereits entschieden.");

        var userId = currentUser.UserId
            ?? throw new CurationConflictException("Für eine Kurationsentscheidung ist ein Benutzer erforderlich.");
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var curatedValue = status == CurationTaskStatus.Rejected
            ? null : customValue ?? task.SuggestedValue;
        var source = status == CurationTaskStatus.Customized
            ? CurationSource.User : CurationSource.EnsetSuggestion;

        task.Status = status;
        task.CuratedValue = curatedValue;
        task.Source = source;
        task.DecidedAtUtc = now;
        task.DecidedByUserId = userId;
        var decision = new CurationDecision
        {
            UserId = userId,
            DecidedAtUtc = now,
            Decision = status,
            OriginalValue = task.OriginalValue,
            SuggestedValue = task.SuggestedValue,
            NewValue = curatedValue,
            Source = source,
            ConfidencePercent = status == CurationTaskStatus.Customized ? 100 : task.ConfidencePercent,
            Reason = reason
        };
        task.Decisions.Add(decision);
        db.Entry(decision).State = EntityState.Added;
        if (curatedValue is not null)
        {
            var previous = await db.CuratedFieldValues.SingleOrDefaultAsync(x =>
                x.EntityType == task.EntityType && x.EntityId == task.EntityId &&
                x.FieldName == task.FieldName && x.ValidToUtc == null, ct);
            if (previous is not null) previous.ValidToUtc = now;
            db.CuratedFieldValues.Add(new CuratedFieldValue
            {
                EntityType = task.EntityType, EntityId = task.EntityId,
                FieldName = task.FieldName, OriginalValue = task.OriginalValue,
                CuratedValue = curatedValue, NormalizedValue = curatedValue.Trim(),
                Source = source, MaturityLevel = DataMaturityLevel.Gold,
                ConfidencePercent = status == CurationTaskStatus.Customized ? 100 : task.ConfidencePercent,
                RuleId = task.RuleId, RuleVersion = task.RuleVersion,
                Confirmed = true, ConfirmedByUserId = userId,
                ConfirmedAtUtc = now, ValidFromUtc = now
            });
        }
        await db.SaveChangesAsync(ct);
        return MapDetail(task);
    }

    private async Task DiscoverTasksAsync(CancellationToken ct)
    {
        var tasks = (await db.CurationTasks.ToListAsync(ct))
            .ToDictionary(x => $"{x.EntityType}|{x.EntityId}|{x.FieldName}");
        var additions = new List<CurationTask>();

        var meters = await db.Meters.AsNoTracking()
            .Where(x => x.Medium == MeterMedium.Unknown)
            .Select(x => new { x.Id, x.Name, x.MeterNumber, x.Unit, x.Quantity }).ToListAsync(ct);
        foreach (var meter in meters)
        {
            var suggestion = SuggestMedium(meter.Name, meter.Unit.ToString(), meter.Quantity.ToString());
            Add(additions, tasks, "MeteringPoint", meter.Id,
                $"{meter.MeterNumber} · {meter.Name}", "Medium", null,
                suggestion.Value, suggestion.Confidence, suggestion.Reason);
        }

        var buildings = await db.Buildings.AsNoTracking()
            .Where(x => !x.Versions.Any())
            .Select(x => new { x.Id, x.BuildingNumber, x.Name }).ToListAsync(ct);
        foreach (var building in buildings)
        {
            var category = SuggestBuildingCategory(building.Name);
            Add(additions, tasks, "Building", building.Id,
                $"{building.BuildingNumber} · {building.Name}", "BuildingCategory",
                null, category.Value, category.Confidence, category.Reason);
            var usage = SuggestUsage(building.Name);
            Add(additions, tasks, "Building", building.Id,
                $"{building.BuildingNumber} · {building.Name}", "PrimaryUseType",
                null, usage.Value, usage.Confidence, usage.Reason);
        }

        var portfolio = await snapshots.GetPortfolio(ct);
        foreach (var building in portfolio.Buildings)
        {
            foreach (var field in building.GoldAssessment.GoldFieldStates
                         .Where(item =>
                             item.State ==
                             BuildingGoldFieldState.PresentUnconfirmed))
            {
                Add(
                    additions,
                    tasks,
                    "Building",
                    building.BuildingId,
                    $"{building.BuildingNumber} · {building.Name}",
                    field.FieldName,
                    field.Value,
                    field.Value!,
                    100,
                    $"{field.Label} ist vorhanden und muss fachlich bestätigt werden.");
            }
        }

        foreach (var system in portfolio.EnergySystems)
        {
            foreach (var field in system.GoldAssessment.GoldFieldStates
                         .Where(item =>
                             item.IsConfirmable &&
                             item.State ==
                             EnergySystemGoldFieldState.PresentUnconfirmed))
            {
                Add(
                    additions,
                    tasks,
                    "EnergySystem",
                    system.EnergySystemId,
                    $"{system.EnergySystemNumber} · {system.Name}",
                    field.FieldName,
                    field.Value,
                    field.Value!,
                    100,
                    $"{field.Label} ist vorhanden und muss fachlich bestätigt werden.");
            }
        }

        var customers = await db.Customers.AsNoTracking()
            .Select(x => new { x.Id, x.CustomerNumber, x.Name }).ToListAsync(ct);
        var unassignedBuildings = await db.Buildings.AsNoTracking()
            .Where(x => !x.CustomerAssignments.Any())
            .Select(x => new { x.Id, x.BuildingNumber, x.Name, x.ExternalIdentifier })
            .ToListAsync(ct);
        foreach (var building in unassignedBuildings)
        {
            var match = customers.FirstOrDefault(customer =>
                (!string.IsNullOrWhiteSpace(building.ExternalIdentifier) &&
                 string.Equals(customer.CustomerNumber, building.ExternalIdentifier,
                     StringComparison.OrdinalIgnoreCase)) ||
                string.Equals(customer.Name, building.Name, StringComparison.OrdinalIgnoreCase));
            if (match is null) continue;
            var externalIdMatch = string.Equals(match.CustomerNumber,
                building.ExternalIdentifier, StringComparison.OrdinalIgnoreCase);
            Add(additions, tasks, "Building", building.Id,
                $"{building.BuildingNumber} · {building.Name}", "CustomerId", null,
                match.Id.ToString(), externalIdMatch ? 98 : 88,
                externalIdMatch
                    ? "Die externe Gebäude-ID entspricht exakt der Kundennummer."
                    : "Gebäude- und Kundenname stimmen exakt überein.");
        }

        var candidateBuildings = await db.Buildings.AsNoTracking()
            .Select(x => new { x.Id, x.BuildingNumber, x.ExternalIdentifier }).ToListAsync(ct);
        var unassignedSystems = await db.EnergySystems.AsNoTracking()
            .Where(x => !x.BuildingAssignments.Any() && x.ExternalIdentifier != null)
            .Select(x => new { x.Id, x.EnergySystemNumber, x.Name, x.ExternalIdentifier })
            .ToListAsync(ct);
        foreach (var system in unassignedSystems)
        {
            var match = candidateBuildings.FirstOrDefault(building =>
                string.Equals(building.BuildingNumber, system.ExternalIdentifier,
                    StringComparison.OrdinalIgnoreCase) ||
                string.Equals(building.ExternalIdentifier, system.ExternalIdentifier,
                    StringComparison.OrdinalIgnoreCase));
            if (match is null) continue;
            Add(additions, tasks, "EnergySystem", system.Id,
                $"{system.EnergySystemNumber} · {system.Name}", "BuildingId", null,
                match.Id.ToString(), 98,
                "Die externe Anlagen-ID entspricht exakt der Gebäudenummer oder externen Gebäude-ID.");
        }

        if (additions.Count == 0) return;
        db.CurationTasks.AddRange(additions);
        await db.SaveChangesAsync(ct);
    }

    public async Task<BuildingGoldProfile?> GetBuildingProfileAsync(Guid id, CancellationToken ct)
    {
        var building = await snapshots.GetBuilding(id, ct);
        if (building is null) return null;
        var fields = await CurrentFields("Building", id, ct);
        string? Field(string name, string? fallback = null) =>
            fields.FirstOrDefault(x => x.FieldName == name)?.NormalizedValue ?? fallback;
        decimal? DecimalField(string name, decimal? fallback = null) =>
            decimal.TryParse(Field(name), System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out var value) ? value : fallback;
        var readiness = Readiness(id, building.GoldAssessment, fields);
        var buildingState = Enum.TryParse<BuildingState>(building.BuildingState, true, out var state)
            ? state : BuildingState.Unknown;
        return new BuildingGoldProfile(id,
            building.CustomerId,
            building.UsageType,
            DecimalField("HeatedAreaSquareMeters", building.HeatedArea),
            building.PostalCode,
            DecimalField("ElectricityConsumptionKwh"), DecimalField("ProductionKwh"),
            DecimalField("HwbKwhPerSquareMeterYear"), buildingState,
            Field("RenovationYear", building.RenovationYear?.ToString()),
            building.BuildingType,
            Field("Address", $"{building.Street} {building.HouseNumber}".Trim()),
            Field("ConstructionYear", building.ConstructionYear?.ToString()),
            Field("EnergyCarrier"), Field("ClimateRegion"), Field("AdditionalClassification"),
            readiness.MaturityLevel, readiness.ReadinessPercent,
            readiness.BlockingIssues.Count == 0 ? "Keine blockierenden Qualitätsprobleme." :
                string.Join(" ", readiness.BlockingIssues), readiness.Fields);
    }

    public async Task<MeteringPointGoldProfile?> GetMeteringPointProfileAsync(Guid id, CancellationToken ct)
    {
        var meter = await scope.ApplyMeterScope(db.Meters).AsNoTracking()
            .Include(x => x.Building).ThenInclude(x => x!.CustomerAssignments)
            .SingleOrDefaultAsync(x => x.Id == id, ct);
        if (meter is null) return null;
        var fields = await CurrentFields("MeteringPoint", id, ct);
        var readings = await scope.ApplyMeterReadingScope(db.MeterReadings).AsNoTracking()
            .Where(x => x.MeterId == id).Select(x => new
            { x.Timestamp, x.IntervalSeconds, x.QualityFlag }).ToListAsync(ct);
        var start = readings.Count == 0 ? null : readings.Min(x => x.Timestamp) as DateTime?;
        var end = readings.Count == 0 ? null : readings.Max(x => x.Timestamp) as DateTime?;
        var intervalSeconds = readings.Where(x => x.IntervalSeconds > 0)
            .GroupBy(x => x.IntervalSeconds!.Value).OrderByDescending(x => x.Count())
            .Select(x => (int?)x.Key).FirstOrDefault();
        var actual = readings.Select(x => x.Timestamp).Distinct().LongCount();
        var expected = start.HasValue && end.HasValue && intervalSeconds.HasValue
            ? (long)Math.Floor((end.Value - start.Value).TotalSeconds / intervalSeconds.Value) + 1 : 0;
        var missing = Math.Max(0, expected - actual);
        decimal Percent(long count) => actual == 0 ? 0 : Math.Round(count * 100m / actual, 2);
        var invalid = readings.LongCount(x => x.QualityFlag is Enset.Domain.Data.DataQuality.Invalid or Enset.Domain.Data.DataQuality.Missing);
        var estimated = readings.LongCount(x => x.QualityFlag == Enset.Domain.Data.DataQuality.Estimated);
        var interpolated = readings.LongCount(x => x.QualityFlag == Enset.Domain.Data.DataQuality.Interpolated);
        var measured = readings.LongCount(x => x.QualityFlag is Enset.Domain.Data.DataQuality.Measured or Enset.Domain.Data.DataQuality.Validated);
        var derived = readings.LongCount(x => x.QualityFlag == Enset.Domain.Data.DataQuality.Calculated);
        var readiness = BuildReadiness(id, "MeteringPoint", fields, new[]
        {
            ("UsageType", Value(fields, "UsageType")),
            ("EnergyCarrier", Value(fields, "Medium", meter.Medium == MeterMedium.Unknown ? null : meter.Medium.ToString())),
            ("MeasurementType", meter.Quantity == MeterQuantity.Unknown ? null : meter.Quantity.ToString()),
            ("Unit", meter.Unit == MeterUnit.Unknown ? null : meter.Unit.ToString()),
            ("MeasurementPeriod", start.HasValue && end.HasValue ? $"{start:O}/{end:O}" : null),
            ("IntervalMinutes", intervalSeconds.HasValue ? (intervalSeconds.Value / 60).ToString() : null),
            ("Quality", invalid == 0 && missing == 0 ? "Valid" : null)
        });
        return new MeteringPointGoldProfile(id, meter.BuildingId,
            meter.Building?.CustomerAssignments.FirstOrDefault()?.CustomerId,
            Value(fields, "UsageType"), Value(fields, "Medium", meter.Medium.ToString()),
            meter.Quantity.ToString(), meter.Unit.ToString(), start, end,
            intervalSeconds / 60, expected, actual, missing, invalid, estimated, interpolated,
            expected == 0 ? 0 : Math.Round(actual * 100m / expected, 2),
            Percent(measured), Percent(derived), null, BuildingState.Unknown,
            readiness.MaturityLevel,
            $"Fehlend: {missing}; ungültig: {invalid}; geschätzt: {estimated}; interpoliert: {interpolated}.",
            readiness.Fields);
    }

    public async Task<CurationReadiness?> GetBuildingReadinessAsync(Guid id, CancellationToken ct) =>
        (await snapshots.GetBuilding(id, ct)) is { } building
            ? Readiness(
                id,
                building.GoldAssessment,
                await CurrentFields("Building", id, ct))
            : null;

    public async Task<CurationReadiness?> GetMeteringPointReadinessAsync(Guid id, CancellationToken ct) =>
        (await GetMeteringPointProfileAsync(id, ct)) is { } p
            ? new CurationReadiness(id, p.MaturityLevel,
                p.FieldMaturity.Count == 0 ? 0 : p.FieldMaturity.Count(x => x.Satisfied) * 100 / p.FieldMaturity.Count,
                p.MaturityLevel == DataMaturityLevel.Gold, p.FieldMaturity,
                p.FieldMaturity.Where(x => !x.Satisfied).Select(x => x.Explanation).ToList()) : null;

    public async Task<int> EvaluateBuildingAsync(Guid id, CancellationToken ct)
    { if (!await scope.CanReadBuilding(id, ct)) throw new CurationNotFoundException("Gebäude nicht gefunden."); var before = await db.CurationTasks.CountAsync(ct); await DiscoverTasksAsync(ct); return await db.CurationTasks.CountAsync(ct) - before; }
    public async Task<int> EvaluateMeteringPointAsync(Guid id, CancellationToken ct)
    { if (!await scope.CanReadMeter(id, ct)) throw new CurationNotFoundException("Zählpunkt nicht gefunden."); var before = await db.CurationTasks.CountAsync(ct); await DiscoverTasksAsync(ct); return await db.CurationTasks.CountAsync(ct) - before; }

    private Task<List<CuratedFieldValue>> CurrentFields(string type, Guid id, CancellationToken ct) =>
        db.CuratedFieldValues.AsNoTracking().Where(x => x.EntityType == type &&
            x.EntityId == id && x.ValidToUtc == null).ToListAsync(ct);
    private Task<bool> IsVisible(CurationTask task, CancellationToken ct) =>
        task.EntityType == "Building" ? scope.CanReadBuilding(task.EntityId, ct) :
        task.EntityType == "MeteringPoint" ? scope.CanReadMeter(task.EntityId, ct) :
        task.EntityType == "EnergySystem" ? IsEnergySystemVisible(task.EntityId, ct) :
        Task.FromResult(currentUser.IsEnsetEmployee);

    private async Task<bool> IsEnergySystemVisible(Guid energySystemId, CancellationToken ct)
    {
        var buildingId = await db.EnergySystemBuildingAssignments
            .Where(a => a.EnergySystemId == energySystemId && a.ValidTo == null)
            .Select(a => (Guid?)a.BuildingId)
            .FirstOrDefaultAsync(ct);
        return buildingId.HasValue && await scope.CanReadBuilding(buildingId.Value, ct);
    }
    private static string? Value(IEnumerable<CuratedFieldValue> fields, string name, string? fallback = null) =>
        fields.FirstOrDefault(x => x.FieldName == name)?.NormalizedValue ?? fallback;
    private static CurationReadiness BuildReadiness(Guid id, string type,
        IReadOnlyList<CuratedFieldValue> fields, IEnumerable<(string Name, string? Value)> required)
    {
        var items = required.Select(x =>
        {
            var curated = fields.FirstOrDefault(f => f.FieldName == x.Name);
            var satisfied = !string.IsNullOrWhiteSpace(x.Value);
            return new FieldReadiness(x.Name,
                curated?.MaturityLevel ?? (satisfied ? DataMaturityLevel.Silver : DataMaturityLevel.Bronze),
                true, satisfied && curated?.MaturityLevel == DataMaturityLevel.Gold,
                satisfied ? $"{x.Name} ist noch nicht fachlich als Gold bestätigt." : $"{x.Name} fehlt.",
                x.Value, curated?.Source);
        }).ToList();
        var percent = items.Count == 0 ? 0 : items.Count(x => x.Satisfied) * 100 / items.Count;
        var maturity = items.All(x => x.Satisfied) ? DataMaturityLevel.Gold :
            items.All(x => x.Value is not null) ? DataMaturityLevel.Silver : DataMaturityLevel.Bronze;
        return new CurationReadiness(id, maturity, percent, maturity == DataMaturityLevel.Gold,
            items, items.Where(x => !x.Satisfied).Select(x => x.Explanation).ToList());
    }

    private static CurationReadiness Readiness(
        Guid id,
        BuildingGoldAssessment assessment,
        IReadOnlyList<CuratedFieldValue> fields)
    {
        var items = assessment.GoldFieldStates.Select(field =>
        {
            var curated = fields.FirstOrDefault(value =>
                value.FieldName == field.FieldName);
            return new FieldReadiness(
                field.FieldName,
                curated?.MaturityLevel ??
                    (field.HasValue
                        ? DataMaturityLevel.Silver
                        : DataMaturityLevel.Bronze),
                false,
                field.HasValue,
                field.UnfulfilledReason ??
                    $"{field.Label} ist fachlich als Gold bestätigt.",
                field.Value,
                curated?.Source)
            {
                Label = field.Label,
                HasValue = field.HasValue,
                IsConfirmed = field.IsConfirmed,
                IsGoldRelevant = field.IsGoldRelevant
            };
        }).ToList();
        return new CurationReadiness(
            id,
            assessment.MaturityLevel,
            assessment.GoldCompletenessPercentage,
            assessment.IsGoldReady,
            items,
            assessment.UnfulfilledReasons)
        {
            ConfirmationPercent = assessment.GoldConfirmationPercentage
        };
    }
    private async Task<int> CountIncompleteMeters(CancellationToken ct)
    {
        var meterIds = await scope.ApplyMeterScope(db.Meters).Select(x => x.Id).ToListAsync(ct);
        var count = 0;
        foreach (var id in meterIds)
            if ((await GetMeteringPointProfileAsync(id, ct)) is { CompletenessPercentage: < 100 }) count++;
        return count;
    }

    private static void Add(List<CurationTask> additions,
        IDictionary<string, CurationTask> tasks,
        string entityType, Guid entityId, string display, string field,
        string? original, string suggested, int confidence, string reason)
    {
        var key = $"{entityType}|{entityId}|{field}";
        var normalized = suggested.Trim();
        if (tasks.TryGetValue(key, out var existing))
        {
            if (existing.SuggestedNormalizedValue == normalized)
                return;
            existing.EntityDisplayName = display;
            existing.OriginalValue = original;
            existing.SuggestedValue = suggested;
            existing.SuggestedNormalizedValue = normalized;
            existing.ConfidencePercent = confidence;
            existing.Reasoning = reason;
            existing.Status = CurationTaskStatus.Open;
            existing.CuratedValue = null;
            existing.DecidedAtUtc = null;
            existing.DecidedByUserId = null;
            return;
        }
        var task = new CurationTask
        {
            EntityType = entityType, EntityId = entityId,
            EntityDisplayName = display, FieldName = field,
            OriginalValue = original, SuggestedValue = suggested,
            SuggestedNormalizedValue = normalized,
            ConfidencePercent = confidence, Reasoning = reason,
            RuleId = field switch
            {
                "PrimaryUseType" => "BUILDING_USAGE_FROM_NAME",
                "BuildingCategory" => "BUILDING_TYPE_FROM_NAME",
                "Medium" => "METER_ENERGY_CARRIER_FROM_MEASUREMENT",
                "CustomerId" => "BUILDING_CUSTOMER_FROM_EXTERNAL_ID",
                "BuildingId" => "ENERGY_SYSTEM_BUILDING_FROM_EXTERNAL_ID",
                _ => "ENSET_DETERMINISTIC_RULE"
            },
            RuleVersion = "1.0"
        };
        tasks[key] = task;
        additions.Add(task);
    }

    public static (string Value, int Confidence, string Reason) SuggestMedium(
        string name, string unit, string quantity)
    {
        var text = $"{name} {unit} {quantity}".ToLowerInvariant();
        if (text.Contains("strom") || text.Contains("electric"))
            return ("Electricity", 96, "Name enthält „Strom“ oder „Electric“; Einheit und Messgröße unterstützen einen Energiezähler.");
        if (text.Contains("wärme") || text.Contains("heat"))
            return ("Heat", 94, "Name oder Messgröße weist auf Wärme hin.");
        if (text.Contains("wasser") || text.Contains("water") || unit.Contains("m3", StringComparison.OrdinalIgnoreCase))
            return ("Water", 90, "Name oder Volumeneinheit weist auf Wasser hin.");
        return ("Electricity", 65, "Die Messgröße ist Energie; ohne eindeutigen Namenshinweis ist die Konfidenz reduziert.");
    }

    public static (string Value, int Confidence, string Reason) SuggestBuildingCategory(string name)
    {
        var value = name.ToLowerInvariant();
        if (value.Contains("schule") || value.Contains("volksschule"))
            return ("School", 97, "Der Gebäudename enthält „Schule“ oder „Volksschule“.");
        if (value.Contains("büro") || value.Contains("amt"))
            return ("Office", 90, "Der Gebäudename enthält „Büro“ oder „Amt“.");
        if (value.Contains("halle"))
            return ("Hall", 91, "Der Gebäudename enthält „Halle“.");
        return ("Other", 55, "Aus dem Gebäudenamen ist kein eindeutiger Typ ableitbar.");
    }

    public static (string Value, int Confidence, string Reason) SuggestUsage(string name)
    {
        var value = name.ToLowerInvariant();
        if (value.Contains("schule") || value.Contains("kindergarten"))
            return ("Public", 95, "Der Name bezeichnet eine öffentliche Bildungs- oder Betreuungseinrichtung.");
        if (value.Contains("wohn") || value.Contains("haus"))
            return ("Residential", 82, "Der Name weist auf Wohnnutzung hin.");
        if (value.Contains("betrieb") || value.Contains("gewerbe"))
            return ("Commercial", 88, "Der Name weist auf betriebliche oder gewerbliche Nutzung hin.");
        return ("Mixed", 50, "Die Nutzungsart ist aus dem Namen nicht eindeutig bestimmbar.");
    }

    private static CurationTaskSummary Map(CurationTask x) =>
        new(x.Id, x.EntityType, x.EntityId, x.EntityDisplayName, x.FieldName,
            x.OriginalValue, x.SuggestedValue, x.ConfidencePercent, x.Reasoning,
            x.Status, x.CuratedValue, x.Source, x.RuleId, x.RuleVersion,
            x.Status is CurationTaskStatus.Accepted or CurationTaskStatus.Customized
                ? DataMaturityLevel.Gold : DataMaturityLevel.Bronze);

    private static CurationTaskDetail MapDetail(CurationTask x) =>
        new(Map(x), x.Decisions.OrderByDescending(d => d.DecidedAtUtc)
            .Select(d => new CurationDecisionDto(d.Id, d.UserId, d.DecidedAtUtc,
                d.Decision, d.OriginalValue, d.SuggestedValue, d.NewValue,
                d.Source, d.ConfidencePercent, d.Reason)).ToList());
}
