interface AnalysisStepProps {
  fileName: string;
  customerCount: number;
  buildingCount: number;
  meterCount: number;
  meterReadingCount: number;
  issueCount: number;
  importId: string;
  onContinue: () => void;
  onBack: () => void;
}

export function AnalysisStep({
  fileName,
  customerCount,
  buildingCount,
  meterCount,
  meterReadingCount,
  issueCount,
  importId,
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

        <div className="import-wizard__summary-card">
          <dt>Zähler</dt>
          <dd>{meterCount}</dd>
        </div>

        <div className="import-wizard__summary-card">
          <dt>Messwerte</dt>
          <dd>{meterReadingCount}</dd>
        </div>
      </dl>

      <details className="import-wizard__technical-details">
        <summary>Technische Details</summary>
        <code>ImportId: {importId}</code>
      </details>

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
