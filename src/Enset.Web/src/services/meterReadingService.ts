import { apiDelete, apiGet, apiPost, apiPut } from "../api";
import type { EntityMutationResult } from "../features/crud/types";
import type { MeterReading, MeterReadingWriteModel } from "../features/meterReadings/types";
import type { PagedResult } from "../types/paging";
export const meterReadingService = {
  list(meterId: string, from?: string, to?: string, page = 1, signal?: AbortSignal) {
    const query = new URLSearchParams({ meteringPointId: meterId, page: String(page), pageSize: "25" });
    if (from) query.set("from", new Date(from).toISOString());
    if (to) query.set("to", new Date(to).toISOString());
    return apiGet<PagedResult<MeterReading>>(`/api/v1/meter-readings?${query}`, { signal });
  },
  create(model: MeterReadingWriteModel) { return apiPost<EntityMutationResult, MeterReadingWriteModel>("/api/v1/meter-readings", model); },
  update(id: string, model: MeterReadingWriteModel) { return apiPut<EntityMutationResult, MeterReadingWriteModel>(`/api/v1/meter-readings/${encodeURIComponent(id)}`, model); },
  remove(id: string, rowVersion: number) { return apiDelete<EntityMutationResult>(`/api/v1/meter-readings/${encodeURIComponent(id)}?rowVersion=${rowVersion}`); },
};
