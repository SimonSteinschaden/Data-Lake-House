import { AnalysisStep } from "./AnalysisStep";
import { CommitStep } from "./CommitStep";
import { CompletedStep } from "./CompletedStep";
import { ResolutionStep } from "./ResolutionStep";
import { UploadStep } from "./UploadStep";
import { WizardStepper } from "./WizardStepper";
import type { ImportWizardStep } from "../types/ImportWizardStep";
import type { ImportIssueViewModel } from "./models/ImportIssueViewModel";
import type { ImportResolutionAction } from "./models/ImportResolutionAction";
import "./ImportWizard.css";

interface ImportWizardProps {
  currentStep: ImportWizardStep;
  selectedFile: File | null;
  customerCount: number;
  buildingCount: number;
  issues: ImportIssueViewModel[];
  onFileSelected: (file: File | null) => void;
  onAnalyze: () => void;
  onResolutionChange: (
    issueId: string,
    action: ImportResolutionAction,
    customValue: string | null,
  ) => void;
  onShowResolutions: () => void;
  onShowCommit: () => void;
  onCommit: () => void;
  onBackToUpload: () => void;
  onBackToAnalysis: () => void;
  onBackToResolution: () => void;
  onRestart: () => void;
}

export function ImportWizard({
  currentStep,
  selectedFile,
  customerCount,
  buildingCount,
  issues,
  onFileSelected,
  onAnalyze,
  onResolutionChange,
  onShowResolutions,
  onShowCommit,
  onCommit,
  onBackToUpload,
  onBackToAnalysis,
  onBackToResolution,
  onRestart,
}: ImportWizardProps) {
  const fileName = selectedFile?.name ?? "Keine Datei";
  const issueCount = issues.length;

  return (
    <section
      className="import-wizard"
      aria-labelledby="import-wizard-title"
    >
      <header className="import-wizard__header">
        <div>
          <p className="import-wizard__eyebrow">Importprozess</p>
          <h2 id="import-wizard-title">
            Kontrollierter Datenimport
          </h2>
        </div>

        <span className="import-wizard__status">
          {getStepLabel(currentStep)}
        </span>
      </header>

      <WizardStepper currentStep={currentStep} />

      <div className="import-wizard__content">
        {currentStep === "upload" && (
          <UploadStep
            selectedFile={selectedFile}
            onFileSelected={onFileSelected}
            onAnalyze={onAnalyze}
          />
        )}

        {currentStep === "analysis" && (
          <AnalysisStep
            fileName={fileName}
            customerCount={customerCount}
            buildingCount={buildingCount}
            issueCount={issueCount}
            onContinue={onShowResolutions}
            onBack={onBackToUpload}
          />
        )}

        {currentStep === "resolution" && (
          <ResolutionStep
            issues={issues}
            onResolutionChange={onResolutionChange}
            onContinue={onShowCommit}
            onBack={onBackToAnalysis}
          />
        )}

        {currentStep === "commit" && (
          <CommitStep
            fileName={fileName}
            customerCount={customerCount}
            buildingCount={buildingCount}
            issueCount={issueCount}
            onCommit={onCommit}
            onBack={onBackToResolution}
          />
        )}

        {currentStep === "completed" && (
          <CompletedStep
            fileName={fileName}
            onRestart={onRestart}
          />
        )}
      </div>
    </section>
  );
}

function getStepLabel(step: ImportWizardStep): string {
  switch (step) {
    case "upload":
      return "Datei auswählen";

    case "analysis":
      return "Analyse";

    case "resolution":
      return "Entscheidungen";

    case "commit":
      return "Freigabe";

    case "completed":
      return "Abgeschlossen";

    default: {
      const exhaustiveCheck: never = step;
      return exhaustiveCheck;
    }
  }
}
