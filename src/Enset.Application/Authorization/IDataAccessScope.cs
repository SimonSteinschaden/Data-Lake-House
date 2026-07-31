using Enset.Domain.Buildings;
using Enset.Domain.Customers;
using Enset.Domain.Documents;
using Enset.Domain.Energy;
using Enset.Domain.DataProducts;

namespace Enset.Application.Authorization;

/// <summary>
/// Applies tenant-aware, database-translatable object access rules.
/// </summary>
public interface IDataAccessScope
{
    IQueryable<Customer> ApplyCustomerScope(IQueryable<Customer> query);
    IQueryable<Building> ApplyBuildingScope(IQueryable<Building> query);
    IQueryable<Meter> ApplyMeterScope(IQueryable<Meter> query);
    IQueryable<MeterReading> ApplyMeterReadingScope(IQueryable<MeterReading> query);
    IQueryable<Document> ApplyDocumentScope(IQueryable<Document> query);
    IQueryable<DataProduct> ApplyDataProductScope(IQueryable<DataProduct> query);

    Task<bool> CanReadCustomer(Guid customerId, CancellationToken cancellationToken = default);
    Task<bool> CanWriteCustomer(Guid customerId, CancellationToken cancellationToken = default);
    Task<bool> CanAdministerCustomer(Guid customerId, CancellationToken cancellationToken = default);
    Task<bool> CanReadBuilding(Guid buildingId, CancellationToken cancellationToken = default);
    Task<bool> CanWriteBuilding(Guid buildingId, CancellationToken cancellationToken = default);
    Task<bool> CanReadMeter(Guid meterId, CancellationToken cancellationToken = default);
    Task<bool> CanWriteMeter(Guid meterId, CancellationToken cancellationToken = default);
    Task<bool> CanReadDocument(Guid documentId, CancellationToken cancellationToken = default);
    Task<bool> CanWriteDocument(Guid documentId, CancellationToken cancellationToken = default);
}
