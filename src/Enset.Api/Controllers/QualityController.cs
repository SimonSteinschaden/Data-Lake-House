using Enset.Api.Authorization;
using Enset.Application.Authorization;
using Enset.Application.Quality;
using Enset.Application.ReadModel;
using Enset.Domain.Quality;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Enset.Api.Controllers;

[ApiController, Produces("application/json")]
[Authorize(Policy=AuthorizationPolicyNames.CustomerReader)]
public sealed class QualityController(
    IQualityPersistenceService persistence,
    IHierarchicalQualityAssessmentService assessments,
    IDataAccessScope scope) : ControllerBase
{
    [HttpGet("api/v1/buildings/{buildingId:guid}/quality-assessment")]
    public async Task<ActionResult<OperationalBuildingQualityAssessment>> Building(Guid buildingId,CancellationToken ct)
    {if(!await scope.CanReadBuilding(buildingId,ct))return NotFound();var values=await assessments.AssessBuildings([buildingId],ct);return values.TryGetValue(buildingId,out var value)?Ok(value):NotFound();}
    [HttpGet("api/v1/meters/{meterId:guid}/quality-assessment")]
    public async Task<ActionResult<MeterQualityAssessment>> Meter(Guid meterId,CancellationToken ct)
    {if(!await scope.CanReadMeter(meterId,ct))return NotFound();var values=await assessments.AssessMeters([meterId],ct);return values.TryGetValue(meterId,out var value)?Ok(value):NotFound();}
    [HttpGet("api/v1/energy-systems/{id:guid}/quality-assessment"),Authorize(Policy=AuthorizationPolicyNames.EnsetEmployee)]
    public async Task<ActionResult<EnergySystemQualityAssessment>> EnergySystem(Guid id,CancellationToken ct)
    {var values=await assessments.AssessEnergySystems([id],ct);return values.TryGetValue(id,out var value)?Ok(value):NotFound();}

    [HttpGet("api/v1/buildings/{id:guid}/inventory-declarations/current")]
    public async Task<ActionResult<BuildingInventoryDeclaration>> CurrentDeclaration(Guid id,CancellationToken ct)
    {if(!await scope.CanReadBuilding(id,ct))return NotFound();return await persistence.GetCurrentDeclaration(id,ct) is{} value?Ok(value):NotFound();}
    [HttpGet("api/v1/buildings/{id:guid}/inventory-declarations"),Authorize(Policy=AuthorizationPolicyNames.EnsetEmployee)]
    public Task<PagedResult<BuildingInventoryDeclaration>> Declarations(Guid id,[FromQuery] int page=1,[FromQuery] int pageSize=50,CancellationToken ct=default)=>persistence.GetDeclarationHistory(id,page,pageSize,ct);
    [HttpPost("api/v1/buildings/{id:guid}/inventory-declarations"),Authorize(Policy=AuthorizationPolicyNames.EnsetEmployee)]
    public Task<BuildingInventoryDeclaration> Declare(Guid id,InventoryDeclarationRequest request,CancellationToken ct)=>persistence.DeclareInventory(id,request,ct);
    [HttpPost("api/v1/buildings/{id:guid}/inventory-declarations/current/invalidate"),Authorize(Policy=AuthorizationPolicyNames.EnsetEmployee)]
    public async Task<IActionResult> Invalidate(Guid id,[FromBody] ReasonRequest request,CancellationToken ct){await persistence.InvalidateInventory(id,request.Reason,ct);return NoContent();}

    [HttpPost("api/v1/meters/{id:guid}/profile-analyses"),Authorize(Policy=AuthorizationPolicyNames.EnsetEmployee)]
    public Task<MeterProfileAnalysis> Start(Guid id,StartProfileAnalysisRequest request,CancellationToken ct)=>persistence.StartAnalysis(id,request.PeriodFromUtc,request.PeriodToUtc,request.AnalysisVersion,ct);
    [HttpGet("api/v1/meters/{id:guid}/profile-analyses"),Authorize(Policy=AuthorizationPolicyNames.EnsetEmployee)]
    public Task<PagedResult<MeterProfileAnalysis>> Analyses(Guid id,[FromQuery] int page=1,[FromQuery] int pageSize=50,CancellationToken ct=default)=>persistence.GetAnalysisHistory(id,page,pageSize,ct);
    [HttpGet("api/v1/meters/{meterId:guid}/profile-analyses/{analysisId:guid}"),Authorize(Policy=AuthorizationPolicyNames.EnsetEmployee)]
    public async Task<ActionResult<MeterProfileAnalysis>> Analysis(Guid meterId,Guid analysisId,CancellationToken ct)=>await persistence.GetAnalysis(meterId,analysisId,ct) is{} value?Ok(value):NotFound();
    [HttpGet("api/v1/meters/{meterId:guid}/profile-analyses/{analysisId:guid}/issues"),Authorize(Policy=AuthorizationPolicyNames.EnsetEmployee)]
    public Task<PagedResult<MeterProfileIssue>> Issues(Guid meterId,Guid analysisId,[FromQuery] int page=1,[FromQuery] int pageSize=50,CancellationToken ct=default)=>persistence.GetIssues(analysisId,page,pageSize,ct);
    [HttpPost("api/v1/meters/{meterId:guid}/profile-analyses/{analysisId:guid}/issues/{issueId:guid}/decisions"),Authorize(Policy=AuthorizationPolicyNames.EnsetEmployee)]
    public Task<MeterProfileCurationDecision> Decide(Guid meterId,Guid analysisId,Guid issueId,CurationDecisionRequest request,CancellationToken ct)=>persistence.Decide(issueId,request,ct);
    [HttpGet("api/v1/meters/{meterId:guid}/profile-analyses/{analysisId:guid}/issues/{issueId:guid}/decisions"),Authorize(Policy=AuthorizationPolicyNames.EnsetEmployee)]
    public Task<PagedResult<MeterProfileCurationDecision>> Decisions(Guid meterId,Guid analysisId,Guid issueId,[FromQuery] int page=1,[FromQuery] int pageSize=50,CancellationToken ct=default)=>persistence.GetDecisionHistory(issueId,page,pageSize,ct);
}
public sealed record ReasonRequest(string Reason);
public sealed record StartProfileAnalysisRequest(DateTime PeriodFromUtc,DateTime PeriodToUtc,string AnalysisVersion);
