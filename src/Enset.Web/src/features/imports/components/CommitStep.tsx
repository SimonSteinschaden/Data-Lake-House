interface CommitStepProps {
  fileName: string;
  customerCount: number;
  buildingCount: number;
  issueCount: number;
  onCommit: () => void;
  onBack: () => void;
}

export function CommitStep({
  fileName,
  customerCount,
  buildingCount,
  issueCount,
  onCommit,
  onBack,
}: CommitStepProps) {
  return (
    <section className="import-wizard__commit">
      <div>
        <h3>Import freigeben</h3>
        <p className="import-wizard__hint">
          Kontrolliere die Zusammenfassung. Erst mit der Freigabe werden
          die Daten tatsächlich geschrieben.
        </p>
      </div>

      <dl className="import-wizard__commit-summary">
        <div>
          <dt>Datei</dt>
          <dd>{fileName}</dd>
        </div>

        <div>
          <dt>Kunden</dt>
          <dd>{customerCount}</dd>
        </div>

        <div>
          <dt>Gebäude</dt>
          <dd>{buildingCount}</dd>
        </div>

        <div>
          <dt>Gelöste Issues</dt>
          <dd>{issueCount}</dd>
        </div>
      </dl>

      <div className="import-wizard__notice">
        <strong>Wichtiger Hinweis</strong>
        <p>
          Dieser Schritt entspricht dem Write Gate des Data Lake House.
          Ohne erfolgreiche Prüfung darf kein Writer ausgeführt werden.
        </p>
      </div>

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
          onClick={onCommit}
        >
          Import verbindlich übernehmen
        </button>
      </div>
    </section>
  );
}