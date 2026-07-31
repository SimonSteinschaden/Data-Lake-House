using Enset.Application.DataProducts.Commands.GenerateDataProduct;
using Enset.Application.DataProducts.Generation.Abstractions;
using Enset.Application.DataProducts.Generation.Models;

namespace Enset.Application.DataProducts.Generation.Services;

public sealed class DataProductGenerationAvailabilityService
    : IDataProductGenerationAvailabilityService
{
    private readonly IDataProductRepository _repository;
    private readonly IDataProductGenerationAuthorizationService _authorization;
    private readonly IReadOnlyDictionary<string, IDataProductGenerator> _generators;

    public DataProductGenerationAvailabilityService(
        IDataProductRepository repository,
        IDataProductGenerationAuthorizationService authorization,
        IEnumerable<IDataProductGenerator> generators)
    {
        _repository = repository;
        _authorization = authorization;
        _generators = generators.ToDictionary(
            x => x.DefinitionCode,
            StringComparer.OrdinalIgnoreCase);
    }

    public async Task<DataProductGenerationAvailability> CheckAsync(
        GenerateDataProductCommand command,
        CancellationToken cancellationToken = default)
    {
        var authorization = await _authorization.AuthorizeAsync(command, cancellationToken);
        if (!authorization.IsAuthorized)
        {
            return DataProductGenerationAvailability.Unauthorized(
                authorization.DenialReason ?? "Nicht berechtigt");
        }

        if (command.PeriodFrom is null || command.PeriodTo is null
            || command.PeriodFrom >= command.PeriodTo)
        {
            return DataProductGenerationAvailability.MissingData("Gültiger Zeitraum");
        }

        var product = await _repository.GetForGenerationAsync(
            command.DataProductId, cancellationToken);
        if (product is null)
        {
            return DataProductGenerationAvailability.MissingData("Data Product");
        }

        if (product.ScopeAssignments.Count != 1)
        {
            return DataProductGenerationAvailability.MissingData("Eindeutiger Scope");
        }

        if (!_generators.TryGetValue(product.Definition.Code, out var generator))
        {
            return DataProductGenerationAvailability.MissingData("Registrierter Generator");
        }

        return await generator.CheckInputAvailabilityAsync(
            new DataProductGenerationContext
            {
                Command = command,
                DataProduct = product,
                Definition = product.Definition
            },
            cancellationToken);
    }
}
