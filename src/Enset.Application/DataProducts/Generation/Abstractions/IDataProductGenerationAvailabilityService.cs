namespace Enset.Application.DataProducts.Generation.Abstractions;

using Enset.Application.DataProducts.Commands.GenerateDataProduct;
using Enset.Application.DataProducts.Generation.Models;

public interface IDataProductGenerationAvailabilityService
{
    Task<DataProductGenerationAvailability> CheckAsync(
        GenerateDataProductCommand command,
        CancellationToken cancellationToken = default);
}