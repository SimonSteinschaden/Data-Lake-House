using System.Text.Json;
using Enset.Application.Authorization;
using Enset.Application.Quality;
using Enset.Domain.Common;
using Enset.Domain.Quality;
using Enset.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Enset.Infrastructure.Quality;

public sealed class EfQualityInvalidationService(EnsetDbContext db, ICurrentUserContext user)
    : IQualityInvalidationService
{
    public async Task InvalidateBuildingInventory(Guid buildingId, string reason, CancellationToken ct)
    {
        var value = await db.BuildingInventoryDeclarations
            .SingleOrDefaultAsync(x => x.BuildingId == buildingId && x.IsCurrent, ct);
        if (value is null) return;
        value.IsCurrent = false;
        value.InvalidatedAtUtc = DateTime.UtcNow;
        value.InvalidationReason = reason;
        Audit("BuildingInventoryDeclaration", value.Id, "InventoryAutoInvalidated", reason);
        await db.SaveChangesAsync(ct);
    }

    public async Task InvalidateMeterAnalysis(Guid meterId, string reason, CancellationToken ct)
    {
        var value = await db.MeterProfileAnalyses
            .SingleOrDefaultAsync(x => x.MeterId == meterId && x.IsCurrent, ct);
        if (value is null) return;
        value.IsCurrent = false;
        value.SupersededAtUtc = DateTime.UtcNow;
        value.Summary = string.IsNullOrWhiteSpace(value.Summary)
            ? reason : $"{value.Summary} | Veraltet: {reason}";
        Audit("MeterProfileAnalysis", value.Id, "AnalysisAutoInvalidated", reason);
        await db.SaveChangesAsync(ct);
    }

    public async Task InvalidateBuildingConfirmations(Guid buildingId, string reason, CancellationToken ct)
    {
        var values = await db.CuratedFieldValues
            .Where(x => x.EntityType == "Building" && x.EntityId == buildingId &&
                x.ValidToUtc == null && x.Confirmed)
            .ToListAsync(ct);
        foreach (var value in values) value.Confirmed = false;
        if (values.Count > 0)
            Audit("Building", buildingId, "FieldConfirmationsAutoReset", reason);
        await InvalidateBuildingInventory(buildingId, reason, ct);
        if (values.Count > 0) await db.SaveChangesAsync(ct);
    }

    private void Audit(string entityType, Guid entityId, string action, string reason) =>
        db.EntityAuditEntries.Add(new EntityAuditEntry
        {
            OperationId = Guid.NewGuid(),
            EntityType = entityType,
            EntityId = entityId,
            ChangedAtUtc = DateTime.UtcNow,
            ChangedByUserId = user.UserId ?? Guid.Empty,
            ActorType = user.UserId.HasValue ? "EnsetEmployee" : "System",
            Action = action,
            ChangeType = EntityChangeType.Updated,
            FieldName = action,
            OldValue = null,
            NewValue = JsonSerializer.Serialize(new { reason }),
            Source = user.UserId.HasValue ? LastModifiedSource.User : LastModifiedSource.System,
            Reason = reason
        });
}
