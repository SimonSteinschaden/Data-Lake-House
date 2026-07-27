import { apiGet, apiPost } from "../api";
import type { CurationReadiness, CurationStatistics, CurationTaskDetail, CurationTaskPage, MeteringPointGoldProfile } from "../features/curation/types";
export const curationService = {
  tasks(query = new URLSearchParams(), signal?: AbortSignal) { return apiGet<CurationTaskPage>(`/api/v1/curation/tasks?${query}`, { signal }); },
  task(id: string, signal?: AbortSignal) { return apiGet<CurationTaskDetail>(`/api/v1/curation/tasks/${encodeURIComponent(id)}`, { signal }); },
  statistics(signal?: AbortSignal) { return apiGet<CurationStatistics>("/api/v1/curation/statistics", { signal }); },
  accept(id: string) { return apiPost<CurationTaskDetail>(`/api/v1/curation/tasks/${encodeURIComponent(id)}/accept`); },
  reject(id: string, reason: string) { return apiPost<CurationTaskDetail, { reason: string }>(`/api/v1/curation/tasks/${encodeURIComponent(id)}/reject`, { reason }); },
  customize(id: string, value: string, reason: string) { return apiPost<CurationTaskDetail, { value: string; reason: string }>(`/api/v1/curation/tasks/${encodeURIComponent(id)}/customize`, { value, reason }); },
  buildingReadiness(id:string,signal?:AbortSignal){return apiGet<CurationReadiness>(`/api/v1/curation/buildings/${encodeURIComponent(id)}/readiness`,{signal});},
  meterReadiness(id:string,signal?:AbortSignal){return apiGet<CurationReadiness>(`/api/v1/curation/metering-points/${encodeURIComponent(id)}/readiness`,{signal});},
  meterProfile(id:string,signal?:AbortSignal){return apiGet<MeteringPointGoldProfile>(`/api/v1/curation/metering-points/${encodeURIComponent(id)}/profile`,{signal});},
};
