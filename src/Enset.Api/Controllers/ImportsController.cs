using Enset.Api.Contracts.Imports.Requests;
using Enset.Api.Contracts.Imports.Responses;
using Enset.Api.Errors;
using Enset.Api.Mapping;
using Enset.Application.Imports.Abstractions;
using Enset.Application.Imports.Exceptions;
using Enset.Application.Imports.WriteGate;
using Microsoft.AspNetCore.Mvc;

namespace Enset.Api.Controllers;

/// <summary>
/// HTTP entry point for import analysis, resolution handling and commit operations.
/// </summary>
[ApiController]
[Route("api/v1/imports")]
[Produces("application/json")]
public sealed class ImportsController : ControllerBase
{
    private static readonly HashSet<string> SupportedExtensions =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ".xlsx",
            ".xlsm"
        };

    private readonly IImportAnalysisService _analysisService;
    private readonly IImportReportRepository _reports;
    private readonly IApplyResolutionService _resolutionService;
    private readonly IImportCommitService _commitService;
    private readonly ILogger<ImportsController> _logger;
    private readonly IWebHostEnvironment _environment;

    public ImportsController(
        IImportAnalysisService analysisService,
        IImportReportRepository reports,
        IApplyResolutionService resolutionService,
        IImportCommitService commitService,
        ILogger<ImportsController> logger,
        IWebHostEnvironment environment)
    {
        _analysisService = analysisService;
        _reports = reports;
        _resolutionService = resolutionService;
        _commitService = commitService;
        _logger = logger;
        _environment = environment;
    }

    [HttpPost("analyze")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(ImportReportResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ImportReportResponse>> Analyze(
        [FromForm] AnalyzeImportRequest request,
        [FromHeader(Name = "X-User-Id")] string? userId, // User ID is required for auditing purposes. Nullable for MVP. TODO:var userId = User.FindFirstValue(ClaimTypes.NameIdentifier); or ICurrentUserService
        CancellationToken cancellationToken)
    {
        if (request.ImportFile.Length == 0)
        {
            return ApiProblems.InvalidImportRequest(
                this,
                "An Excel file is required.");
        }

        var extension = Path.GetExtension(request.ImportFile.FileName);

        if (!SupportedExtensions.Contains(extension))
        {
            return ApiProblems.InvalidImportRequest(
                this,
                "Only .xlsx and .xlsm files are supported.");
        }

        if (string.IsNullOrWhiteSpace(userId))
        {
            return ApiProblems.InvalidImportRequest(
                this,
                "X-User-Id header is required.");
        }

        _logger.LogInformation(
            "Import file received. FileName={FileName}, Length={Length}, ContentType={ContentType}",
            request.ImportFile.FileName,
            request.ImportFile.Length,
            request.ImportFile.ContentType);

        try
        {
            await using var source = request.ImportFile.OpenReadStream();
            var report = await _analysisService.AnalyzeAsync(
                source,
                request.ImportFile.FileName,
                request.ImportFile.ContentType,
                userId,
                cancellationToken);

            return Ok(report.ToResponse());
        }
        catch (InvalidImportFileException exception)
        {
            _logger.LogWarning(
                exception,
                "Invalid import file rejected. FileName={FileName}",
                request.ImportFile.FileName);
            return ApiProblems.InvalidImportFile(
                this,
                exception.Message,
                _environment.IsDevelopment() ? exception.ToString() : null);
        }
    }

    [HttpGet("{importId:guid}")]
    public async Task<ActionResult<ImportReportResponse>> Get(
        Guid importId,
        CancellationToken cancellationToken)
    {
        var report = await _reports.GetAsync(
            importId,
            cancellationToken);

        return report is null
            ? ApiProblems.ImportNotFound(this, importId)
            : Ok(report.ToResponse());
    }

    [HttpPost("{importId:guid}/resolutions")]
    public async Task<ActionResult<ImportReportResponse>> ApplyResolutions(
        Guid importId,
        [FromBody] ApplyImportResolutionRequest request,
        CancellationToken cancellationToken)
    {
        var report = await _reports.GetAsync(
            importId,
            cancellationToken);

        if (report is null)
            return ApiProblems.ImportNotFound(this, importId);

        if (string.IsNullOrWhiteSpace(request.UserId))
        {
            return ApiProblems.InvalidResolutionRequest(
                this,
                "UserId is required.");
        }

        try
        {
            _resolutionService.Apply(
                report,
                request.Resolutions,
                request.UserId,
                DateTime.UtcNow);
        }
        catch (ArgumentException exception)
        {
            return ApiProblems.InvalidResolutionRequest(
                this,
                exception.Message);
        }
        catch (InvalidOperationException exception)
        {
            return ApiProblems.ImportConflict(
                this,
                exception.Message);
        }

        await _reports.SaveAsync(
            report,
            cancellationToken);

        return Ok(report.ToResponse());
    }

    [HttpPost("{importId:guid}/commit")]
    public async Task<ActionResult<ImportReportResponse>> Commit(
        Guid importId,
        [FromBody] CommitImportRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _commitService.CommitAsync(
            new ImportCommitCommand
            {
                ImportId = importId,
                UserId = request.UserId,
                Timestamp = DateTime.UtcNow,
                TargetMode = request.TargetMode,
                TargetWriter = request.TargetWriter,
                TargetLocation = request.TargetLocation,
                ArchiveRawSource = request.ArchiveRawSource
            },
            cancellationToken);

        if (result.Report is null)
            return ApiProblems.ImportNotFound(this, importId);

        if (!result.Succeeded)
        {
            var detail = result.GateResult.Errors.Count == 0
                ? "The import could not be committed."
                : string.Join("; ", result.GateResult.Errors);

            return ApiProblems.ImportConflict(this, detail);
        }

        return Ok(result.Report.ToResponse());
    }
}
