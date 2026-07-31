using Enset.Application.Associations;
using Enset.Domain.Buildings;
using Enset.Domain.Customers;
using Enset.Domain.Energy;
using Enset.Infrastructure.Associations;
using Enset.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Xunit;

namespace Enset.Import.Tests;

public sealed class AssociationManagementTests
{
    [Fact]
    public void Compatibility_matrix_contains_only_the_six_supported_relationships()
    {
        using var db = Context();
        var service = new EfAssociationService(db, TimeProvider.System);
        var types = service.Types();
        Assert.Equal(6, types.Count);
        Assert.Contains(types, x => x.Key == EfAssociationService.CustomerBuilding);
        Assert.Contains(types, x => x.Key == EfAssociationService.MeterSeries &&
                                    !x.SupportsMultipleTargets);
    }

    [Fact]
    public async Task Invalid_validity_and_role_are_blocking()
    {
        await using var db = Context();
        var customer = Customer(); var building = Building();
        db.AddRange(customer, building); await db.SaveChangesAsync();
        var preview = await new EfAssociationService(db, TimeProvider.System).Preview(
            new(EfAssociationService.CustomerBuilding,[customer.Id],[building.Id],
                "Invalid",new DateOnly(2026,2,1),new DateOnly(2026,1,1),false),
            CancellationToken.None);
        Assert.False(preview.CanCommit);
        Assert.Contains(preview.Conflicts, x => x.Code == "INVALID_VALIDITY");
        Assert.Contains(preview.Conflicts, x => x.Code == "INVALID_ROLE");
    }

    [Fact]
    public async Task Customer_building_commit_is_historized_and_audited()
    {
        await using var db = Context();
        var customer = Customer(); var building = Building();
        db.AddRange(customer, building); await db.SaveChangesAsync();
        var service = new EfAssociationService(db, TimeProvider.System);
        var request = new AssociationPreviewRequest(EfAssociationService.CustomerBuilding,
            [customer.Id],[building.Id],"Owner",new DateOnly(2026,1,1),null,true);
        var result = await service.Commit(request,Guid.NewGuid(),CancellationToken.None);
        Assert.Equal(1,result.Created);
        Assert.True(await db.CustomerBuildingAssignments.AnyAsync(x =>
            x.CustomerId==customer.Id&&x.BuildingId==building.Id&&x.IsPrimary));
        Assert.True(await db.AssociationAuditEntries.AnyAsync(x =>
            x.OperationId==result.OperationId&&x.Action=="Created"));
    }

    [Fact]
    public async Task Building_meter_assignment_keeps_current_foreign_key_in_sync()
    {
        await using var db = Context();
        var building = Building();
        var meter = new Meter { MeterNumber="M-1",Name="Meter",IsActive=true };
        db.AddRange(building,meter); await db.SaveChangesAsync();
        var service = new EfAssociationService(db,TimeProvider.System);
        var result = await service.Commit(new(EfAssociationService.BuildingMeter,
            [building.Id],[meter.Id],"MainMeter",new DateOnly(2026,1,1),null,true),
            Guid.NewGuid(),CancellationToken.None);
        Assert.Equal(building.Id,(await db.Meters.FindAsync(meter.Id))!.BuildingId);
        var assignment = await db.BuildingMeterAssignments.SingleAsync();
        await service.Remove(new(EfAssociationService.BuildingMeter,[assignment.Id],
            new DateOnly(2026,6,1),"Test",true),Guid.NewGuid(),CancellationToken.None);
        Assert.Null((await db.Meters.FindAsync(meter.Id))!.BuildingId);
        Assert.Equal(new DateOnly(2026,6,1),assignment.ValidTo);
    }

    [Fact]
    public async Task Entity_search_is_server_paginated_and_case_insensitive()
    {
        await using var db=Context();
        db.Customers.AddRange(
            new Customer{CustomerNumber="C-1",Name="Alpha Wien",IsActive=true},
            new Customer{CustomerNumber="C-2",Name="Beta Graz",IsActive=true});
        await db.SaveChangesAsync();
        var page=await new EfAssociationService(db,TimeProvider.System).SearchEntities(
            new(AssociationEntityType.Customer,"WIEN",1,1),CancellationToken.None);
        Assert.Single(page.Items);
        Assert.Equal("Alpha Wien",page.Items[0].DisplayName);
        Assert.Equal(1,page.TotalCount);
    }

    [Fact]
    public async Task Overlapping_historical_assignment_is_blocking()
    {
        await using var db=Context();
        var customer=Customer();var building=Building();
        db.AddRange(customer,building,new CustomerBuildingAssignment{
            Customer=customer,Building=building,Role=CustomerBuildingRole.Owner,
            ValidFrom=new DateTime(2026,1,1,0,0,0,DateTimeKind.Utc),
            ValidTo=new DateTime(2026,6,30,0,0,0,DateTimeKind.Utc)});
        await db.SaveChangesAsync();
        var preview=await new EfAssociationService(db,TimeProvider.System).Preview(
            new(EfAssociationService.CustomerBuilding,[customer.Id],[building.Id],
                "Owner",new DateOnly(2026,6,1),new DateOnly(2026,12,31),false),
            CancellationToken.None);
        Assert.False(preview.CanCommit);
        Assert.Contains(preview.Conflicts,x=>x.Code=="VALIDITY_OVERLAP");
    }

    [Fact]
    public void Controller_contains_no_direct_database_dependency()
    {
        var source = File.ReadAllText(Path.Combine(RepositoryRoot(),
            "src","Enset.Api","Controllers","AssociationsController.cs"));
        Assert.DoesNotContain("EnsetDbContext",source);
        Assert.DoesNotContain("DbSet<",source);
    }

    private static EnsetDbContext Context()
    {
        var options = new DbContextOptionsBuilder<EnsetDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        return new(options);
    }
    private static Customer Customer()=>new(){Id=Guid.NewGuid(),CustomerNumber="C-1",Name="Customer",IsActive=true};
    private static Building Building()=>new(){Id=Guid.NewGuid(),BuildingNumber="B-1",Name="Building",IsActive=true};
    private static string RepositoryRoot()
    {
        var current=new DirectoryInfo(AppContext.BaseDirectory);
        while(current is not null&&!File.Exists(Path.Combine(current.FullName,"README.md")))current=current.Parent;
        return current?.FullName??throw new DirectoryNotFoundException();
    }
}
