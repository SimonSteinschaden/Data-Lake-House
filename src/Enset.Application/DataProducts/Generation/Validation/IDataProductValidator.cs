using Enset.Application.DataProducts.Generation.Models;

namespace Enset.Application.DataProducts.Generation.Validation;

/// <summary>
/// Definiert die Validierung des Kontexts einer Datenproduktgenerierung.
/// </summary>
public interface IDataProductValidator
{
    Task ValidateAsync(
        DataProductGenerationContext context,
        CancellationToken cancellationToken = default);
}
