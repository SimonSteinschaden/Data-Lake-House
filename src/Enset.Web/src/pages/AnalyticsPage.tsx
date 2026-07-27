import { useEffect, useMemo, useState } from "react";
import { Link, useSearchParams } from "react-router";
import { Card } from "../components/ui/Card";
import { StatCard } from "../components/ui/StatCard";
import type { BuildingSummary } from "../features/buildings/types";
import { PageHeader } from "../layouts/PageHeader";
import { buildingService } from "../services/buildingService";
import "./AnalyticsPage.css";

const PERIODS = {
  "last-12-months": "Letzte 12 Monate",
  "current-year": "Aktuelles Jahr",
  "previous-year": "Vorjahr",
} as const;

const COMPARISON_PERIODS = {
  "previous-year": "Vorjahreszeitraum",
  "previous-period": "Vorheriger Zeitraum",
  none: "Kein Vergleich",
} as const;

type Period = keyof typeof PERIODS;
type ComparisonPeriod = keyof typeof COMPARISON_PERIODS;

function toDateParameter(date: Date) {
  return date.toISOString().slice(0, 10);
}

function datesForPeriod(period: Period) {
  const today = new Date();
  const year = today.getFullYear();

  if (period === "current-year") {
    return { from: `${year}-01-01`, to: toDateParameter(today) };
  }

  if (period === "previous-year") {
    return { from: `${year - 1}-01-01`, to: `${year - 1}-12-31` };
  }

  const from = new Date(today);
  from.setFullYear(from.getFullYear() - 1);
  from.setDate(from.getDate() + 1);
  return { from: toDateParameter(from), to: toDateParameter(today) };
}

function inferPeriod(from: string | null, to: string | null): Period {
  return (Object.keys(PERIODS) as Period[]).find((period) => {
    const dates = datesForPeriod(period);
    return dates.from === from && dates.to === to;
  }) ?? "last-12-months";
}

export function AnalyticsPage() {
  const [searchParams, setSearchParams] = useSearchParams();
  const [buildings, setBuildings] = useState<BuildingSummary[]>([]);
  const [isLoadingBuildings, setIsLoadingBuildings] = useState(true);
  const [buildingError, setBuildingError] = useState("");

  const buildingId = searchParams.get("buildingId") ?? "";
  const period = inferPeriod(searchParams.get("from"), searchParams.get("to"));
  const comparison =
    (searchParams.get("comparison") as ComparisonPeriod | null) ??
    "previous-year";
  const energyCarrier = searchParams.get("energyCarrier") ?? "all";
  const energySystemId = searchParams.get("energySystemId") ?? "all";

  useEffect(() => {
    const controller = new AbortController();
    buildingService
      .list(
        { page: 1, pageSize: 200, sortBy: "name", sortDirection: "asc" },
        controller.signal,
      )
      .then((result) => {
        setBuildings(result.items);
        setBuildingError("");
      })
      .catch(() => {
        if (!controller.signal.aborted) {
          setBuildings([]);
          setBuildingError("Gebäude konnten nicht geladen werden.");
        }
      })
      .finally(() => {
        if (!controller.signal.aborted) setIsLoadingBuildings(false);
      });
    return () => controller.abort();
  }, []);

  const updateParameters = (values: Record<string, string | undefined>) => {
    setSearchParams((current) => {
      const next = new URLSearchParams(current);
      Object.entries(values).forEach(([key, value]) => {
        if (!value || value === "all") next.delete(key);
        else next.set(key, value);
      });
      return next;
    }, { replace: true });
  };

  const changePeriod = (nextPeriod: Period) => {
    updateParameters(datesForPeriod(nextPeriod));
  };

  const changeBuilding = (nextBuildingId: string) => {
    updateParameters({
      buildingId: nextBuildingId || undefined,
      energySystemId: undefined,
    });
  };

  return (
    <section className="object-analysis">
      <PageHeader
        title="Objektanalyse"
        description="Analyse von Energieverbrauch, Anlagen, Lastprofilen und Auffälligkeiten eines ausgewählten Gebäudes."
      />

      <Card
        className="object-analysis__filter-card"
        title="Analysekontext"
        description="Gebäude und fachlichen Betrachtungsrahmen festlegen."
      >
        <div className="object-analysis__filters">
          <label>
            Gebäude / Objekt
            <select
              value={buildingId}
              onChange={(event) => changeBuilding(event.target.value)}
              disabled={isLoadingBuildings}
            >
              <option value="">
                {isLoadingBuildings
                  ? "Gebäude werden geladen …"
                  : "Kein Gebäude ausgewählt"}
              </option>
              {buildings.map((building) => (
                <option key={building.id} value={building.id}>
                  {building.name} · {building.buildingNumber}
                </option>
              ))}
            </select>
            {buildingError && (
              <span className="object-analysis__filter-error">
                {buildingError}
              </span>
            )}
          </label>

          <label>
            Zeitraum
            <select
              value={period}
              onChange={(event) => changePeriod(event.target.value as Period)}
            >
              {Object.entries(PERIODS).map(([value, label]) => (
                <option key={value} value={value}>{label}</option>
              ))}
            </select>
          </label>

          <label>
            Vergleichszeitraum
            <select
              value={comparison}
              onChange={(event) =>
                updateParameters({ comparison: event.target.value })
              }
            >
              {Object.entries(COMPARISON_PERIODS).map(([value, label]) => (
                <option key={value} value={value}>{label}</option>
              ))}
            </select>
          </label>

          <label>
            Energieträger
            <select
              value={energyCarrier}
              onChange={(event) =>
                updateParameters({ energyCarrier: event.target.value })
              }
            >
              <option value="all">Alle</option>
            </select>
          </label>

          <label>
            Anlage
            <select
              value={energySystemId}
              onChange={(event) =>
                updateParameters({ energySystemId: event.target.value })
              }
              disabled={!buildingId}
            >
              <option value="all">Alle</option>
            </select>
          </label>
        </div>
      </Card>

      {!buildingId ? <NoBuildingSelected /> : <AnalysisWorkspace />}
    </section>
  );
}

