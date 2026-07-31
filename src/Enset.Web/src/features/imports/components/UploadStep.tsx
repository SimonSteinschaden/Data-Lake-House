import type { ChangeEvent } from "react";
import { Card } from "../../../components/ui/Card";
import { Button } from "../../../components/ui/Button";

type ImportSourceType =
  | "CRM_Excel"
  | "Csv"
  | "Landesenergiebuchhaltung";

type ImportMedium = "Electricity" | "Heat";

const importSourceOptions: ReadonlyArray<{
  value: ImportSourceType;
  label: string;
  supportedFormats: string;
}> = [
  {
    value: "CRM_Excel",
    label: "CRM Excel",
    supportedFormats: ".xlsx und .xlsm",
  },
  {
    value: "Landesenergiebuchhaltung",
    label: "Landesenergiebuchhaltung (CSV)",
    supportedFormats: ".csv",
  },
  {
    value: "Csv",
    label: "Lastprofil (CSV)",
    supportedFormats: ".csv",
  },
];

interface UploadStepProps {
  selectedFile: File | null;
  sourceType: ImportSourceType;
  medium: ImportMedium | null;
  onFileSelected: (file: File | null) => void;
  onSourceTypeChanged: (value: ImportSourceType) => void;
  onMediumChanged: (value: ImportMedium) => void;
  onAnalyze: () => void;
  isAnalyzing: boolean;
  error: string | null;
}

export function UploadStep({
  selectedFile,
  sourceType,
  medium,
  onFileSelected,
  onSourceTypeChanged,
  onMediumChanged,
  onAnalyze,
  isAnalyzing,
  error,
}: UploadStepProps) {
  function handleFileChange(
    event: ChangeEvent<HTMLInputElement>,
  ) {
    const file = event.target.files?.[0] ?? null;
    onFileSelected(file);
  }

  const requiresMedium =
    sourceType === "Landesenergiebuchhaltung";
  const selectedSource =
    importSourceOptions.find(
      (option) => option.value === sourceType,
    ) ?? importSourceOptions[0];

  const canAnalyze =
    selectedFile !== null &&
    !isAnalyzing &&
    (!requiresMedium || medium !== null);

  return (
    <Card
      title="Importdatei auswählen"
      description="Die Datei wird zunächst ausschließlich analysiert. Es werden noch keine Daten übernommen."
      footer={
        <Button
          type="button"
          variant="primary"
          loading={isAnalyzing}
          disabled={!canAnalyze}
          onClick={onAnalyze}
        >
          Analyse starten
        </Button>
      }
    >
      <div className="import-wizard__upload">
        <div className="import-wizard__file-selection">
          <label
            className="import-wizard__file-button"
            htmlFor="import-file"
            aria-disabled={isAnalyzing}
          >
            Datei auswählen
          </label>

          <input
            id="import-file"
            name="import-file"
            className="import-wizard__file-input"
            type="file"
            onChange={handleFileChange}
            disabled={isAnalyzing}
          />

          {selectedFile ? (
            <dl className="import-wizard__file-summary">
              <div>
                <dt>Dateiname</dt>
                <dd>{selectedFile.name}</dd>
              </div>

              <div>
                <dt>Dateityp</dt>
                <dd>{getFileType(selectedFile)}</dd>
              </div>

              <div>
                <dt>Dateigröße</dt>
                <dd>{formatFileSize(selectedFile.size)}</dd>
              </div>
            </dl>
          ) : (
            <p className="import-wizard__hint">
              Unterstützte Formate:{" "}
              {selectedSource.supportedFormats}
            </p>
          )}
        </div>

        <fieldset disabled={isAnalyzing}>
          <legend>Importquelle</legend>

          {importSourceOptions.map((option) => (
            <label key={option.value}>
              <input
                type="radio"
                name="sourceType"
                checked={sourceType === option.value}
                onChange={() =>
                  onSourceTypeChanged(option.value)
                }
              />
              {option.label}
            </label>
          ))}
        </fieldset>

        {requiresMedium && (
          <fieldset disabled={isAnalyzing}>
            <legend>Medium</legend>

            <label>
              <input
                type="radio"
                name="medium"
                checked={medium === "Electricity"}
                onChange={() =>
                  onMediumChanged("Electricity")
                }
              />
              Strom
            </label>

            <label>
              <input
                type="radio"
                name="medium"
                checked={medium === "Heat"}
                onChange={() =>
                  onMediumChanged("Heat")
                }
              />
              Wärme
            </label>
          </fieldset>
        )}

        {error && (
          <div
            className="import-wizard__error"
            role="alert"
          >
            {error}
          </div>
        )}
      </div>
    </Card>
  );
}

function getFileType(file: File): string {
  const extension = file.name
    .split(".")
    .pop()
    ?.trim()
    .toUpperCase();

  if (extension) {
    return extension;
  }

  return file.type || "Unbekannt";
}

function formatFileSize(sizeInBytes: number): string {
  if (sizeInBytes < 1024) {
    return `${sizeInBytes} B`;
  }

  if (sizeInBytes < 1024 * 1024) {
    return `${(sizeInBytes / 1024).toFixed(1)} KB`;
  }

  return `${(sizeInBytes / (1024 * 1024)).toFixed(1)} MB`;
}
