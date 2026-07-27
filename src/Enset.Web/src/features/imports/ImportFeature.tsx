import { useState } from "react";
import { PageHeader } from "../../layouts/PageHeader";
import { importService } from "../../services/importService";
import type { ImportAnalysisResult } from "./types/ImportAnalysisResult";
import { ImportWizard } from "./components/ImportWizard";
import type { ImportResolutionAction } from "./components/models/ImportResolutionAction";
import type { ImportIssueViewModel } from "./components/models/ImportIssueViewModel";
import type {
  ResolutionDraft,
  WizardState,
} from "./components/models/WizardState";
import "./ImportFeature.css";

function createInitialState(selectedFile: File | null = null): WizardState {
  const sourceType = selectedFile?.name.toLowerCase().endsWith(".csv")
    ? "Csv"
    : "CRM_Excel";
  return {
    currentStep: "upload",
    selectedFile,
    sourceType,
    medium: null,
    importId: null,
    analysisResult: null,
    isAnalyzing: false,
    analysisError: null,
    resolutionDrafts: {},
    isApplyingResolutions: false,
    resolutionError: null,
    resolutionNotice: null,
    isCommitting: false,
    commitError: null,
  };
}

export function ImportFeature() {
  const [state, setState] = useState<WizardState>(createInitialState);

  function handleFileSelected(file: File | null) {
    setState(createInitialState(file));
  }

  async function handleAnalyze() {
    if (!state.selectedFile || state.isAnalyzing)
      return;

    setState(current => ({
      ...current,
      isAnalyzing: true,
      analysisError: null,
    }));

    try {
      const report = await importService.analyzeImport(
        state.selectedFile,
        state.sourceType,
        state.medium,
      );
      setState(current => ({
        ...current,
        importId: report.importId,
        analysisResult: report,
        resolutionDrafts: createResolutionDrafts(report),
        isAnalyzing: false,
        currentStep: "analysis",
      }));
    } catch (error: unknown) {
      setState(current => ({
        ...current,
        isAnalyzing: false,
        analysisError: getErrorMessage(
          error,
          "Die Analyse konnte nicht ausgeführt werden.",
        ),
        importId: null,
        analysisResult: null,
        resolutionDrafts: {},
        currentStep: "upload",
      }));
    }
  }

  function handleResolutionChange(
    issueId: string,
    resolutionAction: ImportResolutionAction,
    customResolvedValue: string | null,
  ) {
    setState(current => ({
      ...current,
      resolutionError: null,
      resolutionNotice: null,
      resolutionDrafts: {
        ...current.resolutionDrafts,
        [issueId]: { resolutionAction, customResolvedValue },
      },
    }));
  }

  async function handleApplyResolutions() {
    if (
      !state.importId ||
      !state.analysisResult ||
      state.isApplyingResolutions
    ) {
      return;
    }

    setState(current => ({
      ...current,
      isApplyingResolutions: true,
      resolutionError: null,
    }));

    try {
      const resolutions = state.analysisResult.issues
        .filter(issue => !issue.isResolved)
        .map(issue => {
          const draft = state.resolutionDrafts[issue.issueId] ?? {
            resolutionAction: issue.resolutionAction,
            customResolvedValue: issue.customResolvedValue,
          };

          return {
            issueId: issue.issueId,
            resolutionAction: draft.resolutionAction,
            customResolvedValue: draft.customResolvedValue,
          };
        })
        .filter(resolution => resolution.resolutionAction !== "None");

      const refreshedReport = await importService.applyResolutions(
        state.importId,
        resolutions,
      );

      if (refreshedReport.status !== "ReadyToCommit") {
        setState(current => ({
          ...current,
          analysisResult: refreshedReport,
          resolutionDrafts: createResolutionDrafts(refreshedReport),
          isApplyingResolutions: false,
          resolutionError:
            refreshedReport.readinessMessage ??
            `${refreshedReport.blockingOpenIssueCount} Issues noch ungelöst.`,
          currentStep: "resolution",
        }));
        return;
      }

      setState(current => ({
        ...current,
        analysisResult: refreshedReport,
        resolutionDrafts: createResolutionDrafts(refreshedReport),
        isApplyingResolutions: false,
        currentStep: "commit",
      }));
    } catch (error: unknown) {
      setState(current => ({
        ...current,
        isApplyingResolutions: false,
        resolutionError: getErrorMessage(
          error,
          "Die Entscheidungen konnten nicht angewendet werden.",
        ),
      }));
    }
  }

  async function handleApplyGroupResolution(
    issueId: string,
    scope:
      | "SingleIssue"
      | "MatchingIssuesInCurrentImport"
      | "MatchingIssueTypeInCurrentImport",
  ) {
    if (!state.importId || state.isApplyingResolutions)
      return;
    const draft = state.resolutionDrafts[issueId];
    if (!draft || draft.resolutionAction === "None") {
      setState(current => ({
        ...current,
        resolutionError: "Bitte zuerst eine Entscheidung für diese Issue-Gruppe wählen.",
      }));
      return;
    }

    setState(current => ({
      ...current,
      isApplyingResolutions: true,
      resolutionError: null,
      resolutionNotice: null,
    }));
    try {
      const result = await importService.applyResolutionRule(
        state.importId,
        issueId,
        draft.resolutionAction,
        draft.customResolvedValue,
        scope,
      );
      setState(current => ({
        ...current,
        analysisResult: result.report,
        resolutionDrafts: createResolutionDrafts(result.report),
        isApplyingResolutions: false,
        resolutionNotice:
          `${result.resolvedIssueCount.toLocaleString("de-AT")} von ` +
          `${result.matchedIssueCount.toLocaleString("de-AT")} gleichartigen Issues gelöst.` +
          (result.failedIssueCount > 0
            ? ` ${result.failedIssueCount.toLocaleString("de-AT")} Werte konnten nicht interpretiert werden.`
            : "") +
          (result.skippedIssueCount > 0
            ? ` ${result.skippedIssueCount.toLocaleString("de-AT")} nicht kompatible Issues wurden übersprungen.`
            : "") +
          ` ${result.remainingBlockingIssueCount.toLocaleString("de-AT")} blockierende Issues verbleiben.`,
      }));
    } catch (error: unknown) {
      setState(current => ({
        ...current,
        isApplyingResolutions: false,
        resolutionError: getErrorMessage(
          error,
          "Die Gruppenentscheidung konnte nicht angewendet werden.",
        ),
      }));
    }
  }

  async function handleCommit() {
    if (
      !state.importId ||
      !state.analysisResult ||
      state.isCommitting
    ) {
      return;
    }

    setState(current => ({
      ...current,
      isCommitting: true,
      commitError: null,
    }));

    try {
      const report = await importService.commitImport(state.importId);

      if (report.status !== "Committed") {
        throw new Error(
          `Der Server meldet nach dem Commit den Status '${report.status}'.`,
        );
      }

      setState(current => ({
        ...current,
        analysisResult: report,
        isCommitting: false,
        currentStep: "completed",
      }));
    } catch (error: unknown) {
      let report = state.analysisResult;
      let refreshFailure: string | null = null;
      try {
        report = await importService.getImportReport(state.importId);
      } catch (refreshError: unknown) {
        refreshFailure = getErrorMessage(
          refreshError,
          "Der aktuelle Serverreport konnte nicht geladen werden.",
        );
      }

      const commitFailure = getErrorMessage(
        error,
        "Der relationale Import konnte nicht ausgeführt werden.",
      );

      setState(current => ({
        ...current,
        analysisResult: report,
        isCommitting: false,
        commitError: refreshFailure
          ? `${commitFailure} ${refreshFailure}`
          : commitFailure,
      }));
    }
  }

  const displayedIssues = state.analysisResult?.issues.map(issue => {
    const draft = state.resolutionDrafts[issue.issueId];
    return draft
      ? {
          ...issue,
          resolutionAction: draft.resolutionAction,
          customResolvedValue: draft.customResolvedValue,
        }
      : issue;
  }) ?? [];

  const moveTo = (currentStep: WizardState["currentStep"]) =>
    setState(current => ({ ...current, currentStep }));

  return (
    <div className="import-feature">
      <PageHeader
          title="Datenimport"
          description="Excel- oder CSV-Daten analysieren, erkannte Konflikte prüfen und den Import kontrolliert freigeben."
      />

      <ImportWizard
        currentStep={state.currentStep}
        selectedFile={state.selectedFile}
        sourceType={state.sourceType}
        medium={state.medium}
        analysisResult={state.analysisResult}
        issues={displayedIssues}
        isAnalyzing={state.isAnalyzing}
        analysisError={state.analysisError}
        isApplyingResolutions={state.isApplyingResolutions}
        resolutionError={state.resolutionError}
        resolutionNotice={state.resolutionNotice}
        isCommitting={state.isCommitting}
        commitError={state.commitError}
        onFileSelected={handleFileSelected}
        onSourceTypeChanged={sourceType =>
          setState(current => ({
            ...current,
            sourceType,
            medium:
              sourceType === "Landesenergiebuchhaltung"
                ? current.medium
                : null,
            analysisError: null,
          }))
        }
        onMediumChanged={medium =>
          setState(current => ({ ...current, medium, analysisError: null }))
        }
        onAnalyze={handleAnalyze}
        onResolutionChange={handleResolutionChange}
        onShowResolutions={() => moveTo("resolution")}
        onApplyResolutions={handleApplyResolutions}
        onApplyGroupResolution={handleApplyGroupResolution}
        onCommit={handleCommit}
        onBackToUpload={() => moveTo("upload")}
        onBackToAnalysis={() => moveTo("analysis")}
        onBackToResolution={() => moveTo("resolution")}
        onRestart={() => setState(createInitialState())}
      />
    </div>
  );
}

function createResolutionDrafts(
  report: ImportAnalysisResult,
): Record<string, ResolutionDraft> {
  return Object.fromEntries(
    report.issues
      .filter(isUserResolvableIssue)
      .map(issue => [
        issue.issueId,
        {
          resolutionAction: issue.resolutionAction,
          customResolvedValue: issue.customResolvedValue,
        },
      ]),
  );
}

function isUserResolvableIssue(issue: ImportIssueViewModel): boolean {
  return issue.requiresUserDecision ||
    (!issue.isResolved && (issue.severity === "Error" || issue.severity === "Critical"));
}

function getErrorMessage(error: unknown, fallback: string): string {
  return error instanceof Error ? error.message : fallback;
}
