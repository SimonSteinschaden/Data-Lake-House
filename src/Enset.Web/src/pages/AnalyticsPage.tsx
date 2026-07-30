import { useEffect, useMemo, useState } from "react";
import { Card } from "../components/ui/Card";
import { StatCard } from "../components/ui/StatCard";
import type {
  AnalyticsValue,
  ObjectAnalyticsProduct,
  ObjectSearchItem,
  SeriesPoint,
} from "../features/objectAnalytics/types";
import { PageHeader } from "../layouts/PageHeader";
import {
  objectAnalyticsService,
  type ObjectAnalyticsFilters,
} from "../services/objectAnalyticsService";
import "./AnalyticsPage.css";

const year = new Date().getFullYear();

export function AnalyticsPage() {
  const [search, setSearch] = useState("");
  const [carrier, setCarrier] = useState("");
  const [quality, setQuality] = useState("");
  const [buildingType, setBuildingType] = useState("");
  const [customerId, setCustomerId] = useState("");
  const [from, setFrom] = useState(`${year}-01-01`);
  const [to, setTo] = useState(`${year + 1}-01-01`);
  const [objects, setObjects] = useState<ObjectSearchItem[]>([]);
  const [buildingId, setBuildingId] = useState("");
  const [product, setProduct] = useState<ObjectAnalyticsProduct | null>(null);
  const [error, setError] = useState("");

  const filters = useMemo<ObjectAnalyticsFilters>(() => ({
    search: search || undefined,
    energyCarrier: carrier || undefined,
    qualityLevel: quality || undefined,
    buildingType: buildingType || undefined,
    customerId: customerId || undefined,
    fromUtc: `${from}T00:00:00Z`,
    toUtc: `${to}T00:00:00Z`,
    pageSize: 100,
  }), [search, carrier, quality, buildingType, customerId, from, to]);

  const buildingTypes = [...new Set(objects
    .map((x) => x.buildingType).filter((x): x is string => Boolean(x)))];
  const customers = [...new Map(objects.filter((x) => x.customerId)
    .map((x) => [x.customerId!, x.customer ?? x.customerId!])).entries()];

  useEffect(() => {
    const controller = new AbortController();
    objectAnalyticsService.search(filters, controller.signal)
      .then((result) => {
        setObjects(result.items);
        if (buildingId && !result.items.some((x) => x.buildingId === buildingId)) {
          setBuildingId("");
          setProduct(null);
        }
        setError("");
      })
      .catch(() => {
        if (!controller.signal.aborted) setError("Objekte konnten nicht geladen werden.");
      });
    return () => controller.abort();
  }, [filters, buildingId]);

  useEffect(() => {
    if (!buildingId) return;
    const controller = new AbortController();
    objectAnalyticsService.analyze(buildingId, filters, controller.signal)
      .then(setProduct)
      .catch(() => {
        if (!controller.signal.aborted) setError("Analyse konnte nicht geladen werden.");
      });
    return () => controller.abort();
  }, [buildingId, filters]);

  return (
    <section className="object-analysis">
      <PageHeader
        title="Objektanalyse"
        description="Interaktive, reproduzierbare Analyse auf Basis kanonischer Snapshots."
      />
      <Card title="Suche und Filter" className="object-analysis__filter-card">
        <div className="object-analysis__filters">
          <label>Globale Suche
            <input value={search} onChange={(e) => setSearch(e.target.value)}
              placeholder="Objekt, Kunde, Gemeinde, Adresse, Zählpunkt" />
          </label>
          <label>Objekt
            <select value={buildingId} onChange={(e) => {
              setBuildingId(e.target.value);
              if (!e.target.value) setProduct(null);
            }}>
              <option value="">Objekt auswählen</option>
              {objects.map((item) => (
                <option key={item.buildingId} value={item.buildingId}>
                  {item.name} · {item.buildingNumber}
                </option>
              ))}
            </select>
          </label>
          <label>Energieträger
            <select value={carrier} onChange={(e) => setCarrier(e.target.value)}>
              <option value="">Alle</option>
              <option value="Electricity">Strom</option>
              <option value="Heat">Wärme</option>
              <option value="Gas">Gas</option>
            </select>
          </label>
          <label>Datenqualität
            <select value={quality} onChange={(e) => setQuality(e.target.value)}>
              <option value="">Alle</option>
              <option value="Gold">Gold</option>
              <option value="Silver">Silver</option>
              <option value="Bronze">Bronze</option>
            </select>
          </label>
          <label>Objekttyp
            <select value={buildingType} onChange={(e) => setBuildingType(e.target.value)}>
              <option value="">Alle</option>
              {buildingTypes.map((value) => <option key={value}>{value}</option>)}
            </select>
          </label>
          <label>Kunde
            <select value={customerId} onChange={(e) => setCustomerId(e.target.value)}>
              <option value="">Alle</option>
              {customers.map(([id, name]) =>
                <option key={id} value={id}>{name}</option>)}
            </select>
          </label>
          <label>Von<input type="date" value={from} onChange={(e) => setFrom(e.target.value)} /></label>
          <label>Bis<input type="date" value={to} onChange={(e) => setTo(e.target.value)} /></label>
        </div>
        <small>{objects.length} passende Objekte · Filter werden serverseitig auf Snapshots angewendet.</small>
        {error && <p className="object-analysis__filter-error">{error}</p>}
      </Card>
      {!product ? <Empty /> : <Workspace product={product} />}
    </section>
  );
}

