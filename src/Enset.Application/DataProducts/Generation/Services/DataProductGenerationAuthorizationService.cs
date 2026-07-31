using Enset.Application.DataProducts.Generation.Abstractions;
using Enset.Application.DataProducts.Commands.GenerateDataProduct;
using Enset.Application.DataProducts.Generation.Models;

namespace Enset.Application.DataProducts.Generation.Services;

/// <summary>
/// Reserviert die Berechtigungsprüfung für Datenproduktgenerierungen.
/// </summary>
public sealed class DataProductGenerationAuthorizationService
    : IDataProductGenerationAuthorizationService
{
    private readonly IDataProductRepository _repository;

    public DataProductGenerationAuthorizationService(IDataProductRepository repository)
    {
        _repository = repository;
    }

    public async Task<DataProductGenerationAuthorizationResult> AuthorizeAsync(
        GenerateDataProductCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command.RequestedByUserId == Guid.Empty || command.CustomerId == Guid.Empty)
            return DataProductGenerationAuthorizationResult.Denied("Benutzer und Kunde sind erforderlich.");

        var product = await _repository.GetForGenerationAsync(command.DataProductId, cancellationToken);
        return product?.CustomerAssignments.Any(x => x.CustomerId == command.CustomerId
            && x.IsActive && x.ValidFrom <= DateTime.UtcNow
            && (x.ValidTo == null || x.ValidTo >= DateTime.UtcNow)) == true
            ? DataProductGenerationAuthorizationResult.Allowed()
            : DataProductGenerationAuthorizationResult.Denied("Der Kunde ist für dieses Data Product nicht berechtigt.");
    }
}
