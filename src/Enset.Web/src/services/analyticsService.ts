import { apiGet } from "../api";
import type {
  ConsumptionByCarrier,
  ConsumptionByLocation,
  ConsumptionByUsageType,
  DataQualitySummary,
  EnergyPortfolio,
  LoadProfile,
  ManagementWarningDetails,
  ManagementWarnings,
  MeteringCoverage,
  MonthlyConsumption,
  PortfolioSummary,
  RegionalBuildingDistribution,
} from "../features/analytics/types";

export interface AnalyticsFilters {
  year: number;
  aggregation?: "QuarterHour" | "Hour" | "Day" | "Week" | "Month";
  customerId?: string;
  buildingId?: string;
  meterId?: string;
  region?: string;
  postalCode?: string;
}

function query(filters: AnalyticsFilters): string {
  const parameters = new URLSearchParams({ year: String(filters.year) });
  if (filters.aggregation) parameters.set("aggregation", filters.aggregation);
  if (filters.customerId) parameters.set("customerId", filters.customerId);
  if (filters.buildingId) parameters.set("buildingId", filters.buildingId);
  if (filters.meterId) parameters.set("meterId", filters.meterId);
  if (filters.region) parameters.set("region", filters.region);
  if (filters.postalCode) parameters.set("postalCode", filters.postalCode);
  return parameters.toString();
}

export const analyticsService = {
  portfolioSummary: (signal?: AbortSignal) =>
    apiGet<PortfolioSummary>("/api/v1/analytics/portfolio-summary", { signal }),
  regionalBuildingDistribution: (signal?: AbortSignal) =>
    apiGet<RegionalBuildingDistribution>(
      "/api/v1/analytics/regional-building-distribution", { signal }),
  loadProfile: (filters: AnalyticsFilters, signal?: AbortSignal) =>
    apiGet<LoadProfile>(
      `/api/v1/analytics/portfolio-load-profile?${query(filters)}`, { signal }),
  monthlyConsumption: (filters: AnalyticsFilters, signal?: AbortSignal) =>
    apiGet<MonthlyConsumption>(
      `/api/v1/analytics/monthly-electricity-consumption?${query(filters)}`,
      { signal }),
  meteringCoverage: (signal?: AbortSignal) =>
    apiGet<MeteringCoverage>("/api/v1/analytics/metering-coverage", { signal }),
  dataQuality: (signal?: AbortSignal) =>
    apiGet<DataQualitySummary>("/api/v1/analytics/data-quality", { signal }),
  energyPortfolio: (signal?: AbortSignal) =>
    apiGet<EnergyPortfolio>("/api/v1/analytics/energy-portfolio", { signal }),
  warnings: (signal?: AbortSignal) =>
    apiGet<ManagementWarnings>("/api/v1/analytics/warnings", { signal }),
  warningsDetails: (
    page = 1,
    pageSize = 50,
    severity?: string,
    signal?: AbortSignal,
  ) => {
    const params = new URLSearchParams({
      page: String(page),
      pageSize: String(pageSize),
    });
    if (severity) {
      params.set("severity", severity);
    }
    return apiGet<ManagementWarningDetails>(
      `/api/v1/analytics/warnings?${params.toString()}`,
      { signal },
    );
  },
  consumptionByLocation: (filters: AnalyticsFilters, signal?: AbortSignal) =>
    apiGet<ConsumptionByLocation>(
      `/api/v1/analytics/consumption-by-location?${query(filters)}&limit=10`,
      { signal }),
  consumptionByUsageType: (filters: AnalyticsFilters, signal?: AbortSignal) =>
    apiGet<ConsumptionByUsageType>(
      `/api/v1/analytics/consumption-by-usage-type?${query(filters)}`, { signal }),
  consumptionByCarrier: (filters: AnalyticsFilters, signal?: AbortSignal) =>
    apiGet<ConsumptionByCarrier>(
      `/api/v1/analytics/consumption-by-carrier?${query(filters)}`, { signal }),
};
