export interface PortfolioSummary {
  customerCount: number;
  buildingCount: number;
  meterCount: number;
  documentCount: number;
  energySystemCount: number;
  calculatedAt: string;
}

export interface RegionalBuildingDistributionItem {
  state: string | null;
  district: string | null;
  municipality: string | null;
  postalCode: string | null;
  buildingCount: number;
  customerCount: number;
  meterCount: number;
}

export interface RegionalBuildingDistribution {
  regions: RegionalBuildingDistributionItem[];
  calculatedAt: string;
}

export interface TimeSeriesPoint {
  timestamp: string;
  value: number;
}

export interface LoadProfile {
  year: number;
  aggregation: string;
  quantity: string;
  unit: string | null;
  points: TimeSeriesPoint[];
  dataCoveragePercent: number;
  lastReadingAt: string | null;
  calculatedAt: string;
}

export interface MonthlyConsumptionPoint {
  month: number;
  value: number | null;
  previousYearValue: number | null;
}

export interface MonthlyConsumption {
  year: number;
  quantity: string;
  unit: string | null;
  months: MonthlyConsumptionPoint[];
  totalConsumption: number | null;
  dataCoveragePercent: number;
  calculatedAt: string;
}

export interface MeteringCoverage {
  buildingsWithReadings: number;
  buildingsWithoutReadings: number;
  activeMeters: number;
  inactiveMeters: number;
  metersWithoutReadings: number;
  lastReadingAt: string | null;
  coveragePercent: number;
  calculatedAt: string;
}

export interface DataQualitySummary {
  overallScore: number | null;
  missingAssignments: number;
  staleReadings: number;
  incompleteMasterData: number;
  invalidUnits: number;
  timeSeriesGaps: number | null;
  duplicates: number | null;
  criticalWarnings: number;
  calculatedAt: string;
}

export interface NamedCount {
  name: string;
  count: number;
}

export interface EnergyPortfolio {
  buildingsWithElectricityMeters: number;
  buildingsWithHeatMeters: number;
  buildingsWithGasMeters: number;
  buildingsWithWaterMeters: number;
  energySystemCount: number;
  energySystemsByType: NamedCount[];
  metersByCarrier: NamedCount[];
  calculatedAt: string;
}

export interface ManagementWarning {
  code: string;
  severity: string;
  message: string;
  affectedCount: number;
}

export interface ManagementWarnings {
  warnings: ManagementWarning[];
  totalAffectedCount: number;
  calculatedAt: string;
}

export interface ManagementWarningDetail {
  code: string;
  severity: string;
  warningType: string;
  entityType: string | null;
  entityId: string | null;
  entity: string | null;
  buildingId: string | null;
  building: string | null;
  meterId: string | null;
  meter: string | null;
  description: string;
  cause: string;
  recommendation: string;
  status: string;
  occurredAt: string;
}

export interface ManagementWarningDetails {
  warnings: ManagementWarningDetail[];
  totalCount: number;
  page: number;
  pageSize: number;
  calculatedAt: string;
}

export interface ConsumptionByLocationItem {
  buildingId: string;
  building: string;
  municipality: string | null;
  postalCode: string | null;
  consumption: number;
  unit: string;
  meterCount: number;
  dataCoveragePercent: number | null;
}

export interface ConsumptionByLocation {
  year: number;
  locations: ConsumptionByLocationItem[];
  calculatedAt: string;
}

export interface ConsumptionCategoryItem {
  name: string;
  consumption: number;
  sharePercent: number;
  unit: string;
  meterCount: number;
  buildingCount: number;
}

export interface ConsumptionByUsageType {
  year: number;
  usageTypes: ConsumptionCategoryItem[];
  calculatedAt: string;
}

export interface ConsumptionByCarrierItem {
  carrier: string;
  consumption: number;
  sharePercent: number;
  unit: string;
  buildingCount: number;
  meterCount: number;
}

export interface ConsumptionByCarrier {
  year: number;
  unit: string;
  carriers: ConsumptionByCarrierItem[];
  calculatedAt: string;
}
