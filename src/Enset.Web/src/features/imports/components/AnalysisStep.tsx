interface AnalysisStepProps {
  fileName: string;
  customerCount: number;
  buildingCount: number;
  issueCount: number;
  onContinue: () => void;
  onBack: () => void;
}

export function AnalysisStep({
  fileName,
  customerCount,
  buildingCount,
  issueCount,
  onContinue,
  onBack,
}: AnalysisStepProps) {
  return (
    <section className="import-wizard__analysis">
      <div>
        <h3>Analyse abgeschlossen</h3>
        <p className="import-wizard__hint">
          Die Importdatei wurde geprüft. Vor der Übernahme können die
          erkannten Daten und Probleme kontrolliert werden.
        </p>
      </div>

      <dl className="import-wizard__summary-grid">
        <div className="import-wizard__summary-card">
          <dt>Datei</dt>
          <dd>{fileName}</dd>
        </div>

        <div className="import-wizard__summary-card">
          <dt>Kunden</dt>
          <dd>{customerCount}</dd>
        </div>

        <div className="import-wizard__summary-card">
          <dt>Gebäude</dt>
          <dd>{buildingCount}</dd>
        </div>

        <div className="import-wizard__summary-card">
          <dt>Issues</dt>
          <dd>{issueCount}</dd>
        </div>
      </dl>

      <div className="import-wizard__actions">
        <button
          type="button"
          className="import-wizard__secondary-action"
          onClick={onBack}
        >
          Zurück
        </button>

        <button
          type="button"
          className="import-wizard__primary-action"
          onClick={onContinue}
        >
          Issues prüfen
        </button>
      </div>
    </section>
  );
}