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
    public Task<CurationTaskPage> GetTasks([FromQuery] CurationTaskQuery query, CancellationToken ct) =>
        service.GetTasksAsync(query, ct);

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

    [HttpGet("buildings/{buildingId:guid}/profile")]
    public async Task<ActionResult<BuildingGoldProfile>> GetBuildingProfile(Guid buildingId, CancellationToken ct)
    { var result = await service.GetBuildingProfileAsync(buildingId, ct); return result is null ? NotFound() : Ok(result); }

    [HttpGet("metering-points/{meteringPointId:guid}/profile")]
    public async Task<ActionResult<MeteringPointGoldProfile>> GetMeteringPointProfile(Guid meteringPointId, CancellationToken ct)
    { var result = await service.GetMeteringPointProfileAsync(meteringPointId, ct); return result is null ? NotFound() : Ok(result); }

    [HttpGet("buildings/{buildingId:guid}/readiness")]
    public async Task<ActionResult<CurationReadiness>> GetBuildingReadiness(Guid buildingId, CancellationToken ct)
    { var result = await service.GetBuildingReadinessAsync(buildingId, ct); return result is null ? NotFound() : Ok(result); }

    [HttpGet("metering-points/{meteringPointId:guid}/readiness")]
    public async Task<ActionResult<CurationReadiness>> GetMeteringPointReadiness(Guid meteringPointId, CancellationToken ct)
    { var result = await service.GetMeteringPointReadinessAsync(meteringPointId, ct); return result is null ? NotFound() : Ok(result); }

    [HttpPost("buildings/{buildingId:guid}/evaluate")]
    public async Task<ActionResult<object>> EvaluateBuilding(Guid buildingId, CancellationToken ct) =>
        Ok(new { createdSuggestions = await service.EvaluateBuildingAsync(buildingId, ct) });

    [HttpPost("metering-points/{meteringPointId:guid}/evaluate")]
    public async Task<ActionResult<object>> EvaluateMeteringPoint(Guid meteringPointId, CancellationToken ct) =>
        Ok(new { createdSuggestions = await service.EvaluateMeteringPointAsync(meteringPointId, ct) });
}
