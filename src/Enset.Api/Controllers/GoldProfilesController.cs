using Enset.Api.Authorization;using Enset.Application.GoldProfiles;using Microsoft.AspNetCore.Authorization;using Microsoft.AspNetCore.Mvc;
namespace Enset.Api.Controllers;
[ApiController,Route("api/v1/gold-profiles"),Authorize(Policy=AuthorizationPolicyNames.EnsetEmployee)]
public sealed class GoldProfilesController(IGoldProfileVersionService service):ControllerBase{
 [HttpGet("{entityType}/{entityId:guid}/versions")]public Task<IReadOnlyList<GoldProfileVersionDto>> Versions(string entityType,Guid entityId,CancellationToken ct)=>service.GetVersions(entityType,entityId,ct);
 [HttpGet("{entityType}/{entityId:guid}/versions/{versionId:guid}")]public async Task<ActionResult<GoldProfileVersionDto>> Version(string entityType,Guid entityId,Guid versionId,CancellationToken ct)=>await service.Get(entityType,entityId,versionId,ct) is{}x?Ok(x):NotFound();
 [HttpGet("{entityType}/{entityId:guid}/current")]public async Task<ActionResult<GoldProfileVersionDto>> Current(string entityType,Guid entityId,CancellationToken ct)=>await service.GetCurrent(entityType,entityId,ct) is{}x?Ok(x):NotFound();
 [HttpPost("{entityType}/{entityId:guid}/create-version")]public Task<GoldProfileVersionDto>Create(string entityType,Guid entityId,CancellationToken ct)=>service.Create(entityType,entityId,ct);
 [HttpPost("{entityType}/{entityId:guid}/versions/{versionId:guid}/release"),Authorize(Policy=AuthorizationPolicyNames.EnsetAdmin)]public Task<GoldProfileVersionDto>Release(string entityType,Guid entityId,Guid versionId,[FromQuery]uint rowVersion,[FromBody]string? reason,CancellationToken ct)=>service.Release(entityType,entityId,versionId,rowVersion,reason,ct);
 [HttpPost("{entityType}/{entityId:guid}/versions/{versionId:guid}/revoke"),Authorize(Policy=AuthorizationPolicyNames.EnsetAdmin)]public Task<GoldProfileVersionDto>Revoke(string entityType,Guid entityId,Guid versionId,[FromQuery]uint rowVersion,[FromBody]string reason,CancellationToken ct)=>service.Revoke(entityType,entityId,versionId,rowVersion,reason,ct);
}
