import { useEffect, useState } from "react";
import { Card } from "../../components/ui/Card";
import { analyticsService } from "../../services/analyticsService";
import type {
  ConsumptionByCarrier,
  ConsumptionByLocation,
  ConsumptionByUsageType,
  LoadProfile,
  MonthlyConsumption,
} from "./types";
import "./ManagementEnergyOverview.css";

const currentYear = new Date().getFullYear();
const months = [
  "Jan", "Feb", "Mär", "Apr", "Mai", "Jun",
  "Jul", "Aug", "Sep", "Okt", "Nov", "Dez",
];

type Aggregation = "QuarterHour" | "Hour" | "Day" | "Week" | "Month";

interface EnergyData {
  loadProfile?: LoadProfile;
  consumption?: MonthlyConsumption;
  locations?: ConsumptionByLocation;
  usageTypes?: ConsumptionByUsageType;
  carriers?: ConsumptionByCarrier;
}

export function ManagementEnergyOverview() {
  const [year, setYear] = useState(currentYear);
  const [aggregation, setAggregation] = useState<Aggregation>("Hour");
  const [data, setData] = useState<EnergyData>({});

  useEffect(() => {
    const controller = new AbortController();
    const filters = { year, aggregation };
    Promise.allSettled([
      analyticsService.loadProfile(filters, controller.signal),
      analyticsService.monthlyConsumption(filters, controller.signal),
      analyticsService.consumptionByLocation(filters, controller.signal),
      analyticsService.consumptionByUsageType(filters, controller.signal),
      analyticsService.consumptionByCarrier(filters, controller.signal),
    ]).then(([loadProfile, consumption, locations, usageTypes, carriers]) => {
      if (controller.signal.aborted) return;
      setData({
        loadProfile: fulfilled(loadProfile),
        consumption: fulfilled(consumption),
        locations: fulfilled(locations),
        usageTypes: fulfilled(usageTypes),
        carriers: fulfilled(carriers),
      });
    });
    return () => controller.abort();
  }, [aggregation, year]);

  return (
    <section className="management-energy" aria-labelledby="management-energy-title">
      <h2 id="management-energy-title" className="dashboard-section-title">
        Portfolio-Energieübersicht
      </h2>

      <LoadProfileFilters
        year={year}
        aggregation={aggregation}
        onYearChange={setYear}
        onAggregationChange={setAggregation}
      />

      <Card
        title="Jahreslastprofil Strom"
        description="Kumulierte elektrische Last aller berücksichtigten Zähler und Gebäude."
      >
        <LoadProfileChart data={data.loadProfile} />
      </Card>

      <Card
        title="Energieverbrauch im Jahresverlauf"
        description="Monatlich aggregierter Stromverbrauch des aktuell berücksichtigten Datenbestands."
      >
        <EnergyConsumptionChart data={data.consumption} year={year} />
      </Card>

      <div className="management-energy__breakdowns">
        <Card title="Verbrauch nach Standort" description="Top-Gebäude nach Energieverbrauch.">
          <HorizontalBars
            items={data.locations?.locations.map(item => ({
              label: item.building,
              value: item.consumption,
              unit: item.unit,
            }))}
            emptyText="Keine zuordenbaren Gebäudeverbräuche vorhanden."
          />
        </Card>
        <Card title="Verbrauch nach Nutzungsart" description="Energieverbrauch nach kanonischer Zähler-Nutzungsart.">
          <HorizontalBars
            items={data.usageTypes?.usageTypes.map(item => ({
              label: item.name,
              value: item.consumption,
              unit: item.unit,
            }))}
            emptyText="Noch keine kanonischen Nutzungsarten zugeordnet."
          />
        </Card>
        <Card title="Verbrauch nach Energieträger" description="Einheitlich nach kWh normalisierte Energiemengen.">
          <HorizontalBars
            items={data.carriers?.carriers.map(item => ({
              label: item.carrier,
              value: item.consumption,
              unit: item.unit,
            }))}
            emptyText="Keine dimensionskompatiblen Energiewerte vorhanden."
          />
        </Card>
      </div>
    </section>
  );
}

