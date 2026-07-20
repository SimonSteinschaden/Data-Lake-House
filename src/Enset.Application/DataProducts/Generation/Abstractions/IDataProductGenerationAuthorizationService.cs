namespace Enset.Application.DataProducts.Generation.Abstractions;

using Enset.Application.DataProducts.Generation.Models;

public interface IDataProductGenerationAuthorizationService
{
    Task<DataProductGenerationAuthorizationResult> AuthorizeAsync(
        GenerateDataProductCommand command,
        CancellationToken cancellationToken = default);
}