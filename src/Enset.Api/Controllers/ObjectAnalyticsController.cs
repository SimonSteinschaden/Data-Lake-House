using Enset.Application.ObjectAnalytics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Enset.Api.Authorization;

namespace Enset.Api.Controllers;

[ApiController]
[Authorize(Policy = AuthorizationPolicyNames.EnsetEmployee)]
[Route("api/v1/object-analytics")]
public sealed class ObjectAnalyticsController(
    IObjectAnalyticsService analytics)
    : ControllerBase
{
    [HttpGet("objects")]
    public async Task<ActionResult<ObjectSearchResult>> Search(
        [FromQuery] ObjectAnalyticsRequest request,
        CancellationToken cancellationToken) =>
        Ok(await analytics.Search(request.ToQuery(), cancellationToken));

    [HttpGet("{buildingId:guid}")]
    public async Task<ActionResult<ObjectAnalyticsProduct>> Analyze(
        Guid buildingId,
        [FromQuery] ObjectAnalyticsRequest request,
        CancellationToken cancellationToken)
    {
        var result = await analytics.Analyze(
            buildingId, request.ToQuery(), cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }
}

public sealed class ObjectAnalyticsRequest
{
    public string? Search { get; init; }
    public string? EnergyCarrier { get; init; }
    public string? BuildingType { get; init; }
    public string? QualityLevel { get; init; }
    public Guid? CustomerId { get; init; }
    public DateTime? FromUtc { get; init; }
    public DateTime? ToUtc { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 25;

    public ObjectSearchQuery ToQuery()
    {
        var year = DateTime.UtcNow.Year;
        return new(
            Search,
            EnergyCarrier,
            BuildingType,
            QualityLevel,
            CustomerId,
            (FromUtc ?? new DateTime(
                year, 1, 1, 0, 0, 0,
                DateTimeKind.Utc)).ToUniversalTime(),
            (ToUtc ?? new DateTime(
                year + 1, 1, 1, 0, 0, 0,
                DateTimeKind.Utc)).ToUniversalTime(),
            Page,
            PageSize);
    }
}
