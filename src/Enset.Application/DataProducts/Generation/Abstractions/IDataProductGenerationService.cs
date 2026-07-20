namespace Enset.Application.DataProducts.Generation.Abstractions;

using Enset.Application.DataProducts.Commands.GenerateDataProduct;

public interface IDataProductGenerationService
{
    Task GenerateAsync(
        GenerateDataProductCommand command,
        CancellationToken cancellationToken = default);
}