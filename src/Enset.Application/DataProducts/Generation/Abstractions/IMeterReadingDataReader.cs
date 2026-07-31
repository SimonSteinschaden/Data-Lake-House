using Enset.Application.DataProducts.Generation.Models;

namespace Enset.Application.DataProducts.Generation.Abstractions;

public interface IMeterReadingDataReader
{
    Task<IReadOnlyList<MeterReadingData>> GetConsumptionReadingsAsync(
        Guid dataProductId,
        DateTime periodFrom,
        DateTime periodTo,
        CancellationToken cancellationToken = default);
}
