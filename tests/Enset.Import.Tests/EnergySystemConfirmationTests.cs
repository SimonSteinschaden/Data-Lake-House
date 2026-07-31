using Enset.Application.Authorization;
using Enset.Application.Crud;
using Enset.Application.Curation;
using Enset.Domain.Buildings;
using Enset.Domain.Curation;
using Enset.Domain.Energy;
using Enset.Domain.Users;
using Enset.Infrastructure.Authorization;
using Enset.Infrastructure.CanonicalSnapshots;
using Enset.Infrastructure.Crud;
using Enset.Infrastructure.Curation;
using Enset.Infrastructure.Persistence;
using Enset.Infrastructure.Quality;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Enset.Import.Tests;

public sealed class EnergySystemConfirmationTests
{
    private sealed class Fixture : IAsyncDisposable
    {
        public EnsetDbContext Db { get; }
        public EfCurationService Curation { get; }
        public EfHierarchicalQualityAssessmentService Assessments { get; }
        public EfEntityCrudService Crud { get; }
        public Guid BuildingId { get; }
        public Guid EnergySystemId { get; }

        private Fixture(EnsetDbContext db, EfCurationService curation,
            EfHierarchicalQualityAssessmentService assessments, EfEntityCrudService crud,
            Guid buildingId, Guid energySystemId)
        {
            Db = db; Curation = curation; Assessments = assessments; Crud = crud;
            BuildingId = buildingId; EnergySystemId = energySystemId;
        }

        public static async Task<Fixture> Create()
        {
            var options = new DbContextOptionsBuilder<EnsetDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
            var user = new CurrentUserContext();
            user.Initialize(Guid.NewGuid(), true, [GlobalUserRole.EnsetEmployee.ToString()]);
            var db = new EnsetDbContext(options, user);

            var building = new Building { BuildingNumber = $"B-{Guid.NewGuid():N}", Name = "Testgebäude" };
            var system = new EnergySystem
            {
                EnergySystemNumber = $"AN-{Guid.NewGuid():N}", Name = "PV-Anlage",
                Type = EnergySystemType.Photovoltaic, RatedPowerKw = 15m
            };
            db.Add(building);
            db.Add(system);
            await db.SaveChangesAsync();
            // Das InMemory-Provider erzeugt keinen echten Concurrency-Token; für
            // Update-Aufrufe wird ein fester Wert wie im übrigen Testbestand verwendet.
            system.RowVersion = 1;
            await db.SaveChangesAsync();
            db.Add(new EnergySystemBuildingAssignment
            {
                EnergySystemId = system.Id, BuildingId = building.Id,
                Role = EnergySystemBuildingRole.LocatedAt
            });
            await db.SaveChangesAsync();

            var scope = new EfDataAccessScope(db, user);
            var reader = new EfCanonicalSnapshotReader(db, scope, TimeProvider.System);
            var curation = new EfCurationService(db, user, TimeProvider.System, scope, reader);
            var assessments = new EfHierarchicalQualityAssessmentService(db);
            var invalidation = new EfQualityInvalidationService(db, user);
            var crud = new EfEntityCrudService(db, scope, qualityInvalidation: invalidation, currentUser: user);

            return new(db, curation, assessments, crud, building.Id, system.Id);
        }

        public ValueTask DisposeAsync() => Db.DisposeAsync();
    }

    [Fact]
    public async Task Complete_but_unconfirmed_energy_system_is_silver_until_confirmed()
    {
        await using var fixture = await Fixture.Create();

        var before = (await fixture.Assessments.AssessEnergySystems([fixture.EnergySystemId], default))
            [fixture.EnergySystemId];
        Assert.Equal(DataMaturityLevel.Silver, before.QualityLevel);

        var tasks = await fixture.Curation.GetTasksAsync(
            new CurationTaskQuery(EntityType: "EnergySystem", EntityId: fixture.EnergySystemId), default);
        var typeTask = Assert.Single(tasks.Items, t => t.FieldName == "Type");
        var powerTask = Assert.Single(tasks.Items, t => t.FieldName == "RatedPowerKw");
        Assert.DoesNotContain(tasks.Items, t => t.FieldName == "EnergySystemNumber");
        Assert.DoesNotContain(tasks.Items, t => t.FieldName == "BuildingAssignment");

        await fixture.Curation.AcceptAsync(typeTask.Id, default);
        var afterOneConfirmed = (await fixture.Assessments.AssessEnergySystems([fixture.EnergySystemId], default))
            [fixture.EnergySystemId];
        Assert.Equal(DataMaturityLevel.Silver, afterOneConfirmed.QualityLevel);

        await fixture.Curation.AcceptAsync(powerTask.Id, default);
        var afterAllConfirmed = (await fixture.Assessments.AssessEnergySystems([fixture.EnergySystemId], default))
            [fixture.EnergySystemId];
        Assert.Equal(DataMaturityLevel.Gold, afterAllConfirmed.QualityLevel);
        Assert.Equal("Bestätigt", afterAllConfirmed.ConfirmationStatus);
    }

