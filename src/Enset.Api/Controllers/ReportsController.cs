using Enset.Application.Reporting;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Enset.Api.Authorization;

namespace Enset.Api.Controllers;

[ApiController]
[Authorize(Policy = AuthorizationPolicyNames.EnsetEmployee)]
[Route("api/v1/reports")]
public sealed class ReportsController(IReportService reports)
    : ControllerBase
{
    [HttpGet("definitions")]
    public ActionResult<IReadOnlyList<ReportDefinition>> Definitions() =>
        Ok(reports.Definitions());

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ReportInstance>>> List(
        CancellationToken cancellationToken) =>
        Ok(await reports.List(cancellationToken));

    [HttpPost]
    public async Task<ActionResult<ReportInstance>> Create(
        [FromBody] CreateReportRequest request,
        CancellationToken cancellationToken)
    {
        var report = await reports.Create(request, cancellationToken);
        return CreatedAtAction(
            nameof(Get), new { reportId = report.ReportId }, report);
    }

    [HttpGet("{reportId:guid}")]
    public async Task<ActionResult<ReportInstance>> Get(
        Guid reportId,
        CancellationToken cancellationToken)
    {
        var report = await reports.Get(reportId, cancellationToken);
        return report is null ? NotFound() : Ok(report);
    }

    [HttpGet("{reportId:guid}/export/{format}")]
    public async Task<IActionResult> Export(
        Guid reportId,
        string format,
        CancellationToken cancellationToken)
    {
        var result = await reports.Export(
            reportId, format, cancellationToken);
        return result is null
            ? NotFound()
            : File(result.Content, result.ContentType, result.FileName);
    }
}
