import type { ImportAnalysisResult } from "../../types/ImportAnalysisResult";
import type { ImportWizardStep } from "../../types/ImportWizardStep";
import type { ImportResolutionAction } from "./ImportResolutionAction";

export interface ResolutionDraft {
  resolutionAction: ImportResolutionAction;
  customResolvedValue: string | null;
}

export interface WizardState {
  currentStep: ImportWizardStep;
  selectedFile: File | null;
  sourceType: "CRM_Excel" | "Csv" | "Landesenergiebuchhaltung";
  medium: "Electricity" | "Heat" | null;
  importId: string | null;
  analysisResult: ImportAnalysisResult | null;
  isAnalyzing: boolean;
  analysisError: string | null;
  resolutionDrafts: Record<string, ResolutionDraft>;
  isApplyingResolutions: boolean;
  resolutionError: string | null;
  resolutionNotice: string | null;
  isCommitting: boolean;
  commitError: string | null;
}
