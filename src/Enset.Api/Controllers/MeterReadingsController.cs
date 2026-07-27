using Enset.Api.Authorization;
using Enset.Application.Crud;
using Enset.Application.ReadModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Enset.Api.Controllers;

[ApiController, Route("api/v1/meter-readings"), Produces("application/json")]
[Authorize(Policy = AuthorizationPolicyNames.CustomerReader)]
public sealed class MeterReadingsController(CrudQueryHandler queries, CrudCommandHandler commands) : ControllerBase
{
    /// <summary>Listet einzelne Messwerte paginiert.</summary>
    [HttpGet]
    public Task<PagedResult<MeterReadingDto>> List([FromQuery] int page = 1, [FromQuery] int pageSize = 50,
        [FromQuery] Guid? meteringPointId = null, CancellationToken ct = default) =>
        queries.Handle(new GetMeterReadingsQuery(page, pageSize, meteringPointId), ct);
    /// <summary>Liefert einen einzelnen Messwert.</summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<MeterReadingDto>> Get(Guid id, CancellationToken ct)
    { var item = await queries.Handle(new GetMeterReadingByIdQuery(id), ct); return item is null ? NotFound() : Ok(item); }
    /// <summary>Legt einen einzelnen Messwert an.</summary>
    [HttpPost, Authorize(Policy = AuthorizationPolicyNames.CustomerWriter)]
    public async Task<ActionResult<EntityMutationResult>> Create(MeterReadingWriteModel request, CancellationToken ct)
    { var r = await commands.Handle(new CreateMeterReadingCommand(request), ct); return CreatedAtAction(nameof(Get), new { id = r.Id }, r); }
    /// <summary>Korrigiert einen Messwert ohne seinen Messwerttyp zu verändern.</summary>
    [HttpPut("{id:guid}"), Authorize(Policy = AuthorizationPolicyNames.CustomerWriter)]
    public Task<EntityMutationResult> Update(Guid id, MeterReadingWriteModel request, CancellationToken ct) =>
        commands.Handle(new UpdateMeterReadingCommand(id, request), ct);
    /// <summary>Invalidiert einen Messwert per Soft Delete.</summary>
    [HttpDelete("{id:guid}"), Authorize(Policy = AuthorizationPolicyNames.CustomerWriter)]
    public Task<EntityMutationResult> Delete(Guid id, [FromQuery] uint rowVersion,
        [FromQuery] string? reason, CancellationToken ct) =>
        commands.Handle(new DeleteMeterReadingCommand(id, rowVersion, reason), ct);
}