    [Fact]
    public async Task Task_discovery_is_idempotent_per_entity_field_and_open_state()
    {
        await using var fixture = await Fixture.Create();

        await fixture.Curation.GetTasksAsync(
            new CurationTaskQuery(EntityType: "EnergySystem", EntityId: fixture.EnergySystemId), default);
        await fixture.Curation.GetTasksAsync(
            new CurationTaskQuery(EntityType: "EnergySystem", EntityId: fixture.EnergySystemId), default);
        await fixture.Curation.GetTasksAsync(
            new CurationTaskQuery(EntityType: "EnergySystem", EntityId: fixture.EnergySystemId), default);

        var typeTaskCount = await fixture.Db.CurationTasks
            .CountAsync(x => x.EntityType == "EnergySystem" && x.EntityId == fixture.EnergySystemId
                && x.FieldName == "Type");
        Assert.Equal(1, typeTaskCount);
    }

    [Fact]
    public async Task Changing_a_gold_relevant_field_resets_confirmation()
    {
        await using var fixture = await Fixture.Create();
        var tasks = await fixture.Curation.GetTasksAsync(
            new CurationTaskQuery(EntityType: "EnergySystem", EntityId: fixture.EnergySystemId), default);
        foreach (var task in tasks.Items) await fixture.Curation.AcceptAsync(task.Id, default);
        var gold = (await fixture.Assessments.AssessEnergySystems([fixture.EnergySystemId], default))
            [fixture.EnergySystemId];
        Assert.Equal(DataMaturityLevel.Gold, gold.QualityLevel);

        var system = await fixture.Db.EnergySystems.AsNoTracking()
            .SingleAsync(x => x.Id == fixture.EnergySystemId);
        await fixture.Crud.UpdateEnergySystemAsync(fixture.EnergySystemId, new(
            system.EnergySystemNumber, system.Name, system.Type.ToString(),
            fixture.BuildingId, 30m, system.CommissionedAt, system.DecommissionedAt,
            RowVersion: 1), default);

        var afterPowerChange = (await fixture.Assessments.AssessEnergySystems([fixture.EnergySystemId], default))
            [fixture.EnergySystemId];
        Assert.NotEqual(DataMaturityLevel.Gold, afterPowerChange.QualityLevel);
    }

    [Fact]
    public async Task Changing_only_the_name_does_not_reset_confirmation()
    {
        await using var fixture = await Fixture.Create();
        var tasks = await fixture.Curation.GetTasksAsync(
            new CurationTaskQuery(EntityType: "EnergySystem", EntityId: fixture.EnergySystemId), default);
        foreach (var task in tasks.Items) await fixture.Curation.AcceptAsync(task.Id, default);
        var gold = (await fixture.Assessments.AssessEnergySystems([fixture.EnergySystemId], default))
            [fixture.EnergySystemId];
        Assert.Equal(DataMaturityLevel.Gold, gold.QualityLevel);

        var system = await fixture.Db.EnergySystems.AsNoTracking()
            .SingleAsync(x => x.Id == fixture.EnergySystemId);
        await fixture.Crud.UpdateEnergySystemAsync(fixture.EnergySystemId, new(
            system.EnergySystemNumber, "Neuer Name", system.Type.ToString(),
            fixture.BuildingId, system.RatedPowerKw, system.CommissionedAt, system.DecommissionedAt,
            RowVersion: 1), default);

        var afterNameChange = (await fixture.Assessments.AssessEnergySystems([fixture.EnergySystemId], default))
            [fixture.EnergySystemId];
        Assert.Equal(DataMaturityLevel.Gold, afterNameChange.QualityLevel);
    }
}
