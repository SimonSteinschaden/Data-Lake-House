import type { PagedResult } from "../../types/paging";
import type { EntityMetadata } from "../crud/types";

export type MeterReadingAggregation =
  | "Raw"
  | "FifteenMinutes"
  | "Hour"
  | "Day"
  | "Month";

export interface MeterListQuery {
  search?: string;
  customerId?: string;
  buildingId?: string;
  isActive?: boolean;
  includeDeleted?: boolean;
  page?: number;
  pageSize?: number;
  sortBy?: string;
  sortDirection?: "asc" | "desc";
}

export interface MeterReadingQuery {
  from?: string;
  to?: string;
  aggregation?: MeterReadingAggregation;
  page?: number;
  pageSize?: number;
  sortDirection?: "asc" | "desc";
}

export interface MeterSummary {
  id: string;
  meterNumber: string;
  name: string;
  medium: string;
  unit: string;
  direction: string;
  buildingId: string | null;
  buildingNumber: string | null;
  buildingName: string | null;
  customerNumber: string | null;
  customerName: string | null;
  annualValue: number | null;
  annualValueOrigin: string | null;
  readingCount: number;
  firstReadingAt: string | null;
  lastReadingAt: string | null;
  dataMaturity: "Bronze" | "Silver" | "Gold";
  goldReadinessPercent: number;
  isDeleted: boolean;
}

export interface MeterDetail extends MeterSummary, EntityMetadata {
  description: string | null;
  externalIdentifier: string | null;
  medium: string;
  manufacturer: string | null;
  model: string | null;
  serialNumber: string | null;
  latestReadingAt: string | null;
  latestValue: number | null;
  latestQuality: string | null;
  buildingNumber: string | null;
  customerNumber: string | null;
  customerName: string | null;
  annualValue: number | null;
  annualValueOrigin: string | null;
  intervalSeconds: number | null;
  isActive: boolean;
  quantity: string;
  type: string;
}

export interface MeterWriteModel {
  meterNumber: string; name: string; buildingId: string; medium: string;
  quantity: string; unit: string; direction: string; type: string;
  energySystemId: string | null; rowVersion: number; description: string | null;
  externalIdentifier: string | null; annualValue: number | null;
}

export interface RawMeterReading {
  timestamp: string;
  value: number;
  quality: string;
  intervalSeconds: number | null;
}

export interface AggregatedMeterReading {
  bucketStart: string;
  readingType: string;
  minimum: number;
  maximum: number;
  average: number;
  sum: number | null;
  firstValue: number;
  lastValue: number;
  delta: number;
  count: number;
}

export interface MeterReadings {
  meterId: string;
  meterNumber: string;
  unit: string;
  quantity: string;
  readingType: string;
  aggregation: string;
  availableFrom: string | null;
  availableTo: string | null;
  requestedFrom: string | null;
  requestedTo: string | null;
  raw: PagedResult<RawMeterReading> | null;
  aggregated: AggregatedMeterReading[] | null;
}
