using Enset.Api.Authorization;
using Enset.Application.ReadModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Enset.Application.Crud;

namespace Enset.Api.Controllers;

[ApiController]
[Route("api/v1/buildings")]
[Produces("application/json")]
[Authorize(Policy = AuthorizationPolicyNames.CustomerReader)]
public sealed class BuildingsController : ControllerBase
{
    private readonly CrudQueryHandler _queries; private readonly CrudCommandHandler _commands;
    public BuildingsController(CrudQueryHandler queries, CrudCommandHandler commands)
        => (_queries, _commands) = (queries, commands);

    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<BuildingSummaryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<BuildingSummaryDto>>> List(
        [FromQuery] BuildingListQuery query, CancellationToken cancellationToken) =>
        Ok(await _queries.Handle(new GetBuildingsQuery(query.Page, query.PageSize, query.Search), cancellationToken));

    [HttpGet("{buildingId:guid}")]
    [ProducesResponseType(typeof(BuildingDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BuildingDetailDto>> Get(Guid buildingId,
        CancellationToken cancellationToken)
    {
        var building = await _queries.Handle(new GetBuildingByIdQuery(buildingId), cancellationToken);
        return building is null
            ? Problem(title: "Building not found", statusCode: StatusCodes.Status404NotFound)
            : Ok(building);
    }

    /// <summary>Legt ein Gebäude einschließlich Kundenzuordnung an.</summary>
    [HttpPost, Authorize(Policy = AuthorizationPolicyNames.CustomerWriter)]
    public async Task<ActionResult<EntityMutationResult>> Create(BuildingWriteModel request, CancellationToken ct)
    { var r = await _commands.Handle(new CreateBuildingCommand(request), ct);
      return CreatedAtAction(nameof(Get), new { buildingId = r.Id }, r); }
    /// <summary>Aktualisiert ein Gebäude mit xmin-Concurrency.</summary>
    [HttpPut("{buildingId:guid}"), Authorize(Policy = AuthorizationPolicyNames.CustomerWriter)]
    public Task<EntityMutationResult> Update(Guid buildingId, BuildingWriteModel request, CancellationToken ct) =>
        _commands.Handle(new UpdateBuildingCommand(buildingId, request), ct);
    /// <summary>Soft Delete; vorhandene Zählpunkte oder Anlagen blockieren.</summary>
    [HttpDelete("{buildingId:guid}"), Authorize(Policy = AuthorizationPolicyNames.CustomerAdmin)]
    public Task<EntityMutationResult> Delete(Guid buildingId, [FromQuery] uint rowVersion, CancellationToken ct) =>
        _commands.Handle(new DeleteBuildingCommand(buildingId, rowVersion), ct);
    /// <summary>Stellt ein soft-gelöschtes Gebäude wieder her.</summary>
    [HttpPost("{buildingId:guid}/restore"), Authorize(Policy = AuthorizationPolicyNames.CustomerAdmin)]
    public Task<EntityMutationResult> Restore(Guid buildingId, [FromQuery] uint rowVersion, CancellationToken ct) =>
        _commands.Handle(new RestoreBuildingCommand(buildingId, rowVersion), ct);
}
