using Enset.Application.Authorization;
using Enset.Application.Crud;
using Enset.Domain.Common;
using Enset.Domain.Customers;
using Enset.Infrastructure.Crud;
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
