import { useState } from "react";

import { ImportWizard } from "./components/ImportWizard";
import type { ImportIssueViewModel } from "./components/models/ImportIssueViewModel";
import type { ImportResolutionAction } from "./components/models/ImportResolutionAction";
import type { ImportWizardStep } from "./types/ImportWizardStep";

import "./ImportFeature.css";


const initialIssues: ImportIssueViewModel[] = [
  {
    issueId: "building-address-conflict",
    entityId: "building-42",
    type: "AddressConflict",
    severity: "Warning",
    message: "Die Adresse unterscheidet sich vom Bestand.",
    fieldName: "Adresse",
    firstValue: "Hauptstrasse 12",
    secondValue: "Hauptstrasse 12A",
    requiresUserDecision: true,
    isResolved: true,
    resolutionAction: "KeepSecond",
    customResolvedValue: null,
  },
  {
    issueId: "customer-vat-missing",
    entityId: "customer-7",
    type: "MissingValue",
    severity: "Info",
    message: "Die UID-Nummer fehlt in der Importdatei.",
    fieldName: "UID",
    firstValue: "ATU12345678",
    secondValue: null,
    requiresUserDecision: false,
    isResolved: true,
    resolutionAction: "None",
    customResolvedValue: null,
  },
];

export function ImportFeature() {
  const [selectedFile, setSelectedFile] =
    useState<File | null>(null);

  const [currentStep, setCurrentStep] =
    useState<ImportWizardStep>("upload");

  const [issues, setIssues] =
    useState<ImportIssueViewModel[]>(initialIssues);

  // Temporäre UI-Daten bis zur API-Anbindung.
  const customerCount = 12;
  const buildingCount = 8;

  function handleFileSelected(file: File | null) {
    setSelectedFile(file);
  }

  function handleAnalyze() {
    if (!selectedFile) {
      return;
    }

    setCurrentStep("analysis");
  }

  function handleResolutionChange(
    issueId: string,
    action: ImportResolutionAction,
    customValue: string | null,
  ) {
    setIssues((currentIssues) =>
      currentIssues.map((issue) =>
        issue.issueId === issueId
          ? {
              ...issue,
              resolutionAction: action,
              customResolvedValue: customValue,
              isResolved:
                !issue.requiresUserDecision ||
                action !== "None",
            }
          : issue,
      ),
    );
  }

  function handleRestart() {
    setSelectedFile(null);
    setCurrentStep("upload");
    setIssues(initialIssues);
  }

  return (
    <div className="import-feature">
      <header className="import-feature__header">
        <div>
          <h1>Datenimport</h1>
          <p>
            Excel-Daten analysieren, erkannte Konflikte prüfen und den
            Import kontrolliert freigeben.
          </p>
        </div>
      </header>

      <ImportWizard
        currentStep={currentStep}
        selectedFile={selectedFile}
        customerCount={customerCount}
        buildingCount={buildingCount}
        issues={issues}
        onFileSelected={handleFileSelected}
        onAnalyze={handleAnalyze}
        onResolutionChange={handleResolutionChange}
        onShowResolutions={() => setCurrentStep("resolution")}
        onShowCommit={() => setCurrentStep("commit")}
        onCommit={() => setCurrentStep("completed")}
        onBackToUpload={() => setCurrentStep("upload")}
        onBackToAnalysis={() => setCurrentStep("analysis")}
        onBackToResolution={() => setCurrentStep("resolution")}
        onRestart={handleRestart}
      />
    </div>
  );
}
