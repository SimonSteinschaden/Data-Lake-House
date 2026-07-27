namespace Enset.Application.Crud;

public sealed record CustomerWriteModel(string CustomerNumber, string Name, string Type,
    string? LegalName, string? Email, string? Phone, string CountryCode, uint RowVersion = 0);
public sealed record BuildingWriteModel(string BuildingNumber, string Name,
    string? ExternalIdentifier, Guid? CustomerId, decimal? GrossFloorAreaM2 = null,
    int? YearOfConstruction = null, decimal? Latitude = null, decimal? Longitude = null,
    uint RowVersion = 0);
public sealed record MeterWriteModel(string MeterNumber, string Name, Guid BuildingId,
    string Medium, string Quantity, string Unit, string Direction, string Type,
    Guid? EnergySystemId, uint RowVersion = 0);
public sealed record EnergySystemWriteModel(string EnergySystemNumber, string Name,
    string Type, Guid BuildingId, decimal? RatedPowerKw = null,
    DateTime? CommissionedAt = null, DateTime? DecommissionedAt = null,
    uint RowVersion = 0);
public sealed record MeterReadingWriteModel(Guid MeterId, DateTime Timestamp, decimal Value,
    string ReadingType, string QualityFlag, int? IntervalSeconds, uint RowVersion = 0,
    string? Reason = null);

public sealed record EntityMutationResult(Guid Id, uint RowVersion, string DataOrigin,
    DateTime CreatedAtUtc, Guid? CreatedByUserId, DateTime? UpdatedAtUtc,
    Guid? UpdatedByUserId, bool IsDeleted);
public sealed record AuditHistoryItem(DateTime ChangedAtUtc, Guid ChangedByUserId,
    string ChangeType, string? FieldName, string? OldValue, string? NewValue,
    string Source, Guid? ImportId, string? Reason);
public sealed record EntityListQuery(int Page = 1, int PageSize = 50, string? Search = null,
    bool IncludeDeleted = false);
public sealed record EnergySystemDto(Guid Id, string EnergySystemNumber, string Name,
    string Type, Guid BuildingId, decimal? RatedPowerKw, DateTime? CommissionedAt,
    DateTime? DecommissionedAt, bool IsActive, string DataOrigin, DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc, bool IsDeleted, uint RowVersion);
public sealed record MeterReadingDto(Guid Id, Guid MeterId, DateTime Timestamp, decimal Value,
    string ReadingType, string QualityFlag, int? IntervalSeconds, string DataOrigin,
    bool IsDeleted, uint RowVersion);

public interface IEntityCrudService
{
    Task<EntityMutationResult> CreateCustomerAsync(CustomerWriteModel model, CancellationToken ct);
    Task<EntityMutationResult> UpdateCustomerAsync(Guid id, CustomerWriteModel model, CancellationToken ct);
    Task<EntityMutationResult> DeleteCustomerAsync(Guid id, uint rowVersion, CancellationToken ct);
    Task<EntityMutationResult> RestoreCustomerAsync(Guid id, uint rowVersion, CancellationToken ct);
    Task<EntityMutationResult> CreateBuildingAsync(BuildingWriteModel model, CancellationToken ct);
    Task<EntityMutationResult> UpdateBuildingAsync(Guid id, BuildingWriteModel model, CancellationToken ct);
    Task<EntityMutationResult> DeleteBuildingAsync(Guid id, uint rowVersion, CancellationToken ct);
    Task<EntityMutationResult> RestoreBuildingAsync(Guid id, uint rowVersion, CancellationToken ct);
    Task<EntityMutationResult> CreateMeterAsync(MeterWriteModel model, CancellationToken ct);
    Task<EntityMutationResult> UpdateMeterAsync(Guid id, MeterWriteModel model, CancellationToken ct);
    Task<EntityMutationResult> DeleteMeterAsync(Guid id, uint rowVersion, CancellationToken ct);
    Task<EntityMutationResult> RestoreMeterAsync(Guid id, uint rowVersion, CancellationToken ct);
    Task<EntityMutationResult> CreateEnergySystemAsync(EnergySystemWriteModel model, CancellationToken ct);
    Task<EntityMutationResult> UpdateEnergySystemAsync(Guid id, EnergySystemWriteModel model, CancellationToken ct);
    Task<EntityMutationResult> DeleteEnergySystemAsync(Guid id, uint rowVersion, CancellationToken ct);
    Task<EntityMutationResult> RestoreEnergySystemAsync(Guid id, uint rowVersion, CancellationToken ct);
    Task<EntityMutationResult> CreateMeterReadingAsync(MeterReadingWriteModel model, CancellationToken ct);
    Task<EntityMutationResult> UpdateMeterReadingAsync(Guid id, MeterReadingWriteModel model, CancellationToken ct);
    Task<EntityMutationResult> DeleteMeterReadingAsync(Guid id, uint rowVersion, string? reason, CancellationToken ct);
    Task<ReadModel.PagedResult<EnergySystemDto>> GetEnergySystemsAsync(EntityListQuery query, CancellationToken ct);
    Task<EnergySystemDto?> GetEnergySystemAsync(Guid id, bool includeDeleted, CancellationToken ct);
    Task<ReadModel.PagedResult<MeterReadingDto>> GetMeterReadingsAsync(Guid? meterId, EntityListQuery query, CancellationToken ct);
    Task<MeterReadingDto?> GetMeterReadingAsync(Guid id, bool includeDeleted, CancellationToken ct);
    Task<IReadOnlyList<AuditHistoryItem>> GetAuditHistoryAsync(string entityType, Guid entityId, CancellationToken ct);
}

public sealed class CrudValidationException(
    IReadOnlyDictionary<string, string[]> errors) : Exception("Validierung fehlgeschlagen.")
{
    public IReadOnlyDictionary<string, string[]> Errors { get; } = errors;
}
public sealed class CrudNotFoundException(string message) : Exception(message);
public sealed class CrudConflictException(string message) : Exception(message);
