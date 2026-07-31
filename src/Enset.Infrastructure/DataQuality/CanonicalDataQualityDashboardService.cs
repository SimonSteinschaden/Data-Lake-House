using Enset.Application.CanonicalSnapshots;
using Enset.Application.Curation;
using Enset.Application.QualityManagement;
using Enset.Application.InternalDataProducts;

namespace Enset.Infrastructure.QualityManagement;

public sealed class CanonicalDataQualityDashboardService(
    ICanonicalSnapshotReader snapshots,
    IImportQualityProductService imports,
    ICurationService curation,
    TimeProvider timeProvider) : IDataQualityDashboardService
{
    public async Task<DataQualityDashboard> Get(CancellationToken cancellationToken)
    {
        var data = await snapshots.GetPortfolio(cancellationToken);
        var importQuality = await imports.GetAsync(cancellationToken);
        var review = await curation.GetStatisticsAsync(cancellationToken);
        var metrics = Metrics(data);
        return new(timeProvider.GetUtcNow().UtcDateTime,
            Average(data.Customers.Select(x => x.Quality.CompletenessPercentage)),
            Average(data.Buildings.Select(x => x.Quality.CompletenessPercentage)),
            Average(data.Meters.Select(x => x.Quality.CompletenessPercentage)),
            metrics,
            data.Customers.Select(x => x.Quality.Level)
                .Concat(data.Buildings.Select(x => x.Quality.Level))
                .Concat(data.Meters.Select(x => x.Quality.Level))
                .GroupBy(x => x.ToString()).Select(x => new QualityDistribution(x.Key, x.Count())).ToArray(),
            Suitability(data),
            importQuality.TotalOpenIssues,
            review.OpenTasks);
    }

    public async Task<QualityMetric?> GetMetric(string code, CancellationToken cancellationToken) =>
        (await Get(cancellationToken)).Metrics.FirstOrDefault(x =>
            x.Code.Equals(code, StringComparison.OrdinalIgnoreCase));

    public async Task<IReadOnlyList<QualityTrendPoint>> GetTrend(string code, int days,
        CancellationToken cancellationToken)
    {
        var metric = await GetMetric(code, cancellationToken);
        if (metric is null) return [];
        var today = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
        return Enumerable.Range(0, Math.Clamp(days, 1, 90)).Reverse()
            .Select(x => new QualityTrendPoint(today.AddDays(-x), metric.Count)).ToArray();
    }

    private static IReadOnlyList<QualityMetric> Metrics(CanonicalSnapshotSet data)
    {
        var noCustomer = data.Buildings.Where(x => x.CustomerId is null).ToArray();
        var noBuilding = data.Meters.Where(x => x.BuildingId is null).ToArray();
        var noCarrier = data.Meters.Where(x => x.Medium is null).ToArray();
        var noAnnual = data.Meters.Where(x => x.Readings.AnnualValue is null).ToArray();
        var incomplete = data.Meters.Where(x =>
            x.Readings.AnnualValueStatus == AnnualValueStatus.IncompleteYear).ToArray();
        var invalid = data.Meters.Where(x => x.Readings.InvalidCount > 0).ToArray();
        var mixed = data.Meters.Where(x => x.Readings.MeasurementCount > 1 &&
                                          x.Readings.IntervalSeconds is null).ToArray();
        var unknownUnit = data.Meters.Where(x =>
            x.Unit is null or Enset.Domain.Energy.MeterUnit.Unknown ||
            x.Quantity is null or Enset.Domain.Energy.MeterQuantity.Unknown).ToArray();
        return
        [
            M("MISSING_CUSTOMER_ASSIGNMENT","Fehlende Kundenzuordnung","Objekte ohne eindeutigen Kunden","Error",noCustomer.Length,noCustomer.Select(x=>x.CustomerId),noCustomer.Select(x=>(Guid?)x.BuildingId),[],"Zuordnung herstellen","/tools/assignments"),
            M("MISSING_BUILDING_ASSIGNMENT","Fehlende Objektzuordnung","Zählpunkte ohne Objekt","Error",noBuilding.Length,noBuilding.Select(x=>x.CustomerId),noBuilding.Select(x=>x.BuildingId),noBuilding.Select(x=>x.MeterId),"Zuordnung herstellen","/tools/assignments"),
            M("MISSING_ENERGY_CARRIER","Fehlender Energieträger","Zählpunkte ohne Energieträger","Warning",noCarrier.Length,noCarrier.Select(x=>x.CustomerId),noCarrier.Select(x=>x.BuildingId),noCarrier.Select(x=>x.MeterId),"Wert prüfen","/tools/data-review"),
            M("MISSING_ANNUAL_VALUE","Fehlende Jahreswerte","Kein vollständiger Jahreswert verfügbar","Warning",noAnnual.Length,noAnnual.Select(x=>x.CustomerId),noAnnual.Select(x=>x.BuildingId),noAnnual.Select(x=>x.MeterId),"Datensatz öffnen","/meters"),
            M("INCOMPLETE_PERIODS","Fehlende Messperioden","Weniger als ein vollständiges Jahr","Warning",incomplete.Length,incomplete.Select(x=>x.CustomerId),incomplete.Select(x=>x.BuildingId),incomplete.Select(x=>x.MeterId),"Import erneut starten","/imports"),
            M("INVALID_READINGS","Ungültige Messwerte","Messwerte mit ungültigem Qualitätsstatus","Error",invalid.Sum(x=>x.Readings.InvalidCount),invalid.Select(x=>x.CustomerId),invalid.Select(x=>x.BuildingId),invalid.Select(x=>x.MeterId),"Datensatz öffnen","/meters"),
            M("MIXED_INTERVALS","Gemischte Intervalle","Kein konstantes Messintervall nachweisbar","Warning",mixed.Length,mixed.Select(x=>x.CustomerId),mixed.Select(x=>x.BuildingId),mixed.Select(x=>x.MeterId),"Wert prüfen","/tools/data-review"),
            M("UNKNOWN_UNITS","Unbekannte Einheiten","Einheit oder Messgröße ist nicht eindeutig","Error",unknownUnit.Length,unknownUnit.Select(x=>x.CustomerId),unknownUnit.Select(x=>x.BuildingId),unknownUnit.Select(x=>x.MeterId),"Wert prüfen","/tools/data-review"),
            M("TIME_SERIES_GAPS","Zeitreihenlücken","Vollständigkeit liegt unter 100 %","Warning",data.Meters.Count(x=>x.Readings.CompletenessPercentage is < 100),data.Meters.Where(x=>x.Readings.CompletenessPercentage is < 100).Select(x=>x.CustomerId),data.Meters.Where(x=>x.Readings.CompletenessPercentage is < 100).Select(x=>x.BuildingId),data.Meters.Where(x=>x.Readings.CompletenessPercentage is < 100).Select(x=>x.MeterId),"Datensatz öffnen","/meters"),
            M("DUPLICATE_READINGS","Doppelte Messwerte","Im kanonischen Snapshot sind keine Dubletten ausgewiesen","Info",0,[],[],[],"Import prüfen","/imports"),
            M("INVALID_TIMESTAMPS","Ungültige Zeitstempel","Ungültige Zeitstempel werden vor dem Snapshot abgewiesen","Info",0,[],[],[],"Import prüfen","/imports"),
            M("IMPLAUSIBLE_VALUES","Negative oder unplausible Werte","Keine separate Plausibilitätskennzahl im Snapshot","Info",0,[],[],[],"Datenprüfung öffnen","/tools/data-review")
        ];
    }

    private static QualityMetric M(string code, string name, string description,
        string severity, long count, IEnumerable<Guid?> customerIds,
        IEnumerable<Guid?> buildingIds, IEnumerable<Guid> meterIds, string action, string url) =>
        new(code, name, description, severity, count, "stable",
            customerIds.Where(x => x.HasValue).Distinct().Count(),
            buildingIds.Where(x => x.HasValue).Distinct().Count(),
            meterIds.Distinct().Count(), action, url);

    private static int Average(IEnumerable<int> values)
    {
        var list = values.ToArray();
        return list.Length == 0 ? 0 : (int)Math.Round(list.Average());
    }

    private static IReadOnlyList<SuitabilityMetric> Suitability(CanonicalSnapshotSet data)
    {
        var all = data.Customers.Select(x => x.Suitability)
            .Concat(data.Buildings.Select(x => x.Suitability))
            .Concat(data.Meters.Select(x => x.Suitability)).ToArray();
        static SuitabilityMetric S(string name, IEnumerable<SuitabilityStatus> values)
        {
            var list = values.ToArray();
            return new(name, list.Count(x => x == SuitabilityStatus.Suitable),
                list.Count(x => x == SuitabilityStatus.NotSuitable));
        }
        return [S("LEB",all.Select(x=>x.Leb)),S("Navigator",all.Select(x=>x.Navigator)),
            S("Benchmark",all.Select(x=>x.Benchmark)),S("ISO 50001",all.Select(x=>x.Iso50001))];
    }
}
