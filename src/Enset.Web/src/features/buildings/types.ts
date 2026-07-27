import type { EntityMetadata } from "../crud/types";

export interface BuildingSummary {
  id: string;
  buildingNumber: string;
  name: string;
  buildingCategory: string | null;
  primaryUseType: string | null;
  customerNumber: string | null;
  customerName: string | null;
  meterCount: number;
  benchmarkState: string;
  dataMaturity: "Bronze" | "Silver" | "Gold";
  goldReadinessPercent: number;
  isDeleted: boolean;
}

export interface BuildingCustomer {
  customerId: string;
  customerNumber: string;
  customerName: string;
  role: string;
  isPrimary: boolean;
}

export interface BuildingMeter {
  id: string;
  meterNumber: string;
  name: string;
  medium: string;
  direction: string;
  unit: string;
  dataMaturity: "Bronze" | "Silver" | "Gold";
  isActive: boolean;
}

export interface BuildingDetail extends EntityMetadata {
  id: string;
  buildingNumber: string;
  name: string;
  externalIdentifier: string | null;
  isActive: boolean;
  meterCount: number;
  firstReadingAt: string | null;
  lastReadingAt: string | null;
  grossFloorAreaM2: number | null;
  heatedFloorAreaM2: number | null;
  yearOfConstruction: number | null;
  yearOfLastMajorRenovation: number | null;
  latitude: number | null;
  longitude: number | null;
  buildingCategory: string | null;
  primaryUseType: string | null;
  benchmarkState: string;
  postalCode: string | null;
  city: string | null;
  street: string | null;
  houseNumber: string | null;
  customers: BuildingCustomer[];
  meters: BuildingMeter[];
}

export interface BuildingWriteModel {
  buildingNumber: string;
  name: string;
  externalIdentifier: string | null;
  customerId: string | null;
  grossFloorAreaM2: number | null;
  heatedFloorAreaM2: number | null;
  yearOfConstruction: number | null;
  yearOfLastMajorRenovation: number | null;
  buildingCategory: string | null;
  primaryUseType: string | null;
  benchmarkState: string | null;
  postalCode: string | null;
  city: string | null;
  street: string | null;
  houseNumber: string | null;
  latitude: number | null;
  longitude: number | null;
  rowVersion: number;
}

export interface BuildingListQuery {
  search?: string;
  customerId?: string;
  includeDeleted?: boolean;
  page?: number;
  pageSize?: number;
  sortBy?: string;
  sortDirection?: "asc" | "desc";
}
