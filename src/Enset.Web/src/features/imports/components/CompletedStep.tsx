interface CompletedStepProps {
  fileName: string;
  onRestart: () => void;
}

export function CompletedStep({
  fileName,
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
          Die Datei <strong>{fileName}</strong> wurde erfolgreich
          verarbeitet und übernommen.
        </p>
      </div>

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