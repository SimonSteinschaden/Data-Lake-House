using Enset.Api.Authorization;
using Enset.Application.DataProducts.Catalog;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Enset.Api.Controllers;

[ApiController, Route("api/v1/data-product-catalog")]
[Authorize(Policy = AuthorizationPolicyNames.CustomerReader)]
public sealed class DataProductCatalogController(IDataProductCatalogService service) : ControllerBase
{
    [HttpGet]
    public IReadOnlyList<DataProductCatalogItem> List([FromQuery] string? search,
        [FromQuery] string? category) => service.List(search, category);

    [HttpGet("dependencies")]
    public IReadOnlyList<DataProductDependency> Dependencies() => service.Dependencies();

    [HttpGet("{code}")]
    public ActionResult<DataProductCatalogItem> Get(string code) =>
        service.Get(code) is { } item ? Ok(item) : NotFound();

    [HttpGet("{code}/schema")]
    public ActionResult<object> Schema(string code) =>
        service.Get(code) is { } item
            ? Ok(new { item.Metadata.Code, item.Metadata.Version, item.Metadata.OutputSchema })
            : NotFound();

    [HttpGet("{code}/preview")]
    public async Task<ActionResult<DataProductPreview>> Preview(string code,
        [FromQuery] Guid? customerId, [FromQuery] Guid? buildingId,
        [FromQuery] DateTime? fromUtc, [FromQuery] DateTime? toUtc,
        [FromQuery] int limit = 50, CancellationToken cancellationToken = default) =>
        await service.Preview(code, customerId, buildingId, fromUtc, toUtc, limit,
            cancellationToken) is { } result ? Ok(result) : NotFound();

    [HttpGet("{code}/export")]
    public async Task<IActionResult> Export(string code, [FromQuery] string format = "json",
        [FromQuery] Guid? customerId = null, [FromQuery] Guid? buildingId = null,
        [FromQuery] DateTime? fromUtc = null, [FromQuery] DateTime? toUtc = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await service.Export(code, format, customerId, buildingId,
                fromUtc, toUtc, cancellationToken);
            return result is null ? NotFound() : File(result.Content, result.ContentType, result.FileName);
        }
        catch (ArgumentException exception)
        {
            return Problem(statusCode: 400, title: "Unsupported export format",
                detail: exception.Message);
        }
    }
}
