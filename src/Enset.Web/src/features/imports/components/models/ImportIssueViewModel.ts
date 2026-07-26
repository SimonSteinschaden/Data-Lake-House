import type { ImportResolutionAction } from "./ImportResolutionAction";

export interface AllowedImportResolutionViewModel {
  type: ImportResolutionAction;
  label: string;
  requiresInput: boolean;
  inputType: "None" | "Text" | "Decimal" | "Integer" | "Date" | "Reference";
  supportsBatch: boolean;
  culture: string | null;
}

export interface ImportIssueViewModel {
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
  allowedResolutions: AllowedImportResolutionViewModel[];
  requiresUserDecision: boolean;
  isResolved: boolean;
  resolutionSource: "None" | "Automatic" | "Manual";
  resolvedAt: string | null;
  resolvedBy: string | null;
  resolutionScope: "SingleIssue" | "MatchingIssuesInCurrentImport" | "MatchingIssueTypeInCurrentImport" | "FutureImports" | null;
  resolutionRuleId: string | null;
  resolutionAction: ImportResolutionAction;
  customResolvedValue: string | null;
}
