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
}

export function ImportIssueTable({
  issues,
  onResolutionChange,
}: ImportIssueTableProps) {
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
            <th>Zweiter Wert</th>
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
            />
          ))}
        </tbody>
      </table>
    </div>
  );
}
