using Enset.Application.Authorization;
using Enset.Domain.Buildings;
using Enset.Domain.Customers;
using Enset.Domain.Data;
using Enset.Domain.Documents;
using Enset.Domain.Energy;
using Enset.Domain.Projects;
using Enset.Domain.Users;
using Enset.Infrastructure.Authorization;
using Enset.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;
using Enset.Application.ReadModel;
using Enset.Infrastructure.ReadModel;

namespace Enset.Import.Tests;

public sealed class AuthorizationArchitectureTests
{
    [Fact]
    public void CurrentUserContext_IsInitializedExactlyOnce()
    {
        var context = new CurrentUserContext();
        var id = Guid.NewGuid();

        context.Initialize(id, true, [GlobalUserRole.EnsetEmployee.ToString()]);

        Assert.True(context.IsAuthenticated);
        Assert.True(context.IsEnsetEmployee);
        Assert.Equal(id, context.UserId);
        Assert.Throws<InvalidOperationException>(() => context.Initialize(id, false, []));
    }

    [Theory]
    [InlineData(-1, null, true)]
    [InlineData(1, null, false)]
    [InlineData(-2, -1, false)]
    public void Assignment_UsesActiveHalfOpenValidityWindow(
        int fromHours, int? toHours, bool expected)
    {
        var now = DateTimeOffset.UtcNow;
        var assignment = new UserCustomerAssignment
        {
            IsActive = true,
            ValidFrom = now.AddHours(fromHours),
            ValidTo = toHours.HasValue ? now.AddHours(toHours.Value) : null
        };

        Assert.Equal(expected, assignment.IsValidAt(now));
    }

    [Fact]
    public async Task CustomerRoles_SeeOnlyTheirTenantGraph()
    {
        await using var fixture = await ScopeFixture.Create(UserCustomerRole.CustomerViewer);

        Assert.Single(await fixture.Scope.ApplyCustomerScope(fixture.Db.Customers).ToListAsync());
        Assert.Single(await fixture.Scope.ApplyBuildingScope(fixture.Db.Buildings).ToListAsync());
        Assert.Single(await fixture.Scope.ApplyMeterScope(fixture.Db.Meters).ToListAsync());
        Assert.Single(await fixture.Scope.ApplyMeterReadingScope(fixture.Db.MeterReadings).ToListAsync());
        Assert.Single(await fixture.Scope.ApplyDocumentScope(fixture.Db.Documents).ToListAsync());
        Assert.True(await fixture.Scope.CanReadCustomer(fixture.AllowedCustomerId));
        Assert.False(await fixture.Scope.CanReadCustomer(fixture.ForeignCustomerId));
        Assert.False(await fixture.Scope.CanWriteCustomer(fixture.AllowedCustomerId));
    }

    [Theory]
    [InlineData(UserCustomerRole.CustomerAdmin, true, true)]
    [InlineData(UserCustomerRole.CustomerUser, true, false)]
    [InlineData(UserCustomerRole.CustomerViewer, false, false)]
    public async Task CustomerRole_WriteAndAdministrationRightsAreSeparated(
        UserCustomerRole role, bool canWrite, bool canAdminister)
    {
        await using var fixture = await ScopeFixture.Create(role);

        Assert.Equal(canWrite,
            await fixture.Scope.CanWriteCustomer(fixture.AllowedCustomerId));
        Assert.Equal(canAdminister,
            await fixture.Scope.CanAdministerCustomer(fixture.AllowedCustomerId));
        Assert.Equal(canWrite,
            await fixture.Scope.CanWriteBuilding(fixture.AllowedBuildingId));
        Assert.Equal(canWrite,
            await fixture.Scope.CanWriteMeter(fixture.AllowedMeterId));
        Assert.Equal(canWrite,
            await fixture.Scope.CanWriteDocument(fixture.AllowedDocumentId));
    }

    [Fact]
    public async Task EnsetEmployee_HasUnrestrictedScopeAndWriteAccess()
    {
        await using var fixture = await ScopeFixture.CreateEmployee();

        Assert.Equal(2, await fixture.Scope.ApplyCustomerScope(fixture.Db.Customers).CountAsync());
        Assert.Equal(2, await fixture.Scope.ApplyBuildingScope(fixture.Db.Buildings).CountAsync());
        Assert.True(await fixture.Scope.CanWriteCustomer(fixture.ForeignCustomerId));
    }

