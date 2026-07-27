import { useEffect, useState } from "react";

import { Card } from "../components/ui/Card";
import { Link } from "react-router";
import { StatCard } from "../components/ui/StatCard";
import { PageHeader } from "../layouts/PageHeader";

import { ManagementEnergyOverview } from "../features/analytics/ManagementEnergyOverview";
import { analyticsService } from "../services/analyticsService";
import type {
  DataQualitySummary,
  EnergyPortfolio,
  ManagementWarnings,
  MeteringCoverage,
  PortfolioSummary,
  RegionalBuildingDistribution,
} from "../features/analytics/types";

import "./DashboardPage.css";

interface ManagementData {
  summary?: PortfolioSummary;
  coverage?: MeteringCoverage;
  quality?: DataQualitySummary;
  energyPortfolio?: EnergyPortfolio;
  warnings?: ManagementWarnings; 
  regional?: RegionalBuildingDistribution;
}

const futureMetrics = [
  "CO₂-Emissionen",
  "Einsparpotenzial",
  "Peak Load",
  "Netzbelastung",
  "Flexibilität",
  "Optimierte Lastkurven",
];

export function DashboardPage() {
  const [data, setData] = useState<ManagementData>({});

  useEffect(() => {
    const controller = new AbortController();

    Promise.allSettled([
      analyticsService.portfolioSummary(controller.signal),
      analyticsService.meteringCoverage(controller.signal),
      analyticsService.dataQuality(controller.signal),
      analyticsService.energyPortfolio(controller.signal),
      analyticsService.warnings(controller.signal),
      analyticsService.regionalBuildingDistribution(controller.signal),
    ]).then(([summary, coverage, quality, energyPortfolio, warnings, regional]) => {
      if (controller.signal.aborted) {
        return;
      }

      setData({
        summary: settled(summary),
        coverage: settled(coverage),
        quality: settled(quality),
        energyPortfolio: settled(energyPortfolio),
        warnings: settled(warnings),
        regional: settled(regional),
      });
    });

    return () => controller.abort();
  }, []);

  return (
    <section className="management-dashboard">
      <PageHeader
        title="Dashboard"
        description="Überblick über Regionen, Objekte, Energieinfrastruktur und Datenqualität."
      />

      <Card
        title="Regionale Übersicht"
        description="Gebäude und Objekte nach Postleitzahl und regionaler Verteilung."
      >
        <div
          className="management-dashboard__map"
          role="img"
          aria-label="Kartenplatzhalter für Österreich und Regionen"
        >
          <div className="management-dashboard__map-mark">
            AT
          </div>

          <div className="management-dashboard__map-content">
            <strong>
              Kartenansicht Österreich / Region
            </strong>

            <span>
              {data.regional?.regions.length
                ? `${data.regional.regions.length.toLocaleString("de-AT")} regionale Datenpunkte`
                : "Keine geokodierten Gebäudedaten verfügbar"}
            </span>
          </div>

          <p>
            Gemeindegrenzen werden in einer späteren
            Ausbaustufe ergänzt.
          </p>
        </div>
      </Card>

      <section aria-labelledby="management-totals">
        <h2
          id="management-totals"
          className="dashboard-section-title"
        >
          Portfolioübersicht
        </h2>

        <div className="management-dashboard__stats">
          <StatCard
            title="Kunden"
            value={displayTotal(data.summary?.customerCount)}
            subtitle="Im zentralen Datenbestand"
          />

          <StatCard
            title="Gebäude"
            value={displayTotal(data.summary?.buildingCount)}
            subtitle="Erfasste Objekte"
          />

          <StatCard
            title="Zähler"
            value={displayTotal(data.summary?.meterCount)}
            subtitle="Erfasste Messstellen"
          />

          <StatCard
            title="Dokumente"
            value={displayTotal(data.summary?.documentCount)}
            subtitle="Im zentralen Datenbestand"
          />

          <StatCard
            title="Datenqualität"
            value="Bewertung noch nicht verfügbar"
          />

          <StatCard
            title="Warnungen"
            value={displayTotal(data.warnings?.totalAffectedCount)}
            subtitle="Betroffene Objekte in Warnkategorien"
          />
        </div>
      </section>

      <ManagementEnergyOverview />

      <section aria-labelledby="portfolio-status">
        <h2
          id="portfolio-status"
          className="dashboard-section-title"
        >
          Portfolio- und Datenstatus
        </h2>

        <div className="management-dashboard__operations">
          <Card
            title="Messdatenabdeckung"
            description="Übersicht über die Verfügbarkeit energierelevanter Messdaten."
          >
            <dl className="management-dashboard__status-list">
              <div>
                <dt>Gebäude mit Messdaten</dt>
                <dd>{displayTotal(data.coverage?.buildingsWithReadings)}</dd>
              </div>

              <div>
                <dt>Gebäude ohne Messdaten</dt>
                <dd>{displayTotal(data.coverage?.buildingsWithoutReadings)}</dd>
              </div>

              <div>
                <dt>Aktive Zähler</dt>
                <dd>{displayTotal(data.coverage?.activeMeters)}</dd>
              </div>
            </dl>

            <p className="dashboard-empty">
              Abdeckungsquote: {displayPercent(data.coverage?.coveragePercent)}
            </p>
          </Card>

          <Card
            title="Datenqualität"
            description="Qualitätsstatus der Gebäude-, Anlagen- und Messdaten."
          >
            <ul className="management-dashboard__quality-list">
              <li>
                <span>Fehlende Zuordnungen</span>
                <strong>{displayTotal(data.quality?.missingAssignments)}</strong>
              </li>

              <li>
                <span>Veraltete Messwerte</span>
                <strong>{displayTotal(data.quality?.staleReadings)}</strong>
              </li>

              <li>
                <span>Unvollständige Stammdaten</span>
                <strong>{displayTotal(data.quality?.incompleteMasterData)}</strong>
              </li>
            </ul>

            <p>
              Kritische Warnungen:{' '}
              <Link to="/tools/data-quality/warnings?severity=Critical">
                {displayTotal(data.quality?.criticalWarnings)}
              </Link>
            </p>

            <p>
              <Link to="/tools/data-quality/warnings">
                Details anzeigen
              </Link>
            </p>
          </Card>

          <Card
            title="Energieportfolio"
            description="Struktur des erfassten Gebäude- und Anlagenportfolios."
          >
            <dl className="management-dashboard__status-list">
              <div>
                <dt>Gebäude mit Stromzählern</dt>
                <dd>{displayTotal(data.energyPortfolio?.buildingsWithElectricityMeters)}</dd>
              </div>

              <div>
                <dt>Gebäude mit Wärmezählern</dt>
                <dd>{displayTotal(data.energyPortfolio?.buildingsWithHeatMeters)}</dd>
              </div>

              <div>
                <dt>Erfasste Energieanlagen</dt>
                <dd>{displayTotal(data.energyPortfolio?.energySystemCount)}</dd>
              </div>
            </dl>

            <p className="dashboard-empty">
              Ausschließlich aus dem Data Product „EnergyPortfolioStructure“.
            </p>
          </Card>
        </div>
      </section>

      <section aria-labelledby="advanced-energy-metrics">
        <h2
          id="advanced-energy-metrics"
          className="dashboard-section-title"
        >
          Erweiterte Energiekennzahlen
        </h2>

        <div className="management-dashboard__future">
          {futureMetrics.map((metric) => (
            <div
              key={metric}
              className="management-dashboard__future-card"
            >
              <span>{metric}</span>
              <strong>In Vorbereitung</strong>
            </div>
          ))}
        </div>
      </section>
    </section>
  );
}

function displayTotal(value?: number): string {
  return value === undefined
    ? "—"
    : value.toLocaleString("de-AT");
}

function displayPercent(value?: number | null): string {
  return value === undefined || value === null
    ? "—"
    : `${value.toLocaleString("de-AT", { maximumFractionDigits: 1 })} %`;
}

function settled<T>(result: PromiseSettledResult<T>): T | undefined {
  return result.status === "fulfilled" ? result.value : undefined;
}
