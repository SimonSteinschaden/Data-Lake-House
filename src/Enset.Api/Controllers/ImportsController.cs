using Enset.Api.Contracts.Imports.Requests;
using Enset.Api.Contracts.Imports.Responses;
using Enset.Api.Errors;
using Enset.Api.Mapping;
using Enset.Application.Imports.Abstractions;
using Enset.Application.Imports.WriteGate;
using Microsoft.AspNetCore.Mvc;

namespace Enset.Api.Controllers;

/// <summary>
/// Controller for handling import-related operations.
/// HTTP entry point for import analysis, resolution application, and commit operations.
/// </summary>
[ApiController]
[Route("api/v1/imports")]
public sealed class ImportsController : ControllerBase
{
    private static readonly HashSet<string> SupportedExtensions =
        new(StringComparer.OrdinalIgnoreCase) { ".xlsx", ".xlsm" };

    private readonly IImportAnalysisService _analysisService;
    private readonly IImportReportRepository _reports;
    private readonly IApplyResolutionService _resolutionService;
    private readonly IImportCommitService _commitService;

    public ImportsController(
        IImportAnalysisService analysisService,
        IImportReportRepository reports,
        IApplyResolutionService resolutionService,
        IImportCommitService commitService)
    {
        _analysisService = analysisService;
        _reports = reports;
        _resolutionService = resolutionService;
        _commitService = commitService;
    }

    [HttpPost("analyze")]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<ImportReportResponseDto>> Analyze(
        [FromForm] AnalyzeImportFormRequest request,
        [FromHeader(Name = "X-User-Id")] string userId,
        CancellationToken cancellationToken)
    {
        if (request.File.Length == 0)
            return ApiProblems.InvalidImportRequest(this, "An Excel file is required.");

        if (!SupportedExtensions.Contains(Path.GetExtension(request.File.FileName)))
            return ApiProblems.InvalidImportRequest(this, "Only .xlsx and .xlsm files are supported.");

        if (string.IsNullOrWhiteSpace(userId))
            return ApiProblems.InvalidImportRequest(this, "X-User-Id header is required.");

        await using var source = request.File.OpenReadStream();

        var report = await _analysisService.AnalyzeAsync(
            source,
            request.File.FileName,
            request.File.ContentType,
            userId,
            cancellationToken);

        return Ok(report.ToResponse());
    }

    [HttpGet("{importId:guid}")]
    public async Task<ActionResult<ImportReportResponseDto>> Get(
        Guid importId,
        CancellationToken cancellationToken)
    {
        var report = await _reports.GetAsync(importId, cancellationToken);

        return report is null
            ? ApiProblems.ImportNotFound(this, importId)
            : Ok(report.ToResponse());
    }

    [HttpPost("{importId:guid}/resolutions")]
    public async Task<ActionResult<ImportReportResponseDto>> ApplyResolutions(
        Guid importId,
        [FromBody] ApplyImportResolutionRequest request,
        CancellationToken cancellationToken)
    {
        var report = await _reports.GetAsync(importId, cancellationToken);
        if (report is null)
            return ApiProblems.ImportNotFound(this, importId);

        if (string.IsNullOrWhiteSpace(request.UserId))
            return ApiProblems.InvalidResolutionRequest(this, "UserId is required.");

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
            return ApiProblems.InvalidResolutionRequest(this, exception.Message);
        }
        catch (InvalidOperationException exception)
        {
            return ApiProblems.ImportConflict(this, exception.Message);
        }

        await _reports.SaveAsync(report, cancellationToken);

        return Ok(report.ToResponse());
    }

    [HttpPost("{importId:guid}/commit")]
    public async Task<ActionResult<ImportReportResponseDto>> Commit(
        Guid importId,
        [FromBody] CommitImportApiRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _commitService.CommitAsync(
            new ImportCommitRequest
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