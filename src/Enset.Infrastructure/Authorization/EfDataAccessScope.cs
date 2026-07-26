using Enset.Application.Authorization;
using Enset.Domain.Buildings;
using Enset.Domain.Customers;
using Enset.Domain.Documents;
using Enset.Domain.Energy;
using Enset.Domain.DataProducts;
using Enset.Domain.Users;
using Enset.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Enset.Infrastructure.Authorization;

public sealed class EfDataAccessScope : IDataAccessScope
{
    private readonly EnsetDbContext _db;
    private readonly ICurrentUserContext _currentUser;

    public EfDataAccessScope(EnsetDbContext db, ICurrentUserContext currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public IQueryable<Customer> ApplyCustomerScope(IQueryable<Customer> query)
    {
        if (_currentUser.IsEnsetEmployee)
            return query;

        var assignments = ValidAssignments();
        return query.Where(customer => assignments.Any(a => a.CustomerId == customer.Id));
    }

    public IQueryable<Building> ApplyBuildingScope(IQueryable<Building> query)
    {
        if (_currentUser.IsEnsetEmployee)
            return query;

        var assignments = ValidAssignments();
        var now = DateTime.UtcNow;
        return query.Where(building => building.CustomerAssignments.Any(link =>
            link.ValidFrom <= now && (link.ValidTo == null || link.ValidTo > now) &&
            assignments.Any(a => a.CustomerId == link.CustomerId)));
    }

    public IQueryable<Meter> ApplyMeterScope(IQueryable<Meter> query)
    {
        if (_currentUser.IsEnsetEmployee)
            return query;

        var assignments = ValidAssignments();
        var now = DateTime.UtcNow;
        return query.Where(meter => meter.Building != null &&
            meter.Building.CustomerAssignments.Any(link =>
                link.ValidFrom <= now && (link.ValidTo == null || link.ValidTo > now) &&
                assignments.Any(a => a.CustomerId == link.CustomerId)));
    }

    public IQueryable<MeterReading> ApplyMeterReadingScope(IQueryable<MeterReading> query)
    {
        if (_currentUser.IsEnsetEmployee)
            return query;

        var assignments = ValidAssignments();
        var now = DateTime.UtcNow;
        return query.Where(reading => reading.Meter.Building != null &&
            reading.Meter.Building.CustomerAssignments.Any(link =>
                link.ValidFrom <= now && (link.ValidTo == null || link.ValidTo > now) &&
                assignments.Any(a => a.CustomerId == link.CustomerId)));
    }

    public IQueryable<Document> ApplyDocumentScope(IQueryable<Document> query)
    {
        if (_currentUser.IsEnsetEmployee)
            return query;

        var assignments = ValidAssignments();
        return query.Where(document =>
            assignments.Any(a => a.CustomerId == document.Project.CustomerId));
    }

    public IQueryable<DataProduct> ApplyDataProductScope(IQueryable<DataProduct> query)
    {
        if (_currentUser.IsEnsetEmployee)
            return query;

        var assignments = ValidAssignments();
        return query.Where(product => product.ScopeAssignments.Any(scope =>
            (scope.CustomerId != null &&
             assignments.Any(a => a.CustomerId == scope.CustomerId)) ||
            (scope.BuildingId != null && scope.Building!.CustomerAssignments.Any(link =>
                assignments.Any(a => a.CustomerId == link.CustomerId)))));
    }

    public Task<bool> CanReadCustomer(Guid id, CancellationToken ct = default) =>
        ApplyCustomerScope(_db.Customers).AnyAsync(x => x.Id == id, ct);

    public Task<bool> CanWriteCustomer(Guid id, CancellationToken ct = default) =>
        HasCustomerRole(id, true, ct);

    public Task<bool> CanAdministerCustomer(Guid id, CancellationToken ct = default) =>
        HasCustomerRole(id, false, ct, UserCustomerRole.CustomerAdmin);

    public Task<bool> CanReadBuilding(Guid id, CancellationToken ct = default) =>
        ApplyBuildingScope(_db.Buildings).AnyAsync(x => x.Id == id, ct);

    public Task<bool> CanWriteBuilding(Guid id, CancellationToken ct = default) =>
        CanWriteObjectCustomer(_db.CustomerBuildingAssignments
            .Where(x => x.BuildingId == id).Select(x => x.CustomerId), ct);

    public Task<bool> CanReadMeter(Guid id, CancellationToken ct = default) =>
        ApplyMeterScope(_db.Meters).AnyAsync(x => x.Id == id, ct);

    public Task<bool> CanWriteMeter(Guid id, CancellationToken ct = default) =>
        CanWriteObjectCustomer(_db.Meters.Where(x => x.Id == id && x.BuildingId != null)
            .SelectMany(x => x.Building!.CustomerAssignments.Select(a => a.CustomerId)), ct);

    public Task<bool> CanReadDocument(Guid id, CancellationToken ct = default) =>
        ApplyDocumentScope(_db.Documents).AnyAsync(x => x.Id == id, ct);

    public Task<bool> CanWriteDocument(Guid id, CancellationToken ct = default) =>
        CanWriteObjectCustomer(_db.Documents.Where(x => x.Id == id)
            .Select(x => x.Project.CustomerId), ct);

    private IQueryable<UserCustomerAssignment> ValidAssignments()
    {
        if (!_currentUser.UserId.HasValue)
            return _db.UserCustomerAssignments.Where(_ => false);

        var userId = _currentUser.UserId.Value;
        var now = DateTimeOffset.UtcNow;
        return _db.UserCustomerAssignments.Where(a => a.UserId == userId &&
            a.IsActive && a.ValidFrom <= now &&
            (a.ValidTo == null || a.ValidTo > now));
    }

    private Task<bool> HasCustomerRole(Guid customerId, bool allowUser,
        CancellationToken ct, params UserCustomerRole[] roles)
    {
        if (_currentUser.IsEnsetEmployee)
            return Task.FromResult(true);

        var allowed = roles.Length > 0
            ? roles
            : allowUser
                ? new[] { UserCustomerRole.CustomerAdmin, UserCustomerRole.CustomerUser }
                : new[] { UserCustomerRole.CustomerAdmin };
        return ValidAssignments().AnyAsync(a =>
            a.CustomerId == customerId && allowed.Contains(a.Role), ct);
    }

    private async Task<bool> CanWriteObjectCustomer(
        IQueryable<Guid> customerIds, CancellationToken ct)
    {
        if (_currentUser.IsEnsetEmployee)
            return true;

        return await ValidAssignments().AnyAsync(a =>
            customerIds.Contains(a.CustomerId) &&
            (a.Role == UserCustomerRole.CustomerAdmin ||
             a.Role == UserCustomerRole.CustomerUser), ct);
    }
}
