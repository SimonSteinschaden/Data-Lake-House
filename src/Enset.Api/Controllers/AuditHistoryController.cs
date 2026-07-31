using Enset.Api.Authorization;
using Enset.Application.Crud;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Enset.Api.Controllers;

[ApiController, Route("api/v1/audit-history"), Produces("application/json")]
[Authorize(Policy = AuthorizationPolicyNames.CustomerReader)]
public sealed class AuditHistoryController(CrudQueryHandler queries) : ControllerBase
{
    /// <summary>Liefert den serverseitigen Änderungsverlauf chronologisch.</summary>
    /// <remarks>Die Parameter page und pageSize bereiten eine stabile Pagination vor.</remarks>
    [HttpGet("{entityType}/{entityId:guid}")]
    public async Task<ActionResult<IReadOnlyList<AuditHistoryItem>>> Get(string entityType, Guid entityId,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 100, CancellationToken ct = default)
    {
        return Ok(await queries.Handle(new GetAuditHistoryQuery(entityType, entityId, page, pageSize), ct));
    }
}
