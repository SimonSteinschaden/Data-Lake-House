using Enset.Application.Analytics;
using Enset.Domain.Buildings;
using Enset.Domain.Customers;
using Enset.Domain.Data;
using Enset.Domain.Energy;
using Enset.Infrastructure.Analytics;
using Enset.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Enset.Import.Tests;

public sealed class AnalyticsDataProductTests
{
    [Fact]
    public async Task Portfolio_summary_returns_canonical_entity_counts()
    {
        await using var db = CreateDatabase();
        db.Customers.Add(new Customer
        {
            Id = Guid.NewGuid(),
            CustomerNumber = "C-1",
            Name = "Customer"
        });
        db.Buildings.Add(new Building
        {
            Id = Guid.NewGuid(),
            BuildingNumber = "B-1",
            Name = "Building"
        });
        await db.SaveChangesAsync();

        var product = await Service(db).GetPortfolioSummaryAsync(default);

        Assert.Equal(1, product.CustomerCount);
        Assert.Equal(1, product.BuildingCount);
        Assert.Equal(0, product.MeterCount);
    }

    [Fact]
    public async Task Load_profile_uses_only_power_and_normalizes_to_kw()
    {
        await using var db = CreateDatabase();
        var wattMeter = Meter(MeterQuantity.Power, MeterUnit.W);
        var megawattMeter = Meter(MeterQuantity.Power, MeterUnit.MW);
        var energyMeter = Meter(MeterQuantity.Energy, MeterUnit.KWh);
        db.Meters.AddRange(wattMeter, megawattMeter, energyMeter);
        var timestamp = new DateTime(2026, 1, 1, 10, 10, 0, DateTimeKind.Utc);
        db.MeterReadings.AddRange(
            Reading(wattMeter, timestamp, 2_000m, MeterReadingType.Instantaneous),
            Reading(megawattMeter, timestamp, 1m, MeterReadingType.Instantaneous),
            Reading(energyMeter, timestamp, 9_999m, MeterReadingType.IntervalValue));
        await db.SaveChangesAsync();

        var product = await Service(db).GetPortfolioLoadProfileAsync(
            new AnalyticsQuery(2026), default);

        Assert.Equal("Power", product.Quantity);
        Assert.Equal("kW", product.Unit);
        Assert.Single(product.Points);
        Assert.Equal(1_002m, product.Points[0].Value);
    }

    [Fact]
    public async Task Monthly_consumption_excludes_cumulative_values_and_normalizes_energy()
    {
        await using var db = CreateDatabase();
        var whMeter = Meter(MeterQuantity.Energy, MeterUnit.Wh);
        var mwhMeter = Meter(MeterQuantity.Energy, MeterUnit.MWh);
        db.Meters.AddRange(whMeter, mwhMeter);
        var timestamp = new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc);
        db.MeterReadings.AddRange(
            Reading(whMeter, timestamp, 2_000m, MeterReadingType.IntervalValue),
            Reading(mwhMeter, timestamp, 1m, MeterReadingType.IntervalValue),
            Reading(mwhMeter, timestamp.AddHours(1), 500m, MeterReadingType.CumulativeValue));
        await db.SaveChangesAsync();

        var product = await Service(db).GetMonthlyElectricityConsumptionAsync(
            new AnalyticsQuery(2026), default);

        Assert.Equal("Energy", product.Quantity);
        Assert.Equal("kWh", product.Unit);
        Assert.Equal(1_002m, product.Months.Single(x => x.Month == 2).Value);
        Assert.Equal(1_002m, product.TotalConsumption);
    }

    [Fact]
    public async Task Usage_type_product_is_empty_without_canonical_assignment()
    {
        await using var db = CreateDatabase();

        var product = await Service(db).GetConsumptionByUsageTypeAsync(
            new AnalyticsQuery(2026), default);

        Assert.Empty(product.UsageTypes);
    }

    private static EnsetDbContext CreateDatabase()
    {
        var options = new DbContextOptionsBuilder<EnsetDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new EnsetDbContext(options);
    }

    private static EfAnalyticsDataProductService Service(EnsetDbContext db) =>
        new(db, TimeProvider.System);

    private static Meter Meter(MeterQuantity quantity, MeterUnit unit) => new()
    {
        Id = Guid.NewGuid(),
        MeterNumber = Guid.NewGuid().ToString("N"),
        Name = "Meter",
        Medium = MeterMedium.Electricity,
        Quantity = quantity,
        Unit = unit,
        Direction = MeterDirection.Consumption,
        IsActive = true
    };

    private static MeterReading Reading(
        Meter meter,
        DateTime timestamp,
        decimal value,
        MeterReadingType readingType) => new()
    {
        MeterId = meter.Id,
        Meter = meter,
        Timestamp = timestamp,
        Value = value,
        ReadingType = readingType,
        QualityFlag = DataQuality.Measured
    };
}
