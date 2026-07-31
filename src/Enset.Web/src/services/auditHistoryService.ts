import { apiGet } from "../api";
import type { AuditHistoryItem } from "../features/crud/types";
import type { PagedResult } from "../types/paging";

export const auditHistoryService = {
  get(entityType: string, entityId: string, signal?: AbortSignal) {
    return apiGet<PagedResult<AuditHistoryItem>>(
      `/api/v1/audit-history/${encodeURIComponent(entityType)}/${encodeURIComponent(entityId)}?page=1&pageSize=100`,
      { signal },
    );
  },
};
