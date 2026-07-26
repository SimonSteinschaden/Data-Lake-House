namespace Enset.Application.ReadModel;

public sealed record CustomerListQuery(
    string? Search = null,
    bool? IsActive = null,
    int Page = 1,
    int PageSize = 50,
    string SortBy = "name",
    string SortDirection = "asc");

public sealed record BuildingListQuery(
    string? Search = null,
    Guid? CustomerId = null,
    bool? IsActive = null,
    int Page = 1,
    int PageSize = 50,
    string SortBy = "name",
    string SortDirection = "asc");

public sealed record MeterListQuery(
    string? Search = null,
    Guid? CustomerId = null,
    Guid? BuildingId = null,
    bool? IsActive = null,
    int Page = 1,
    int PageSize = 50,
    string SortBy = "meterNumber",
    string SortDirection = "asc");

public enum MeterReadingAggregation
{
    Raw,
    FifteenMinutes,
    Hour,
    Day,
    Month
}

public sealed record MeterReadingQuery(
    DateTime? From = null,
    DateTime? To = null,
    MeterReadingAggregation Aggregation = MeterReadingAggregation.Raw,
    int Page = 1,
    int PageSize = 100,
    string SortDirection = "asc");
