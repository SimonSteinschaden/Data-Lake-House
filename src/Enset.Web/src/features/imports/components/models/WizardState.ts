import type { ImportAnalysisResult } from "../../types/ImportAnalysisResult";
import type { ImportWizardStep } from "../../types/ImportWizardStep";

export interface WizardState {
  currentStep: ImportWizardStep;
  selectedFile: File | null;
  importId: string | null;
  analysisResult: ImportAnalysisResult | null;
  isAnalyzing: boolean;
  analysisError: string | null;
}
