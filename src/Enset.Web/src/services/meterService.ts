import { apiGet } from "../api";
import type {
  MeterDetail,
  MeterListQuery,
  MeterReadingQuery,
  MeterReadings,
  MeterSummary,
} from "../features/meters/types";
import type { PagedResult } from "../types/paging";

const listQueryString = (query: MeterListQuery): string => {
  const parameters = new URLSearchParams();
  if (query.search?.trim()) parameters.set("search", query.search.trim());
  if (query.customerId?.trim()) parameters.set("customerId", query.customerId.trim());
  if (query.buildingId?.trim()) parameters.set("buildingId", query.buildingId.trim());
  if (query.isActive !== undefined) parameters.set("isActive", String(query.isActive));
  parameters.set("page", String(query.page ?? 1));
  parameters.set("pageSize", String(query.pageSize ?? 50));
  parameters.set("sortBy", query.sortBy ?? "meterNumber");
  parameters.set("sortDirection", query.sortDirection ?? "asc");
  return parameters.toString();
};

const readingsQueryString = (query: MeterReadingQuery): string => {
  const parameters = new URLSearchParams();
  if (query.from) parameters.set("from", new Date(query.from).toISOString());
  if (query.to) parameters.set("to", new Date(query.to).toISOString());
  parameters.set("aggregation", query.aggregation ?? "Raw");
  parameters.set("page", String(query.page ?? 1));
  parameters.set("pageSize", String(query.pageSize ?? 100));
  parameters.set("sortDirection", query.sortDirection ?? "asc");
  return parameters.toString();
};

const requireMeterId = (meterId: string): string => {
  const id = meterId.trim();
  if (!id) throw new Error("Für den Zählerabruf ist eine Meter-ID erforderlich.");
  return encodeURIComponent(id);
};

export const meterService = {
  list(query: MeterListQuery = {}, signal?: AbortSignal): Promise<PagedResult<MeterSummary>> {
    return apiGet<PagedResult<MeterSummary>>(
      `/api/v1/meters?${listQueryString(query)}`, { signal },
    );
  },

  get(meterId: string, signal?: AbortSignal): Promise<MeterDetail> {
    return apiGet<MeterDetail>(`/api/v1/meters/${requireMeterId(meterId)}`, { signal });
  },

  getReadings(meterId: string, query: MeterReadingQuery = {}, signal?: AbortSignal): Promise<MeterReadings> {
    return apiGet<MeterReadings>(
      `/api/v1/meters/${requireMeterId(meterId)}/readings?${readingsQueryString(query)}`, { signal },
    );
  },
};
