using Enset.Application.Authorization;
using Enset.Application.Curation;
using Enset.Domain.Curation;
using Enset.Domain.Energy;
using Enset.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Enset.Infrastructure.Curation;

public sealed class EfCurationService(
    EnsetDbContext db,
    ICurrentUserContext currentUser,
    TimeProvider timeProvider) : ICurationService
{
    public async Task<IReadOnlyList<CurationTaskSummary>> GetTasksAsync(CancellationToken ct)
    {
        await DiscoverTasksAsync(ct);
        return await db.CurationTasks.AsNoTracking()
            .OrderBy(x => x.Status).ThenByDescending(x => x.ConfidencePercent)
            .ThenBy(x => x.EntityDisplayName)
            .Select(x => Map(x)).ToListAsync(ct);
    }

    public async Task<CurationTaskDetail?> GetTaskAsync(Guid id, CancellationToken ct)
    {
        await DiscoverTasksAsync(ct);
        var task = await db.CurationTasks.AsNoTracking().Include(x => x.Decisions)
            .SingleOrDefaultAsync(x => x.Id == id, ct);
        return task is null ? null : MapDetail(task);
    }

    public Task<CurationTaskDetail> AcceptAsync(Guid id, CancellationToken ct) =>
        DecideAsync(id, CurationTaskStatus.Accepted, null, null, ct);

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
        return new CurationStatistics(bronze, Math.Max(0, entityCount - gold), gold,
            groups.Sum(x => x.Count), groups);
    }

    private async Task<CurationTaskDetail> DecideAsync(Guid id, CurationTaskStatus status,
        string? customValue, string? reason, CancellationToken ct)
    {
        var task = await db.CurationTasks.Include(x => x.Decisions)
            .SingleOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new CurationNotFoundException("Die Kurationsaufgabe wurde nicht gefunden.");
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
        task.Decisions.Add(new CurationDecision
        {
            UserId = userId,
            DecidedAtUtc = now,
            Decision = status,
            OriginalValue = task.OriginalValue,
            SuggestedValue = task.SuggestedValue,
            NewValue = curatedValue,
            Source = source,
            ConfidencePercent = task.ConfidencePercent,
            Reason = reason
        });
        await db.SaveChangesAsync(ct);
        return MapDetail(task);
    }

    private async Task DiscoverTasksAsync(CancellationToken ct)
    {
        var keys = await db.CurationTasks.AsNoTracking()
            .Select(x => x.EntityType + "|" + x.EntityId + "|" + x.FieldName)
            .ToHashSetAsync(ct);
        var additions = new List<CurationTask>();

        var meters = await db.Meters.AsNoTracking()
            .Where(x => x.Medium == MeterMedium.Unknown)
            .Select(x => new { x.Id, x.Name, x.MeterNumber, x.Unit, x.Quantity }).ToListAsync(ct);
        foreach (var meter in meters)
        {
            var suggestion = SuggestMedium(meter.Name, meter.Unit.ToString(), meter.Quantity.ToString());
            Add(additions, keys, "MeteringPoint", meter.Id,
                $"{meter.MeterNumber} · {meter.Name}", "Medium", null,
                suggestion.Value, suggestion.Confidence, suggestion.Reason);
        }

        var buildings = await db.Buildings.AsNoTracking()
            .Where(x => !x.Versions.Any())
            .Select(x => new { x.Id, x.BuildingNumber, x.Name }).ToListAsync(ct);
        foreach (var building in buildings)
        {
            var category = SuggestBuildingCategory(building.Name);
            Add(additions, keys, "Building", building.Id,
                $"{building.BuildingNumber} · {building.Name}", "BuildingCategory",
                null, category.Value, category.Confidence, category.Reason);
            var usage = SuggestUsage(building.Name);
            Add(additions, keys, "Building", building.Id,
                $"{building.BuildingNumber} · {building.Name}", "PrimaryUseType",
                null, usage.Value, usage.Confidence, usage.Reason);
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
            Add(additions, keys, "Building", building.Id,
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
            Add(additions, keys, "EnergySystem", system.Id,
                $"{system.EnergySystemNumber} · {system.Name}", "BuildingId", null,
                match.Id.ToString(), 98,
                "Die externe Anlagen-ID entspricht exakt der Gebäudenummer oder externen Gebäude-ID.");
        }

        if (additions.Count == 0) return;
        db.CurationTasks.AddRange(additions);
        await db.SaveChangesAsync(ct);
    }

    private static void Add(List<CurationTask> tasks, HashSet<string> keys,
        string entityType, Guid entityId, string display, string field,
        string? original, string suggested, int confidence, string reason)
    {
        if (!keys.Add($"{entityType}|{entityId}|{field}")) return;
        tasks.Add(new CurationTask
        {
            EntityType = entityType, EntityId = entityId,
            EntityDisplayName = display, FieldName = field,
            OriginalValue = original, SuggestedValue = suggested,
            ConfidencePercent = confidence, Reasoning = reason
        });
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
            x.Status, x.CuratedValue, x.Source);

    private static CurationTaskDetail MapDetail(CurationTask x) =>
        new(Map(x), x.Decisions.OrderByDescending(d => d.DecidedAtUtc)
            .Select(d => new CurationDecisionDto(d.Id, d.UserId, d.DecidedAtUtc,
                d.Decision, d.OriginalValue, d.SuggestedValue, d.NewValue,
                d.Source, d.ConfidencePercent, d.Reason)).ToList());
}
