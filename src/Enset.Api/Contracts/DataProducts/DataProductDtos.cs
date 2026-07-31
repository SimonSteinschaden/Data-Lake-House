namespace Enset.Api.Contracts.DataProducts;

public sealed record DataProductSummaryDto(Guid Id, string Code, string Name,
    string Category, string Status, string Scope, Guid? ScopeId,
    int? LatestVersion);

public sealed record GenerationAvailabilityDto(bool IsAvailable,
    bool IsAuthorized, bool HasRequiredInputData,
    IReadOnlyCollection<string> MissingInputs,
    IReadOnlyCollection<string> Warnings);

public sealed record GenerateDataProductRequest(Guid CustomerId,
    DateTime PeriodFrom, DateTime PeriodTo,
    IReadOnlyDictionary<string, string>? Parameters = null);

public sealed record GenerateDataProductResponse(Guid GenerationRunId,
    string Status, Guid DataProductId, int Version);

public sealed record DataProductValueDto(string Key, decimal? NumericValue,
    string? TextValue, bool? BooleanValue, DateTime? DateTimeValue,
    string? Unit, string Quality);

public sealed record LatestDataProductVersionDto(Guid DataProductId,
    int Version, string Status, DateTime GeneratedAt, DateTime? PeriodFrom,
    DateTime? PeriodTo, string Quality, string? GenerationStatus,
    IReadOnlyCollection<string> Warnings,
    IReadOnlyCollection<DataProductValueDto> Values);

public sealed record VersionHistoryDto(int Version, string Status,
    DateTime GeneratedAt, string Quality);
