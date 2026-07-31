using Enset.Application.DataProducts.Generation.Abstractions;
using Enset.Application.DataProducts.Generation.Models;
using Enset.Application.DataProducts.Generation.Services;
using Enset.Domain.Data;

namespace Enset.Application.DataProducts.Generation.Generators;

/// <summary>
/// Reserviert die Generierung eines Energieprofils für ein Gebäude.
/// </summary>
public sealed class BuildingEnergyProfileGenerator
    : IDataProductGenerator
{
    public const string ProductCode = "BUILDING_ENERGY_PROFILE";
    private readonly IBuildingDataReader _buildingReader;
    private readonly IMeterReadingDataReader _readingReader;

    public BuildingEnergyProfileGenerator(
        IBuildingDataReader buildingReader,
        IMeterReadingDataReader readingReader)
    {
        _buildingReader = buildingReader;
        _readingReader = readingReader;
    }

    public string DefinitionCode => ProductCode;

    public async Task<DataProductGenerationAvailability> CheckInputAvailabilityAsync(
        DataProductGenerationContext context,
        CancellationToken cancellationToken = default)
    {
        if (context.PeriodFrom is null || context.PeriodTo is null
            || context.PeriodFrom >= context.PeriodTo)
        {
            return DataProductGenerationAvailability.MissingData(["Gültiger Zeitraum"]);
        }

        var building = await _buildingReader.GetAsync(
            context.DataProduct.Id, context.PeriodFrom.Value,
            context.PeriodTo.Value, cancellationToken);
        return building is null || building.MeterIds.Count == 0
            ? DataProductGenerationAvailability.MissingData(["Building-Meter"])
            : DataProductGenerationAvailability.Available();
    }

    public async Task GenerateAsync(
        DataProductGenerationContext context,
        CancellationToken cancellationToken = default)
    {
        if (context.PeriodFrom is null || context.PeriodTo is null)
        {
            throw new InvalidOperationException("Ein Zeitraum ist erforderlich.");
        }

        var building = await _buildingReader.GetAsync(
            context.DataProduct.Id, context.PeriodFrom.Value,
            context.PeriodTo.Value, cancellationToken)
            ?? throw new InvalidOperationException("Building-Scope ist ungültig.");
        var readings = await _readingReader.GetConsumptionReadingsAsync(
            context.DataProduct.Id, context.PeriodFrom.Value,
            context.PeriodTo.Value, cancellationToken);
        if (readings.Count == 0)
        {
            throw new InvalidOperationException("Keine verwendbaren Messwerte vorhanden.");
        }

        var calculation = EnergyCalculationService.Calculate(readings);
        var sortedPower = calculation.PowerKw.OrderBy(x => x).ToArray();
        var percentileIndex = sortedPower.Length == 0
            ? -1
            : (int)Math.Floor((sortedPower.Length - 1) * 0.05m);
        var valid = readings.Count(x => x.Quality is not DataQuality.Invalid
            and not DataQuality.Missing);
        var quality = decimal.Round(valid * 100m / readings.Count, 2);

        Add(context, "BUILDING_TOTAL_CONSUMPTION", calculation.TotalKWh, "kWh");
        Add(context, "BUILDING_BASE_LOAD", percentileIndex < 0 ? 0 : sortedPower[percentileIndex], "kW");
        Add(context, "BUILDING_PEAK_LOAD", sortedPower.Length == 0 ? 0 : sortedPower[^1], "kW");
        Add(context, "NUMBER_OF_METERS", readings.Select(x => x.MeterId).Distinct().Count(), "count");
        Add(context, "DATA_QUALITY", quality, "%");
        foreach (var warning in calculation.Warnings) context.Warnings.Add(warning);
        context.Quality = quality == 100m ? DataQuality.Validated : DataQuality.Estimated;
    }

    private static void Add(DataProductGenerationContext context, string key, decimal value, string unit) =>
        context.Values.Add(new Enset.Domain.DataProducts.DataProductValue
        { Key = key, NumericValue = value, Unit = unit });
}
