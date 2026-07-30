using System.Globalization;
using System.Text;
using System.Text.Json;
using ClosedXML.Excel;
using Enset.Application.CanonicalSnapshots;
using Enset.Application.DataProducts.Catalog;

namespace Enset.Infrastructure.DataProducts;

public sealed class CanonicalDataProductCatalogService(
    ICanonicalSnapshotReader snapshots, TimeProvider timeProvider)
    : IDataProductCatalogService
{
    private static readonly IReadOnlyList<DataProductMetadata> Catalog = CreateCatalog();

    public IReadOnlyList<DataProductCatalogItem> List(string? search = null, string? category = null) =>
        Catalog.Where(x => string.IsNullOrWhiteSpace(search) ||
                           $"{x.Code} {x.Name} {x.GermanName} {x.Description}".Contains(search,
                               StringComparison.OrdinalIgnoreCase))
            .Where(x => string.IsNullOrWhiteSpace(category) ||
                        x.Category.Equals(category, StringComparison.OrdinalIgnoreCase))
            .Select(x => new DataProductCatalogItem(x, timeProvider.GetUtcNow().UtcDateTime))
            .ToArray();

    public DataProductCatalogItem? Get(string code) =>
        Catalog.FirstOrDefault(x => x.Code.Equals(code, StringComparison.OrdinalIgnoreCase)) is { } item
            ? new(item, timeProvider.GetUtcNow().UtcDateTime) : null;

    public IReadOnlyList<DataProductDependency> Dependencies() =>
        Catalog.Select(x => new DataProductDependency(x.Code,
            x.UsedProducts.Count == 0 ? ["CANONICAL_SNAPSHOTS"] : x.UsedProducts)).ToArray();

    public async Task<DataProductPreview?> Preview(string code, Guid? customerId,
        Guid? buildingId, DateTime? fromUtc, DateTime? toUtc, int limit,
        CancellationToken cancellationToken)
    {
        var metadata = Catalog.FirstOrDefault(x =>
            x.Code.Equals(code, StringComparison.OrdinalIgnoreCase));
        if (metadata is null) return null;
        var portfolio = await snapshots.GetPortfolio(cancellationToken);
        var meters = portfolio.Meters.Where(x =>
            (!customerId.HasValue || x.CustomerId == customerId) &&
            (!buildingId.HasValue || x.BuildingId == buildingId)).ToArray();
        var rows = BuildRows(metadata.Code, portfolio, meters, fromUtc, toUtc)
            .Take(Math.Clamp(limit, 1, 500)).ToArray();
        return new(metadata, timeProvider.GetUtcNow().UtcDateTime, rows);
    }

    public async Task<DataProductExport?> Export(string code, string format,
        Guid? customerId, Guid? buildingId, DateTime? fromUtc, DateTime? toUtc,
        CancellationToken cancellationToken)
    {
        var preview = await Preview(code, customerId, buildingId, fromUtc, toUtc, 500,
            cancellationToken);
        if (preview is null) return null;
        format = format.Trim().ToLowerInvariant();
        var safeName = code.ToLowerInvariant().Replace('_', '-');
        return format switch
        {
            "json" => new($"{safeName}.json", "application/json",
                JsonSerializer.SerializeToUtf8Bytes(preview, new JsonSerializerOptions { WriteIndented = true })),
            "csv" => new($"{safeName}.csv", "text/csv; charset=utf-8", Csv(preview.Rows)),
            "xlsx" or "excel" => new($"{safeName}.xlsx",
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", Excel(preview)),
            _ => throw new ArgumentException($"Unsupported export format '{format}'.")
        };
    }

    private static IEnumerable<IReadOnlyDictionary<string, object?>> BuildRows(
        string code, CanonicalSnapshotSet portfolio, IReadOnlyList<MeterCanonicalSnapshot> meters,
        DateTime? fromUtc, DateTime? toUtc)
    {
        if (code == "ENERGY_SYSTEM_INVENTORY")
            return portfolio.EnergySystems.Select(x => Row(("energySystemId", x.EnergySystemId),
                ("buildingId", x.BuildingId), ("type", x.Type), ("energyCarrier", x.EnergyCarrier),
                ("purpose", x.Purpose), ("installedPower", x.InstalledPower),
                ("qualityLevel", x.Quality.Level.ToString())));
        if (code is "DATA_QUALITY_SUMMARY" or "MISSING_DATA_SUMMARY")
            return portfolio.Buildings.Select(x => Row(("buildingId", x.BuildingId),
                ("object", x.Name), ("customer", x.CustomerName),
                ("completeness", x.Quality.CompletenessPercentage),
                ("qualityLevel", x.Quality.Level.ToString()),
                ("missingFields", string.Join(", ", Missing(x)))));
        if (code is "BUILDING_ENERGY_COST_SUMMARY" or "BUILDING_CO2_SUMMARY")
            return portfolio.Buildings.Select(x => Row(("buildingId", x.BuildingId),
                ("object", x.Name), ("customer", x.CustomerName),
                (code == "BUILDING_CO2_SUMMARY" ? "co2" : "cost", null),
                ("status", "NotAvailable"),
                ("reason", code == "BUILDING_CO2_SUMMARY"
                    ? "No canonical emission factors are available."
                    : "No canonical tariff data are available."),
                ("qualityLevel", x.Quality.Level.ToString())));
        if (code is "BUILDING_ENERGY_PROFILE" or "BUILDING_ANNUAL_ENERGY_BALANCE")
            return portfolio.Buildings.Select(x =>
            {
                var assigned = meters.Where(m => m.BuildingId == x.BuildingId).ToArray();
                return Row(("buildingId", x.BuildingId), ("object", x.Name),
                    ("customer", x.CustomerName), ("periodFrom", fromUtc),
                    ("periodTo", toUtc), ("meterCount", assigned.Length),
                    ("annualValue", assigned.Where(m => m.Readings.AnnualValueStatus == AnnualValueStatus.CompleteYear)
                        .Sum(m => m.Readings.AnnualValue ?? 0)),
                    ("qualityLevel", x.Quality.Level.ToString()),
                    ("suitability", x.Suitability.Benchmark.ToString()));
            });
        if (code == "PEAK_LOAD_PROFILE")
            return meters.Select(x => Row(("meterId", x.MeterId), ("meterNumber", x.MeterNumber),
                ("peakValue", x.ReadingValues
                    .Where(r => (!fromUtc.HasValue || r.Timestamp >= fromUtc) &&
                                (!toUtc.HasValue || r.Timestamp <= toUtc))
                    .Select(r => (decimal?)r.Value).Max()),
                ("unit", x.Unit?.ToString()), ("periodFrom", fromUtc), ("periodTo", toUtc)));
        if (code == "LOAD_DURATION_CURVE")
            return meters.SelectMany(x => x.ReadingValues
                    .Where(r => (!fromUtc.HasValue || r.Timestamp >= fromUtc) &&
                                (!toUtc.HasValue || r.Timestamp <= toUtc))
                    .OrderByDescending(r => r.Value).Select((r, rank) =>
                        Row(("meterId", x.MeterId), ("rank", rank + 1),
                            ("value", r.Value), ("unit", r.Unit?.ToString()))));
        if (code is "PORTFOLIO_ENERGY_SUMMARY" or "ENERGY_CARRIER_BREAKDOWN"
            or "USAGE_BREAKDOWN" or "BENCHMARK_PROFILE" or "ISO_50001_ENPI")
            return meters.GroupBy(x => code == "USAGE_BREAKDOWN"
                    ? x.UsageType ?? "Unbekannt" : x.Medium?.ToString() ?? "Unbekannt")
                .Select(g => Row(("group", g.Key), ("meterCount", g.Count()),
                    ("completeAnnualValue", g.Where(x => x.Readings.AnnualValueStatus ==
                        AnnualValueStatus.CompleteYear).Sum(x => x.Readings.AnnualValue ?? 0)),
                    ("unit", g.Select(x => x.Unit?.ToString()).Distinct().Count() == 1
                        ? g.First().Unit?.ToString() : null)));
        return meters.Select(x => Row(("meterId", x.MeterId), ("meterNumber", x.MeterNumber),
            ("buildingId", x.BuildingId), ("object", x.BuildingName), ("customer", x.CustomerName),
            ("energyCarrier", x.Medium?.ToString()), ("measurementCount", x.Readings.MeasurementCount),
            ("periodStart", x.Readings.PeriodStart), ("periodEnd", x.Readings.PeriodEnd),
            ("annualValue", x.Readings.AnnualValue), ("annualValueStatus", x.Readings.AnnualValueStatus.ToString()),
            ("completeness", x.Readings.CompletenessPercentage), ("qualityLevel", x.Quality.Level.ToString())));
    }

    private static string[] Missing(BuildingCanonicalSnapshot x) =>
        new[] { (x.CustomerId is null, "Kunde"), (string.IsNullOrWhiteSpace(x.UsageType), "Nutzung"),
            (string.IsNullOrWhiteSpace(x.BuildingType), "Gebäudetyp"),
            (!x.HeatedArea.HasValue, "Fläche") }.Where(x => x.Item1).Select(x => x.Item2).ToArray();

    private static IReadOnlyDictionary<string, object?> Row(params (string Key, object? Value)[] values) =>
        values.ToDictionary(x => x.Key, x => x.Value);

    private static byte[] Csv(IReadOnlyList<IReadOnlyDictionary<string, object?>> rows)
    {
        if (rows.Count == 0) return Encoding.UTF8.GetBytes("");
        var columns = rows.SelectMany(x => x.Keys).Distinct().ToArray();
        static string Cell(object? value) => $"\"{Convert.ToString(value, CultureInfo.InvariantCulture)?.Replace("\"", "\"\"")}\"";
        var lines = new[] { string.Join(';', columns.Select(Cell)) }
            .Concat(rows.Select(row => string.Join(';', columns.Select(c =>
                Cell(row.GetValueOrDefault(c))))));
        return Encoding.UTF8.GetBytes(string.Join(Environment.NewLine, lines));
    }

    private static byte[] Excel(DataProductPreview preview)
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.AddWorksheet("Data Product");
        var columns = preview.Rows.SelectMany(x => x.Keys).Distinct().ToArray();
        for (var c = 0; c < columns.Length; c++) sheet.Cell(1, c + 1).Value = columns[c];
        for (var r = 0; r < preview.Rows.Count; r++)
            for (var c = 0; c < columns.Length; c++)
                sheet.Cell(r + 2, c + 1).Value = Convert.ToString(
                    preview.Rows[r].GetValueOrDefault(columns[c]), CultureInfo.InvariantCulture) ?? "";
        sheet.Row(1).Style.Font.Bold = true;
        sheet.Columns().AdjustToContents();
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    private static IReadOnlyList<DataProductMetadata> CreateCatalog()
    {
        static DataProductMetadata M(string code, string name, string de, string category,
            string description, string aggregation, string[]? products = null) => new(
            code, name, de, description, category, new(1, 0, 0), "ENSET Data Management",
            ["CanonicalSnapshotSet"], products ?? [], ["documented JSON object rows"],
            "ICanonicalSnapshotReader", "CanonicalVersion", "Bronze/Silver/Gold",
            "LEB, Navigator, Benchmark, ISO 50001", "On demand",
            ["json", "csv", "xlsx"], $"/api/v1/data-product-catalog/{code.ToLowerInvariant()}",
            "freely selectable; no extrapolation", aggregation,
            "Missing values remain null and incomplete years are not extrapolated.",
            "Import → relational store → canonical snapshots → data product");
        return
        [
            M("BUILDING_ENERGY_PROFILE","Building Energy Profile","Gebäudeenergieprofil","Gebäude","Energie- und Qualitätsprofil eines Objekts.","Building"),
            M("METER_CONSUMPTION_SUMMARY","Meter Consumption Summary","Zählpunkt-Verbrauchsübersicht","Zeitreihen","Kanonische Verbrauchs- und Vollständigkeitsübersicht.","Meter"),
            M("BUILDING_ANNUAL_ENERGY_BALANCE","Building Annual Energy Balance","Jahresenergiebilanz Gebäude","Gebäude","Vollständige Jahreswerte ohne Hochrechnung.","Building/year",["BUILDING_ENERGY_PROFILE"]),
            M("BUILDING_ENERGY_COST_SUMMARY","Building Energy Cost Summary","Energiekostenübersicht Gebäude","Gebäude","Kostenprojektion; Werte bleiben ohne Kosteneingang leer.","Building/year",["BUILDING_ANNUAL_ENERGY_BALANCE"]),
            M("BUILDING_CO2_SUMMARY","Building CO2 Summary","CO₂-Übersicht Gebäude","Nachhaltigkeit","Emissionsprojektion; keine erfundenen Faktoren.","Building/year",["BUILDING_ANNUAL_ENERGY_BALANCE"]),
            M("PEAK_LOAD_PROFILE","Peak Load Profile","Spitzenlastprofil","Zeitreihen","Spitzenlasten aus kanonischen Messwerten.","Meter/interval"),
            M("LOAD_DURATION_CURVE","Load Duration Curve","Lastdauerlinie","Zeitreihen","Absteigend sortierte kanonische Lastwerte.","Meter/interval"),
            M("ENERGY_CARRIER_BREAKDOWN","Energy Carrier Breakdown","Energieträgerverteilung","Analysen","Verteilung nach Energieträger.","Portfolio/carrier"),
            M("USAGE_BREAKDOWN","Usage Breakdown","Nutzungsverteilung","Analysen","Verteilung nach Nutzungstyp.","Portfolio/usage"),
            M("ENERGY_SYSTEM_INVENTORY","Energy System Inventory","Energiesystem-Inventar","Inventar","Kanonisches Inventar vorhandener Energiesysteme.","Energy system"),
            M("DATA_QUALITY_SUMMARY","Data Quality Summary","Datenqualitätsübersicht","Qualität","Qualitätsstufen und Vollständigkeit.","Entity"),
            M("MISSING_DATA_SUMMARY","Missing Data Summary","Fehlende-Daten-Übersicht","Qualität","Fehlende fachliche Pflichtinformationen.","Entity"),
            M("METER_READING_COMPLETENESS","Meter Reading Completeness","Messwertvollständigkeit","Qualität","Messwertabdeckung und Lückenindikatoren.","Meter"),
            M("BENCHMARK_PROFILE","Benchmark Profile","Benchmarkprofil","Benchmark","Vergleichsfähige Energiekennzahlen.","Portfolio/group"),
            M("PORTFOLIO_ENERGY_SUMMARY","Portfolio Energy Summary","Portfolio-Energieübersicht","Benchmark","Konsolidierte vollständige Jahreswerte.","Portfolio"),
            M("RENEWABLE_GENERATION_SUMMARY","Renewable Generation Summary","Erneuerbare-Erzeugungsübersicht","Nachhaltigkeit","Erzeugungswerte erneuerbarer Anlagen.","Meter"),
            M("ISO_50001_ENPI","ISO 50001 Energy Performance Indicators","ISO 50001 Energiekennzahlen","Nachhaltigkeit","Nachvollziehbare EnPI-Projektion.","Portfolio/group",["PORTFOLIO_ENERGY_SUMMARY"]),
            M("LEB_EXPORT_DATASET","LEB Export Dataset","LEB-Exportdatensatz","Export","Kanonischer Datensatz für den bestehenden LEB-Export.","LEB contract")
        ];
    }
}
