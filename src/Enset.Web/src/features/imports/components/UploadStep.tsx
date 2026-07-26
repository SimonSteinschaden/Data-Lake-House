import type { ChangeEvent } from "react";

type ImportSourceType = "EnsetWorkbook" | "Landesenergiebuchhaltung";
type ImportMedium = "Electricity" | "Heat";

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
  function handleFileChange(event: ChangeEvent<HTMLInputElement>) {
    const file = event.target.files?.[0] ?? null;
    onFileSelected(file);
  }

  return (
    <section className="import-wizard__upload">
      <div>
        <h3>Importdatei wählen</h3>

        <p className="import-wizard__hint">
          Die Datei wird zunächst ausschließlich analysiert. Es werden noch
          keine Daten übernommen.
        </p>
      </div>

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
          accept=".xlsx,.xlsm,.csv,text/csv"
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
            Unterstützte Formate: .xlsx, .xlsm und .csv
          </p>
        )}
      </div>

      <fieldset disabled={isAnalyzing}>
        <legend>Importquelle</legend>

        <label>
          <input
            type="radio"
            name="sourceType"
            checked={sourceType === "EnsetWorkbook"}
            onChange={() => onSourceTypeChanged("EnsetWorkbook")}
          />
          ENSET Workbook
        </label>

        <label>
          <input
            type="radio"
            name="sourceType"
            checked={sourceType === "Landesenergiebuchhaltung"}
            onChange={() =>
              onSourceTypeChanged("Landesenergiebuchhaltung")
            }
          />
          Landesenergiebuchhaltung
        </label>
      </fieldset>

      {sourceType === "Landesenergiebuchhaltung" && (
        <fieldset disabled={isAnalyzing}>
          <legend>Medium</legend>

          <label>
            <input
              type="radio"
              name="medium"
              checked={medium === "Electricity"}
              onChange={() => onMediumChanged("Electricity")}
            />
            Strom
          </label>

          <label>
            <input
              type="radio"
              name="medium"
              checked={medium === "Heat"}
              onChange={() => onMediumChanged("Heat")}
            />
            Wärme
          </label>
        </fieldset>
      )}

      {error && (
        <div className="import-wizard__error" role="alert">
          {error}
        </div>
      )}

      <div className="import-wizard__actions">
        <button
          type="button"
          className="import-wizard__primary-action"
          disabled={
            !selectedFile ||
            isAnalyzing ||
            (sourceType === "Landesenergiebuchhaltung" && !medium)
          }
          onClick={onAnalyze}
          aria-busy={isAnalyzing}
        >
          {isAnalyzing ? "Analyse läuft …" : "Analyse starten"}
        </button>
      </div>
    </section>
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