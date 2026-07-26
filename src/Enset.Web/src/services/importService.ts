import { apiRequest } from "../api";
import type { ImportIssueViewModel } from "../features/imports/components/models/ImportIssueViewModel";
import type { ImportResolutionAction } from "../features/imports/components/models/ImportResolutionAction";
import type {
  ImportAnalysisResult,
  ImportStatus,
} from "../features/imports/types/ImportAnalysisResult";

interface ImportSourceFileResponse {
  fileName: string;
}

interface ImportIssueResponse {
  issueId: string;
  entityId: string | null;
  type: string;
  severity: string;
  message: string;
  fieldName: string | null;
  sourceRowNumber: number | null;
  firstValue: string | null;
  secondValue: string | null;
  valuePattern: string;
  targetDataType: string;
  numberFormatPattern: string;
  exampleValues: string[];
  matchingIssueCount: number;
  compatibleIssueTypeCount: number;
  supportsGroupResolution: boolean;
  supportedScopes: Array<
    | "SingleIssue"
    | "MatchingIssuesInCurrentImport"
    | "MatchingIssueTypeInCurrentImport"
  >;
  allowedResolutions: Array<{
    type: ImportResolutionAction;
    label: string;
    requiresInput: boolean;
    inputType: "None" | "Text" | "Decimal" | "Integer" | "Date" | "Reference";
    supportsBatch: boolean;
    culture: string | null;
  }>;
  requiresUserDecision: boolean;
  isResolved: boolean;
  resolutionSource: "None" | "Automatic" | "Manual";
  resolvedAt: string | null;
  resolvedBy: string | null;
  resolutionScope: "SingleIssue" | "MatchingIssuesInCurrentImport" | "FutureImports" | null;
  resolutionRuleId: string | null;
  resolutionAction: ImportResolutionAction;
  customResolvedValue: string | null;
}

interface ImportReportResponse {
  importId: string;
  status: ImportStatus;
  sourceFile: ImportSourceFileResponse | null;
  customerCount: number;
  buildingCount: number;
  meterCount: number;
  meterReadingCount: number;
  issueCount: number;
  returnedIssueCount: number;
  hasMoreIssues: boolean;
  unresolvedIssueCount: number;
  openIssueCount: number;
  blockingOpenIssueCount: number;
  automaticallyResolvedIssueCount: number;
  manuallyResolvedIssueCount: number;
  readinessMessage: string | null;
  issues: ImportIssueResponse[];
  sourceColumns: Array<{
    index: number;
    originalHeader: string | null;
    effectiveHeader: string;
    wasHeaderGenerated: boolean;
    hasData: boolean;
    valueCount: number;
  }>;
}

export interface ImportIssueResolutionRequest {
  issueId: string;
  resolutionAction: ImportResolutionAction;
  customResolvedValue: string | null;
}

interface ApplyResolutionsRequest {
  resolutions: ImportIssueResolutionRequest[];
}

interface CommitImportRequest {
  targetMode: "Upsert";
  targetWriter: "Database";
  targetLocation: null;
  archiveRawSource: true;
}

interface ApplyResolutionRuleResponse {
  ruleId: string;
  matchedIssueCount: number;
  resolvedIssueCount: number;
  failedIssueCount: number;
  skippedIssueCount: number;
  remainingBlockingIssueCount: number;
  status: ImportStatus;
  report: ImportReportResponse;
}

const importPath = (importId: string): string => {
  const value = importId.trim();
  if (!value) throw new Error("Eine Import-ID ist erforderlich.");
  return `/api/v1/imports/${encodeURIComponent(value)}`;
};

