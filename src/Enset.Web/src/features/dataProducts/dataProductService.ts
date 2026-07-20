import type { Availability, DataProductSummary, ProductVersion, VersionHistory } from "./types";

const json = async <T>(response: Response): Promise<T> => {
  if (!response.ok) { const problem = await response.json().catch(() => ({})); throw new Error(problem.detail ?? problem.title ?? "API-Fehler"); }
  return response.json() as Promise<T>;
};
export const dataProductService = {
  list: () => fetch("/api/v1/data-products").then(json<DataProductSummary[]>),
  get: (id: string) => fetch(`/api/v1/data-products/${id}`).then(json<DataProductSummary>),
  availability: (id: string, customerId: string, from: string, to: string) => fetch(`/api/v1/data-products/${id}/generation-availability?customerId=${customerId}&periodFrom=${from}&periodTo=${to}`, { headers: { "X-User-Id": customerId } }).then(json<Availability>),
  generate: (id: string, customerId: string, from: string, to: string) => fetch(`/api/v1/data-products/${id}/generate`, { method: "POST", headers: { "Content-Type": "application/json", "X-User-Id": customerId }, body: JSON.stringify({ customerId, periodFrom: from, periodTo: to }) }).then(json),
  latest: (id: string) => fetch(`/api/v1/data-products/${id}/versions/latest`).then(json<ProductVersion>),
  versions: (id: string) => fetch(`/api/v1/data-products/${id}/versions`).then(json<VersionHistory[]>),
};
