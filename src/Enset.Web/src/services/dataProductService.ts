import { apiGet, apiPost } from "../api";
import type {
  Availability,
  DataProductSummary,
  GenerateDataProductResponse,
  ProductVersion,
  VersionHistory,
} from "../features/dataProducts/types";

interface GenerateDataProductRequest {
  customerId: string;
  periodFrom: string;
  periodTo: string;
}

const productPath = (id: string): string => {
  const value = id.trim();
  if (!value) throw new Error("Eine Data-Product-ID ist erforderlich.");
  return `/api/v1/data-products/${encodeURIComponent(value)}`;
};

export const dataProductService = {
  list: (): Promise<DataProductSummary[]> =>
    apiGet<DataProductSummary[]>("/api/v1/data-products"),

  get: (id: string): Promise<DataProductSummary> =>
    apiGet<DataProductSummary>(productPath(id)),

  availability(
    id: string,
    customerId: string,
    from: string,
    to: string,
  ): Promise<Availability> {
    const parameters = new URLSearchParams({
      customerId,
      periodFrom: new Date(from).toISOString(),
      periodTo: new Date(to).toISOString(),
    });
    return apiGet<Availability>(
      `${productPath(id)}/generation-availability?${parameters.toString()}`,
    );
  },

  generate(
    id: string,
    customerId: string,
    from: string,
    to: string,
  ): Promise<GenerateDataProductResponse> {
    return apiPost<GenerateDataProductResponse, GenerateDataProductRequest>(
      `${productPath(id)}/generate`,
      {
        customerId,
        periodFrom: new Date(from).toISOString(),
        periodTo: new Date(to).toISOString(),
      },
    );
  },

  latest: (id: string): Promise<ProductVersion> =>
    apiGet<ProductVersion>(`${productPath(id)}/versions/latest`),

  versions: (id: string): Promise<VersionHistory[]> =>
    apiGet<VersionHistory[]>(`${productPath(id)}/versions`),
};
