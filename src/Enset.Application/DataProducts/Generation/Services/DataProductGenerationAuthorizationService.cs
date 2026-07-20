using Enset.Application.DataProducts.Generation.Abstractions;
using Enset.Application.DataProducts.Generation.Models;

namespace Enset.Application.DataProducts.Generation.Services;

/// <summary>
/// Reserviert die Berechtigungsprüfung für Datenproduktgenerierungen.
/// </summary>
public sealed class DataProductGenerationAuthorizationService
    : IDataProductGenerationAuthorizationService
{
    public Task<DataProductGenerationAuthorizationResult> AuthorizeAsync(
        Guid userId,
        Guid customerId,
        Guid dataProductDefinitionId,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}
