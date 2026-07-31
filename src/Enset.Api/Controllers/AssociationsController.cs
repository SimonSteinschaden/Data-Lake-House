using Enset.Api.Authorization;
using Enset.Application.Associations;
using Enset.Application.Authorization;
using Enset.Infrastructure.Associations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Enset.Api.Controllers;

[ApiController,Route("api/v1/associations"),Produces("application/json")]
[Authorize(Policy=AuthorizationPolicyNames.EnsetEmployee)]
public sealed class AssociationsController(
    IAssociationService service, ICurrentUserContext currentUser) : ControllerBase
{
    [HttpGet("types")]
    public IReadOnlyList<AssociationTypeDefinition> Types()=>service.Types();

    [HttpGet("entities")]
    public Task<AssociationEntityPage> Entities([FromQuery] AssociationEntityQuery query,
        CancellationToken cancellationToken)=>service.SearchEntities(query,cancellationToken);

    [HttpGet]
    public Task<IReadOnlyList<AssociationListItem>> List([FromQuery]string associationType,
        [FromQuery]Guid? sourceId,[FromQuery]Guid? targetId,[FromQuery]DateOnly? validAt,
        [FromQuery]bool includeHistorical=false,CancellationToken cancellationToken=default)=>
        service.List(associationType,sourceId,targetId,validAt,includeHistorical,cancellationToken);

    [HttpPost("preview")]
    public Task<AssociationPreviewResponse> Preview(AssociationPreviewRequest request,
        CancellationToken cancellationToken)=>service.Preview(request,cancellationToken);

    [HttpPost]
    public async Task<ActionResult<AssociationCommandResponse>> Commit(
        AssociationPreviewRequest request,CancellationToken cancellationToken)
    {
        try{return Ok(await service.Commit(request,UserId(),cancellationToken));}
        catch(AssociationValidationException ex){return ConflictProblem(ex.Preview);}
    }

    [HttpPost("remove-preview")]
    public Task<AssociationPreviewResponse> RemovePreview(RemoveAssociationRequest request,
        CancellationToken cancellationToken)=>service.RemovePreview(request,cancellationToken);

    [HttpPost("remove")]
    public async Task<ActionResult<AssociationCommandResponse>> Remove(
        RemoveAssociationRequest request,CancellationToken cancellationToken)
    {
        try{return Ok(await service.Remove(request,UserId(),cancellationToken));}
        catch(AssociationValidationException ex){return ConflictProblem(ex.Preview);}
    }

    [HttpPatch("{id:guid}/primary")]
    public async Task<ActionResult<AssociationCommandResponse>> Primary(Guid id,
        SetPrimaryAssociationRequest request,CancellationToken cancellationToken)
    {
        if(id!=request.AssociationId)return Problem(statusCode:400,title:"Association id mismatch");
        try{return Ok(await service.SetPrimary(request,UserId(),cancellationToken));}
        catch(AssociationValidationException ex){return ConflictProblem(ex.Preview);}
    }

    [HttpGet("history")]
    public Task<IReadOnlyList<AssociationAuditItem>> History([FromQuery]string? associationType,
        [FromQuery]Guid? sourceId,[FromQuery]Guid? targetId,
        CancellationToken cancellationToken)=>service.History(associationType,sourceId,targetId,cancellationToken);

    private Guid UserId()=>currentUser.UserId??throw new UnauthorizedAccessException("Authenticated user required.");
    private ObjectResult ConflictProblem(AssociationPreviewResponse preview)=>
        Problem(statusCode:409,title:"Association conflict",
            detail:string.Join(" ",preview.Conflicts.Select(x=>x.Message)),
            extensions:new Dictionary<string,object?>{{"preview",preview}});
}
