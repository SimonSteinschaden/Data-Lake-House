import { apiGet } from "../api";
import type {
  BuildingDetail,
  BuildingListQuery,
  BuildingSummary,
} from "../features/buildings/types";
import type { PagedResult } from "../types/paging";

const queryString = (query: BuildingListQuery): string => {
  const parameters = new URLSearchParams();
  if (query.search?.trim()) parameters.set("search", query.search.trim());
  if (query.customerId?.trim()) parameters.set("customerId", query.customerId.trim());
  if (query.isActive !== undefined) parameters.set("isActive", String(query.isActive));
  parameters.set("page", String(query.page ?? 1));
  parameters.set("pageSize", String(query.pageSize ?? 50));
  parameters.set("sortBy", query.sortBy ?? "name");
  parameters.set("sortDirection", query.sortDirection ?? "asc");
  return parameters.toString();
};

export const buildingService = {
  list(query: BuildingListQuery = {}, signal?: AbortSignal): Promise<PagedResult<BuildingSummary>> {
    return apiGet<PagedResult<BuildingSummary>>(
      `/api/v1/buildings?${queryString(query)}`, { signal },
    );
  },

  get(buildingId: string, signal?: AbortSignal): Promise<BuildingDetail> {
    if (!buildingId.trim()) {
      return Promise.reject(new Error("Für den Gebäudeabruf ist eine Building-ID erforderlich."));
    }
    return apiGet<BuildingDetail>(
      `/api/v1/buildings/${encodeURIComponent(buildingId)}`, { signal },
    );
  },
};