    [Fact]
    public async Task ExpiredAssignment_HidesExistingObjectForNotFoundSemantics()
    {
        await using var fixture = await ScopeFixture.Create(
            UserCustomerRole.CustomerAdmin, expired: true);

        Assert.False(await fixture.Scope.CanReadCustomer(fixture.AllowedCustomerId));
        Assert.False(await fixture.Scope.CanWriteCustomer(fixture.AllowedCustomerId));
    }

    [Fact]
    public async Task ReadService_AppliesTenantScopeBeforeProjectionAndPagination()
    {
        await using var fixture = await ScopeFixture.Create(UserCustomerRole.CustomerViewer);
        var service = new EfEntityReadService(fixture.Db, fixture.Scope);

        var customers = await service.GetCustomersAsync(new CustomerListQuery(PageSize: 1));
        var buildings = await service.GetBuildingsAsync(new BuildingListQuery());
        var meters = await service.GetMetersAsync(new MeterListQuery());

        Assert.Single(customers.Items);
        Assert.Equal(1, customers.TotalCount);
        Assert.Single(buildings.Items);
        Assert.Single(meters.Items);
        Assert.Null(await service.GetCustomerAsync(fixture.ForeignCustomerId));
        Assert.Null(await service.GetBuildingAsync(fixture.ForeignBuildingId));
        Assert.Null(await service.GetMeterAsync(fixture.ForeignMeterId));
    }

