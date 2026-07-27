namespace Enset.Application.ReadModel;

public sealed record CustomerSummaryDto(Guid Id, string CustomerNumber, string Name,
    string? PostalCode, string? City, string? Phone, string? Email,
    bool IsActive, bool IsDeleted, int BuildingCount);

public sealed record CustomerBuildingDto(Guid Id, string BuildingNumber, string Name,
    string Role, bool IsPrimary, string? UsageType, int MeterCount, string DataMaturity);

public sealed record CustomerDetailDto(Guid Id, string CustomerNumber, string Name,
    string? LegalName, string Type, string? Email, string? Phone, string? ContactPerson, string? Website,
    string? Street, string? HouseNumber, string? PostalCode, string? City,
    string CountryCode, bool IsActive, IReadOnlyList<CustomerBuildingDto> Buildings,
    string DataOrigin, DateTime CreatedAtUtc, Guid? CreatedByUserId,
    DateTime? UpdatedAtUtc, Guid? UpdatedByUserId, bool IsDeleted, uint RowVersion,
    int MeterCount, int EnergySystemCount);

public sealed record BuildingSummaryDto(Guid Id, string BuildingNumber, string Name,
    string? BuildingCategory, string? PrimaryUseType, string? CustomerNumber,
    string? CustomerName, int MeterCount, string BenchmarkState,
    string DataMaturity, int GoldReadinessPercent, bool IsDeleted);

public sealed record BuildingCustomerDto(Guid CustomerId, string CustomerNumber,
    string CustomerName, string Role, bool IsPrimary);

public sealed record BuildingMeterDto(Guid Id, string MeterNumber, string Name,
    string Medium, string Direction, string Unit, string DataMaturity, bool IsActive);

public sealed record BuildingDetailDto(Guid Id, string BuildingNumber, string Name,
    string? ExternalIdentifier, bool IsActive, int MeterCount,
    DateTime? FirstReadingAt, DateTime? LastReadingAt,
    IReadOnlyList<BuildingCustomerDto> Customers,
    IReadOnlyList<BuildingMeterDto> Meters, string DataOrigin, DateTime CreatedAtUtc,
    Guid? CreatedByUserId, DateTime? UpdatedAtUtc, Guid? UpdatedByUserId,
    bool IsDeleted, uint RowVersion, decimal? GrossFloorAreaM2,
    int? YearOfConstruction, decimal? Latitude, decimal? Longitude,
    string? BuildingCategory, string? PrimaryUseType, decimal? HeatedFloorAreaM2,
    int? YearOfLastMajorRenovation, string BenchmarkState, string? PostalCode,
    string? City, string? Street, string? HouseNumber);

public sealed record MeterSummaryDto(Guid Id, string MeterNumber, string Name,
    string Medium, string Unit, string Direction, Guid? BuildingId, string? BuildingNumber,
    string? BuildingName, string? CustomerNumber, string? CustomerName,
    decimal? AnnualValue, string? AnnualValueOrigin, long ReadingCount,
    DateTime? FirstReadingAt, DateTime? LastReadingAt, string DataMaturity,
    int GoldReadinessPercent, bool IsDeleted);

public sealed record MeterDetailDto(Guid Id, string MeterNumber, string Name,
    string? Description, string? ExternalIdentifier, string Medium, string Quantity,
    string Unit, string Direction, string Type, string? Manufacturer, string? Model,
    string? SerialNumber, Guid? BuildingId, string? BuildingName, bool IsActive,
    long ReadingCount, DateTime? FirstReadingAt, DateTime? LastReadingAt,
    DateTime? LatestReadingAt, decimal? LatestValue, string? LatestQuality,
    string DataOrigin, DateTime CreatedAtUtc, Guid? CreatedByUserId,
    DateTime? UpdatedAtUtc, Guid? UpdatedByUserId, bool IsDeleted, uint RowVersion,
    string? BuildingNumber, string? CustomerNumber, string? CustomerName,
    decimal? AnnualValue, string? AnnualValueOrigin, int? IntervalSeconds);

public sealed record RawMeterReadingDto(DateTime Timestamp, decimal Value,
    string Quality, int? IntervalSeconds);

public sealed record AggregatedMeterReadingDto(DateTime BucketStart, string ReadingType, decimal Minimum,
    decimal Maximum, decimal Average, decimal? Sum, decimal FirstValue,
    decimal LastValue, decimal Delta, int Count);

public sealed record MeterReadingsDto(Guid MeterId, string MeterNumber, string Unit,
    string Quantity, string ReadingType, string Aggregation,
    DateTime? AvailableFrom, DateTime? AvailableTo, DateTime? RequestedFrom,
    DateTime? RequestedTo, PagedResult<RawMeterReadingDto>? Raw,
    IReadOnlyList<AggregatedMeterReadingDto>? Aggregated);
