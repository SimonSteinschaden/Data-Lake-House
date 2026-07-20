import { useState } from "react";
import { importService } from "../../services/importService";
import { ImportWizard } from "./components/ImportWizard";
import type { ImportResolutionAction } from "./components/models/ImportResolutionAction";
import type { WizardState } from "./components/models/WizardState";
import "./ImportFeature.css";

const initialState: WizardState = {
  currentStep: "upload", selectedFile: null, importId: null,
  analysisResult: null, isAnalyzing: false, analysisError: null,
};

export function ImportFeature() {
  const [state, setState] = useState<WizardState>(initialState);

  function handleFileSelected(file: File | null) {
    setState({ ...initialState, selectedFile: file });
  }

  async function handleAnalyze() {
    if (!state.selectedFile || state.isAnalyzing) return;
    setState(current => ({ ...current, isAnalyzing: true, analysisError: null }));
    try {
      const result = await importService.analyzeImport(state.selectedFile, "development-user");
      setState(current => ({ ...current, importId: result.importId,
        analysisResult: result, isAnalyzing: false, currentStep: "analysis" }));
    } catch (error: unknown) {
      const message = error instanceof Error ? error.message : "Die Analyse konnte nicht ausgeführt werden.";
      setState(current => ({ ...current, isAnalyzing: false, analysisError: message,
        importId: null, analysisResult: null, currentStep: "upload" }));
    }
  }

  function handleResolutionChange(issueId: string, action: ImportResolutionAction, customValue: string | null) {
    setState(current => {
      if (!current.analysisResult) return current;
      const issues = current.analysisResult.issues.map(issue => issue.issueId === issueId
        ? { ...issue, resolutionAction: action, customResolvedValue: customValue,
            isResolved: !issue.requiresUserDecision || action !== "None" }
        : issue);
      return { ...current, analysisResult: { ...current.analysisResult, issues } };
    });
  }

  const moveTo = (currentStep: WizardState["currentStep"]) =>
    setState(current => ({ ...current, currentStep }));

  return <div className="import-feature">
    <header className="import-feature__header"><div><h1>Datenimport</h1><p>Excel-Daten analysieren, erkannte Konflikte prüfen und den Import kontrolliert freigeben.</p></div></header>
    <ImportWizard currentStep={state.currentStep} selectedFile={state.selectedFile}
      analysisResult={state.analysisResult} issues={state.analysisResult?.issues ?? []}
      isAnalyzing={state.isAnalyzing} analysisError={state.analysisError}
      onFileSelected={handleFileSelected} onAnalyze={handleAnalyze}
      onResolutionChange={handleResolutionChange} onShowResolutions={() => moveTo("resolution")}
      onShowCommit={() => moveTo("commit")} onCommit={() => moveTo("completed")}
      onBackToUpload={() => moveTo("upload")} onBackToAnalysis={() => moveTo("analysis")}
      onBackToResolution={() => moveTo("resolution")} onRestart={() => setState(initialState)} />
  </div>;
}
