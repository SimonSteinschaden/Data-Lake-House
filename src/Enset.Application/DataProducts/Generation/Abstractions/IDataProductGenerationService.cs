namespace Enset.Application.DataProducts.Generation.Abstractions;

using Enset.Application.DataProducts.Commands.GenerateDataProduct;
using Enset.Domain.DataProducts;

public interface IDataProductGenerationService
{
    Task<DataProductVersion> GenerateAsync(
        GenerateDataProductCommand command,
        CancellationToken cancellationToken = default);
}
