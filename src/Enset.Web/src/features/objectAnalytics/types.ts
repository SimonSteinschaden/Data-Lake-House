export interface SnapshotQuality {
  level: string;
  completenessPercentage: number;
  validityPercentage: number;
  consistencyPercentage: number;
  curationPercentage: number;
}

export interface AnalyticsValue {
  value: number | null;
  unit: string;
  status: string;
  source: string;
  rule: string;
}

export interface SeriesPoint {
  timestamp: string;
  value: number;
}

export interface Category {
  name: string;
  value: number;
  unit: string;
}

export interface ObjectSearchItem {
  buildingId: string;
  buildingNumber: string;
  name: string;
  customerId: string | null;
  customer: string | null;
  buildingType: string | null;
  municipality: string | null;
  address: string | null;
  meterNumbers: string[];
  quality: SnapshotQuality;
}

export interface ObjectSearchResult {
  items: ObjectSearchItem[];
  totalCount: number;
  page: number;
  pageSize: number;
}

export interface ObjectAnalyticsProduct {
  buildingId: string;
  buildingNumber: string;
  buildingName: string;
  fromUtc: string;
  toUtc: string;
  quality: SnapshotQuality;
  totalConsumption: AnalyticsValue;
  electricityConsumption: AnalyticsValue;
  heatConsumption: AnalyticsValue;
  energyProduction: AnalyticsValue;
  energyCosts: AnalyticsValue;
  co2Emissions: AnalyticsValue;
  peakLoad: AnalyticsValue;
  peakLoadAt: string | null;
  consumptionPerSquareMeter: AnalyticsValue;
  consumptionPerUser: AnalyticsValue;
  selfConsumption: AnalyticsValue;
  selfSufficiency: AnalyticsValue;
  monthlyConsumption: SeriesPoint[];
  dailyLoadProfile: SeriesPoint[];
  weeklyLoadProfile: SeriesPoint[];
  consumptionByCarrier: Category[];
  consumptionByUsageType: Category[];
  energySystems: {
    energySystemId: string;
    type: string | null;
    energyCarrier: string | null;
    installedPower: number | null;
    constructionYear: number | null;
    status: string;
  }[];
  totalInstalledPower: number | null;
  anomalies: { code: string; severity: string; message: string; rule: string }[];
  warnings: { code: string; severity: string; message: string; rule: string }[];
  completeness: { name: string; status: string; reason: string }[];
  consumptionPreviousYear: {
    current: number | null;
    previous: number | null;
    absoluteChange: number | null;
    percentChange: number | null;
    unit: string;
    status: string;
  };
  benchmark: {
    status: string;
    objectValue: number | null;
    comparisonValue: number | null;
    unit: string;
    comparisonCount: number;
    rule: string;
  };
}