const mapReport = (report: ImportReportResponse): ImportAnalysisResult => ({
  importId: report.importId,
  status: report.status,
  fileName: report.sourceFile?.fileName ?? "Unbekannte Datei",
  customerCount: report.customerCount,
  buildingCount: report.buildingCount,
  meterCount: report.meterCount,
  meterReadingCount: report.meterReadingCount,
  issueCount: report.issueCount,
  returnedIssueCount: report.returnedIssueCount ?? report.issues.length,
  hasMoreIssues: report.hasMoreIssues ?? false,
  unresolvedIssueCount: report.unresolvedIssueCount ?? 0,
  openIssueCount: report.openIssueCount ?? report.unresolvedIssueCount ?? 0,
  blockingOpenIssueCount:
    report.blockingOpenIssueCount ?? report.unresolvedIssueCount ?? 0,
  automaticallyResolvedIssueCount:
    report.automaticallyResolvedIssueCount ?? 0,
  manuallyResolvedIssueCount: report.manuallyResolvedIssueCount ?? 0,
  readinessMessage: report.readinessMessage ?? null,
  issues: report.issues.map((issue): ImportIssueViewModel => ({ ...issue })),
  sourceColumns: report.sourceColumns ?? [],
});

export const importService = {
  async analyzeImport(
    file: File,
    sourceType: "EnsetWorkbook" | "Landesenergiebuchhaltung",
    medium: "Electricity" | "Heat" | null,
    signal?: AbortSignal,
  ): Promise<ImportAnalysisResult> {
    const formData = new FormData();
    formData.append("ImportFile", file, file.name);
    formData.append("SourceType", sourceType);
    if (medium) formData.append("Medium", medium);
    const report = await apiRequest<ImportReportResponse>("/api/v1/imports/analyze", {
      method: "POST",
      body: formData,
      signal,
    });
    return mapReport(report);
  },

  async getImportReport(importId: string, signal?: AbortSignal): Promise<ImportAnalysisResult> {
    const report = await apiRequest<ImportReportResponse>(importPath(importId), {
      method: "GET",
      signal,
    });
    return mapReport(report);
  },

  async applyResolutions(
    importId: string,
    resolutions: ImportIssueResolutionRequest[],
    signal?: AbortSignal,
  ): Promise<ImportAnalysisResult> {
    const report = await apiRequest<ImportReportResponse>(
      `${importPath(importId)}/resolutions`,
      {
        method: "POST",
        body: JSON.stringify({ resolutions } satisfies ApplyResolutionsRequest),
        signal,
      },
    );
    return mapReport(report);
  },

  async applyResolutionRule(
    importId: string,
    seedIssueId: string,
    resolutionAction: ImportResolutionAction,
    resolutionPayload: string | null,
    scope:
      | "SingleIssue"
      | "MatchingIssuesInCurrentImport"
      | "MatchingIssueTypeInCurrentImport" =
        "MatchingIssuesInCurrentImport",
    signal?: AbortSignal,
  ): Promise<{
    report: ImportAnalysisResult;
    matchedIssueCount: number;
    resolvedIssueCount: number;
    failedIssueCount: number;
    skippedIssueCount: number;
    remainingBlockingIssueCount: number;
  }> {
    const response = await apiRequest<ApplyResolutionRuleResponse>(
      `${importPath(importId)}/resolution-rules`,
      {
        method: "POST",
        body: JSON.stringify({
          ruleId: crypto.randomUUID(),
          seedIssueId,
          scope,
          resolutionType:
            resolutionAction === "ParseDeAt" ||
            resolutionAction === "ParseInvariant"
              ? "ParseWithCulture"
              : "ExistingAction",
          resolutionAction,
          resolutionPayload,
        }),
        signal,
      },
    );
    return {
      report: mapReport(response.report),
      matchedIssueCount: response.matchedIssueCount,
      resolvedIssueCount: response.resolvedIssueCount,
      failedIssueCount: response.failedIssueCount,
      skippedIssueCount: response.skippedIssueCount,
      remainingBlockingIssueCount: response.remainingBlockingIssueCount,
    };
  },

  async commitImport(importId: string, signal?: AbortSignal): Promise<ImportAnalysisResult> {
    const request: CommitImportRequest = {
      targetMode: "Upsert",
      targetWriter: "Database",
      targetLocation: null,
      archiveRawSource: true,
    };
    const report = await apiRequest<ImportReportResponse>(
      `${importPath(importId)}/commit`,
      {
        method: "POST",
        body: JSON.stringify(request),
        signal,
      },
    );
    return mapReport(report);
  },
};
