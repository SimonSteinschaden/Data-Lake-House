using Enset.Api.Authorization;
using Enset.Application.Curation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Enset.Api.Controllers;

[ApiController, Route("api/v1/curation"), Produces("application/json")]
[Authorize(Policy = AuthorizationPolicyNames.EnsetEmployee)]
public sealed class CurationController(ICurationService service) : ControllerBase
{
    [HttpGet("tasks")]
    public Task<IReadOnlyList<CurationTaskSummary>> GetTasks(CancellationToken ct) =>
        service.GetTasksAsync(ct);

    [HttpGet("tasks/{id:guid}")]
    public async Task<ActionResult<CurationTaskDetail>> GetTask(Guid id, CancellationToken ct)
    {
        var task = await service.GetTaskAsync(id, ct);
        return task is null ? NotFound() : Ok(task);
    }

    [HttpPost("tasks/{id:guid}/accept")]
    public Task<CurationTaskDetail> Accept(Guid id, CancellationToken ct) =>
        service.AcceptAsync(id, ct);

    [HttpPost("tasks/{id:guid}/reject")]
    public Task<CurationTaskDetail> Reject(Guid id,
        RejectCurationRequest? request, CancellationToken ct) =>
        service.RejectAsync(id, request?.Reason, ct);

    [HttpPost("tasks/{id:guid}/customize")]
    public Task<CurationTaskDetail> Customize(Guid id,
        CustomizeCurationRequest request, CancellationToken ct) =>
        service.CustomizeAsync(id, request.Value, request.Reason, ct);

    [HttpGet("statistics")]
    public Task<CurationStatistics> GetStatistics(CancellationToken ct) =>
        service.GetStatisticsAsync(ct);
}
