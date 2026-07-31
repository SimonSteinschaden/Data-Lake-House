using Enset.Api.Authorization;
using Enset.Application.InternalDataProducts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Enset.Api.Controllers;

[ApiController]
[Route("api/v1/internal-data-products")]
[Authorize(Policy = AuthorizationPolicyNames.CustomerReader)]
public sealed class InternalDataProductsController(
    IBuildingSummaryProductService buildings,
    IMeterSummaryProductService meters,
    ICustomerSummaryProductService customers,
    IPortfolioSummaryProductService portfolio,
    IImportQualityProductService imports) : ControllerBase
{
    [HttpGet("buildings/{buildingId:guid}/summary")]
    public async Task<ActionResult<BuildingSummaryProduct>> Building(
        Guid buildingId, CancellationToken ct)
    {
        var result = await buildings.GetAsync(buildingId, ct);
        return result is null
            ? Problem(statusCode: StatusCodes.Status404NotFound,
                title: "Building not found")
            : Ok(result);
    }

    [HttpGet("meters/{meteringPointId:guid}/summary")]
    public async Task<ActionResult<MeterSummaryProduct>> Meter(
        Guid meteringPointId, CancellationToken ct)
    {
        var result = await meters.GetAsync(meteringPointId, ct);
        return result is null
            ? Problem(statusCode: StatusCodes.Status404NotFound,
                title: "Metering point not found")
            : Ok(result);
    }

    [HttpGet("customers/{customerId:guid}/summary")]
    public async Task<ActionResult<CustomerSummaryProduct>> Customer(
        Guid customerId, CancellationToken ct)
    {
        var result = await customers.GetAsync(customerId, ct);
        return result is null
            ? Problem(statusCode: StatusCodes.Status404NotFound,
                title: "Customer not found")
            : Ok(result);
    }

    [HttpGet("portfolio/summary")]
    public Task<PortfolioSummaryProduct> Portfolio(CancellationToken ct) =>
        portfolio.GetAsync(ct);

    [HttpGet("import-quality")]
    public Task<ImportQualityProduct> ImportQuality(CancellationToken ct) =>
        imports.GetAsync(ct);
}
