namespace Enset.Application.ReadModel;

public interface IEntityReadService
{
    Task<PagedResult<CustomerSummaryDto>> GetCustomersAsync(CustomerListQuery query,
        CancellationToken cancellationToken = default);
    Task<CustomerDetailDto?> GetCustomerAsync(Guid customerId,
        CancellationToken cancellationToken = default);
    Task<PagedResult<BuildingSummaryDto>> GetBuildingsAsync(BuildingListQuery query,
        CancellationToken cancellationToken = default);
    Task<BuildingDetailDto?> GetBuildingAsync(Guid buildingId,
        CancellationToken cancellationToken = default);
    Task<PagedResult<MeterSummaryDto>> GetMetersAsync(MeterListQuery query,
        CancellationToken cancellationToken = default);
    Task<MeterDetailDto?> GetMeterAsync(Guid meterId,
        CancellationToken cancellationToken = default);
    Task<MeterReadingsDto?> GetMeterReadingsAsync(Guid meterId, MeterReadingQuery query,
        CancellationToken cancellationToken = default);
}
