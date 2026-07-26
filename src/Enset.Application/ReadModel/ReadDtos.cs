namespace Enset.Application.ReadModel;

public sealed record CustomerSummaryDto(Guid Id, string CustomerNumber, string Name,
    string Type, bool IsActive, int BuildingCount);

public sealed record CustomerBuildingDto(Guid Id, string BuildingNumber, string Name,
    string Role, bool IsPrimary);

public sealed record CustomerDetailDto(Guid Id, string CustomerNumber, string Name,
    string? LegalName, string Type, string? Email, string? Phone, string? Website,
    string? Street, string? HouseNumber, string? PostalCode, string? City,
    string CountryCode, bool IsActive, IReadOnlyList<CustomerBuildingDto> Buildings);

public sealed record BuildingSummaryDto(Guid Id, string BuildingNumber, string Name,
    string? ExternalIdentifier, bool IsActive, int MeterCount,
    DateTime? FirstReadingAt, DateTime? LastReadingAt);

public sealed record BuildingCustomerDto(Guid CustomerId, string CustomerNumber,
    string CustomerName, string Role, bool IsPrimary);

public sealed record BuildingMeterDto(Guid Id, string MeterNumber, string Name,
    string Unit, string Quantity, bool IsActive);

public sealed record BuildingDetailDto(Guid Id, string BuildingNumber, string Name,
    string? ExternalIdentifier, bool IsActive, int MeterCount,
    DateTime? FirstReadingAt, DateTime? LastReadingAt,
    IReadOnlyList<BuildingCustomerDto> Customers,
    IReadOnlyList<BuildingMeterDto> Meters);

public sealed record MeterSummaryDto(Guid Id, string MeterNumber, string Name,
    string Unit, string Quantity, string Direction, string Type, bool IsActive,
    Guid? BuildingId, string? BuildingName, long ReadingCount,
    DateTime? FirstReadingAt, DateTime? LastReadingAt);

public sealed record MeterDetailDto(Guid Id, string MeterNumber, string Name,
    string? Description, string? ExternalIdentifier, string Medium, string Quantity,
    string Unit, string Direction, string Type, string? Manufacturer, string? Model,
    string? SerialNumber, Guid? BuildingId, string? BuildingName, bool IsActive,
    long ReadingCount, DateTime? FirstReadingAt, DateTime? LastReadingAt,
    DateTime? LatestReadingAt, decimal? LatestValue, string? LatestQuality);

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
