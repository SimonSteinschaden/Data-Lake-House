using Enset.Application.DataProducts.Generation.Abstractions;
using Enset.Application.DataProducts.Generation.Models;
using Enset.Application.DataProducts.Generation.Services;
using Enset.Domain.Data;
using Enset.Domain.DataProducts;

namespace Enset.Application.DataProducts.Generation.Generators;

public sealed class MeterConsumptionSummaryGenerator
    : IDataProductGenerator
{
    public const string ProductCode = "METER_CONSUMPTION_SUMMARY";

    private readonly IMeterReadingDataReader _meterReadingDataReader;

    public MeterConsumptionSummaryGenerator(
        IMeterReadingDataReader meterReadingDataReader)
    {
        _meterReadingDataReader = meterReadingDataReader;
    }

    public string DefinitionCode => ProductCode;

    public async Task<DataProductGenerationAvailability>
        CheckInputAvailabilityAsync(
            DataProductGenerationContext context,
            CancellationToken cancellationToken = default)
    {
        if (context.PeriodFrom is null || context.PeriodTo is null)
        {
            return DataProductGenerationAvailability.MissingData(
                "PeriodFrom",
                "PeriodTo");
        }

        if (context.PeriodFrom >= context.PeriodTo)
        {
            return DataProductGenerationAvailability.MissingData(
                "Der angegebene Zeitraum ist ungültig.");
        }

        var readings =
            await _meterReadingDataReader.GetConsumptionReadingsAsync(
                context.DataProduct.Id,
                context.PeriodFrom.Value,
                context.PeriodTo.Value,
                cancellationToken);

        if (readings.Count == 0)
        {
            return DataProductGenerationAvailability.MissingData(
                "MeterReadings");
        }

        return DataProductGenerationAvailability.Available();
    }

    public async Task GenerateAsync(
        DataProductGenerationContext context,
        CancellationToken cancellationToken = default)
    {
        if (context.PeriodFrom is null || context.PeriodTo is null)
        {
            throw new InvalidOperationException(
                "Für die Verbrauchszusammenfassung ist ein Zeitraum erforderlich.");
        }

        var readings =
            await _meterReadingDataReader.GetConsumptionReadingsAsync(
                context.DataProduct.Id,
                context.PeriodFrom.Value,
                context.PeriodTo.Value,
                cancellationToken);

        if (readings.Count == 0)
        {
            throw new InvalidOperationException(
                "Für den gewählten Zeitraum wurden keine Messwerte gefunden.");
        }

        var calculation = EnergyCalculationService.Calculate(readings);
        foreach (var warning in calculation.Warnings)
        {
            context.Warnings.Add(warning);
        }

        var validCount = readings.Count(x => x.Quality is not DataQuality.Invalid
            and not DataQuality.Missing);
        var quality = readings.Count == 0
            ? 0m
            : decimal.Round(validCount * 100m / readings.Count, 2);

        context.Values.Add(
            CreateValue(
                key: "TOTAL_CONSUMPTION",
                value: calculation.TotalKWh,
                unit: "kWh"));

        context.Values.Add(
            CreateValue(
                key: "READING_COUNT",
                value: readings.Count,
                unit: "count"));

        context.Values.Add(CreateValue("DATA_QUALITY", quality, "%"));
        context.Quality = quality == 100m ? DataQuality.Validated : DataQuality.Estimated;

        context.Values.Add(
            CreateValue(
                key: "METER_COUNT",
                value: readings
                    .Select(reading => reading.MeterId)
                    .Distinct()
                    .Count(),
                unit: "count"));
    }

    private static DataProductValue CreateValue(
        string key,
        decimal value,
        string unit)
    {
        return new DataProductValue
        {
            Key = key,
            NumericValue = value,
            Unit = unit
        };
    }
}
