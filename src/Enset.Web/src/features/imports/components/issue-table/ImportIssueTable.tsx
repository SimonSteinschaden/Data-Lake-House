import type { ImportIssueViewModel } from "../models/ImportIssueViewModel";
import type { ImportResolutionAction } from "../models/ImportResolutionAction";
import { ImportIssueRow } from "./ImportIssueRow";
import "./ImportIssueTable.css";

interface ImportIssueTableProps {
  issues: ImportIssueViewModel[];
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
  disabled?: boolean;
}

export function ImportIssueTable({
  issues,
  onResolutionChange,
  onApplyGroupResolution,
  disabled = false,
}: ImportIssueTableProps) {
  const showSecondValue = issues.some(issue =>
    issue.secondValue !== null && issue.secondValue !== undefined);
  if (issues.length === 0) {
    return (
      <div className="import-issue-table__empty">
        Keine Issues erkannt.
      </div>
    );
  }

  return (
    <div className="import-issue-table__container">
      <table className="import-issue-table">
        <thead>
          <tr>
            <th>Schweregrad</th>
            <th>Typ</th>
            <th>Feld</th>
            <th>Erster Wert</th>
            {showSecondValue && <th>Zweiter Wert</th>}
            <th>Meldung</th>
            <th>Entscheidung</th>
          </tr>
        </thead>

        <tbody>
          {issues.map((issue) => (
            <ImportIssueRow
              key={issue.issueId}
              issue={issue}
              onResolutionChange={onResolutionChange}
              onApplyGroupResolution={onApplyGroupResolution}
              disabled={disabled}
              showSecondValue={showSecondValue}
            />
          ))}
        </tbody>
      </table>
    </div>
  );
}
