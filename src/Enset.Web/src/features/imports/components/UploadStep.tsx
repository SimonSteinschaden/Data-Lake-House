import type { ChangeEvent } from "react";

interface UploadStepProps {
  selectedFile: File | null;
  onFileSelected: (file: File | null) => void;
  onAnalyze: () => void;
}

export function UploadStep({
  selectedFile,
  onFileSelected,
  onAnalyze,
}: UploadStepProps) {
  function handleFileChange(
    event: ChangeEvent<HTMLInputElement>,
  ) {
    const file = event.target.files?.[0] ?? null;
    onFileSelected(file);
  }

  return (
    <section className="import-wizard__upload">
      <div>
        <h3>Excel-Datei auswählen</h3>
        <p className="import-wizard__hint">
          Die Datei wird zunächst ausschließlich analysiert. Es werden
          noch keine Daten übernommen.
        </p>
      </div>

      <label
        className="import-wizard__file-label"
        htmlFor="import-file"
      >
        Importdatei
      </label>

      <input
        id="import-file"
        name="import-file"
        type="file"
        accept=".xlsx,.xlsm"
        onChange={handleFileChange}
      />

      {selectedFile ? (
        <div className="import-wizard__file-summary">
          <strong>{selectedFile.name}</strong>
          <span>{formatFileSize(selectedFile.size)}</span>
          <span>Typ: {selectedFile.type || "Unbekannt"}</span>
        </div>
      ) : (
        <p className="import-wizard__hint">
          Unterstützte Formate: .xlsx und .xlsm
        </p>
      )}

      <div className="import-wizard__actions">
        <button
          type="button"
          className="import-wizard__primary-action"
          disabled={!selectedFile}
          onClick={onAnalyze}
        >
          Analyse starten
        </button>
      </div>
    </section>
  );
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