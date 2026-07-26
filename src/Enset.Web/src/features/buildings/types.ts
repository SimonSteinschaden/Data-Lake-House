export interface BuildingSummary {
  id: string;
  buildingNumber: string;
  name: string;
  externalIdentifier: string | null;
  isActive: boolean;
  meterCount: number;
  firstReadingAt: string | null;
  lastReadingAt: string | null;
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
  unit: string;
  quantity: string;
  isActive: boolean;
}

export interface BuildingDetail extends BuildingSummary {
  customers: BuildingCustomer[];
  meters: BuildingMeter[];
}

export interface BuildingListQuery {
  search?: string;
  customerId?: string;
  isActive?: boolean;
  page?: number;
  pageSize?: number;
  sortBy?: string;
  sortDirection?: "asc" | "desc";
}
