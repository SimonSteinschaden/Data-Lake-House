using Enset.Api.Authorization;
using Enset.Application.ReadModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Enset.Api.Controllers;

[ApiController]
[Route("api/v1/customers")]
[Produces("application/json")]
[Authorize(Policy = AuthorizationPolicyNames.CustomerReader)]
public sealed class CustomersController : ControllerBase
{
    private readonly IEntityReadService _reads;
    public CustomersController(IEntityReadService reads) => _reads = reads;

    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<CustomerSummaryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<CustomerSummaryDto>>> List(
        [FromQuery] CustomerListQuery query, CancellationToken cancellationToken) =>
        Ok(await _reads.GetCustomersAsync(query, cancellationToken));

    [HttpGet("{customerId:guid}")]
    [ProducesResponseType(typeof(CustomerDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CustomerDetailDto>> Get(Guid customerId,
        CancellationToken cancellationToken)
    {
        var customer = await _reads.GetCustomerAsync(customerId, cancellationToken);
        return customer is null
            ? Problem(title: "Customer not found", statusCode: StatusCodes.Status404NotFound)
            : Ok(customer);
    }
}
