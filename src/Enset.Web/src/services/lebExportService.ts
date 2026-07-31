import { apiPost } from "../api/apiClient";
import { authenticatedFetch } from "../api/authenticatedFetch";

export interface LebExportValidationItem {
  code: string;
  table: string;
  rowId: string | null;
  field: string;
  message: string;
  severity: "Error" | "Warning";
}

export interface LebExportValidationResult {
  canExport: boolean;
  errors: LebExportValidationItem[];
  warnings: LebExportValidationItem[];
}

type LebExportFormat = "csv" | "excel";

const emptyRequest = {};

export class LebExportValidationError extends Error {
  constructor(
    public readonly validation: LebExportValidationResult,
  ) {
    super("Der LEB-Export enthält blockierende Validierungsfehler.");
  }
}

async function download(format: LebExportFormat): Promise<void> {
  const response = await authenticatedFetch.fetch(
    `/api/v1/exports/leb/${format}`,
    {
      method: "POST",
      body: JSON.stringify(emptyRequest),
    },
  );

  if (!response.ok) {
    if (response.status === 422) {
      throw new LebExportValidationError(
        (await response.json()) as LebExportValidationResult,
      );
    }

    throw new Error(
      `Der Download konnte nicht erstellt werden (${response.status}).`,
    );
  }

  const blob = await response.blob();
  const url = URL.createObjectURL(blob);
  const link = document.createElement("a");
  link.href = url;
  link.download =
    getFileName(response.headers.get("content-disposition")) ??
    `NoeLebExport.${format === "csv" ? "zip" : "xlsx"}`;
  document.body.appendChild(link);
  link.click();
  link.remove();
  URL.revokeObjectURL(url);
}

function getFileName(contentDisposition: string | null): string | null {
  if (!contentDisposition) {
    return null;
  }

  const encoded = contentDisposition.match(/filename\*=UTF-8''([^;]+)/i)?.[1];
  if (encoded) {
    return decodeURIComponent(encoded);
  }

  return (
    contentDisposition.match(/filename="?([^";]+)"?/i)?.[1] ?? null
  );
}

export const lebExportService = {
  validate(): Promise<LebExportValidationResult> {
    return apiPost<LebExportValidationResult>(
      "/api/v1/exports/leb/validate",
      emptyRequest,
    );
  },

  downloadCsv(): Promise<void> {
    return download("csv");
  },

  downloadExcel(): Promise<void> {
    return download("excel");
  },
};
