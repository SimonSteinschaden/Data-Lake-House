using Enset.Api.Contracts.DataProducts;
using Enset.Api.Mapping;
using Enset.Application.DataProducts.Commands.GenerateDataProduct;
using Enset.Application.DataProducts.Generation.Abstractions;
using Microsoft.AspNetCore.Mvc;
using Enset.Application.Authorization;
using Microsoft.AspNetCore.Authorization;
using Enset.Api.Authorization;

namespace Enset.Api.Controllers;

[ApiController]
[Route("api/v1/data-products")]
[Produces("application/json")]
[Authorize(Policy = AuthorizationPolicyNames.CustomerReader)]
public sealed class DataProductsController : ControllerBase
{
    private readonly IDataProductRepository _repository;
    private readonly IDataProductGenerationAvailabilityService _availability;
    private readonly IDataProductGenerationService _generation;
    private readonly ICurrentUserContext _currentUser;

    public DataProductsController(IDataProductRepository repository,
        IDataProductGenerationAvailabilityService availability,
        IDataProductGenerationService generation,
        ICurrentUserContext currentUser)
    {
        _repository = repository;
        _availability = availability;
        _generation = generation;
        _currentUser = currentUser;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<DataProductSummaryDto>>> List(CancellationToken ct) =>
        Ok((await _repository.ListAsync(ct)).Select(x => x.ToSummary()));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<DataProductSummaryDto>> Get(Guid id, CancellationToken ct)
    {
        var product = await _repository.GetForGenerationAsync(id, ct);
        return product is null ? NotFoundProblem(id) : Ok(product.ToSummary());
    }

    [HttpGet("{id:guid}/generation-availability")]
    public async Task<ActionResult<GenerationAvailabilityDto>> Availability(Guid id,
        [FromQuery] Guid customerId, [FromQuery] DateTime periodFrom,
        [FromQuery] DateTime periodTo,
        CancellationToken ct)
    {
        var result = await _availability.CheckAsync(
            new GenerateDataProductCommand(id, customerId, CurrentUserId(), periodFrom, periodTo), ct);
        return Ok(new GenerationAvailabilityDto(result.IsAvailable,
            result.IsAuthorized, result.HasRequiredInputData,
            result.MissingInputs, result.Warnings));
    }

    [HttpPost("{id:guid}/generate")]
    [Authorize(Policy = AuthorizationPolicyNames.CustomerWriter)]
    public async Task<ActionResult<GenerateDataProductResponse>> Generate(Guid id,
        GenerateDataProductRequest request, CancellationToken ct)
    {
        if (request.PeriodFrom >= request.PeriodTo)
            return Problem(title: "Invalid period", statusCode: 400);
        try
        {
            var version = await _generation.GenerateAsync(new GenerateDataProductCommand(
                id, request.CustomerId, CurrentUserId(), request.PeriodFrom,
                request.PeriodTo, request.Parameters), ct);
            return Ok(new GenerateDataProductResponse(version.GenerationRunId!.Value,
                version.GenerationRun!.Status.ToString(), id, version.VersionNumber));
        }
        catch (UnauthorizedAccessException ex)
        { return Problem(title: "Forbidden", detail: ex.Message, statusCode: 403); }
        catch (InvalidOperationException ex)
        { return Problem(title: "Generation unavailable", detail: ex.Message, statusCode: 422); }
    }

    [HttpGet("{id:guid}/versions/latest")]
    public async Task<ActionResult<LatestDataProductVersionDto>> Latest(Guid id, CancellationToken ct)
    {
        var version = await _repository.GetLatestVersionAsync(id, ct);
        return version is null ? NotFoundProblem(id) : Ok(version.ToDto());
    }

    [HttpGet("{id:guid}/versions")]
    public async Task<ActionResult<IReadOnlyCollection<VersionHistoryDto>>> Versions(Guid id, CancellationToken ct) =>
        Ok((await _repository.GetVersionsAsync(id, ct)).Select(x =>
            new VersionHistoryDto(x.VersionNumber, x.Status.ToString(), x.GeneratedAt, x.Quality.ToString())));

    private ObjectResult NotFoundProblem(Guid id) => Problem(
        title: "Data Product not found", detail: $"Data Product '{id}' was not found.", statusCode: 404);

    private Guid CurrentUserId() => _currentUser.UserId ??
        throw new UnauthorizedAccessException("An authenticated user is required.");
}
