using Enset.Application.CanonicalSnapshots;
using Enset.Application.ObjectAnalytics;
using Enset.Application.Reporting;
using Enset.Domain.Curation;
using Enset.Domain.Data;
using Enset.Domain.Energy;
using Enset.Domain.GoldProfiles;
using Enset.Infrastructure.ObjectAnalytics;
using Enset.Infrastructure.Reporting;
using Xunit;

namespace Enset.Import.Tests;

public sealed class ObjectAnalyticsAndReportingTests
{
    [Fact]
    public async Task Analytics_ComputesOnlySupportedCanonicalKpis()
    {
        var service = new CanonicalObjectAnalyticsService(
            new SnapshotReader(Portfolio()), TimeProvider.System);
        var result = await service.Analyze(
            BuildingId,
            Query(),
            default);

        Assert.NotNull(result);
        Assert.Equal(30m, result.TotalConsumption.Value);
        Assert.Equal(30m, result.ElectricityConsumption.Value);
        Assert.Equal(5m, result.PeakLoad.Value);
        Assert.Null(result.EnergyCosts.Value);
        Assert.Null(result.Co2Emissions.Value);
        Assert.Equal("NotAvailable", result.EnergyCosts.Status);
        Assert.Equal(2, result.MonthlyConsumption.Count);
    }

    [Fact]
    public async Task Search_IsCaseInsensitiveAcrossCanonicalFields()
    {
        var service = new CanonicalObjectAnalyticsService(
            new SnapshotReader(Portfolio()), TimeProvider.System);

        var result = await service.Search(
            Query() with { Search = "HAUPTSTRAßE" },
            default);

        var item = Assert.Single(result.Items);
        Assert.Equal(BuildingId, item.BuildingId);
    }

    [Fact]
    public async Task Reports_AreVersionedAndExportFrozenProduct()
    {
        var root = Path.Combine(
            Path.GetTempPath(), $"enset-reports-{Guid.NewGuid():N}");
        try
        {
            var analytics = new CanonicalObjectAnalyticsService(
                new SnapshotReader(Portfolio()), TimeProvider.System);
            var reports = new FileReportService(
                root, analytics, TimeProvider.System);
            var request = new CreateReportRequest(
                ReportType.ObjectEnergy,
                BuildingId,
                From,
                To,
                "Test recipient");

            var first = await reports.Create(request, default);
            var second = await reports.Create(request, default);
            var pdf = await reports.Export(first.ReportId, "pdf", default);
            var excel = await reports.Export(first.ReportId, "xlsx", default);

            Assert.Equal(1, first.Version);
            Assert.Equal(2, second.Version);
            Assert.StartsWith("%PDF-", System.Text.Encoding.ASCII.GetString(
                pdf!.Content, 0, 5));
            Assert.True(excel!.Content.Length > 1000);
            Assert.Equal(2, (await reports.List(default)).Count);
        }
        finally
        {
            if (Directory.Exists(root))
                Directory.Delete(root, true);
        }
    }

    [Fact]
    public void ObjectAnalytics_HasNoDbContextDependency()
    {
        var constructor = typeof(CanonicalObjectAnalyticsService)
            .GetConstructors().Single();
        Assert.Contains(constructor.GetParameters(),
            x => x.ParameterType == typeof(ICanonicalSnapshotReader));
        Assert.DoesNotContain(constructor.GetParameters(),
            x => x.ParameterType.Name.Contains("DbContext"));
    }

