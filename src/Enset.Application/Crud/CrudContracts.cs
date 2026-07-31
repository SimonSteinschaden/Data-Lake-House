namespace Enset.Application.Crud;

public sealed record CustomerWriteModel(string CustomerNumber, string Name, string Type,
    string? LegalName, string? Email, string? Phone, string CountryCode, uint RowVersion = 0,
    string? ContactPerson = null, string? Street = null, string? HouseNumber = null,
    string? PostalCode = null, string? City = null);
public interface IBuildingMutationModel
{
    string Name { get; }
    string? ExternalIdentifier { get; }
    Guid? CustomerId { get; }
    decimal? GrossFloorAreaM2 { get; }
    int? YearOfConstruction { get; }
    string? BuildingCategory { get; }
    string? PrimaryUseType { get; }
    decimal? HeatedFloorAreaM2 { get; }
    int? YearOfLastMajorRenovation { get; }
    string? BuildingState { get; }
    string? PostalCode { get; }
    string? City { get; }
    string? Street { get; }
    string? HouseNumber { get; }
}

public sealed record BuildingCreateRequest(string Name,
    string? ExternalIdentifier, Guid? CustomerId, decimal? GrossFloorAreaM2 = null,
    int? YearOfConstruction = null,
    string? BuildingCategory = null, string? PrimaryUseType = null,
    decimal? HeatedFloorAreaM2 = null, int? YearOfLastMajorRenovation = null,
    string? BuildingState = null, string? PostalCode = null, string? City = null,
    string? Street = null, string? HouseNumber = null) : IBuildingMutationModel;
public sealed record BuildingUpdateRequest(string Name,
    string? ExternalIdentifier, Guid? CustomerId, uint RowVersion,
    decimal? GrossFloorAreaM2 = null, int? YearOfConstruction = null,
    string? BuildingCategory = null, string? PrimaryUseType = null,
    decimal? HeatedFloorAreaM2 = null, int? YearOfLastMajorRenovation = null,
    string? BuildingState = null, string? PostalCode = null, string? City = null,
    string? Street = null, string? HouseNumber = null) : IBuildingMutationModel;
public sealed record MeterWriteModel(string MeterNumber, string Name, Guid BuildingId,
    string Medium, string Quantity, string Unit, string Direction, string Type,
    Guid? EnergySystemId, uint RowVersion = 0, string? Description = null,
    string? ExternalIdentifier = null, decimal? AnnualValue = null);
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
    Task<EntityMutationResult> CreateBuildingAsync(BuildingCreateRequest model, CancellationToken ct);
    Task<EntityMutationResult> UpdateBuildingAsync(Guid id, BuildingUpdateRequest model, CancellationToken ct);
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

public interface IBuildingNumberGenerator
{
    Task<string> NextAsync(CancellationToken cancellationToken);
}

public sealed class CrudValidationException(
    IReadOnlyDictionary<string, string[]> errors) : Exception("Validierung fehlgeschlagen.")
{
    public IReadOnlyDictionary<string, string[]> Errors { get; } = errors;
}
public sealed class CrudNotFoundException(string message) : Exception(message);
public sealed class CrudConflictException(string message) : Exception(message);
