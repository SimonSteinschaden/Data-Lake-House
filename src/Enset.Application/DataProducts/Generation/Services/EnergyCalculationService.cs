using Enset.Application.DataProducts.Generation.Models;
using Enset.Domain.Energy;

namespace Enset.Application.DataProducts.Generation.Services;

public sealed record EnergyCalculationResult(
    decimal TotalKWh,
    IReadOnlyCollection<decimal> PowerKw,
    IReadOnlyCollection<string> Warnings);

public static class EnergyCalculationService
{
    public static EnergyCalculationResult Calculate(
        IEnumerable<MeterReadingData> source)
    {
        var warnings = new List<string>();
        var energy = 0m;
        var powers = new List<decimal>();

        foreach (var meter in source
            .Where(x => x.Direction == MeterDirection.Consumption)
            .GroupBy(x => x.MeterId))
        {
            var readings = meter.OrderBy(x => x.Timestamp).ToArray();
            foreach (var reading in readings)
            {
                if (reading.Quality is Enset.Domain.Data.DataQuality.Invalid
                    or Enset.Domain.Data.DataQuality.Missing)
                {
                    continue;
                }

                if (reading.ReadingType == MeterReadingType.IntervalValue
                    && reading.Quantity == MeterQuantity.Energy)
                {
                    var kwh = ToKWh(reading.Value, reading.Unit);
                    energy += kwh;
                    if (reading.IntervalSeconds is > 0)
                    {
                        powers.Add(kwh * 3600m / reading.IntervalSeconds.Value);
                    }
                    else
                    {
                        warnings.Add($"Messintervall für Meter {meter.Key} fehlt.");
                    }
                }
                else if (reading.ReadingType == MeterReadingType.Instantaneous
                    && reading.Quantity == MeterQuantity.Power)
                {
                    powers.Add(ToKw(reading.Value, reading.Unit));
                }
            }

            var cumulative = readings
                .Where(x => x.ReadingType == MeterReadingType.CumulativeValue
                    && x.Quantity == MeterQuantity.Energy)
                .ToArray();
            for (var index = 1; index < cumulative.Length; index++)
            {
                var difference = ToKWh(cumulative[index].Value, cumulative[index].Unit)
                    - ToKWh(cumulative[index - 1].Value, cumulative[index - 1].Unit);
                if (difference < 0)
                {
                    warnings.Add($"Negativer Zählerstandssprung für Meter {meter.Key}.");
                }
                else
                {
                    energy += difference;
                }
            }
        }

        return new EnergyCalculationResult(energy, powers, warnings);
    }

    private static decimal ToKWh(decimal value, string unit) =>
        unit.ToUpperInvariant() switch
        {
            "WH" => value / 1000m,
            "KWH" => value,
            "MWH" => value * 1000m,
            _ => throw new InvalidOperationException($"Inkompatible Energieeinheit '{unit}'.")
        };

    private static decimal ToKw(decimal value, string unit) =>
        unit.ToUpperInvariant() switch
        {
            "W" => value / 1000m,
            "KW" => value,
            "MW" => value * 1000m,
            _ => throw new InvalidOperationException($"Inkompatible Leistungseinheit '{unit}'.")
        };
}
