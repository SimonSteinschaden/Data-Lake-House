using Enset.Application.Imports.DTOs;
using Enset.Application.Imports.Enums;
using Enset.Application.Imports.Reports;
using Enset.Application.Imports.WriteGate;
using Enset.Domain.Buildings;
using Enset.Domain.Geography;
using Enset.Infrastructure.Imports.Database;
using Enset.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Enset.Import.Tests;

public sealed class PhaseAImportPersistenceTests
{
    [Fact]
    public async Task BuildingImportCreatesVersionAndPreservesMissingFieldsOnUpdate()
    {
        await using var db = CreateDatabase();
        db.Countries.Add(new Country
        {
            IsoCode2 = "AT",
            Name = "Österreich"
        });
        await db.SaveChangesAsync();

        await WriteAsync(db, new BuildingImportDto
        {
            ExternalBuildingId = "B-1",
            ExternalCustomerId = "C-1",
            BuildingName = "Rathaus",
            BuildingType = "Office",
            UsageType = "Public",
            Street = "Hauptplatz",
            HouseNumber = "1",
            PostalCode = "3100",
            City = "St. Pölten",
            Country = "AT",
            ConstructionYear = 1980,
            ConditionedFloorAreaM2 = 1250m,
            NumberOfFloors = 3
        });

        var initial = await db.BuildingVersions
            .Include(version => version.Address)
            .SingleAsync(version => version.ValidTo == null);
        Assert.Equal(BuildingCategory.Office, initial.BuildingCategory);
        Assert.Equal(PrimaryUseType.Public, initial.PrimaryUseType);
        Assert.Equal(1980, initial.YearOfConstruction);
        Assert.Equal(1250m, initial.ConditionedFloorAreaM2);
        Assert.Equal("St. Pölten", initial.Address!.City);

        await WriteAsync(db, new BuildingImportDto
        {
            ExternalBuildingId = "B-1",
            ExternalCustomerId = "C-1",
            BuildingName = "Rathaus neu",
            NumberOfFloors = 4
        });

        var versions = await db.BuildingVersions
            .OrderBy(version => version.VersionNumber)
            .ToListAsync();
        Assert.Equal(2, versions.Count);
        Assert.NotNull(versions[0].ValidTo);
        Assert.Null(versions[1].ValidTo);
        Assert.Equal(1980, versions[1].YearOfConstruction);
        Assert.Equal(1250m, versions[1].ConditionedFloorAreaM2);
        Assert.Equal(4, versions[1].NumberOfFloors);
    }

    [Fact]
    public async Task MeterPersistsOriginalNumberNameAndImportedAnnualTotal()
    {
        await using var db = CreateDatabase();
        const string meterNumber = "AT001000000000000001";
        await WriteAsync(
            db,
            building: null,
            meter: new MeterImportDto
            {
                MeterNumber = meterNumber,
                Name = "Hauptzähler",
                ProfileName = "Electricity",
                Unit = "kWh",
                AnnualValue = 1234.5m,
                AnnualValueReferenceYear = 2025,
                AllowUnassignedBuilding = true
            });

        var meter = Assert.Single(await db.Meters.ToListAsync());
        Assert.Equal(meterNumber, meter.MeterNumber);
        Assert.Equal("Hauptzähler", meter.Name);
        Assert.NotEqual(Guid.Empty, meter.Id);
        Assert.Equal(1234.5m, meter.AnnualValue);
        Assert.Equal(2025, meter.AnnualValueReferenceYear);
        Assert.Equal("ImportedAnnualTotal", meter.AnnualValueOrigin);
    }

    private static EnsetDbContext CreateDatabase()
    {
        var options = new DbContextOptionsBuilder<EnsetDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(warnings => warnings.Ignore(
                Microsoft.EntityFrameworkCore.Diagnostics
                    .InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        return new EnsetDbContext(options);
    }

    private static Task WriteAsync(
        EnsetDbContext db,
        BuildingImportDto? building,
        MeterImportDto? meter = null)
    {
        var importId = Guid.NewGuid();
        var report = new ImportReport
        {
            ImportId = importId,
            Customers = building is null
                ? []
                : [new CustomerImportDto
                {
                    ExternalCustomerId = "C-1",
                    CompanyName = "Gemeinde"
                }],
            Buildings = building is null ? [] : [building],
            Meters = meter is null ? [] : [meter]
        };
        return new DatabaseImportWriter(db).WriteAsync(
            new ImportWriteContext
            {
                ImportId = importId,
                Report = report,
                TargetMode = ImportTargetMode.Upsert,
                TargetWriter = ImportWriterType.Database,
                UserId = "phase-a-test",
                Timestamp = DateTime.UtcNow
            });
    }
}
