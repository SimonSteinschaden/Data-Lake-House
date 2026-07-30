using Enset.Application.Authorization;
using Enset.Domain.Buildings;
using Enset.Domain.Customers;
using Enset.Domain.Energy;
using Enset.Domain.Users;
using Enset.Infrastructure.Authorization;
using Enset.Infrastructure.CanonicalSnapshots;
using Enset.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Enset.Import.Tests;

public sealed class CanonicalSnapshotBatchTests
{
    [Fact]
    public async Task Batch_reads_fifty_customers_buildings_and_meters()
    {
        await using var fixture = await Fixture.Create(50);

        var customers = await fixture.Reader.GetCustomers(
            fixture.CustomerIds, default);
        var buildings = await fixture.Reader.GetBuildings(
            fixture.BuildingIds, default);
        var meters = await fixture.Reader.GetMeters(
            fixture.MeterIds, default);

        Assert.Equal(50, customers.Count);
        Assert.Equal(50, buildings.Count);
        Assert.Equal(50, meters.Count);
    }

    [Fact]
    public async Task Single_and_batch_paths_have_identical_semantics()
    {
        await using var fixture = await Fixture.Create(1);
        var id = fixture.MeterIds[0];

        var single = await fixture.Reader.GetMeter(id, default);
        var batch = Assert.Single(await fixture.Reader.GetMeters([id], default));

        Assert.Equal(single!.MeterNumber, batch.MeterNumber);
        Assert.Equal(single.Name, batch.Name);
        Assert.Equal(single.Quality, batch.Quality);
        Assert.Equal(single.Suitability, batch.Suitability);
        Assert.Equal(single.Readings, batch.Readings);
    }

    [Fact]
    public void Portfolio_reader_has_constant_query_budget_and_no_single_loop()
    {
        var source = File.ReadAllText(FindSource(
            "src", "Enset.Infrastructure", "CanonicalSnapshots",
            "EfCanonicalSnapshotReader.cs"));
        var portfolio = source[
            source.IndexOf("GetPortfolio(", StringComparison.Ordinal)..
            source.IndexOf("private async Task<IReadOnlyDictionary",
                StringComparison.Ordinal)];

        Assert.Equal(12, EfCanonicalSnapshotReader.PortfolioQueryBudget);
        Assert.Contains("GetCustomers(customerIds", portfolio);
        Assert.Contains("GetBuildings(buildingIds", portfolio);
        Assert.Contains("GetMeters(meterIds", portfolio);
        Assert.DoesNotContain("foreach", portfolio);
        Assert.DoesNotContain("GetCustomer(id", portfolio);
        Assert.DoesNotContain("GetBuilding(id", portfolio);
        Assert.DoesNotContain("GetMeter(id", portfolio);
    }

    [Fact]
    public void Curated_values_are_only_read_by_canonical_snapshot_reader()
    {
        var root = FindRoot();
        var downstreamDirectories = new[]
        {
            Path.Combine(root, "src", "Enset.Infrastructure", "ReadModel"),
            Path.Combine(root, "src", "Enset.Infrastructure",
                "InternalDataProducts"),
            Path.Combine(root, "src", "Enset.Infrastructure", "Exports"),
            Path.Combine(root, "src", "Enset.Api")
        };
        var offenders = downstreamDirectories
            .SelectMany(path => Directory.EnumerateFiles(
                path,
                "*.cs",
                SearchOption.AllDirectories))
            .Where(path => File.ReadAllText(path)
                .Contains("db.CuratedFieldValues",
                    StringComparison.Ordinal))
            .ToArray();

        Assert.Empty(offenders);
    }

    private sealed class Fixture : IAsyncDisposable
    {
        private readonly EnsetDbContext db;
        public EfCanonicalSnapshotReader Reader { get; }
        public Guid[] CustomerIds { get; }
        public Guid[] BuildingIds { get; }
        public Guid[] MeterIds { get; }

        private Fixture(
            EnsetDbContext db,
            EfCanonicalSnapshotReader reader,
            Guid[] customerIds,
            Guid[] buildingIds,
            Guid[] meterIds)
        {
            this.db = db;
            Reader = reader;
            CustomerIds = customerIds;
            BuildingIds = buildingIds;
            MeterIds = meterIds;
        }

        public static async Task<Fixture> Create(int count)
        {
            var options = new DbContextOptionsBuilder<EnsetDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            var db = new EnsetDbContext(options);
            var customers = Enumerable.Range(1, count)
                .Select(i => new Customer
                {
                    CustomerNumber = $"C-{i:000}",
                    Name = $"Customer {i}",
                    City = "St. Pölten",
                    Type = CustomerType.Company
                })
                .ToArray();
            var buildings = customers.Select((customer, i) =>
            {
                var building = new Building
                {
                    BuildingNumber = $"B-{i + 1:000}",
                    Name = $"Building {i + 1}"
                };
                building.CustomerAssignments.Add(
                    new CustomerBuildingAssignment
                    {
                        Customer = customer,
                        Building = building,
                        IsPrimary = true,
                        ValidFrom = DateTime.UtcNow
                    });
                return building;
            }).ToArray();
            var meters = buildings.Select((building, i) => new Meter
            {
                MeterNumber = $"AT{i + 1:000000}",
                Name = $"Meter {i + 1}",
                Building = building,
                Medium = MeterMedium.Electricity,
                Quantity = MeterQuantity.Energy,
                Unit = MeterUnit.KWh,
                Direction = MeterDirection.Consumption,
                Type = MeterType.Physical
            }).ToArray();
            db.AddRange(customers);
            db.AddRange(buildings);
            db.AddRange(meters);
            await db.SaveChangesAsync();

            var user = new CurrentUserContext();
            user.Initialize(
                Guid.NewGuid(),
                true,
                [GlobalUserRole.EnsetEmployee.ToString()]);
            var scope = new EfDataAccessScope(db, user);
            return new(
                db,
                new EfCanonicalSnapshotReader(
                    db,
                    scope,
                    TimeProvider.System),
                customers.Select(x => x.Id).ToArray(),
                buildings.Select(x => x.Id).ToArray(),
                meters.Select(x => x.Id).ToArray());
        }

        public ValueTask DisposeAsync() => db.DisposeAsync();
    }

    private static string FindSource(params string[] segments) =>
        Path.Combine([FindRoot(), .. segments]);

    private static string FindRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null &&
               !Directory.Exists(Path.Combine(directory.FullName, "src")))
            directory = directory.Parent;
        return directory?.FullName ??
            throw new DirectoryNotFoundException();
    }
}