function Workspace({ product }: { product: ObjectAnalyticsProduct }) {
  const values = [
    ["Gesamtverbrauch", product.totalConsumption],
    ["Strom", product.electricityConsumption],
    ["Wärme", product.heatConsumption],
    ["Erzeugung", product.energyProduction],
    ["Energiekosten", product.energyCosts],
    ["CO₂-Emissionen", product.co2Emissions],
    ["Spitzenlast", product.peakLoad],
    ["Verbrauch je Fläche", product.consumptionPerSquareMeter],
  ] as const;
  return (
    <div className="object-analysis__workspace">
      <div className="object-analysis__stats">
        {values.map(([title, value]) => (
          <StatCard key={title} title={title} value={format(value)}
            subtitle={`${value.source} · Quality ${product.quality.level}`} />
        ))}
      </div>
      <div className="object-analysis__panels">
        <Card title="Monatsverlauf"><Bars points={product.monthlyConsumption} /></Card>
        <Card title="Tageslastprofil"><Bars points={product.dailyLoadProfile} /></Card>
        <Card title="Verbrauch nach Energieträger">
          {product.consumptionByCarrier.length === 0 ? <NotAvailable /> :
            <table><tbody>{product.consumptionByCarrier.map((x) =>
              <tr key={x.name}><th>{x.name}</th><td>{x.value.toLocaleString("de-AT")} {x.unit}</td></tr>)}</tbody></table>}
        </Card>
        <Card title="Vorjahresvergleich und Benchmark">
          <p>Vorjahr: {product.consumptionPreviousYear.status === "Available"
            ? `${product.consumptionPreviousYear.percentChange?.toLocaleString("de-AT")} %`
            : "nicht verfügbar"}</p>
          <p>Benchmark: {product.benchmark.status === "Available"
            ? `${product.benchmark.comparisonValue?.toLocaleString("de-AT")} ${product.benchmark.unit}`
            : "Benchmark derzeit nicht verfügbar"}</p>
          <small>{product.benchmark.rule}</small>
        </Card>
        <Card title="Anlagen">
          <p>Installierte Gesamtleistung: {product.totalInstalledPower ?? "nicht verfügbar"}</p>
          {product.energySystems.length === 0 ? <NotAvailable /> :
            <ul>{product.energySystems.map((x) =>
              <li key={x.energySystemId}>{x.type ?? "Unbekannt"} · {x.installedPower ?? "–"} kW · {x.status}</li>)}</ul>}
        </Card>
        <Card title="Datenvollständigkeit">
          <div className="traffic-lights">{product.completeness.map((x) =>
            <span key={x.name} className={`traffic-light traffic-light--${x.status.toLowerCase()}`}
              title={x.reason}>{x.name}</span>)}</div>
        </Card>
        <Card title="Auffälligkeiten und Warnungen">
          {[...product.anomalies, ...product.warnings].length === 0
            ? <p>Keine regelbasierten Auffälligkeiten erkannt.</p>
            : <ul>{[...product.anomalies, ...product.warnings].map((x) =>
              <li key={`${x.code}-${x.message}`}><strong>{x.severity}</strong> {x.message}<small>{x.rule}</small></li>)}</ul>}
        </Card>
      </div>
    </div>
  );
}

function Bars({ points }: { points: SeriesPoint[] }) {
  if (points.length === 0) return <NotAvailable />;
  const max = Math.max(...points.map((x) => x.value), 1);
  return <div className="analysis-bars">{points.map((point) =>
    <div key={point.timestamp} className="analysis-bar"
      style={{ height: `${Math.max(3, point.value / max * 100)}%` }}
      title={`${new Date(point.timestamp).toLocaleString("de-AT")}: ${point.value.toLocaleString("de-AT")}`} />)}</div>;
}

function format(value: AnalyticsValue) {
  return value.value === null ? "nicht verfügbar" :
    `${value.value.toLocaleString("de-AT", { maximumFractionDigits: 2 })} ${value.unit}`;
}

function Empty() {
  return <div className="object-analysis__empty"><strong>Kein Objekt ausgewählt</strong>
    <p>Suche und Filter verwenden, anschließend ein Objekt auswählen.</p></div>;
}

function NotAvailable() {
  return <p className="object-analysis__panel-empty">Keine Daten verfügbar.</p>;
}
