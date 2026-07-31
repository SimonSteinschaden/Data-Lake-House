import { apiDelete, apiGet, apiPost, apiPut } from "../api";
import type {
  BuildingDetail,
  BuildingListQuery,
  BuildingSummary,
  BuildingFormModel,
  BuildingCreateRequest,
  BuildingUpdateRequest,
} from "../features/buildings/types";
import type { EntityMutationResult } from "../features/crud/types";
import type { PagedResult } from "../types/paging";

const queryString = (query: BuildingListQuery): string => {
  const parameters = new URLSearchParams();
  if (query.search?.trim()) parameters.set("search", query.search.trim());
  if (query.customerId?.trim()) parameters.set("customerId", query.customerId.trim());
  if (query.includeDeleted) parameters.set("includeDeleted", "true");
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
      `/api/v1/buildings/${encodeURIComponent(buildingId)}?includeDeleted=true`, { signal },
    );
  },
  create(model: BuildingFormModel) {
    const { rowVersion, ...request } = model;
    void rowVersion;
    return apiPost<EntityMutationResult, BuildingCreateRequest>("/api/v1/buildings", request);
  },
  update(id: string, model: BuildingFormModel) {
    const request: BuildingUpdateRequest = model;
    return apiPut<EntityMutationResult, BuildingUpdateRequest>(
      `/api/v1/buildings/${encodeURIComponent(id)}`, request);
  },
  remove(id: string, rowVersion: number) {
    return apiDelete<EntityMutationResult>(`/api/v1/buildings/${encodeURIComponent(id)}?rowVersion=${rowVersion}`);
  },
  restore(id: string, rowVersion: number) {
    return apiPost<EntityMutationResult>(`/api/v1/buildings/${encodeURIComponent(id)}/restore?rowVersion=${rowVersion}`);
  },
};
