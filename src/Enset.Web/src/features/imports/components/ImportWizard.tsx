import { AnalysisStep } from "./AnalysisStep";
import { CommitStep } from "./CommitStep";
import { CompletedStep } from "./CompletedStep";
import { ResolutionStep } from "./ResolutionStep";
import { UploadStep } from "./UploadStep";
import { WizardStepper } from "./WizardStepper";

import type { ImportWizardStep } from "../types/ImportWizardStep";
import type { ImportAnalysisResult } from "../types/ImportAnalysisResult";
import type { ImportIssueViewModel } from "./models/ImportIssueViewModel";
import type { ImportResolutionAction } from "./models/ImportResolutionAction";

import "./ImportWizard.css";

interface ImportWizardProps {
  currentStep: ImportWizardStep;
  selectedFile: File | null;
  sourceType: "CRM_Excel" | "Csv" | "Landesenergiebuchhaltung";
  medium: "Electricity" | "Heat" | null;
  analysisResult: ImportAnalysisResult | null;
  issues: ImportIssueViewModel[];

  isAnalyzing: boolean;
  analysisError: string | null;

  isApplyingResolutions: boolean;
  resolutionError: string | null;
  resolutionNotice: string | null;

  isCommitting: boolean;
  commitError: string | null;

  onFileSelected: (file: File | null) => void;

  onSourceTypeChanged: (
    value: "CRM_Excel" | "Csv" | "Landesenergiebuchhaltung",
  ) => void;

  onMediumChanged: (
    value: "Electricity" | "Heat",
  ) => void;

  onAnalyze: () => void;

  onResolutionChange: (
    issueId: string,
    action: ImportResolutionAction,
    customValue: string | null,
  ) => void;

  onShowResolutions: () => void;
  onApplyResolutions: () => void;

  onApplyGroupResolution: (
    issueId: string,
    scope:
      | "SingleIssue"
      | "MatchingIssuesInCurrentImport"
      | "MatchingIssueTypeInCurrentImport",
  ) => void;

  onCommit: () => void;
  onBackToUpload: () => void;
  onBackToAnalysis: () => void;
  onBackToResolution: () => void;
  onRestart: () => void;
}

export function ImportWizard({
  currentStep,
  selectedFile,
  sourceType,
  medium,
  analysisResult,
  issues,
  isAnalyzing,
  analysisError,
  isApplyingResolutions,
  resolutionError,
  resolutionNotice,
  isCommitting,
  commitError,
  onFileSelected,
  onSourceTypeChanged,
  onMediumChanged,
  onAnalyze,
  onResolutionChange,
  onShowResolutions,
  onApplyResolutions,
  onApplyGroupResolution,
  onCommit,
  onBackToUpload,
  onBackToAnalysis,
  onBackToResolution,
  onRestart,
}: ImportWizardProps) {
  const fileName =
    analysisResult?.fileName ??
    selectedFile?.name ??
    "Keine Datei";

  const customerCount =
    analysisResult?.customerCount ?? 0;

  const buildingCount =
    analysisResult?.buildingCount ?? 0;

  const issueCount =
    analysisResult?.issueCount ?? 0;

  return (
    <section
      className="import-wizard"
      aria-label="Kontrollierter Datenimport"
    >
      <WizardStepper currentStep={currentStep} />

      <div className="import-wizard__content">
        {currentStep === "upload" && (
          <UploadStep
            selectedFile={selectedFile}
            sourceType={sourceType}
            medium={medium}
            onFileSelected={onFileSelected}
            onSourceTypeChanged={onSourceTypeChanged}
            onMediumChanged={onMediumChanged}
            onAnalyze={onAnalyze}
            isAnalyzing={isAnalyzing}
            error={analysisError}
          />
        )}

        {currentStep === "analysis" &&
          analysisResult && (
            <AnalysisStep
              fileName={fileName}
              customerCount={customerCount}
              buildingCount={buildingCount}
              meterCount={analysisResult.meterCount}
              meterReadingCount={
                analysisResult.meterReadingCount
              }
              issueCount={issueCount}
              importId={analysisResult.importId}
              profile={analysisResult}
              onContinue={onShowResolutions}
              onBack={onBackToUpload}
            />
          )}

        {currentStep === "resolution" && (
          <ResolutionStep
            issues={issues}
            sourceColumns={
              analysisResult?.sourceColumns ?? []
            }
            issueCount={analysisResult?.issueCount}
            hasMoreIssues={analysisResult?.hasMoreIssues}
            totalIssueCount={
              analysisResult?.issueCount ?? 0
            }
            automaticallyResolvedIssueCount={
              analysisResult
                ?.automaticallyResolvedIssueCount ?? 0
            }
            manuallyResolvedIssueCount={
              analysisResult
                ?.manuallyResolvedIssueCount ?? 0
            }
            openIssueCount={
              analysisResult?.openIssueCount ?? 0
            }
            isApplying={isApplyingResolutions}
            error={resolutionError}
            notice={resolutionNotice}
            onResolutionChange={
              onResolutionChange
            }
            onApplyGroupResolution={
              onApplyGroupResolution
            }
            onContinue={onApplyResolutions}
            onBack={onBackToAnalysis}
          />
        )}

        {currentStep === "commit" && (
          <CommitStep
            fileName={fileName}
            customerCount={customerCount}
            buildingCount={buildingCount}
            meterCount={
              analysisResult?.meterCount ?? 0
            }
            meterReadingCount={
              analysisResult?.meterReadingCount ?? 0
            }
            issueCount={issueCount}
            status={
              analysisResult?.status ?? "Pending"
            }
            isCommitting={isCommitting}
            error={commitError}
            onCommit={onCommit}
            onBack={onBackToResolution}
          />
        )}

        {currentStep === "completed" && (
          <CompletedStep
            fileName={fileName}
            importId={
              analysisResult?.importId ?? ""
            }
            status={
              analysisResult?.status ?? "Committed"
            }
            onRestart={onRestart}
          />
        )}
      </div>
    </section>
  );
}
