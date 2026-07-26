import { apiGet } from "../api";
import type {
  CustomerDetail,
  CustomerListQuery,
  CustomerSummary,
  PagedResult,
} from "../features/customers/types";

const createQueryString = (query: CustomerListQuery): string => {
  const parameters = new URLSearchParams();

  if (query.search?.trim()) {
    parameters.set("search", query.search.trim());
  }

  if (query.isActive !== undefined) {
    parameters.set("isActive", String(query.isActive));
  }

  parameters.set("page", String(query.page ?? 1));
  parameters.set("pageSize", String(query.pageSize ?? 50));
  parameters.set("sortBy", query.sortBy ?? "name");
  parameters.set("sortDirection", query.sortDirection ?? "asc");

  return parameters.toString();
};

export const customerService = {
  list(
    query: CustomerListQuery = {},
    signal?: AbortSignal,
  ): Promise<PagedResult<CustomerSummary>> {
    const queryString = createQueryString(query);

    return apiGet<PagedResult<CustomerSummary>>(
      `/api/v1/customers?${queryString}`, { signal },
    );
  },

  get(customerId: string, signal?: AbortSignal): Promise<CustomerDetail> {
    if (!customerId.trim()) {
      return Promise.reject(
        new Error("Für den Kundenabruf ist eine Customer-ID erforderlich."),
      );
    }

    return apiGet<CustomerDetail>(
      `/api/v1/customers/${encodeURIComponent(customerId)}`, { signal },
    );
  },
};
