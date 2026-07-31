using Enset.Api.Authorization;
using Enset.Application.Exports.LEB.Abstractions;
using Enset.Application.Exports.LEB.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Enset.Api.Controllers;

[ApiController]
[Route("api/v1/exports/leb")]
[Authorize(Policy = AuthorizationPolicyNames.CustomerReader)]
public sealed class LebExportsController(ILebExportService exports) : ControllerBase
{
    [HttpPost("validate")]
    public async Task<ActionResult> Validate(
        [FromBody] LebExportRequest request, CancellationToken ct)
    {
        try
        {
            return Ok(await exports.ValidateAsync(request, ct));
        }
        catch (ArgumentException exception)
        {
            return Problem(statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid LEB export request", detail: exception.Message);
        }
    }

    [HttpPost("csv")]
    public Task<IActionResult> Csv(
        [FromBody] LebExportRequest request, CancellationToken ct) =>
        Export(() => exports.ExportCsvAsync(request, ct));

    [HttpPost("excel")]
    public Task<IActionResult> Excel(
        [FromBody] LebExportRequest request, CancellationToken ct) =>
        Export(() => exports.ExportExcelAsync(request, ct));

    private async Task<IActionResult> Export(Func<Task<LebExportFile>> create)
    {
        try
        {
            var file = await create();
            return File(file.Content, file.ContentType, file.FileName);
        }
        catch (LebExportValidationException exception)
        {
            return UnprocessableEntity(exception.Validation);
        }
        catch (ArgumentException exception)
        {
            return Problem(statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid LEB export request", detail: exception.Message);
        }
    }
}
