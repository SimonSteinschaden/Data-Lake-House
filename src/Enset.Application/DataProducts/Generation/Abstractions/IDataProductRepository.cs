using Enset.Domain.DataProducts;

namespace Enset.Application.DataProducts.Generation.Abstractions;

public interface IDataProductRepository
{
    Task<DataProduct?> GetForGenerationAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DataProduct>> ListAsync(CancellationToken cancellationToken = default);
    Task<DataProductVersion?> GetLatestVersionAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DataProductVersion>> GetVersionsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<int> GetNextVersionNumberAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddVersionAsync(DataProductVersion version, CancellationToken cancellationToken = default);
}
