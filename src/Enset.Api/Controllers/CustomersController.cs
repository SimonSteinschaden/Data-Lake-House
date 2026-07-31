using Enset.Api.Authorization;
using Enset.Application.ReadModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Enset.Application.Crud;

namespace Enset.Api.Controllers;

[ApiController]
[Route("api/v1/customers")]
[Produces("application/json")]
[Authorize(Policy = AuthorizationPolicyNames.CustomerReader)]
public sealed class CustomersController : ControllerBase
{
    private readonly CrudQueryHandler _queries;
    private readonly CrudCommandHandler _commands;
    public CustomersController(CrudQueryHandler queries, CrudCommandHandler commands)
        => (_queries, _commands) = (queries, commands);

    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<CustomerSummaryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<CustomerSummaryDto>>> List(
        [FromQuery] CustomerListQuery query, CancellationToken cancellationToken) =>
        Ok(await _queries.Handle(new GetCustomersQuery(query.Page, query.PageSize, query.Search, query.IncludeDeleted), cancellationToken));

    [HttpGet("{customerId:guid}")]
    [ProducesResponseType(typeof(CustomerDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CustomerDetailDto>> Get(Guid customerId,
        [FromQuery] bool includeDeleted,
        CancellationToken cancellationToken)
    {
        var customer = await _queries.Handle(new GetCustomerByIdQuery(customerId, includeDeleted), cancellationToken);
        return customer is null
            ? Problem(title: "Customer not found", statusCode: StatusCodes.Status404NotFound)
            : Ok(customer);
    }

    /// <summary>Legt einen Kunden an.</summary>
    [HttpPost, Authorize(Policy = AuthorizationPolicyNames.EnsetEmployee)]
    [ProducesResponseType(typeof(EntityMutationResult), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<EntityMutationResult>> Create(CustomerWriteModel request, CancellationToken ct)
    {
        var result = await _commands.Handle(new CreateCustomerCommand(request), ct);
        return CreatedAtAction(nameof(Get), new { customerId = result.Id }, result);
    }

    /// <summary>Aktualisiert einen Kunden mit xmin-Concurrency.</summary>
    [HttpPut("{customerId:guid}"), Authorize(Policy = AuthorizationPolicyNames.CustomerWriter)]
    public Task<EntityMutationResult> Update(Guid customerId, CustomerWriteModel request, CancellationToken ct) =>
        _commands.Handle(new UpdateCustomerCommand(customerId, request), ct);

    /// <summary>Löscht einen Kunden fachlich; Beziehungen blockieren die Aktion.</summary>
    [HttpDelete("{customerId:guid}"), Authorize(Policy = AuthorizationPolicyNames.CustomerAdmin)]
    [ProducesResponseType(typeof(EntityMutationResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public Task<EntityMutationResult> Delete(Guid customerId, [FromQuery] uint rowVersion, CancellationToken ct) =>
        _commands.Handle(new DeleteCustomerCommand(customerId, rowVersion), ct);

    /// <summary>Stellt einen soft-gelöschten Kunden wieder her.</summary>
    [HttpPost("{customerId:guid}/restore"), Authorize(Policy = AuthorizationPolicyNames.CustomerAdmin)]
    public Task<EntityMutationResult> Restore(Guid customerId, [FromQuery] uint rowVersion, CancellationToken ct) =>
        _commands.Handle(new RestoreCustomerCommand(customerId, rowVersion), ct);
}