function NoBuildingSelected() {
  return (
    <div className="object-analysis__empty">
      <strong>Kein Gebäude ausgewählt</strong>
      <p>
        Wählen Sie ein Gebäude aus, um Energiekennzahlen, Lastprofile und
        Anlageninformationen anzuzeigen.
      </p>
      <Link to="/buildings">Zu den Gebäuden</Link>
    </div>
  );
}

const tabs = ["Übersicht", "Energie", "Anlagen", "Kosten", "Emissionen", "Lastprofile"];

type BuildingDataProduct =
  | "BuildingEnergySummary"
  | "BuildingLoadProfile"
  | "BuildingEnergyConsumptionByCarrier"
  | "BuildingEnergyConsumptionByUsageType"
  | "BuildingEnergySystemPerformance"
  | "BuildingAnomalySummary";

const analysisPanels: {
  title: string;
  dataProduct: BuildingDataProduct;
}[] = [
  { title: "Lastprofil", dataProduct: "BuildingLoadProfile" },
  {
    title: "Verbrauch nach Energieträger",
    dataProduct: "BuildingEnergyConsumptionByCarrier",
  },
  {
    title: "Verbrauch nach Nutzungsart",
    dataProduct: "BuildingEnergyConsumptionByUsageType",
  },
  {
    title: "Anlagenübersicht",
    dataProduct: "BuildingEnergySystemPerformance",
  },
  {
    title: "Auffälligkeiten und Warnungen",
    dataProduct: "BuildingAnomalySummary",
  },
];

function AnalysisWorkspace() {
  const panelList = useMemo(() => analysisPanels, []);

  return (
    <div className="object-analysis__workspace">
      <nav className="object-analysis__tabs" aria-label="Analysebereiche">
        {tabs.map((tab, index) => (
          <button
            key={tab}
            type="button"
            className={index === 0 ? "object-analysis__tab object-analysis__tab--active" : "object-analysis__tab"}
            disabled={index !== 0}
            aria-current={index === 0 ? "page" : undefined}
            title={index !== 0 ? "In Vorbereitung" : undefined}
          >
            {tab}
            {index !== 0 && <span>In Vorbereitung</span>}
          </button>
        ))}
      </nav>

      <div
        className="object-analysis__stats"
        data-product="BuildingEnergySummary"
      >
        {["Energieverbrauch", "Energiekosten", "CO₂-Emissionen", "Spitzenlast"].map(
          (title) => (
            <StatCard
              key={title}
              title={title}
              value="Keine Daten"
              subtitle="Aggregierte Analysedaten sind noch nicht verfügbar."
            />
          ),
        )}
      </div>

      <div className="object-analysis__panels">
        {panelList.map((panel) => (
          <AnalysisPanel key={panel.dataProduct} {...panel} />
        ))}
      </div>
    </div>
  );
}

function AnalysisPanel({
  title,
  dataProduct,
}: {
  title: string;
  dataProduct: BuildingDataProduct;
}) {
  return (
    <Card className="object-analysis__panel" title={title} data-product={dataProduct}>
      <div className="object-analysis__panel-empty">
        <strong>Keine Analysedaten verfügbar</strong>
        <p>
          Für dieses Gebäude sind noch keine aggregierten Analysedaten
          verfügbar.
        </p>
      </div>
    </Card>
  );
}
