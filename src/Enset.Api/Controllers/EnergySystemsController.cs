using Enset.Api.Authorization;
using Enset.Application.Crud;
using Enset.Application.ReadModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Enset.Api.Controllers;

[ApiController, Route("api/v1/energy-systems"), Produces("application/json")]
[Authorize(Policy = AuthorizationPolicyNames.CustomerReader)]
public sealed class EnergySystemsController(CrudQueryHandler queries, CrudCommandHandler commands) : ControllerBase
{
    /// <summary>Listet Anlagen paginiert.</summary>
    [HttpGet]
    public Task<PagedResult<EnergySystemDto>> List([FromQuery] int page = 1, [FromQuery] int pageSize = 50,
        [FromQuery] string? search = null, CancellationToken ct = default) =>
        queries.Handle(new GetEnergySystemsQuery(page, pageSize, search), ct);
    /// <summary>Liefert eine Anlage.</summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<EnergySystemDto>> Get(Guid id, CancellationToken ct)
    { var item = await queries.Handle(new GetEnergySystemByIdQuery(id), ct); return item is null ? NotFound() : Ok(item); }
    /// <summary>Legt eine Anlage mit Gebäudezuordnung an.</summary>
    [HttpPost, Authorize(Policy = AuthorizationPolicyNames.CustomerWriter)]
    public async Task<ActionResult<EntityMutationResult>> Create(EnergySystemWriteModel request, CancellationToken ct)
    { var r = await commands.Handle(new CreateEnergySystemCommand(request), ct); return CreatedAtAction(nameof(Get), new { id = r.Id }, r); }
    /// <summary>Aktualisiert eine Anlage mit xmin-Concurrency.</summary>
    [HttpPut("{id:guid}"), Authorize(Policy = AuthorizationPolicyNames.CustomerWriter)]
    public Task<EntityMutationResult> Update(Guid id, EnergySystemWriteModel request, CancellationToken ct) =>
        commands.Handle(new UpdateEnergySystemCommand(id, request), ct);
    /// <summary>Soft Delete; abhängige Zählpunkte blockieren.</summary>
    [HttpDelete("{id:guid}"), Authorize(Policy = AuthorizationPolicyNames.CustomerAdmin)]
    public Task<EntityMutationResult> Delete(Guid id, [FromQuery] uint rowVersion, CancellationToken ct) =>
        commands.Handle(new DeleteEnergySystemCommand(id, rowVersion), ct);
    /// <summary>Stellt eine soft-gelöschte Anlage wieder her.</summary>
    [HttpPost("{id:guid}/restore"), Authorize(Policy = AuthorizationPolicyNames.CustomerAdmin)]
    public Task<EntityMutationResult> Restore(Guid id, [FromQuery] uint rowVersion, CancellationToken ct) =>
        commands.Handle(new RestoreEnergySystemCommand(id, rowVersion), ct);
}