    private static readonly Guid BuildingId = Guid.Parse(
        "11111111-1111-1111-1111-111111111111");
    private static readonly Guid MeterId = Guid.Parse(
        "22222222-2222-2222-2222-222222222222");
    private static readonly DateTime From =
        new(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime To =
        new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    private static ObjectSearchQuery Query() =>
        new(null, null, null, null, null, From, To);

    private static CanonicalSnapshotSet Portfolio()
    {
        var building = new BuildingCanonicalSnapshot(
            BuildingId, "OBJ-1", "Rathaus", null, null, "Gemeinde",
            "Office", "Administration", null, "Hauptstraße", "1",
            "3100", "St. Pölten", "G-1", "St. Pölten", 2000, null,
            1000, 900, 800, 600, null, null, null, true,
            Quality(), Suitability(), Version());
        var meter = new MeterCanonicalSnapshot(
            MeterId, "AT001", "Strom", BuildingId, "OBJ-1", "Rathaus",
            null, "Gemeinde", MeterMedium.Electricity,
            MeterDirection.Consumption, MeterQuantity.Energy,
            MeterUnit.KWh, null, true,
            new CanonicalReadingSummary(
                3, From, From.AddMonths(1), MeterUnit.KWh,
                MeterReadingType.IntervalValue, MeterQuantity.Energy,
                900, 0, 0, 0, 3, 0, 100, null,
                AnnualValueStatus.IncompleteYear),
            Quality(), Suitability(), Version())
        {
            ReadingValues =
            [
                new(From, 10, MeterUnit.KWh,
                    MeterReadingType.IntervalValue, 900,
                    "Measured", "Test", false),
                new(From.AddMonths(1), 20, MeterUnit.KWh,
                    MeterReadingType.IntervalValue, 900,
                    "Measured", "Test", false)
            ]
        };
        var power = meter with
        {
            MeterId = Guid.NewGuid(),
            MeterNumber = "AT002",
            Quantity = MeterQuantity.Power,
            Unit = MeterUnit.KW,
            ReadingValues =
            [
                new(From, 5, MeterUnit.KW,
                    MeterReadingType.IntervalValue, 900,
                    "Measured", "Test", false)
            ]
        };
        return new([], [building], [meter, power], []);
    }

    private static SnapshotQuality Quality() =>
        new(DataMaturityLevel.Silver, 90, 100, 100, 50);

    private static SnapshotSuitability Suitability() =>
        new(SuitabilityStatus.Suitable, SuitabilityStatus.Suitable,
            SuitabilityStatus.Suitable, SuitabilityStatus.Suitable);

    private static CanonicalVersion Version() =>
        new(Guid.NewGuid(), 1, From, "Test",
            GoldProfileReleaseStatus.Draft);

    private sealed class SnapshotReader(CanonicalSnapshotSet portfolio)
        : ICanonicalSnapshotReader
    {
        public Task<CanonicalSnapshotSet> GetPortfolio(
            CancellationToken cancellationToken) =>
            Task.FromResult(portfolio);
        public Task<IReadOnlyList<CustomerCanonicalSnapshot>> GetCustomers(
            IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<CustomerCanonicalSnapshot>>([]);
        public Task<IReadOnlyList<BuildingCanonicalSnapshot>> GetBuildings(
            IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<BuildingCanonicalSnapshot>>(
                portfolio.Buildings.Where(x => ids.Contains(x.BuildingId)).ToArray());
        public Task<IReadOnlyList<MeterCanonicalSnapshot>> GetMeters(
            IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<MeterCanonicalSnapshot>>(
                portfolio.Meters.Where(x => ids.Contains(x.MeterId)).ToArray());
        public Task<CustomerCanonicalSnapshot?> GetCustomer(
            Guid id, CancellationToken cancellationToken) =>
            Task.FromResult<CustomerCanonicalSnapshot?>(null);
        public Task<BuildingCanonicalSnapshot?> GetBuilding(
            Guid id, CancellationToken cancellationToken) =>
            Task.FromResult(portfolio.Buildings.SingleOrDefault(
                x => x.BuildingId == id));
        public Task<MeterCanonicalSnapshot?> GetMeter(
            Guid id, CancellationToken cancellationToken) =>
            Task.FromResult(portfolio.Meters.SingleOrDefault(
                x => x.MeterId == id));
    }
}
