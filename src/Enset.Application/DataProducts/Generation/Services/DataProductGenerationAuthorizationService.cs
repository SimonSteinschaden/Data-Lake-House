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
    public Task<DataProductGenerationAuthorizationResult> AuthorizeAsync(
        GenerateDataProductCommand command,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(DataProductGenerationAuthorizationResult.Allowed());
    }
}
