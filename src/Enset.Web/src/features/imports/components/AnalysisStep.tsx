import { Button } from "../../../components/ui/Button";
import { Card } from "../../../components/ui/Card";
import { StatCard } from "../../../components/ui/StatCard";
import type { ImportAnalysisResult } from "../types/ImportAnalysisResult";

interface AnalysisStepProps {
  fileName: string;
  customerCount: number;
  buildingCount: number;
  meterCount: number;
  meterReadingCount: number;
  issueCount: number;
  importId: string;
  profile: ImportAnalysisResult;
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
  profile,
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
            Prüfhinweise prüfen
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
            title="Prüfhinweise"
            value={issueCount}
          />
        </div>

        <dl className="import-wizard__analysis-details">
          <div>
            <dt>Datei</dt>
            <dd>{fileName}</dd>
          </div>
          {profile.assignedMeterId && <div><dt>Zählpunkt</dt>
            <dd>{profile.defaultMeterNumber ?? profile.assignedMeterId}</dd></div>}
          <div><dt>Zeitraum</dt><dd>{profile.measurementPeriodStart
            ? `${new Date(profile.measurementPeriodStart).toLocaleString("de-AT")} – ${
              new Date(profile.measurementPeriodEnd!).toLocaleString("de-AT")}` : "Nicht erkannt"}</dd></div>
          <div><dt>Intervall</dt><dd>{profile.intervalSeconds
            ? `${profile.intervalSeconds / 60} Minuten` : "Noch festzulegen"}</dd></div>
          <div><dt>Gültige / ungültige Werte</dt>
            <dd>{profile.validReadingCount} / {profile.invalidReadingCount}</dd></div>
          <div><dt>Erwartete / fehlende Intervalle</dt>
            <dd>{profile.expectedIntervalCount} / {profile.missingIntervalCount}</dd></div>
          <div><dt>Dubletten</dt><dd>{profile.duplicateReadingCount}</dd></div>
        </dl>

        <details className="import-wizard__technical-details">
          <summary>Technische Details</summary>
          <code>ImportId: {importId}</code>
        </details>
      </div>
    </Card>
  );
}
