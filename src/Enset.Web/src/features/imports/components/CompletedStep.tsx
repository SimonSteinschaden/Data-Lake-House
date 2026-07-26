interface CompletedStepProps {
  fileName: string;
  importId: string;
  status: string;
  onRestart: () => void;
}

export function CompletedStep({
  fileName,
  importId,
  status,
  onRestart,
}: CompletedStepProps) {
  return (
    <section className="import-wizard__completed">
      <div
        className="import-wizard__success-marker"
        aria-hidden="true"
      >
        ✓
      </div>

      <div>
        <h3>Import abgeschlossen</h3>
        <p>
          Die Datei <strong>{fileName}</strong> wurde erfolgreich in die
          relationale Datenbank übernommen.
        </p>
      </div>

      <details className="import-wizard__technical-details">
        <summary>Technische Details</summary>
        <code>ImportId: {importId}</code>
        <code>Status: {status}</code>
      </details>

      <button
        type="button"
        className="import-wizard__primary-action"
        onClick={onRestart}
      >
        Neuen Import starten
      </button>
    </section>
  );
}
