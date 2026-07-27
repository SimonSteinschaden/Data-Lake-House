using Enset.Api.Authorization;
using Enset.Application.ReadModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Enset.Api.Controllers;

[ApiController]
[Route("api/v1/meters")]
[Produces("application/json")]
[Authorize(Policy = AuthorizationPolicyNames.CustomerReader)]
public sealed class MetersController : ControllerBase
{
    private readonly IEntityReadService _reads;
    public MetersController(IEntityReadService reads) => _reads = reads;

    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<MeterSummaryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<MeterSummaryDto>>> List(
        [FromQuery] MeterListQuery query, CancellationToken cancellationToken) =>
        Ok(await _reads.GetMetersAsync(query, cancellationToken));

    [HttpGet("{meterId:guid}")]
    [ProducesResponseType(typeof(MeterDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MeterDetailDto>> Get(Guid meterId,
        [FromQuery] bool includeDeleted,
        CancellationToken cancellationToken)
    {
        var meter = await _reads.GetMeterAsync(meterId, includeDeleted, cancellationToken);
        return meter is null
            ? Problem(title: "Meter not found", statusCode: StatusCodes.Status404NotFound)
            : Ok(meter);
    }

    [HttpGet("{meterId:guid}/readings")]
    [ProducesResponseType(typeof(MeterReadingsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MeterReadingsDto>> Readings(Guid meterId,
        [FromQuery] MeterReadingQuery query, CancellationToken cancellationToken)
    {
        try
        {
            var readings = await _reads.GetMeterReadingsAsync(
                meterId, query, cancellationToken);
            return readings is null
                ? Problem(title: "Meter not found", statusCode: StatusCodes.Status404NotFound)
                : Ok(readings);
        }
        catch (ArgumentException exception)
        {
            return Problem(title: "Invalid meter reading query",
                detail: exception.Message, statusCode: StatusCodes.Status400BadRequest);
        }
    }
}
