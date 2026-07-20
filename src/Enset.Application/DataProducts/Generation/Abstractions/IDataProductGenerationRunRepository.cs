using Enset.Domain.DataProducts;

namespace Enset.Application.DataProducts.Generation.Abstractions;

public interface IDataProductGenerationRunRepository
{
    Task AddAsync(DataProductGenerationRun run, CancellationToken cancellationToken = default);
    Task UpdateAsync(DataProductGenerationRun run, CancellationToken cancellationToken = default);
}
