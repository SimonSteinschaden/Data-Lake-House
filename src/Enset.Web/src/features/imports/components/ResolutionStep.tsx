import type { ImportIssueViewModel } from "./models/ImportIssueViewModel";
import type { ImportResolutionAction } from "./models/ImportResolutionAction";
import { ImportIssueTable } from "./issue-table/ImportIssueTable";

interface ResolutionStepProps {
  issues: ImportIssueViewModel[];
  onResolutionChange: (
    issueId: string,
    action: ImportResolutionAction,
    customValue: string | null,
  ) => void;
  onContinue: () => void;
  onBack: () => void;
}

export function ResolutionStep({
  issues,
  onResolutionChange,
  onContinue,
  onBack,
}: ResolutionStepProps) {
  const decisionIssues = issues.filter(
    (issue) => issue.requiresUserDecision,
  );

  const resolvedIssueCount = decisionIssues.filter(
    isIssueResolved,
  ).length;

  const unresolvedIssueCount =
    decisionIssues.length - resolvedIssueCount;

  const allIssuesResolved = unresolvedIssueCount === 0;

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
          <strong>{issues.length}</strong>
          <span>Issues insgesamt</span>
        </div>

        <div>
          <strong>{resolvedIssueCount}</strong>
          <span>Gelöst</span>
        </div>

        <div>
          <strong>{unresolvedIssueCount}</strong>
          <span>Offen</span>
        </div>
      </div>

      <ImportIssueTable
        issues={issues}
        onResolutionChange={onResolutionChange}
      />

      <div className="import-wizard__actions">
        <button
          type="button"
          className="import-wizard__secondary-action"
          onClick={onBack}
        >
          Zurück
        </button>

        <button
          type="button"
          className="import-wizard__primary-action"
          disabled={!allIssuesResolved}
          onClick={onContinue}
        >
          Zur Freigabe
        </button>
      </div>
    </section>
  );
}

function isIssueResolved(
  issue: ImportIssueViewModel,
): boolean {
  if (!issue.requiresUserDecision) {
    return true;
  }

  if (issue.resolutionAction === "None") {
    return false;
  }

  if (issue.resolutionAction === "UseCustomValue") {
    return Boolean(issue.customResolvedValue?.trim());
  }

  return true;
}
