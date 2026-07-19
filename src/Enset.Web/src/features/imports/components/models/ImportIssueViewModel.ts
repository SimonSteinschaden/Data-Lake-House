import type { ImportResolutionAction } from "./ImportResolutionAction";

export interface ImportIssueViewModel {
  issueId: string;
  entityId: string | null;
  type: string;
  severity: string;
  message: string;
  fieldName: string | null;
  firstValue: string | null;
  secondValue: string | null;
  requiresUserDecision: boolean;
  isResolved: boolean;
  resolutionAction: ImportResolutionAction;
  customResolvedValue: string | null;
}