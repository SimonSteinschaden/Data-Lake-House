import { apiGet, apiPost } from "../api";
import { authenticatedFetch } from "../api/authenticatedFetch";

export interface ReportDefinition {
  type: string;
  title: string;
  description: string;
  supportedFormats: string[];
}

export interface ReportInstance {
  reportId: string;
  type: string;
  buildingId: string;
  buildingName: string;
  fromUtc: string;
  toUtc: string;
  version: number;
  createdAtUtc: string;
  recipient: string;
  releaseStatus: string;
  qualityLevel: string;
  suitability: string;
}

export const reportService = {
  definitions: () =>
    apiGet<ReportDefinition[]>("/api/v1/reports/definitions"),
  list: () => apiGet<ReportInstance[]>("/api/v1/reports"),
  create: (request: {
    type: string;
    buildingId: string;
    fromUtc: string;
    toUtc: string;
    recipient: string;
  }) => apiPost<ReportInstance, typeof request>("/api/v1/reports", request),
  download: async (report: ReportInstance, format: string) => {
    const response = await authenticatedFetch.fetch(
      `/api/v1/reports/${report.reportId}/export/${format}`,
    );
    if (!response.ok) throw new Error("Export fehlgeschlagen.");
    const blob = await response.blob();
    const url = URL.createObjectURL(blob);
    const anchor = document.createElement("a");
    anchor.href = url;
    anchor.download = `${report.type}-${report.buildingName}-v${report.version}.${format}`;
    anchor.click();
    URL.revokeObjectURL(url);
  },
};
