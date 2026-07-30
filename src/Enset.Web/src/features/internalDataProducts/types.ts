export interface DataMaturitySummary {
  bronzeMaturity: number;
  silverMaturity: number;
  goldMaturity: number;
  goldMaturityPercentage: number;
}

export interface ReadinessSummary {
  status: string;
  percentage: number;
  blockingIssues: string[];
}

export interface EnergySummaryItem {
  energyCarrier: string;
  direction: string;
  annualValue: number;
  unit: string;
  origin: string;
  meterCount: number;
}

export interface DataQualityWarning {
  code: string;
  severity: string;
  message: string;
}
export interface PortfolioReadinessSummary {
  product: string;
  status: string;
  percentage: number | null;
  readyScopeCount: number;
  blockedScopeCount: number;
  topBlockers: string[];
  recommendations: string[];
  detailPath: string;
}

export interface PortfolioSummaryProduct {
  customerCount: number;
  activeCustomerCount: number;
  buildingCount: number;
  activeBuildingCount: number;
  meterCount: number;
  activeMeterCount: number;
  totalAnnualConsumption: number | null;
  totalAnnualGeneration: number | null;
  energySummaries: EnergySummaryItem[];
  averageMaturity: DataMaturitySummary;
  releasedBuildingGoldProfileCount: number;
  releasedMeterGoldProfileCount: number;
  metersWithoutReleasedGoldProfile: number;
  openCurationTaskCount: number;
  readyBuildingBenchmarkCount: number;
  readyEnergyBenchmarkCount: number;
  readyNormalizedLoadProfileCount: number;
  readyNormalizedGenerationProfileCount: number;
  blockedReadinessCount: number;
  buildingsWithoutMeters: number;
  metersWithoutReadings: number;
  metersWithIncompleteProfiles: number;
  buildingsWithoutReleasedGoldProfile: number;
  lastSuccessfulImportAt: string | null;
  failedImportCountLast30Days: number;
  openImportIssueCount: number;
  dataQualityWarnings: DataQualityWarning[];
  readinessSummaries: PortfolioReadinessSummary[];
  calculatedAt: string;
}

export interface ImportQualityItem {
  importId: string;
  fileName: string | null;
  importType: string | null;
  createdAt: string;
  status: string;
  issueCount: number;
  blockingIssueCount: number;
  warningCount: number;
  targetMeteringPointId: string | null;
  targetMeteringPointNumber: string | null;
  committedAt: string | null;
}

export interface ImportQualityProduct {
  totalImportCount: number;
  successfulImportCount: number;
  failedImportCount: number;
  pendingImportCount: number;
  importsWithOpenIssuesCount: number;
  totalOpenIssues: number;
  totalBlockingIssues: number;
  totalWarnings: number;
  openResolutionCount: number;
  openCurationTaskCount: number;
  latestImports: ImportQualityItem[];
  calculatedAt: string;
}

export interface BuildingSummaryProduct {
  buildingId: string;
  buildingNumber: string;
  name: string;
  meterCount: number;
  annualConsumption: number | null;
  annualGeneration: number | null;
  unit: string | null;
  maturity: DataMaturitySummary;
  openCurationTaskCount: number;
  dataQualityWarnings: DataQualityWarning[];
}

export interface MeterSummaryProduct {
  meteringPointId: string;
  annualValue: number | null;
  annualValueOrigin: string;
  readingCount: number;
  completenessPercentage: number | null;
  maturity: DataMaturitySummary;
  openCurationTaskCount: number;
}

export interface CustomerSummaryProduct {
  customerId: string;
  buildingCount: number;
  meterCount: number;
  totalAnnualConsumption: number | null;
  totalAnnualGeneration: number | null;
  energySummaries: EnergySummaryItem[];
  averageMaturity: DataMaturitySummary;
  openCurationTaskCount: number;
  dataQualityWarnings: DataQualityWarning[];
}
