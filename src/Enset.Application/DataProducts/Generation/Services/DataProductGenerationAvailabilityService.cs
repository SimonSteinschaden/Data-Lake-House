using Enset.Application.DataProducts.Generation.Abstractions;
using Enset.Application.DataProducts.Commands.GenerateDataProduct;
using Enset.Application.DataProducts.Generation.Models;

namespace Enset.Application.DataProducts.Generation.Services;

/// <summary>
/// Reserviert die Prüfung der fachlichen und technischen Generierungsverfügbarkeit.
/// </summary>
public sealed class DataProductGenerationAvailabilityService
    : IDataProductGenerationAvailabilityService
{
    public Task<DataProductGenerationAvailability> CheckAsync(
        GenerateDataProductCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command.PeriodFrom is null || command.PeriodTo is null
            || command.PeriodFrom >= command.PeriodTo)
        {
            return Task.FromResult(
                DataProductGenerationAvailability.MissingData("Gültiger Zeitraum"));
        }

        return Task.FromResult(DataProductGenerationAvailability.Available());
    }
}
