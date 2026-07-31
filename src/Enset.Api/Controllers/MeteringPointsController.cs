using Enset.Api.Authorization;
using Enset.Application.Crud;
using Enset.Application.ReadModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Enset.Api.Controllers;

[ApiController, Route("api/v1/metering-points"), Produces("application/json")]
[Authorize(Policy = AuthorizationPolicyNames.CustomerReader)]
public sealed class MeteringPointsController(CrudQueryHandler queries, CrudCommandHandler commands) : ControllerBase
{
    /// <summary>Listet Zählpunkte paginiert.</summary>
    [HttpGet, ProducesResponseType(typeof(PagedResult<MeterSummaryDto>), StatusCodes.Status200OK)]
    public Task<PagedResult<MeterSummaryDto>> List([FromQuery] int page = 1, [FromQuery] int pageSize = 50,
        [FromQuery] string? search = null, [FromQuery] bool includeDeleted = false,
        CancellationToken ct = default) =>
        queries.Handle(new GetMeteringPointsQuery(page, pageSize, search, includeDeleted), ct);
    /// <summary>Liefert einen Zählpunkt.</summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<MeterDetailDto>> Get(Guid id, [FromQuery] bool includeDeleted, CancellationToken ct)
    { var item = await queries.Handle(new GetMeteringPointByIdQuery(id, includeDeleted), ct); return item is null ? NotFound() : Ok(item); }
    /// <summary>Legt einen Zählpunkt an.</summary>
    [HttpPost, Authorize(Policy = AuthorizationPolicyNames.CustomerWriter)]
    public async Task<ActionResult<EntityMutationResult>> Create(MeterWriteModel request, CancellationToken ct)
    { var r = await commands.Handle(new CreateMeteringPointCommand(request), ct); return CreatedAtAction(nameof(Get), new { id = r.Id }, r); }
    /// <summary>Aktualisiert einen Zählpunkt mit xmin-Concurrency.</summary>
    [HttpPut("{id:guid}"), Authorize(Policy = AuthorizationPolicyNames.CustomerWriter)]
    public Task<EntityMutationResult> Update(Guid id, MeterWriteModel request, CancellationToken ct) =>
        commands.Handle(new UpdateMeteringPointCommand(id, request), ct);
    /// <summary>Soft Delete; Messwerte blockieren die Löschung.</summary>
    [HttpDelete("{id:guid}"), Authorize(Policy = AuthorizationPolicyNames.CustomerAdmin)]
    public Task<EntityMutationResult> Delete(Guid id, [FromQuery] uint rowVersion, CancellationToken ct) =>
        commands.Handle(new DeleteMeteringPointCommand(id, rowVersion), ct);
    /// <summary>Stellt einen soft-gelöschten Zählpunkt wieder her.</summary>
    [HttpPost("{id:guid}/restore"), Authorize(Policy = AuthorizationPolicyNames.CustomerAdmin)]
    public Task<EntityMutationResult> Restore(Guid id, [FromQuery] uint rowVersion, CancellationToken ct) =>
        commands.Handle(new RestoreMeteringPointCommand(id, rowVersion), ct);
}