    [Fact]
    public async Task MeterReadings_AreScopedPaginatedAndAggregatedServerSide()
    {
        await using var fixture = await ScopeFixture.Create(UserCustomerRole.CustomerUser);
        var service = new EfEntityReadService(fixture.Db, fixture.Scope);
        var start = DateTime.SpecifyKind(DateTime.UtcNow.Date, DateTimeKind.Utc);
        fixture.Db.MeterReadings.RemoveRange(fixture.Db.MeterReadings);
        fixture.Db.MeterReadings.AddRange(
            Reading(fixture.AllowedMeterId, start, 2),
            Reading(fixture.AllowedMeterId, start.AddMinutes(5), 3),
            Reading(fixture.ForeignMeterId, start, 999));
        await fixture.Db.SaveChangesAsync();

        var raw = await service.GetMeterReadingsAsync(fixture.AllowedMeterId,
            new MeterReadingQuery(start, start.AddHours(1), PageSize: 1));
        var aggregate = await service.GetMeterReadingsAsync(fixture.AllowedMeterId,
            new MeterReadingQuery(start, start.AddHours(1),
                MeterReadingAggregation.FifteenMinutes));

        Assert.NotNull(raw);
        Assert.Equal(2, raw.Raw!.TotalCount);
        Assert.Single(raw.Raw.Items);
        Assert.NotNull(aggregate);
        Assert.Single(aggregate.Aggregated!);
        Assert.Equal(5, aggregate.Aggregated![0].Sum);
        Assert.Null(await service.GetMeterReadingsAsync(fixture.ForeignMeterId,
            new MeterReadingQuery()));
        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.GetMeterReadingsAsync(fixture.AllowedMeterId,
                new MeterReadingQuery(start.AddHours(1), start)));
    }

    private static MeterReading Reading(Guid meterId, DateTime timestamp, decimal value) => new()
    {
        MeterId = meterId,
        Timestamp = timestamp,
        Value = value,
        ReadingType = MeterReadingType.IntervalValue,
        QualityFlag = DataQuality.Measured
    };

    private sealed class ScopeFixture : IAsyncDisposable
    {
        public EnsetDbContext Db { get; }
        public EfDataAccessScope Scope { get; }
        public Guid AllowedCustomerId { get; }
        public Guid ForeignCustomerId { get; }
        public Guid AllowedBuildingId { get; }
        public Guid ForeignBuildingId { get; }
        public Guid AllowedMeterId { get; }
        public Guid ForeignMeterId { get; }
        public Guid AllowedDocumentId { get; }

        private ScopeFixture(EnsetDbContext db, EfDataAccessScope scope,
            Guid allowedCustomerId, Guid foreignCustomerId, Guid allowedBuildingId,
            Guid foreignBuildingId, Guid allowedMeterId, Guid foreignMeterId,
            Guid allowedDocumentId)
        {
            Db = db;
            Scope = scope;
            AllowedCustomerId = allowedCustomerId;
            ForeignCustomerId = foreignCustomerId;
            AllowedBuildingId = allowedBuildingId;
            ForeignBuildingId = foreignBuildingId;
            AllowedMeterId = allowedMeterId;
            ForeignMeterId = foreignMeterId;
            AllowedDocumentId = allowedDocumentId;
        }

        public static Task<ScopeFixture> Create(UserCustomerRole role, bool expired = false) =>
            CreateCore(role, employee: false, expired);

        public static Task<ScopeFixture> CreateEmployee() =>
            CreateCore(null, employee: true, expired: false);

        private static async Task<ScopeFixture> CreateCore(
            UserCustomerRole? role, bool employee, bool expired)
        {
            var options = new DbContextOptionsBuilder<EnsetDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            var db = new EnsetDbContext(options);
            var userId = Guid.NewGuid();
            var allowedCustomer = Customer("ALLOWED");
            var foreignCustomer = Customer("FOREIGN");
            var allowedBuilding = Building("ALLOWED-B", allowedCustomer);
            var foreignBuilding = Building("FOREIGN-B", foreignCustomer);
            var allowedMeter = Meter("ALLOWED-M", allowedBuilding);
            var foreignMeter = Meter("FOREIGN-M", foreignBuilding);
            var project = new Project
            {
                Customer = allowedCustomer,
                Name = "Allowed project"
            };
            var document = new Document
            {
                Project = project,
                FilePath = "allowed.pdf"
            };

            db.AddRange(allowedCustomer, foreignCustomer, allowedBuilding,
                foreignBuilding, allowedMeter, foreignMeter, project, document,
                new MeterReading
                {
                    Meter = allowedMeter,
                    Timestamp = DateTime.UtcNow,
                    Value = 1,
                    QualityFlag = DataQuality.Measured
                },
                new MeterReading
                {
                    Meter = foreignMeter,
                    Timestamp = DateTime.UtcNow.AddMinutes(1),
                    Value = 2,
                    QualityFlag = DataQuality.Measured
                });

            var context = new CurrentUserContext();
            context.Initialize(userId, employee,
                employee ? [GlobalUserRole.EnsetEmployee.ToString()] : [role!.Value.ToString()]);
            if (!employee)
            {
                db.UserCustomerAssignments.Add(new UserCustomerAssignment
                {
                    UserId = userId,
                    Customer = allowedCustomer,
                    Role = role!.Value,
                    ValidFrom = DateTimeOffset.UtcNow.AddDays(-2),
                    ValidTo = expired ? DateTimeOffset.UtcNow.AddDays(-1) : null,
                    IsActive = true,
                    User = new ApplicationUser
                    {
                        Id = userId,
                        ExternalIdentity = "test-user",
                        DisplayName = "Test User",
                        Email = "test@example.test"
                    }
                });
            }

            await db.SaveChangesAsync();
            return new ScopeFixture(db, new EfDataAccessScope(db, context),
                allowedCustomer.Id, foreignCustomer.Id, allowedBuilding.Id,
                foreignBuilding.Id, allowedMeter.Id, foreignMeter.Id, document.Id);
        }

        private static Customer Customer(string number) => new()
        {
            CustomerNumber = number,
            Name = number,
            Type = CustomerType.Company
        };

        private static Building Building(string number, Customer customer)
        {
            var building = new Building { BuildingNumber = number, Name = number };
            building.CustomerAssignments.Add(new CustomerBuildingAssignment
            {
                Customer = customer,
                Building = building,
                ValidFrom = DateTime.UtcNow.AddDays(-1)
            });
            return building;
        }

        private static Meter Meter(string number, Building building) => new()
        {
            MeterNumber = number,
            Name = number,
            Building = building
        };

        public ValueTask DisposeAsync() => Db.DisposeAsync();
    }
}
