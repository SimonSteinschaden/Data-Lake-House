import { useMemo, useState } from "react";
import type { ImportIssueViewModel } from "./models/ImportIssueViewModel";
import type { ImportResolutionAction } from "./models/ImportResolutionAction";
import { ImportIssueTable } from "./issue-table/ImportIssueTable";
import type { ImportSourceColumn } from "../types/ImportAnalysisResult";

interface ResolutionStepProps {
  issues: ImportIssueViewModel[];
  sourceColumns: ImportSourceColumn[];
  onResolutionChange: (
    issueId: string,
    action: ImportResolutionAction,
    customValue: string | null,
  ) => void;
  onApplyGroupResolution: (
    issueId: string,
    scope:
      | "SingleIssue"
      | "MatchingIssuesInCurrentImport"
      | "MatchingIssueTypeInCurrentImport",
  ) => void;
  onContinue: () => void;
  onBack: () => void;
  isApplying: boolean;
  error: string | null;
  notice: string | null;
  issueCount?: number;
  hasMoreIssues?: boolean;
  totalIssueCount: number;
  automaticallyResolvedIssueCount: number;
  manuallyResolvedIssueCount: number;
  openIssueCount: number;
}

export function ResolutionStep({
  issues,
  sourceColumns,
  onResolutionChange,
  onApplyGroupResolution,
  onContinue,
  onBack,
  isApplying,
  error,
  notice,
  issueCount,
  hasMoreIssues,
  totalIssueCount,
  automaticallyResolvedIssueCount,
  manuallyResolvedIssueCount,
  openIssueCount,
}: ResolutionStepProps) {
  const [showOnlyOpenIssues, setShowOnlyOpenIssues] = useState(false);

  const visibleIssues = useMemo(
    () =>
      showOnlyOpenIssues
        ? issues.filter((issue) => !issue.isResolved)
        : issues,
    [issues, showOnlyOpenIssues],
  );

  const hasMissingOpenIssueDetails =
    showOnlyOpenIssues &&
    openIssueCount > 0 &&
    visibleIssues.length === 0;

  function continueToReadiness() {
    setShowOnlyOpenIssues(true);
    onContinue();
  }

  return (
    <section className="import-wizard__resolution">
      <div>
        <h3>Importprobleme prüfen</h3>
        <p className="import-wizard__hint">
          Entscheidungspflichtige Issues müssen gelöst werden, bevor der
          Import freigegeben werden kann.
        </p>
      </div>

      <div className="import-wizard__resolution-summary">
        <div>
          <strong>{totalIssueCount}</strong>
          <span>Issues insgesamt</span>
        </div>

        <div>
          <strong>{automaticallyResolvedIssueCount}</strong>
          <span>Automatisch gelöst</span>
        </div>

        <div>
          <strong>{manuallyResolvedIssueCount}</strong>
          <span>Manuell gelöst</span>
        </div>

        <div>
          <strong>{openIssueCount}</strong>
          <span>Offen</span>
        </div>
      </div>

      {hasMissingOpenIssueDetails ? (
      <div className="import-wizard__error" role="alert">
        Der Server meldet {openIssueCount} offene Issues, hat jedoch keine
        zugehörigen Issue-Details geliefert.
      </div>
          ) : (
            <ImportIssueTable
              issues={visibleIssues}
              onResolutionChange={onResolutionChange}
              onApplyGroupResolution={onApplyGroupResolution}
              disabled={isApplying}
            />
          )}

      {hasMoreIssues && (
        <p className="import-wizard__hint">
          {visibleIssues.length.toLocaleString("de-AT")} repräsentative Issues von{" "}
          {(issueCount ?? issues.length).toLocaleString("de-AT")} werden angezeigt.
          Gruppenentscheidungen werden serverseitig auf alle Treffer angewendet.
        </p>
      )}

      {notice && <p className="import-wizard__hint">{notice}</p>}

      {sourceColumns.some(column => column.wasHeaderGenerated) && (
        <div className="import-issue-table__container">
          <h4>Automatisch benannte Quellspalten</h4>
          <table className="import-issue-table">
            <thead>
              <tr>
                <th>Originalbezeichnung</th>
                <th>Vorläufiger Name</th>
                <th>Spaltenposition</th>
                <th>Enthält Daten</th>
              </tr>
            </thead>
            <tbody>
              {sourceColumns
                .filter(column => column.wasHeaderGenerated)
                .map(column => (
                  <tr key={column.index}>
                    <td>leer</td>
                    <td>{column.effectiveHeader}</td>
                    <td>{column.index}</td>
                    <td>{column.hasData ? "Ja" : "Nein"}</td>
                  </tr>
                ))}
            </tbody>
          </table>
        </div>
      )}

      {error && (
        <div className="import-wizard__error" role="alert">
          {error}
        </div>
      )}

      <div className="import-wizard__actions">
        <button
          type="button"
          className="import-wizard__secondary-action"
          disabled={isApplying}
          onClick={onBack}
        >
          Zurück
        </button>

        <button
          type="button"
          className="import-wizard__primary-action"
          disabled={isApplying}
          onClick={continueToReadiness}
          aria-busy={isApplying}
        >
          {isApplying ? "Entscheidungen werden angewendet …" : "Zur Freigabe"}
        </button>
      </div>
    </section>
  );
}
