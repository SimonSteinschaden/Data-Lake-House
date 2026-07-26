using Enset.Api.Authorization;
using Enset.Application.ReadModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Enset.Api.Controllers;

[ApiController]
[Route("api/v1/buildings")]
[Produces("application/json")]
[Authorize(Policy = AuthorizationPolicyNames.CustomerReader)]
public sealed class BuildingsController : ControllerBase
{
    private readonly IEntityReadService _reads;
    public BuildingsController(IEntityReadService reads) => _reads = reads;

    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<BuildingSummaryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<BuildingSummaryDto>>> List(
        [FromQuery] BuildingListQuery query, CancellationToken cancellationToken) =>
        Ok(await _reads.GetBuildingsAsync(query, cancellationToken));

    [HttpGet("{buildingId:guid}")]
    [ProducesResponseType(typeof(BuildingDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BuildingDetailDto>> Get(Guid buildingId,
        CancellationToken cancellationToken)
    {
        var building = await _reads.GetBuildingAsync(buildingId, cancellationToken);
        return building is null
            ? Problem(title: "Building not found", statusCode: StatusCodes.Status404NotFound)
            : Ok(building);
    }
}
