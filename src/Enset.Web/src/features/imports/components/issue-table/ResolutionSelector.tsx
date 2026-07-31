import { useEffect, useState, type ChangeEvent } from "react";
import { meterService } from "../../../../services/meterService";
import type { MeterSummary } from "../../../meters/types";
import type {
  AllowedImportResolutionViewModel,
} from "../models/ImportIssueViewModel";
import type { ImportResolutionAction } from "../models/ImportResolutionAction";

interface ResolutionSelectorProps {
  value: ImportResolutionAction;
  customValue: string | null;
  options: AllowedImportResolutionViewModel[];
  suggestions?: string[];
  disabled?: boolean;
  onChange: (
    action: ImportResolutionAction,
    customValue: string | null,
  ) => void;
}

export function ResolutionSelector({
  value,
  customValue,
  options,
  suggestions = [],
  disabled = false,
  onChange,
}: ResolutionSelectorProps) {
  const selected = options.find(option => option.type === value);
  const [meters, setMeters] = useState<MeterSummary[]>([]);

  useEffect(() => {
    if (selected?.inputType !== "Reference") return;
    const controller = new AbortController();
    meterService.list(
      { page: 1, pageSize: 100, isActive: true },
      controller.signal,
    ).then(result => setMeters(result.items)).catch(() => setMeters([]));
    return () => controller.abort();
  }, [selected?.inputType]);

  function handleActionChange(event: ChangeEvent<HTMLSelectElement>) {
    const action = event.target.value as ImportResolutionAction;
    const option = options.find(candidate => candidate.type === action);
    onChange(action, option?.requiresInput ? customValue : null);
  }

  return (
    <div className="resolution-selector">
      <select
        value={value}
        disabled={disabled}
        onChange={handleActionChange}
        aria-label="Resolution auswählen"
      >
        <option value="None">Keine Entscheidung</option>
        {options.map(option => (
          <option key={option.type} value={option.type}>
            {option.label}
          </option>
        ))}
      </select>

      {selected?.requiresInput && selected.inputType === "Reference" && (
        <select
          value={customValue ?? ""}
          disabled={disabled}
          aria-label="Zielzähler auswählen"
          onChange={event => onChange(value, event.target.value)}
        >
          <option value="">Zähler auswählen</option>
          {meters.map(meter => (
            <option key={meter.id} value={meter.id}>
              {meter.meterNumber} · {meter.name}
            </option>
          ))}
        </select>
      )}
      {selected?.requiresInput && selected.inputType !== "Reference" && (
        <input
          type={htmlInputType(selected.inputType)}
          inputMode={selected.inputType === "Decimal" ? "decimal" : undefined}
          value={customValue ?? ""}
          disabled={disabled}
          placeholder={selected.label}
          list={suggestions.length > 0 ? "csv-header-options" : undefined}
          onChange={event => onChange(value, event.target.value)}
        />
      )}
      {suggestions.length > 0 && (
        <datalist id="csv-header-options">
          {suggestions.map(suggestion => (
            <option key={suggestion} value={suggestion} />
          ))}
        </datalist>
      )}
    </div>
  );
}

function htmlInputType(
  type: AllowedImportResolutionViewModel["inputType"],
): "text" | "number" | "date" {
  if (type === "Decimal" || type === "Integer") return "number";
  if (type === "Date") return "date";
  return "text";
}
