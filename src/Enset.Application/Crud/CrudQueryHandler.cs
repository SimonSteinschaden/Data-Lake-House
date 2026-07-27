using Enset.Application.ReadModel;

namespace Enset.Application.Crud;

public sealed class CrudQueryHandler(IEntityReadService reads, IEntityCrudService crud)
{
    public Task<PagedResult<CustomerSummaryDto>> Handle(GetCustomersQuery q, CancellationToken ct) =>
        reads.GetCustomersAsync(new(q.Search, Page: q.Page, PageSize: q.PageSize, IncludeDeleted: q.IncludeDeleted), ct);
    public Task<CustomerDetailDto?> Handle(GetCustomerByIdQuery q, CancellationToken ct) =>
        reads.GetCustomerAsync(q.Id, q.IncludeDeleted, ct);
    public Task<PagedResult<BuildingSummaryDto>> Handle(GetBuildingsQuery q, CancellationToken ct) =>
        reads.GetBuildingsAsync(new(q.Search, CustomerId: q.CustomerId, Page: q.Page,
            PageSize: q.PageSize, IncludeDeleted: q.IncludeDeleted), ct);
    public Task<BuildingDetailDto?> Handle(GetBuildingByIdQuery q, CancellationToken ct) =>
        reads.GetBuildingAsync(q.Id, q.IncludeDeleted, ct);
    public Task<PagedResult<MeterSummaryDto>> Handle(GetMeteringPointsQuery q, CancellationToken ct) =>
        reads.GetMetersAsync(new(q.Search, Page: q.Page, PageSize: q.PageSize,
            IncludeDeleted: q.IncludeDeleted), ct);
    public Task<MeterDetailDto?> Handle(GetMeteringPointByIdQuery q, CancellationToken ct) =>
        reads.GetMeterAsync(q.Id, q.IncludeDeleted, ct);
    public Task<PagedResult<EnergySystemDto>> Handle(GetEnergySystemsQuery q, CancellationToken ct) =>
        crud.GetEnergySystemsAsync(new(q.Page, q.PageSize, q.Search, q.IncludeDeleted), ct);
    public Task<EnergySystemDto?> Handle(GetEnergySystemByIdQuery q, CancellationToken ct) =>
        crud.GetEnergySystemAsync(q.Id, q.IncludeDeleted, ct);
    public Task<PagedResult<MeterReadingDto>> Handle(GetMeterReadingsQuery q, CancellationToken ct) =>
        crud.GetMeterReadingsAsync(q.MeteringPointId, new(q.Page, q.PageSize, IncludeDeleted: q.IncludeDeleted), ct);
    public Task<MeterReadingDto?> Handle(GetMeterReadingByIdQuery q, CancellationToken ct) =>
        crud.GetMeterReadingAsync(q.Id, q.IncludeDeleted, ct);
    public async Task<IReadOnlyList<AuditHistoryItem>> Handle(GetAuditHistoryQuery q, CancellationToken ct)
    {
        var allowed = new[] { "Customer", "Building", "Meter", "EnergySystem", "MeterReading" };
        var canonical = allowed.FirstOrDefault(x => x.Equals(q.EntityType, StringComparison.OrdinalIgnoreCase));
        if (canonical is null)
            throw new CrudValidationException(new Dictionary<string, string[]>
                { [nameof(q.EntityType)] = ["Der Entitätstyp ist ungültig."] });
        var history = await crud.GetAuditHistoryAsync(canonical, q.EntityId, ct);
        return history.Skip((Math.Max(1, q.Page) - 1) * Math.Clamp(q.PageSize, 1, 200))
            .Take(Math.Clamp(q.PageSize, 1, 200)).ToList();
    }
}
