import type { ImportIssueViewModel } from "../components/models/ImportIssueViewModel";

export interface ImportAnalysisResult {
  importId: string;
  fileName: string;
  customerCount: number;
  buildingCount: number;
  meterCount: number;
  meterReadingCount: number;
  issueCount: number;
  issues: ImportIssueViewModel[];
}

export interface ProblemDetails {
  title?: string;
  detail?: string;
  status?: number;
  errors?: Record<string, string[]>;
}
