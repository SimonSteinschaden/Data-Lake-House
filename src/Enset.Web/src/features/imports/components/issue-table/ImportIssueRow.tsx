import { useState } from "react";
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
  onApplyGroupResolution: (
    issueId: string,
    scope:
      | "SingleIssue"
      | "MatchingIssuesInCurrentImport"
      | "MatchingIssueTypeInCurrentImport",
  ) => void;
  disabled?: boolean;
  showSecondValue?: boolean;
}

export function ImportIssueRow({
  issue,
  onResolutionChange,
  onApplyGroupResolution,
  disabled = false,
  showSecondValue = false,
}: ImportIssueRowProps) {
  const canResolve = !issue.isResolved && issue.allowedResolutions.length > 0;
  const selectedOption = issue.allowedResolutions.find(
    option => option.type === issue.resolutionAction);
  const [scope, setScope] = useState<
    | "SingleIssue"
    | "MatchingIssuesInCurrentImport"
    | "MatchingIssueTypeInCurrentImport"
  >(issue.supportedScopes.includes("MatchingIssuesInCurrentImport")
    ? "MatchingIssuesInCurrentImport"
    : issue.supportedScopes.includes("MatchingIssueTypeInCurrentImport")
      ? "MatchingIssueTypeInCurrentImport"
      : "SingleIssue");
  const affectedIssueCount =
    scope === "SingleIssue"
      ? 1
      : scope === "MatchingIssueTypeInCurrentImport"
      ? issue.compatibleIssueTypeCount
      : issue.matchingIssueCount;
  const canApplyBatch =
    issue.supportsGroupResolution &&
    selectedOption?.supportsBatch &&
    Math.max(
      issue.matchingIssueCount,
      issue.compatibleIssueTypeCount,
    ) > 1;

  return (
    <tr>
      <td>
        <span className={`issue-severity issue-severity--${issue.severity.toLowerCase()}`}>
          {issue.severity}
        </span>
      </td>
      <td>
        <strong>{issue.type}</strong>
        {issue.matchingIssueCount > 1 && (
          <div>
            {issue.matchingIssueCount.toLocaleString("de-AT")} betroffene Issues
          </div>
        )}
        {issue.numberFormatPattern !== "None" && (
          <div>Erkanntes Format: {numberPatternLabel(issue.numberFormatPattern)}</div>
        )}
        {issue.exampleValues.length > 0 && (
          <div>Beispiele: {issue.exampleValues.join(" · ")}</div>
        )}
      </td>
      <td>{issue.fieldName ?? "—"}</td>
      <td>{issue.firstValue ?? "—"}</td>
      {showSecondValue && <td>{issue.secondValue ?? "—"}</td>}
      <td>{issue.message}</td>
      <td>
        {canResolve ? (
          <>
            <ResolutionSelector
              value={issue.resolutionAction}
              customValue={issue.customResolvedValue}
              options={issue.allowedResolutions}
              suggestions={csvHeaderSuggestions(issue.secondValue)}
              disabled={disabled}
              onChange={(action, customValue) =>
                onResolutionChange(issue.issueId, action, customValue)
              }
            />
            {canApplyBatch && (
                <>
                  <label>
                    Anwenden auf:
                    <select
                      value={scope}
                      disabled={disabled}
                      onChange={event => setScope(event.target.value as
                        typeof scope)}
                    >
                      {issue.supportedScopes.includes("SingleIssue") && (
                        <option value="SingleIssue">Nur dieses Issue</option>
                      )}
                      {issue.supportedScopes.includes(
                        "MatchingIssuesInCurrentImport",
                      ) && (
                        <option value="MatchingIssuesInCurrentImport">
                          Alle passenden Issues
                        </option>
                      )}
                      {issue.supportedScopes.includes(
                        "MatchingIssueTypeInCurrentImport",
                      ) && (
                        <option value="MatchingIssueTypeInCurrentImport">
                          Alle kompatiblen {issue.type}-Issues
                        </option>
                      )}
                    </select>
                  </label>
                  <div>
                    Diese Entscheidung wird auf{" "}
                    {affectedIssueCount.toLocaleString("de-AT")} Issues angewendet.
                  </div>
                  <button
                    type="button"
                    disabled={
                      disabled ||
                      issue.resolutionAction === "None" ||
                      affectedIssueCount === 0
                    }
                    onClick={() =>
                      onApplyGroupResolution(issue.issueId, scope)
                    }
                  >
                    Entscheidung anwenden
                  </button>
                </>
              )}
          </>
        ) : (
          <span>
            {issue.isResolved ? "Gelöst" : "Keine interaktive Resolution"}
          </span>
        )}
      </td>
    </tr>
  );
}

function csvHeaderSuggestions(value: string | null): string[] {
  if (!value?.startsWith("[")) return [];
  try {
    const parsed: unknown = JSON.parse(value);
    return Array.isArray(parsed) &&
      parsed.every(item => typeof item === "string")
      ? parsed
      : [];
  } catch {
    return [];
  }
}

function numberPatternLabel(pattern: string): string {
  switch (pattern) {
    case "AustrianDecimal":
      return "Österreichisch";
    case "InvariantDecimal":
      return "International";
    case "AmbiguousDecimal":
      return "Mehrdeutig";
    case "Invalid":
      return "Ungültig";
    default:
      return pattern;
  }
}
