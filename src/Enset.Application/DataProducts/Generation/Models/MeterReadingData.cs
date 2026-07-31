namespace Enset.Application.DataProducts.Generation.Models;

public sealed record MeterReadingData(
    Guid MeterId,
    DateTime Timestamp,
    decimal Value,
    string Unit,
    Enset.Domain.Energy.MeterReadingType ReadingType,
    Enset.Domain.Energy.MeterQuantity Quantity,
    Enset.Domain.Energy.MeterDirection Direction,
    Enset.Domain.Data.DataQuality Quality,
    int? IntervalSeconds);
