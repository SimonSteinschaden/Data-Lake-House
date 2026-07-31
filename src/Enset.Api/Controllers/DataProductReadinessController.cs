using Enset.Api.Authorization;using Enset.Application.GoldProfiles;using Microsoft.AspNetCore.Authorization;using Microsoft.AspNetCore.Mvc;
namespace Enset.Api.Controllers;
[ApiController,Route("api/v1/data-product-readiness"),Authorize(Policy=AuthorizationPolicyNames.EnsetEmployee)]
public sealed class DataProductReadinessController(IDataProductReadinessService service):ControllerBase{
 [HttpGet("{dataProductType}/{scopeType}/{scopeId:guid}")]public Task<DataProductReadinessResult>One(DataProductType dataProductType,string scopeType,Guid scopeId,CancellationToken ct)=>service.Evaluate(dataProductType,scopeType,scopeId,ct);
 [HttpGet("{scopeType}/{scopeId:guid}")]public Task<IReadOnlyList<DataProductReadinessResult>>All(string scopeType,Guid scopeId,CancellationToken ct)=>service.EvaluateAll(scopeType,scopeId,ct);
}
