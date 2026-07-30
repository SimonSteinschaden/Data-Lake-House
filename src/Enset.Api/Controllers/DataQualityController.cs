using Enset.Api.Authorization;
using Enset.Application.QualityManagement;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Enset.Api.Controllers;

[ApiController, Route("api/v1/data-quality")]
[Authorize(Policy = AuthorizationPolicyNames.EnsetEmployee)]
public sealed class DataQualityController(IDataQualityDashboardService service) : ControllerBase
{
    [HttpGet("dashboard")]
    public Task<DataQualityDashboard> Dashboard(CancellationToken cancellationToken) =>
        service.Get(cancellationToken);

    [HttpGet("problems/{code}")]
    public async Task<ActionResult<QualityMetric>> Detail(string code,
        CancellationToken cancellationToken) =>
        await service.GetMetric(code, cancellationToken) is { } result ? Ok(result) : NotFound();

    [HttpGet("problems/{code}/trend")]
    public Task<IReadOnlyList<QualityTrendPoint>> Trend(string code,
        [FromQuery] int days = 30, CancellationToken cancellationToken = default) =>
        service.GetTrend(code, days, cancellationToken);
}
