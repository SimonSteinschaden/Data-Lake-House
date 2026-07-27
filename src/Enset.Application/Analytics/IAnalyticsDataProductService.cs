namespace Enset.Application.Analytics;

public interface IAnalyticsDataProductService
{
    Task<PortfolioSummaryDataProduct> GetPortfolioSummaryAsync(CancellationToken cancellationToken);
    Task<RegionalBuildingDistributionDataProduct> GetRegionalBuildingDistributionAsync(CancellationToken cancellationToken);
    Task<ElectricityPortfolioLoadProfileDataProduct> GetPortfolioLoadProfileAsync(AnalyticsQuery query, CancellationToken cancellationToken);
    Task<MonthlyElectricityConsumptionDataProduct> GetMonthlyElectricityConsumptionAsync(AnalyticsQuery query, CancellationToken cancellationToken);
    Task<MeteringCoverageSummaryDataProduct> GetMeteringCoverageAsync(CancellationToken cancellationToken);
    Task<DataQualitySummaryDataProduct> GetDataQualityAsync(CancellationToken cancellationToken);
    Task<EnergyPortfolioStructureDataProduct> GetEnergyPortfolioAsync(CancellationToken cancellationToken);
    Task<ManagementWarningsDataProduct> GetWarningsAsync(CancellationToken cancellationToken);
    Task<ManagementWarningDetailsDataProduct> GetWarningsDetailsAsync(
        int page,
        int pageSize,
        string? severity,
        CancellationToken cancellationToken);
    Task<EnergyConsumptionByLocationDataProduct> GetConsumptionByLocationAsync(AnalyticsQuery query, int limit, CancellationToken cancellationToken);
    Task<EnergyConsumptionByUsageTypeDataProduct> GetConsumptionByUsageTypeAsync(AnalyticsQuery query, CancellationToken cancellationToken);
    Task<TopEnergySystemsByConsumptionDataProduct> GetTopEnergySystemsAsync(AnalyticsQuery query, int limit, CancellationToken cancellationToken);
    Task<EnergyConsumptionByCarrierDataProduct> GetConsumptionByCarrierAsync(AnalyticsQuery query, CancellationToken cancellationToken);
}
