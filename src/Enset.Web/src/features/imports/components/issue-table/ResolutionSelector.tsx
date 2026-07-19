import type { ChangeEvent } from "react";
import type { ImportResolutionAction } from "../models/ImportResolutionAction";

interface ResolutionSelectorProps {
  value: ImportResolutionAction;
  customValue: string | null;
  disabled?: boolean;
  onChange: (
    action: ImportResolutionAction,
    customValue: string | null,
  ) => void;
}

const resolutionOptions: Array<{
  value: ImportResolutionAction;
  label: string;
}> = [
  { value: "None", label: "Keine Entscheidung" },
  { value: "KeepFirst", label: "Ersten Wert behalten" },
  { value: "KeepSecond", label: "Zweiten Wert behalten" },
  { value: "UseCustomValue", label: "Eigenen Wert verwenden" },
  { value: "KeepSeparate", label: "Getrennt behalten" },
];

export function ResolutionSelector({
  value,
  customValue,
  disabled = false,
  onChange,
}: ResolutionSelectorProps) {
  function handleActionChange(event: ChangeEvent<HTMLSelectElement>) {
    const action = event.target.value as ImportResolutionAction;

    onChange(
      action,
      action === "UseCustomValue" ? customValue : null,
    );
  }

  function handleCustomValueChange(
    event: ChangeEvent<HTMLInputElement>,
  ) {
    onChange("UseCustomValue", event.target.value);
  }

  return (
    <div className="resolution-selector">
      <select
        value={value}
        disabled={disabled}
        onChange={handleActionChange}
        aria-label="Resolution auswählen"
      >
        {resolutionOptions.map((option) => (
          <option
            key={option.value}
            value={option.value}
          >
            {option.label}
          </option>
        ))}
      </select>

      {value === "UseCustomValue" && (
        <input
          type="text"
          value={customValue ?? ""}
          disabled={disabled}
          placeholder="Eigenen Wert eingeben"
          onChange={handleCustomValueChange}
        />
      )}
    </div>
  );
}
