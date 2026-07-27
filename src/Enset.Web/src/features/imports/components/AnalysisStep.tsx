import { Button } from "../../../components/ui/Button";
import { Card } from "../../../components/ui/Card";
import { StatCard } from "../../../components/ui/StatCard";

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
    <Card
      title="Analyse abgeschlossen"
      description="Die Importdatei wurde geprüft. Vor der Übernahme können die erkannten Daten und Probleme kontrolliert werden."
      footer={
        <>
          <Button
            type="button"
            variant="secondary"
            onClick={onBack}
          >
            Zurück
          </Button>

          <Button
            type="button"
            variant="primary"
            onClick={onContinue}
          >
            Issues prüfen
          </Button>
        </>
      }
    >
      <div className="import-wizard__analysis">
        <div className="import-wizard__statistics">
          <StatCard
            title="Kunden"
            value={customerCount}
          />

          <StatCard
            title="Gebäude"
            value={buildingCount}
          />

          <StatCard
            title="Zähler"
            value={meterCount}
          />

          <StatCard
            title="Messwerte"
            value={meterReadingCount}
          />

          <StatCard
            title="Issues"
            value={issueCount}
          />
        </div>

        <dl className="import-wizard__analysis-details">
          <div>
            <dt>Datei</dt>
            <dd>{fileName}</dd>
          </div>
        </dl>

        <details className="import-wizard__technical-details">
          <summary>Technische Details</summary>
          <code>ImportId: {importId}</code>
        </details>
      </div>
    </Card>
  );
}