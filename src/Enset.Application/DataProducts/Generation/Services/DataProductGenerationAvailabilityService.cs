using Enset.Application.DataProducts.Generation.Abstractions;
using Enset.Application.DataProducts.Generation.Models;

namespace Enset.Application.DataProducts.Generation.Services;

/// <summary>
/// Reserviert die Prüfung der fachlichen und technischen Generierungsverfügbarkeit.
/// </summary>
public sealed class DataProductGenerationAvailabilityService
    : IDataProductGenerationAvailabilityService
{
    public Task<DataProductGenerationAvailability> CheckAsync(
        DataProductGenerationAvailabilityRequest request,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}
