import type { ImportIssueViewModel } from "../components/models/ImportIssueViewModel";

export interface ImportAnalysisResult {
  importId: string;
  status: ImportStatus;
  fileName: string;
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
  issues: ImportIssueViewModel[];
  sourceColumns: ImportSourceColumn[];
}

export interface ImportSourceColumn {
  index: number;
  originalHeader: string | null;
  effectiveHeader: string;
  wasHeaderGenerated: boolean;
  hasData: boolean;
  valueCount: number;
}

export type ImportStatus =
  | "Pending"
  | "Analyzing"
  | "AwaitingResolution"
  | "ReadyToCommit"
  | "Committing"
  | "Committed"
  | "Failed";

export interface ProblemDetails {
  title?: string;
  detail?: string;
  status?: number;
  errors?: Record<string, string[]>;
}
