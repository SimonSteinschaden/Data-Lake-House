import type { ImportResolutionAction } from "./ImportResolutionAction";

export interface ImportResolutionSelection {
  issueId: string;
  resolutionAction: ImportResolutionAction;
  customResolvedValue: string | null;
}