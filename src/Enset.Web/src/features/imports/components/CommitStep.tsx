import type { MeterReadingImportProgress } from "../../../services/importService";

interface CommitStepProps {
  fileName: string;
  customerCount: number;
  buildingCount: number;
  meterCount: number;
  meterReadingCount: number;
  issueCount: number;
  status: string;
  isCommitting: boolean;
  error: string | null;
  progress: MeterReadingImportProgress | null;
  onCommit: () => void;
  onCancel: () => void;
  onBack: () => void;
}

export function CommitStep({
  fileName,
  customerCount,
  buildingCount,
  meterCount,
  meterReadingCount,
  issueCount,
  status,
  isCommitting,
  error,
  progress,
  onCommit,
  onCancel,
  onBack,
}: CommitStepProps) {
  return (
    <section className="import-wizard__commit">
      <div>
        <h3>Import freigeben</h3>
        <p className="import-wizard__hint">
          Kontrolliere die Zusammenfassung. Erst mit der Freigabe werden
          die Daten tatsächlich relational in PostgreSQL geschrieben.
        </p>
      </div>

      <dl className="import-wizard__commit-summary">
        <div><dt>Datei</dt><dd>{fileName}</dd></div>
        <div><dt>Kunden</dt><dd>{customerCount}</dd></div>
        <div><dt>Gebäude</dt><dd>{buildingCount}</dd></div>
        <div><dt>Zähler</dt><dd>{meterCount}</dd></div>
        <div><dt>Messwerte</dt><dd>{meterReadingCount}</dd></div>
        <div><dt>Prüfhinweise</dt><dd>{issueCount}</dd></div>
        <div><dt>Serverstatus</dt><dd>{status}</dd></div>
      </dl>

      <div className="import-wizard__notice">
        <strong>Wichtiger Hinweis</strong>
        <p>
          Der Server prüft vor dem Schreiben das Write Gate. Der Wizard
          wertet ausschließlich den zurückgegebenen Importbericht aus.
        </p>
      </div>

      {error && (
        <div className="import-wizard__error" role="alert">
          {error}
        </div>
      )}

      {progress && (
        <div className="import-wizard__notice" aria-live="polite">
          <strong>Phase: {progress.phase}</strong>
          <progress max={100} value={progress.progressPercent} />
          <p>
            Gelesen: {progress.readRows} · geschrieben: {progress.writtenRows}
            {" · "}verworfen: {progress.rejectedRows}
            {" · "}Dubletten: {progress.duplicateRows}
          </p>
        </div>
      )}

      <div className="import-wizard__actions">
        <button
          type="button"
          className="import-wizard__secondary-action"
          disabled={isCommitting}
          onClick={onBack}
        >
          Zurück
        </button>
        {isCommitting && (
          <button type="button" className="import-wizard__secondary-action"
            onClick={onCancel}>
            Import abbrechen
          </button>
        )}

        <button
          type="button"
          className="import-wizard__primary-action"
          disabled={isCommitting || status !== "ReadyToCommit"}
          onClick={onCommit}
          aria-busy={isCommitting}
        >
          {isCommitting
            ? "Import wird geschrieben …"
            : "Import verbindlich übernehmen"}
        </button>
      </div>
    </section>
  );
}
