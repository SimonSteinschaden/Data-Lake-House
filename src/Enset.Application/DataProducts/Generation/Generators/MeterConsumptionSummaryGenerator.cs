using Enset.Application.DataProducts.Generation.Abstractions;
using Enset.Application.DataProducts.Generation.Models;

namespace Enset.Application.DataProducts.Generation.Generators;

/// <summary>
/// Reserviert die Generierung einer Verbrauchszusammenfassung für einen Zähler.
/// </summary>
public sealed class MeterConsumptionSummaryGenerator
    : IDataProductGenerator
{
    public string DefinitionCode => "METER_CONSUMPTION_SUMMARY";

    public Task<DataProductGenerationResult> GenerateAsync(
        DataProductGenerationContext context,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}
