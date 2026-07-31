namespace Enset.Application.DataProducts.Generation.Models;

public sealed record BuildingData(
    Guid BuildingId,
    string Name,
    IReadOnlyCollection<Guid> MeterIds);
