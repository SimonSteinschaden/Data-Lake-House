using Enset.Application.Authorization;
using Enset.Application.CanonicalSnapshots;
using Enset.Application.Crud;
using Enset.Application.Curation;
using Enset.Application.InternalDataProducts;
using Enset.Domain.Common;
using Enset.Domain.Customers;
using Enset.Domain.Curation;
using Enset.Domain.Geography;
using Enset.Infrastructure.Crud;
using Enset.Infrastructure.Curation;
using Enset.Infrastructure.InternalDataProducts;
using Enset.Infrastructure.Authorization;
using Enset.Infrastructure.CanonicalSnapshots;
using Enset.Infrastructure.Persistence;
using Enset.Api.Errors;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Xunit;

namespace Enset.Import.Tests;

public sealed class CrudApiFoundationTests
{
    [Fact]
    public async Task CreateCustomer_SetsServerAuditAndManualOrigin()
    {
        var userId = Guid.NewGuid();
        await using var db = Context(userId);
        var service = new EfEntityCrudService(db);

        var result = await service.CreateCustomerAsync(
            new("K-100", "Testkunde", "Company", null, null, null, "AT"), default);

        var entity = await db.Customers.SingleAsync();
        Assert.Equal(userId, entity.CreatedByUserId);
        Assert.Equal(DataOrigin.ManuallyCreated, entity.DataOrigin);
        Assert.Equal(result.Id, entity.Id);
        Assert.Contains(await db.EntityAuditEntries.ToListAsync(),
            x => x.EntityId == entity.Id && x.ChangeType == EntityChangeType.Created);
    }

    [Fact]
    public async Task ManualUpdate_MarksImportedEntityAsImportedAndModified_AndAuditsField()
    {
        var userId = Guid.NewGuid();
        await using var db = Context(userId);
        var entity = new Customer { CustomerNumber = "K-200", Name = "Alt",
            Type = CustomerType.Company, DataOrigin = DataOrigin.Imported,
            LastModifiedSource = LastModifiedSource.Import };
        db.Customers.Add(entity);
        await db.SaveChangesAsync();
        db.EntityAuditEntries.RemoveRange(db.EntityAuditEntries);
        await db.SaveChangesAsync();

        entity.Name = "Neu";
        entity.LastModifiedSource = LastModifiedSource.User;
        await db.SaveChangesAsync();

        Assert.Equal(DataOrigin.ImportedAndModified, entity.DataOrigin);
        Assert.Contains(await db.EntityAuditEntries.ToListAsync(),
            x => x.EntityId == entity.Id && x.FieldName == nameof(Customer.Name) &&
                 x.OldValue == "Alt" && x.NewValue == "Neu");
    }

    [Fact]
    public async Task SoftDeletedCustomer_IsHiddenButCanBeLoadedForRestore()
    {
        await using var db = Context(Guid.NewGuid());
        var customer = new Customer { CustomerNumber = "K-300", Name = "Gelöscht",
            Type = CustomerType.Company };
        db.Customers.Add(customer);
        await db.SaveChangesAsync();
        customer.IsDeleted = true;
        await db.SaveChangesAsync();

        Assert.Empty(await db.Customers.ToListAsync());
        Assert.Single(await db.Customers.IgnoreQueryFilters().ToListAsync());
        Assert.Contains(await db.EntityAuditEntries.ToListAsync(),
            x => x.EntityId == customer.Id && x.ChangeType == EntityChangeType.SoftDeleted);
    }

