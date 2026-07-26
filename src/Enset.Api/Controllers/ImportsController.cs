using Enset.Api.Contracts.Imports.Requests;
using Enset.Api.Contracts.Imports.Responses;
using Enset.Api.Errors;
using Enset.Api.Mapping;
using Enset.Application.Imports.Abstractions;
using Enset.Application.Imports.Exceptions;
using Enset.Application.Imports.WriteGate;
using Microsoft.AspNetCore.Mvc;
using Enset.Application.Authorization;
using Microsoft.AspNetCore.Authorization;
using Enset.Api.Authorization;
using Enset.Application.Imports.Resolution;

namespace Enset.Api.Controllers;

/// <summary>
/// HTTP entry point for import analysis, resolution handling and commit operations.
/// </summary>
[ApiController]
[Route("api/v1/imports")]
[Produces("application/json")]
[Authorize(Policy = AuthorizationPolicyNames.Authenticated)]
public sealed class ImportsController : ControllerBase
{
    private static readonly HashSet<string> SupportedExtensions =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ".xlsx",
            ".xlsm",
            ".csv"
        };

    private readonly IImportAnalysisService _analysisService;
    private readonly IImportReportRepository _reports;
    private readonly IApplyResolutionService _resolutionService;
    private readonly IImportCommitService _commitService;
    private readonly ILogger<ImportsController> _logger;
    private readonly IWebHostEnvironment _environment;
    private readonly ICurrentUserContext _currentUser;

    public ImportsController(
        IImportAnalysisService analysisService,
        IImportReportRepository reports,
        IApplyResolutionService resolutionService,
        IImportCommitService commitService,
        ILogger<ImportsController> logger,
        IWebHostEnvironment environment,
        ICurrentUserContext currentUser)
    {
        _analysisService = analysisService;
        _reports = reports;
        _resolutionService = resolutionService;
        _commitService = commitService;
        _logger = logger;
        _environment = environment;
        _currentUser = currentUser;
    }

    [HttpPost("analyze")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(ImportReportResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ImportReportResponse>> Analyze(
        [FromForm] AnalyzeImportRequest request,
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
                "Only .xlsx, .xlsm and .csv files are supported.");
        }

        if (!_currentUser.UserId.HasValue)
        {
            return ApiProblems.InvalidImportRequest(
                this,
                "An authenticated user is required.");
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
                _currentUser.UserId.Value.ToString(),
                cancellationToken,
                request.SourceType,
                request.Medium,
                request.DefaultMeterNumber);

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

        if (!_currentUser.UserId.HasValue)
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
                _currentUser.UserId.Value.ToString(),
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

    [HttpPost("{importId:guid}/resolution-rules")]
    public async Task<ActionResult<ApplyResolutionRuleResponse>> ApplyResolutionRule(
        Guid importId,
        [FromBody] ApplyResolutionRuleRequest request,
        CancellationToken cancellationToken)
    {
        var report = await _reports.GetAsync(importId, cancellationToken);
        if (report is null)
            return ApiProblems.ImportNotFound(this, importId);
        if (!_currentUser.UserId.HasValue)
            return ApiProblems.InvalidResolutionRequest(
                this, "An authenticated user is required.");

        try
        {
            var result = _resolutionService.ApplyRule(
                report,
                new ApplyResolutionRuleCommand
                {
                    RuleId = request.RuleId,
                    SeedIssueId = request.SeedIssueId,
                    Scope = request.Scope,
                    ResolutionType = request.ResolutionType,
                    ResolutionAction = request.ResolutionAction,
                    ResolutionPayload = request.ResolutionPayload
                },
                _currentUser.UserId.Value.ToString(),
                DateTime.UtcNow);

            await _reports.SaveAsync(report, cancellationToken);
            return Ok(new ApplyResolutionRuleResponse
            {
                RuleId = result.RuleId,
                MatchedIssueCount = result.MatchedIssueCount,
                ResolvedIssueCount = result.ResolvedIssueCount,
                FailedIssueCount = result.FailedIssueCount,
                SkippedIssueCount = result.SkippedIssueCount,
                RemainingBlockingIssueCount = result.RemainingBlockingIssueCount,
                Status = result.Status,
                Report = report.ToResponse()
            });
        }
        catch (ArgumentException exception)
        {
            return ApiProblems.InvalidResolutionRequest(this, exception.Message);
        }
        catch (InvalidOperationException exception)
        {
            return ApiProblems.ImportConflict(this, exception.Message);
        }
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
                UserId = _currentUser.UserId?.ToString() ?? string.Empty,
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
