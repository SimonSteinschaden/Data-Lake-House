export type { PagedResult } from "../../types/paging";

export interface CustomerSummary {
  id: string;
  customerNumber: string;
  name: string;
  type: string;
  isActive: boolean;
  buildingCount: number;
}

export interface CustomerBuilding {
  id: string;
  buildingNumber: string;
  name: string;
  role: string;
  isPrimary: boolean;
}

import type { EntityMetadata } from "../crud/types";

export interface CustomerDetail extends EntityMetadata {
  id: string;
  customerNumber: string;
  name: string;
  legalName: string | null;
  type: string;
  email: string | null;
  phone: string | null;
  website: string | null;
  street: string | null;
  houseNumber: string | null;
  postalCode: string | null;
  city: string | null;
  countryCode: string;
  isActive: boolean;
  buildings: CustomerBuilding[];
}

export interface CustomerListQuery {
  search?: string;
  isActive?: boolean;
  page?: number;
  pageSize?: number;
  sortBy?: string;
  sortDirection?: "asc" | "desc";
}

export interface CustomerWriteModel {
  customerNumber: string;
  name: string;
  type: string;
  legalName: string | null;
  email: string | null;
  phone: string | null;
  countryCode: string;
  rowVersion: number;
}
