import { apiDelete, apiGet, apiPost, apiPut } from "../api";
import type { EnergySystem, EnergySystemWriteModel } from "../features/energySystems/types";
import type { EntityMutationResult } from "../features/crud/types";
import type { PagedResult } from "../types/paging";

export const energySystemService = {
  list(signal?: AbortSignal) {
    return apiGet<PagedResult<EnergySystem>>("/api/v1/energy-systems?page=1&pageSize=200", { signal });
  },
  create(model: EnergySystemWriteModel) {
    return apiPost<EntityMutationResult, EnergySystemWriteModel>("/api/v1/energy-systems", model);
  },
  update(id: string, model: EnergySystemWriteModel) {
    return apiPut<EntityMutationResult, EnergySystemWriteModel>(`/api/v1/energy-systems/${encodeURIComponent(id)}`, model);
  },
  remove(id: string, rowVersion: number) {
    return apiDelete<EntityMutationResult>(`/api/v1/energy-systems/${encodeURIComponent(id)}?rowVersion=${rowVersion}`);
  },
  restore(id: string, rowVersion: number) {
    return apiPost<EntityMutationResult>(`/api/v1/energy-systems/${encodeURIComponent(id)}/restore?rowVersion=${rowVersion}`);
  },
};
