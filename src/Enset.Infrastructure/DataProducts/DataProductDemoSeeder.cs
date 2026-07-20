using Enset.Domain.Buildings;
using Enset.Domain.Customers;
using Enset.Domain.Data;
using Enset.Domain.DataProducts;
using Enset.Domain.Energy;
using Enset.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Enset.Infrastructure.DataProducts;

public static class DataProductDemoSeeder
{
    public static async Task SeedDataProductDemoAsync(
        this IServiceProvider services,
        CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<EnsetDbContext>();
        await db.Database.MigrateAsync(cancellationToken);
        if (await db.DataProductDefinitions.AnyAsync(cancellationToken)) return;

        var customer = new Customer { Id = Guid.Parse("10000000-0000-0000-0000-000000000001"), CustomerNumber = "DEMO-001", Name = "ENSET Demo Kunde", Type = CustomerType.Company };
        var building = new Building { Id = Guid.Parse("20000000-0000-0000-0000-000000000001"), BuildingNumber = "BLD-DEMO-001", Name = "ENSET Demo Gebäude" };
        var meter1 = CreateMeter("30000000-0000-0000-0000-000000000001", "M-DEMO-01", building);
        var meter2 = CreateMeter("30000000-0000-0000-0000-000000000002", "M-DEMO-02", building);
        var meterDefinition = Definition("40000000-0000-0000-0000-000000000001", "METER_CONSUMPTION_SUMMARY", "Meter Consumption Summary", DataProductScopeType.Meter);
        var buildingDefinition = Definition("40000000-0000-0000-0000-000000000002", "BUILDING_ENERGY_PROFILE", "Building Energy Profile", DataProductScopeType.Building);
        var meterProduct = Product("50000000-0000-0000-0000-000000000001", "DP-METER-DEMO", meterDefinition);
        var buildingProduct = Product("50000000-0000-0000-0000-000000000002", "DP-BUILDING-DEMO", buildingDefinition);
        meterProduct.CustomerAssignments.Add(Assignment(meterProduct, customer));
        buildingProduct.CustomerAssignments.Add(Assignment(buildingProduct, customer));
        meterProduct.ScopeAssignments.Add(new DataProductScopeAssignment { DataProduct = meterProduct, ScopeType = DataProductScopeType.Meter, Meter = meter1 });
        buildingProduct.ScopeAssignments.Add(new DataProductScopeAssignment { DataProduct = buildingProduct, ScopeType = DataProductScopeType.Building, Building = building });

        db.AddRange(customer, building, meter1, meter2, meterDefinition, buildingDefinition, meterProduct, buildingProduct);
        var start = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        for (var hour = 0; hour < 24 * 30; hour++)
        {
            db.MeterReadings.Add(Reading(meter1, start.AddHours(hour), 1.25m + hour % 7 / 10m));
            db.MeterReadings.Add(Reading(meter2, start.AddHours(hour), .55m + hour % 5 / 10m));
        }
        await db.SaveChangesAsync(cancellationToken);
    }

    private static Meter CreateMeter(string id, string number, Building building) => new() { Id = Guid.Parse(id), MeterNumber = number, Name = number, Building = building, Medium = MeterMedium.Electricity, Quantity = MeterQuantity.Energy, Unit = MeterUnit.KWh, Direction = MeterDirection.Consumption, Type = MeterType.Physical };
    private static MeterReading Reading(Meter meter, DateTime timestamp, decimal value) => new() { Meter = meter, MeterId = meter.Id, Timestamp = timestamp, Value = value, ReadingType = MeterReadingType.IntervalValue, QualityFlag = DataQuality.Validated, IntervalSeconds = 3600 };
    private static DataProductDefinition Definition(string id, string code, string name, DataProductScopeType scope) { var definition = new DataProductDefinition { Id = Guid.Parse(id), Code = code, Name = name, Category = DataProductCategory.Energy, ResultType = DataProductResultType.Calculated }; definition.AllowedScopes.Add(new DataProductDefinitionScope { DataProductDefinition = definition, ScopeType = scope }); return definition; }
    private static DataProduct Product(string id, string number, DataProductDefinition definition) => new() { Id = Guid.Parse(id), ProductNumber = number, Name = definition.Name, Definition = definition, Status = DataProductStatus.Active };
    private static DataProductCustomerAssignment Assignment(DataProduct product, Customer customer) => new() { DataProduct = product, Customer = customer, Role = DataProductCustomerRole.Owner, ValidFrom = DateTime.UtcNow.AddYears(-1) };
}
