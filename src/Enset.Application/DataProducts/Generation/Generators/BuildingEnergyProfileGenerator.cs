using Enset.Application.DataProducts.Generation.Abstractions;
using Enset.Application.DataProducts.Generation.Models;

namespace Enset.Application.DataProducts.Generation.Generators;

/// <summary>
/// Reserviert die Generierung eines Energieprofils für ein Gebäude.
/// </summary>
public sealed class BuildingEnergyProfileGenerator
    : IDataProductGenerator
{
    public string DefinitionCode => "BUILDING_ENERGY_PROFILE";

    public Task<DataProductGenerationResult> GenerateAsync(
        DataProductGenerationContext context,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}
