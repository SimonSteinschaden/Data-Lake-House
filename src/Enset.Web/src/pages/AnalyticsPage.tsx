import { useEffect, useState } from "react";
import { Card } from "../components/ui/Card";
import { StatCard } from "../components/ui/StatCard";
import { PageHeader } from "../layouts/PageHeader";
import { buildingService } from "../services/buildingService";
import type { BuildingSummary } from "../features/buildings/types";
import "./AnalyticsPage.css";

export function AnalyticsPage() {
  const [buildings, setBuildings] = useState<BuildingSummary[]>([]);
  const [buildingId, setBuildingId] = useState("");

  useEffect(() => {
    const controller = new AbortController();
    buildingService
      .list({ page: 1, pageSize: 100, sortBy: "name" }, controller.signal)
      .then((result) => setBuildings(result.items))
      .catch(() => setBuildings([]));
    return () => controller.abort();
  }, []);

  return (
    <section className="object-analysis">
      <PageHeader
        title="Objektanalyse"
        description="Analyse von Energieverbrauch, Anlagen, Kosten, Emissionen und Lastprofilen eines ausgewählten Gebäudes."
      />

      <Card
        title="Analysekontext"
        description="Wählen Sie ein Gebäude und die gewünschten fachlichen Filter aus."
      >
        <ObjectAnalysisFilters
          buildings={buildings}
          buildingId={buildingId}
          onBuildingChanged={setBuildingId}
        />
      </Card>

      {buildingId ? (
        <AnalysisWorkspace />
      ) : (
        <div className="object-analysis__empty">
          <strong>Kein Gebäude ausgewählt</strong>
          <p>
            Bitte wählen Sie ein Gebäude aus, um Energiekennzahlen und
            Lastprofile anzuzeigen.
          </p>
        </div>
      )}
    </section>
  );
}

interface ObjectAnalysisFiltersProps {
  buildings: BuildingSummary[];
  buildingId: string;
  onBuildingChanged: (buildingId: string) => void;
}

function ObjectAnalysisFilters({
  buildings,
  buildingId,
  onBuildingChanged,
}: ObjectAnalysisFiltersProps) {
  return (
    <div className="object-analysis__filters">
      <label>
        Gebäude / Objekt
        <select
          value={buildingId}
          onChange={(event) => onBuildingChanged(event.target.value)}
        >
          <option value="">Kein Objekt ausgewählt</option>
          {buildings.map((building) => (
            <option key={building.id} value={building.id}>
              {building.name} · {building.buildingNumber}
            </option>
          ))}
        </select>
      </label>
      <label>
        Zeitraum
        <select defaultValue="last-12-months">
          <option value="last-12-months">Letzte 12 Monate</option>
          <option value="current-year">Aktuelles Jahr</option>
          <option value="previous-year">Vorjahr</option>
        </select>
      </label>
      <label>
        Vergleichszeitraum
        <select defaultValue="none">
          <option value="none">Kein Vergleich</option>
          <option value="previous-period">Vorheriger Zeitraum</option>
          <option value="previous-year">Vorjahreszeitraum</option>
        </select>
      </label>
      <label>
        Energieträger
        <select defaultValue="all">
          <option value="all">Alle Energieträger</option>
        </select>
      </label>
      <label>
        Anlagenfilter
        <select defaultValue="all">
          <option value="all">Alle Anlagen</option>
        </select>
      </label>
    </div>
  );
}

function AnalysisWorkspace() {
  return (
    <div className="object-analysis__workspace">
      <div className="object-analysis__stats">
        <StatCard
          title="Energieverbrauch"
          value="—"
          subtitle="Noch nicht verfügbar"
        />
        <StatCard
          title="Energiekosten"
          value="—"
          subtitle="Noch nicht verfügbar"
        />
        <StatCard title="CO₂" value="—" subtitle="Noch nicht verfügbar" />
        <StatCard
          title="Spitzenlast"
          value="—"
          subtitle="Noch nicht verfügbar"
        />
      </div>

      <div className="object-analysis__panels">
        <AnalysisPanel
          title="Lastprofil"
          description="Zeitreihen, Spitzenlasten und spätere Flexibilitätsanalyse."
        />
        <AnalysisPanel
          title="Energieverbrauch"
          description="Verbrauch nach Zeitraum, Energieträger und Vergleichsperiode."
        />
        <AnalysisPanel
          title="Anlagenübersicht"
          description="Anlagen, Energieflüsse und spätere Optimierungsergebnisse."
        />
        <AnalysisPanel
          title="Warnungen"
          description="Objektbezogene Auffälligkeiten und Datenqualitätsbefunde."
        />
      </div>
    </div>
  );
}

function AnalysisPanel({
  title,
  description,
}: {
  title: string;
  description: string;
}) {
  return (
    <Card title={title} description={description}>
      <div className="object-analysis__panel-placeholder">
        <span>Analysefläche vorbereitet</span>
        <small>Noch keine Daten verfügbar</small>
      </div>
    </Card>
  );
}
