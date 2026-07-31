using Enset.Api.Authorization;
using Enset.Application.Analytics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Enset.Api.Controllers;

[ApiController]
[Route("api/v1/analytics")]
[Produces("application/json")]
[Authorize(Policy = AuthorizationPolicyNames.EnsetEmployee)]
public sealed class AnalyticsController : ControllerBase
{
    private readonly IAnalyticsDataProductService _analytics;

    public AnalyticsController(IAnalyticsDataProductService analytics) =>
        _analytics = analytics;

    [HttpGet("portfolio-summary")]
    public async Task<ActionResult<PortfolioSummaryDataProduct>> PortfolioSummary(
        CancellationToken cancellationToken) =>
        Ok(await _analytics.GetPortfolioSummaryAsync(cancellationToken));

    [HttpGet("regional-building-distribution")]
    public async Task<ActionResult<RegionalBuildingDistributionDataProduct>>
        RegionalBuildingDistribution(CancellationToken cancellationToken) =>
        Ok(await _analytics.GetRegionalBuildingDistributionAsync(cancellationToken));

    [HttpGet("portfolio-load-profile")]
    public async Task<ActionResult<ElectricityPortfolioLoadProfileDataProduct>>
        PortfolioLoadProfile([FromQuery] AnalyticsRequest request,
            CancellationToken cancellationToken) =>
        Ok(await _analytics.GetPortfolioLoadProfileAsync(
            request.ToQuery(), cancellationToken));

    [HttpGet("monthly-electricity-consumption")]
    public async Task<ActionResult<MonthlyElectricityConsumptionDataProduct>>
        MonthlyElectricityConsumption([FromQuery] AnalyticsRequest request,
            CancellationToken cancellationToken) =>
        Ok(await _analytics.GetMonthlyElectricityConsumptionAsync(
            request.ToQuery(), cancellationToken));

    [HttpGet("metering-coverage")]
    public async Task<ActionResult<MeteringCoverageSummaryDataProduct>> MeteringCoverage(
        CancellationToken cancellationToken) =>
        Ok(await _analytics.GetMeteringCoverageAsync(cancellationToken));

    [HttpGet("data-quality")]
    public async Task<ActionResult<DataQualitySummaryDataProduct>> DataQuality(
        CancellationToken cancellationToken) =>
        Ok(await _analytics.GetDataQualityAsync(cancellationToken));

    [HttpGet("energy-portfolio")]
    public async Task<ActionResult<EnergyPortfolioStructureDataProduct>> EnergyPortfolio(
        CancellationToken cancellationToken) =>
        Ok(await _analytics.GetEnergyPortfolioAsync(cancellationToken));

    [HttpGet("warnings")]
    public async Task<ActionResult> Warnings(
        [FromQuery] int page = 0,
        [FromQuery] int pageSize = 50,
        [FromQuery] string? severity = null,
        CancellationToken cancellationToken = default)
    {
        if (page > 0)
        {
            return Ok(await _analytics.GetWarningsDetailsAsync(page, pageSize, severity, cancellationToken));
        }

        return Ok(await _analytics.GetWarningsAsync(cancellationToken));
    }

    [HttpGet("consumption-by-location")]
    public async Task<ActionResult<EnergyConsumptionByLocationDataProduct>>
        ConsumptionByLocation([FromQuery] AnalyticsRequest request,
            [FromQuery] int limit = 10, CancellationToken cancellationToken = default) =>
        Ok(await _analytics.GetConsumptionByLocationAsync(
            request.ToQuery(), limit, cancellationToken));

    [HttpGet("consumption-by-usage-type")]
    public async Task<ActionResult<EnergyConsumptionByUsageTypeDataProduct>>
        ConsumptionByUsageType([FromQuery] AnalyticsRequest request,
            CancellationToken cancellationToken) =>
        Ok(await _analytics.GetConsumptionByUsageTypeAsync(
            request.ToQuery(), cancellationToken));

    [HttpGet("top-energy-systems")]
    public async Task<ActionResult<TopEnergySystemsByConsumptionDataProduct>>
        TopEnergySystems([FromQuery] AnalyticsRequest request,
            [FromQuery] int limit = 10, CancellationToken cancellationToken = default) =>
        Ok(await _analytics.GetTopEnergySystemsAsync(
            request.ToQuery(), limit, cancellationToken));

    [HttpGet("consumption-by-carrier")]
    public async Task<ActionResult<EnergyConsumptionByCarrierDataProduct>>
        ConsumptionByCarrier([FromQuery] AnalyticsRequest request,
            CancellationToken cancellationToken) =>
        Ok(await _analytics.GetConsumptionByCarrierAsync(
            request.ToQuery(), cancellationToken));
}

public sealed class AnalyticsRequest
{
    public int Year { get; init; } = DateTime.UtcNow.Year;
    public Guid? CustomerId { get; init; }
    public Guid? BuildingId { get; init; }
    public Guid? MeterId { get; init; }
    public string? Region { get; init; }
    public string? PostalCode { get; init; }
    public string Aggregation { get; init; } = "Hour";

    public AnalyticsQuery ToQuery() => new(
        Year, CustomerId, BuildingId, MeterId, Region, PostalCode, Aggregation);
}
