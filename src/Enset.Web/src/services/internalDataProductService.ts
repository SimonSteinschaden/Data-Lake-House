import { apiGet } from "../api";
import type {
  BuildingSummaryProduct,
  CustomerSummaryProduct,
  ImportQualityProduct,
  MeterSummaryProduct,
  PortfolioSummaryProduct,
} from "../features/internalDataProducts/types";

const root = "/api/v1/internal-data-products";

export const internalDataProductService = {
  portfolio: (signal?: AbortSignal) =>
    apiGet<PortfolioSummaryProduct>(`${root}/portfolio/summary`, { signal }),
  importQuality: (signal?: AbortSignal) =>
    apiGet<ImportQualityProduct>(`${root}/import-quality`, { signal }),
  building: (id: string, signal?: AbortSignal) =>
    apiGet<BuildingSummaryProduct>(`${root}/buildings/${id}/summary`, { signal }),
  meter: (id: string, signal?: AbortSignal) =>
    apiGet<MeterSummaryProduct>(`${root}/meters/${id}/summary`, { signal }),
  customer: (id: string, signal?: AbortSignal) =>
    apiGet<CustomerSummaryProduct>(`${root}/customers/${id}/summary`, { signal }),
};
