import type { ImportIssueViewModel } from "../features/imports/components/models/ImportIssueViewModel";
import type { ImportResolutionAction } from "../features/imports/components/models/ImportResolutionAction";
import type { ImportAnalysisResult, ProblemDetails } from "../features/imports/types/ImportAnalysisResult";

interface ImportSourceFileResponse { fileName: string }
interface ImportIssueResponse {
  issueId: string; entityId: string | null; type: string; severity: string;
  message: string; fieldName: string | null; firstValue: string | null;
  secondValue: string | null; requiresUserDecision: boolean; isResolved: boolean;
  resolutionAction: ImportResolutionAction; customResolvedValue: string | null;
}
interface ImportReportResponse {
  importId: string; sourceFile: ImportSourceFileResponse | null;
  customerCount: number; buildingCount: number; meterCount: number;
  meterReadingCount: number; issueCount: number; issues: ImportIssueResponse[];
}

const apiBaseUrl = (import.meta.env.VITE_API_BASE_URL ?? "http://localhost:5000").replace(/\/$/, "");

export class ImportApiError extends Error {
  constructor(message: string, public readonly status?: number) { super(message); this.name = "ImportApiError"; }
}

export async function analyzeImport(file: File, userId: string, signal?: AbortSignal): Promise<ImportAnalysisResult> {
  const formData = new FormData();
  formData.append("ImportFile", file, file.name);
  let response: Response;
  try {
    response = await fetch(`${apiBaseUrl}/api/v1/imports/analyze`, {
      method: "POST", headers: { "X-User-Id": userId }, body: formData, signal,
    });
  } catch (error: unknown) {
    if (error instanceof DOMException && error.name === "AbortError") throw error;
    throw new ImportApiError(`Die API ist nicht erreichbar. Bitte prüfe, ob sie unter ${apiBaseUrl} läuft.`);
  }

  const payload = await readJson(response);
  if (!response.ok) throw problemError(payload, response.status);
  if (!isImportReportResponse(payload)) throw new ImportApiError("Die API hat ein ungültiges Analyseergebnis zurückgegeben.", response.status);
  return mapReport(payload);
}

async function readJson(response: Response): Promise<unknown> {
  try { return await response.json(); }
  catch { throw new ImportApiError(`Die API-Antwort (HTTP ${response.status}) enthält kein gültiges JSON.`, response.status); }
}

function problemError(payload: unknown, fallbackStatus: number): ImportApiError {
  const problem = isRecord(payload) ? payload as ProblemDetails : {};
  const validation = problem.errors ? Object.values(problem.errors).flat().join(" ") : "";
  const message = [problem.title, problem.detail, validation].filter(Boolean).join(": ")
    || `Die Analyse ist mit HTTP ${fallbackStatus} fehlgeschlagen.`;
  return new ImportApiError(message, problem.status ?? fallbackStatus);
}

function mapReport(report: ImportReportResponse): ImportAnalysisResult {
  return {
    importId: report.importId, fileName: report.sourceFile?.fileName ?? "Unbekannte Datei",
    customerCount: report.customerCount, buildingCount: report.buildingCount,
    meterCount: report.meterCount, meterReadingCount: report.meterReadingCount,
    issueCount: report.issueCount, issues: report.issues.map(mapIssue),
  };
}

function mapIssue(issue: ImportIssueResponse): ImportIssueViewModel { return { ...issue }; }
function isRecord(value: unknown): value is Record<string, unknown> { return typeof value === "object" && value !== null; }
function isImportReportResponse(value: unknown): value is ImportReportResponse {
  if (!isRecord(value)) return false;
  return typeof value.importId === "string" && Array.isArray(value.issues)
    && typeof value.customerCount === "number" && typeof value.buildingCount === "number"
    && typeof value.meterCount === "number" && typeof value.meterReadingCount === "number"
    && typeof value.issueCount === "number";
}

export const importService = { analyzeImport };