function LoadProfileFilters({
  year,
  aggregation,
  onYearChange,
  onAggregationChange,
}: {
  year: number;
  aggregation: Aggregation;
  onYearChange: (year: number) => void;
  onAggregationChange: (aggregation: Aggregation) => void;
}) {
  return (
    <div className="management-energy__filters" aria-label="Filter der Portfolio-Energieübersicht">
      <label>
        Kalenderjahr
        <select value={year} onChange={event => onYearChange(Number(event.target.value))}>
          {[0, 1, 2, 3, 4].map(offset => (
            <option key={currentYear - offset} value={currentYear - offset}>
              {currentYear - offset}
            </option>
          ))}
        </select>
      </label>
      <label>Region<select disabled><option>Gesamtes Portfolio</option></select></label>
      <label>Postleitzahl<select disabled><option>Alle Postleitzahlen</option></select></label>
      <label>Kunde<select disabled><option>Alle Kunden</option></select></label>
      <label>Gebäude<select disabled><option>Alle Gebäude</option></select></label>
      <label>Zähler<select disabled><option>Alle Stromzähler</option></select></label>
      <label>
        Zeitauflösung
        <select
          value={aggregation}
          onChange={event => onAggregationChange(event.target.value as Aggregation)}
        >
          <option value="QuarterHour">15 Minuten</option>
          <option value="Hour">Stunde</option>
          <option value="Day">Tag</option>
          <option value="Week">Woche</option>
          <option value="Month">Monat</option>
        </select>
      </label>
      <div className="management-energy__filter-status">
        Objektfilter vorbereitet
      </div>
    </div>
  );
}

function LoadProfileChart({ data }: { data?: LoadProfile }) {
  const points = data?.points ?? [];
  return (
    <figure className="energy-chart energy-chart--load">
      <div className="energy-chart__y-label">{data?.unit ?? "Leistung"}</div>
      <div className="energy-chart__plot">
        {points.length > 0
          ? <LineSeries values={points.map(point => point.value)} />
          : <ChartEmptyState
              title="Kein Portfolio-Lastprofil verfügbar"
              text="Es liegen keine dimensionskompatiblen elektrischen Leistungswerte für den Filter vor."
            />}
      </div>
      <div className="energy-chart__x-label">
        Zeit · Kalenderjahr {data?.year ?? currentYear}
        {data ? ` · Datenabdeckung ${formatNumber(data.dataCoveragePercent)} %` : ""}
      </div>
    </figure>
  );
}

function EnergyConsumptionChart({
  data,
  year,
}: {
  data?: MonthlyConsumption;
  year: number;
}) {
  const available = data?.months.some(month => month.value !== null) ?? false;
  const max = Math.max(0, ...(data?.months.map(month => month.value ?? 0) ?? []));
  return (
    <figure className="energy-chart energy-chart--consumption">
      <div className="energy-chart__y-label">{data?.unit ?? "Energie"}</div>
      <div className="energy-chart__plot energy-chart__plot--bars">
        {available
          ? data?.months.map(month => (
              <div
                key={month.month}
                className="energy-chart__bar"
                style={{ height: `${max === 0 ? 0 : (month.value ?? 0) / max * 100}%` }}
                title={`${months[month.month - 1]}: ${formatNumber(month.value)} ${data.unit ?? ""}`}
              />
            ))
          : <ChartEmptyState
              title="Kein Monatsverbrauch verfügbar"
              text={`Für ${year} liegen keine kompatiblen Intervall-Energiewerte vor.`}
            />}
      </div>
      <div className="energy-chart__months" aria-hidden="true">
        {months.map(month => <span key={month}>{month}</span>)}
      </div>
    </figure>
  );
}

function LineSeries({ values }: { values: number[] }) {
  const maximum = Math.max(...values);
  const minimum = Math.min(...values);
  const range = maximum - minimum || 1;
  const denominator = Math.max(values.length - 1, 1);
  const coordinates = values.map((value, index) =>
    `${index / denominator * 100},${100 - (value - minimum) / range * 100}`).join(" ");
  return (
    <svg className="energy-chart__line" viewBox="0 0 100 100" preserveAspectRatio="none" aria-label="Aggregiertes Lastprofil">
      <polyline points={coordinates} />
    </svg>
  );
}

function HorizontalBars({
  items,
  emptyText,
}: {
  items?: Array<{ label: string; value: number; unit: string }>;
  emptyText: string;
}) {
  if (!items?.length) return <p className="dashboard-empty">{emptyText}</p>;
  const maximum = Math.max(...items.map(item => item.value));
  return (
    <div className="energy-bars">
      {items.map(item => (
        <div className="energy-bars__row" key={item.label}>
          <span>{item.label}</span>
          <div><i style={{ width: `${maximum === 0 ? 0 : item.value / maximum * 100}%` }} /></div>
          <strong>{formatNumber(item.value)} {item.unit}</strong>
        </div>
      ))}
    </div>
  );
}

function ChartEmptyState({ title, text }: { title: string; text: string }) {
  return <div className="energy-chart__empty"><strong>{title}</strong><span>{text}</span></div>;
}

function fulfilled<T>(result: PromiseSettledResult<T>): T | undefined {
  return result.status === "fulfilled" ? result.value : undefined;
}

function formatNumber(value: number | null | undefined): string {
  return value === null || value === undefined
    ? "—"
    : value.toLocaleString("de-AT", { maximumFractionDigits: 2 });
}