    [Fact]
    public void CustomerValidator_ReturnsFieldBasedErrors()
    {
        var validator = new CustomerWriteModelValidator();
        var model = new CustomerWriteModel("", "", "Company", null, null, null, "A");
        var result = validator.Validate(model);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, x => x.PropertyName == nameof(CustomerWriteModel.Name));
        Assert.Contains(result.Errors, x => x.PropertyName == nameof(CustomerWriteModel.CountryCode));
    }

    [Theory]
    [InlineData("123")]
    [InlineData("12345")]
    [InlineData("12A4")]
    public void BuildingValidator_ReturnsDetailedPostalCodeError(string postalCode)
    {
        var validator = new BuildingCreateRequestValidator();
        var model = new BuildingCreateRequest(
            "Testgebäude",
            null,
            Guid.NewGuid(),
            PostalCode: postalCode);

        var result = validator.Validate(model);

        var error = Assert.Single(result.Errors,
            item => item.PropertyName == nameof(BuildingCreateRequest.PostalCode));
        Assert.Equal("Die PLZ muss aus genau vier Ziffern bestehen.",
            error.ErrorMessage);
    }

    [Fact]
    public async Task DuplicateCustomerNumber_IsRejectedAsConflict()
    {
        await using var db = Context(Guid.NewGuid());
        var service = new EfEntityCrudService(db);
        var model = new CustomerWriteModel("K-400", "Eins", "Company", null, null, null, "AT");
        await service.CreateCustomerAsync(model, default);

        await Assert.ThrowsAsync<CrudConflictException>(() =>
            service.CreateCustomerAsync(model with { Name = "Zwei" }, default));
    }

    [Fact]
    public async Task CreateBuilding_PersistsSelectedBuildingStateUnderCanonicalFieldName()
    {
        await using var db = Context(Guid.NewGuid());
        var service = new EfEntityCrudService(db);

        var result = await service.CreateBuildingAsync(
            new BuildingCreateRequest(
                "Testgebäude",
                null,
                null,
                BuildingState: "Improved"),
            default);

        var state = await db.CuratedFieldValues.SingleAsync(x =>
            x.EntityType == "Building" &&
            x.EntityId == result.Id &&
            x.ValidToUtc == null);
        Assert.Equal("BuildingState", state.FieldName);
        Assert.Equal(BuildingState.Improved.ToString(), state.NormalizedValue);
        Assert.False(state.Confirmed);
    }

    [Fact]
    public async Task CreateBuilding_GeneratesInternalNumberAndAllowsMissingExternalIdentifier()
    {
        await using var db = Context(Guid.NewGuid());
        var created = await new EfEntityCrudService(db).CreateBuildingAsync(
            new BuildingCreateRequest(
                "Gebäude ohne Fremdkennung",
                null,
                null),
            default);

        var building = await db.Buildings.SingleAsync(x => x.Id == created.Id);
        Assert.Matches("^BLD-[0-9]{6,}$", building.BuildingNumber);
        Assert.Null(building.ExternalIdentifier);
    }

    [Fact]
    public async Task UpdateBuilding_PreservesNumberAndChangesExternalIdentifier()
    {
        await using var db = Context(Guid.NewGuid());
        var service = new EfEntityCrudService(db);
        var created = await service.CreateBuildingAsync(
            new BuildingCreateRequest("Gebäude", "EXT-ALT", null),
            default);
        var building = await db.Buildings.SingleAsync(x => x.Id == created.Id);
        var originalNumber = building.BuildingNumber;
        building.RowVersion = 1;
        await db.SaveChangesAsync();

        await service.UpdateBuildingAsync(
            created.Id,
            new BuildingUpdateRequest(
                "Gebäude geändert",
                "EXT-NEU",
                null,
                1),
            default);

        var reloaded = await db.Buildings.SingleAsync(x => x.Id == created.Id);
        Assert.Equal(originalNumber, reloaded.BuildingNumber);
        Assert.Equal("EXT-NEU", reloaded.ExternalIdentifier);
    }

    [Fact]
    public async Task ConcurrentNumberGeneration_ReturnsDistinctNumbers()
    {
        var tasks = Enumerable.Range(0, 32).Select(async _ =>
        {
            await using var db = Context(Guid.NewGuid());
            return await new EfBuildingNumberGenerator(db).NextAsync(default);
        });

        var numbers = await Task.WhenAll(tasks);

        Assert.Equal(numbers.Length, numbers.Distinct().Count());
    }

    [Fact]
    public async Task UpdateBuilding_ChangesBuildingState_AndCanonicalReloadReturnsIt()
    {
        var userId = Guid.NewGuid();
        await using var db = Context(userId);
        db.Countries.Add(new Country
            { IsoCode2 = "AT", IsoCode3 = "AUT", Name = "Österreich" });
        await db.SaveChangesAsync();
        var service = new EfEntityCrudService(db);
        var created = await service.CreateBuildingAsync(
            new BuildingCreateRequest("Testgebäude", "EXT-OLD", null,
                BuildingCategory: "Apartment",
                PrimaryUseType: "Commercial",
                BuildingState: "Existing",
                PostalCode: "1000",
                City: "Testort"),
            default);
        var building = await db.Buildings.SingleAsync(x => x.Id == created.Id);
        building.RowVersion = 1;
        await db.SaveChangesAsync();

        await service.UpdateBuildingAsync(
            created.Id,
            new BuildingUpdateRequest("Testgebäude Neu", "EXT-NEW", null,
                RowVersion: 1,
                BuildingCategory: "House",
                PrimaryUseType: "Residential",
                BuildingState: "Improved",
                PostalCode: "2000",
                City: "Neuer Testort"),
            default);

        var currentUser = new CurrentUserContext();
        currentUser.Initialize(userId, true, ["EnsetEmployee"]);
        var reader = new EfCanonicalSnapshotReader(
            db,
            new EfDataAccessScope(db, currentUser),
            TimeProvider.System);
        var reloaded = await reader.GetBuilding(created.Id, default);

        Assert.NotNull(reloaded);
        Assert.Equal("House", reloaded.BuildingType);
        Assert.Equal("Residential", reloaded.UsageType);
        Assert.Equal("Improved", reloaded.BuildingState);
        Assert.Equal("2000", reloaded.PostalCode);
        Assert.Equal(100, reloaded.GoldAssessment.GoldCompletenessPercentage);
        Assert.Empty(reloaded.GoldAssessment.MissingReasons);
        Assert.All(reloaded.GoldAssessment.GoldFieldStates, item => Assert.True(item.HasValue));
        Assert.Equal("EXT-NEW", (await db.Buildings.SingleAsync()).ExternalIdentifier);
    }

    [Fact]
    public async Task BuildingGoldProduct_RefreshesAfterCurationConfirmation()
    {
        var userId = Guid.NewGuid();
        await using var db = Context(userId);
        db.Countries.Add(new Country
            { IsoCode2 = "AT", IsoCode3 = "AUT", Name = "Österreich" });
        await db.SaveChangesAsync();
        var created = await new EfEntityCrudService(db).CreateBuildingAsync(
            new BuildingCreateRequest(
                "Gold-Testgebäude",
                null,
                null,
                BuildingCategory: "House",
                PrimaryUseType: "Residential",
                BuildingState: "Existing",
                PostalCode: "1090",
                City: "Wien"),
            default);
        var currentUser = new CurrentUserContext();
        currentUser.Initialize(userId, true, ["EnsetEmployee"]);
        var scope = new EfDataAccessScope(db, currentUser);
        var reader = new EfCanonicalSnapshotReader(
            db,
            scope,
            TimeProvider.System);
        IBuildingSummaryProductService products =
            new EfInternalDataProductService(
                db,
                reader,
                TimeProvider.System);
        var before = await products.GetAsync(created.Id, default);

        Assert.NotNull(before);
        Assert.Equal(100, before.GoldAssessment.GoldCompletenessPercentage);
        Assert.Equal(0, before.GoldAssessment.GoldConfirmationPercentage);

        var curation = new EfCurationService(
            db,
            currentUser,
            TimeProvider.System,
            scope,
            reader);
        var tasks = await curation.GetTasksAsync(
            new CurationTaskQuery(
                EntityType: "Building",
                FieldName: "BuildingCategory",
                EntityId: created.Id),
            default);
        var task = Assert.Single(tasks.Items);
        db.ChangeTracker.Clear();
        await curation.AcceptAsync(task.Id, default);
        var after = await products.GetAsync(created.Id, default);

        Assert.NotNull(after);
        Assert.Equal(100, after.GoldAssessment.GoldCompletenessPercentage);
        Assert.Equal(25, after.GoldAssessment.GoldConfirmationPercentage);
        Assert.Equal(
            BuildingGoldFieldState.Confirmed,
            after.GoldAssessment.GoldFieldStates.Single(field =>
                field.FieldName == "BuildingCategory").State);
    }

    [Fact]
    public async Task MissingEntity_IsRejectedAsNotFound()
    {
        await using var db = Context(Guid.NewGuid());
        var service = new EfEntityCrudService(db);
        var model = new CustomerWriteModel("K-500", "Fehlt", "Company", null, null, null, "AT", 1);
        await Assert.ThrowsAsync<CrudNotFoundException>(() =>
            service.UpdateCustomerAsync(Guid.NewGuid(), model, default));
    }

    [Fact]
    public async Task CrudExceptions_AreMappedToRfc7807StatusCodes()
    {
        var writer = new CapturingProblemDetailsService();
        var handler = new CrudExceptionHandler(writer);
        var context = new DefaultHttpContext();

        Assert.True(await handler.TryHandleAsync(context,
            new CrudConflictException("Zwischenzeitlich geändert."), default));
        Assert.Equal(StatusCodes.Status409Conflict, context.Response.StatusCode);
        Assert.Equal(StatusCodes.Status409Conflict, writer.Context!.ProblemDetails.Status);

        context = new DefaultHttpContext();
        Assert.True(await handler.TryHandleAsync(context,
            new CrudValidationException(new Dictionary<string, string[]>
                { ["Name"] = ["Name ist erforderlich."] }), default));
        Assert.Equal(StatusCodes.Status400BadRequest, context.Response.StatusCode);
    }

    private static EnsetDbContext Context(Guid userId)
    {
        var current = new CurrentUserContext();
        current.Initialize(userId, true, ["EnsetAdmin"]);
        var options = new DbContextOptionsBuilder<EnsetDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        return new EnsetDbContext(options, current);
    }

    private sealed class CapturingProblemDetailsService : IProblemDetailsService
    {
        public ProblemDetailsContext? Context { get; private set; }
        public ValueTask WriteAsync(ProblemDetailsContext context)
        {
            Context = context;
            return ValueTask.CompletedTask;
        }
        public ValueTask<bool> TryWriteAsync(ProblemDetailsContext context)
        {
            Context = context;
            return ValueTask.FromResult(true);
        }
    }
}
