import { apiGet } from "../api";
import type {
  ObjectAnalyticsProduct,
  ObjectSearchResult,
} from "../features/objectAnalytics/types";

export interface ObjectAnalyticsFilters {
  search?: string;
  energyCarrier?: string;
  buildingType?: string;
  qualityLevel?: string;
  customerId?: string;
  fromUtc: string;
  toUtc: string;
  page?: number;
  pageSize?: number;
}

function parameters(filters: ObjectAnalyticsFilters) {
  const result = new URLSearchParams({
    fromUtc: filters.fromUtc,
    toUtc: filters.toUtc,
    page: String(filters.page ?? 1),
    pageSize: String(filters.pageSize ?? 25),
  });
  Object.entries(filters).forEach(([key, value]) => {
    if (value && !["fromUtc", "toUtc", "page", "pageSize"].includes(key)) {
      result.set(key, String(value));
    }
  });
  return result.toString();
}

export const objectAnalyticsService = {
  search: (filters: ObjectAnalyticsFilters, signal?: AbortSignal) =>
    apiGet<ObjectSearchResult>(
      `/api/v1/object-analytics/objects?${parameters(filters)}`,
      { signal },
    ),
  analyze: (
    buildingId: string,
    filters: ObjectAnalyticsFilters,
    signal?: AbortSignal,
  ) =>
    apiGet<ObjectAnalyticsProduct>(
      `/api/v1/object-analytics/${buildingId}?${parameters(filters)}`,
      { signal },
    ),
};
