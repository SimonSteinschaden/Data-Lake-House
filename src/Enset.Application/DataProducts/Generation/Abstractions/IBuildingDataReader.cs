using Enset.Application.DataProducts.Generation.Models;

namespace Enset.Application.DataProducts.Generation.Abstractions;

public interface IBuildingDataReader
{
    Task<BuildingData?> GetAsync(
        Guid dataProductId,
        DateTime periodFrom,
        DateTime periodTo,
        CancellationToken cancellationToken = default);
}
