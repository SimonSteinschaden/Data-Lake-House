import { apiGet, apiPost } from "../api";
import type { CurationStatistics, CurationTask, CurationTaskDetail } from "../features/curation/types";
export const curationService = {
  tasks(signal?: AbortSignal) { return apiGet<CurationTask[]>("/api/v1/curation/tasks", { signal }); },
  task(id: string, signal?: AbortSignal) { return apiGet<CurationTaskDetail>(`/api/v1/curation/tasks/${encodeURIComponent(id)}`, { signal }); },
  statistics(signal?: AbortSignal) { return apiGet<CurationStatistics>("/api/v1/curation/statistics", { signal }); },
  accept(id: string) { return apiPost<CurationTaskDetail>(`/api/v1/curation/tasks/${encodeURIComponent(id)}/accept`); },
  reject(id: string, reason: string) { return apiPost<CurationTaskDetail, { reason: string }>(`/api/v1/curation/tasks/${encodeURIComponent(id)}/reject`, { reason }); },
  customize(id: string, value: string, reason: string) { return apiPost<CurationTaskDetail, { value: string; reason: string }>(`/api/v1/curation/tasks/${encodeURIComponent(id)}/customize`, { value, reason }); },
};
