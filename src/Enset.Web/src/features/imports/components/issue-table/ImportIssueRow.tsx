import type { ImportIssueViewModel } from "../models/ImportIssueViewModel";
import type { ImportResolutionAction } from "../models/ImportResolutionAction";
import { ResolutionSelector } from "./ResolutionSelector";

interface ImportIssueRowProps {
  issue: ImportIssueViewModel;
  onResolutionChange: (
    issueId: string,
    action: ImportResolutionAction,
    customValue: string | null,
  ) => void;
}

export function ImportIssueRow({
  issue,
  onResolutionChange,
}: ImportIssueRowProps) {
  return (
    <tr>
      <td>
        <span
          className={`issue-severity issue-severity--${issue.severity.toLowerCase()}`}
        >
          {issue.severity}
        </span>
      </td>

      <td>{issue.type}</td>

      <td>{issue.fieldName ?? "—"}</td>

      <td>{issue.firstValue ?? "—"}</td>

      <td>{issue.secondValue ?? "—"}</td>

      <td>{issue.message}</td>

      <td>
        {issue.requiresUserDecision ? (
          <ResolutionSelector
            value={issue.resolutionAction}
            customValue={issue.customResolvedValue}
            onChange={(action, customValue) =>
              onResolutionChange(
                issue.issueId,
                action,
                customValue,
              )
            }
          />
        ) : (
          <span>Keine Entscheidung erforderlich</span>
        )}
      </td>
    </tr>
  );
}
