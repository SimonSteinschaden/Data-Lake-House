import type { ReactNode } from "react";
import { Link } from "react-router";
import type {
  EnergySummaryItem,
  ImportQualityProduct,
  PortfolioReadinessSummary,
} from "./types";
import { carrier, direction, number } from "./dashboardFormat";

export function DashboardSection({ id, title, description, children }: {
  id: string; title: string; description: string; children: ReactNode;
}) {
  return <section className="cockpit-section" aria-labelledby={id}>
    <header className="cockpit-section__header">
      <div><p className="cockpit-section__eyebrow">Dashboard</p>
        <h2 id={id}>{title}</h2><p>{description}</p></div>
    </header>
    {children}
  </section>;
}

export function DashboardKpiCard({ title, value, hint, to, tone = "default" }: {
  title: string; value: ReactNode; hint?: string; to?: string;
  tone?: "default" | "attention" | "positive";
}) {
  const content = <><span className="cockpit-kpi__title">{title}</span>
    <strong className="cockpit-kpi__value">{value}</strong>
    {hint && <span className="cockpit-kpi__hint">{hint}</span>}</>;
  return to
    ? <Link className={`cockpit-kpi cockpit-kpi--${tone}`} to={to}>{content}</Link>
    : <article className={`cockpit-kpi cockpit-kpi--${tone}`}>{content}</article>;
}

export function EnergySummaryChart({ items }: { items: EnergySummaryItem[] }) {
  const units = [...new Set(items.map((item) => item.unit))];
  if (items.length === 0) return <EmptyDashboardState
    message="Für das Portfolio sind noch keine belastbaren Jahreswerte vorhanden." />;
  return <div className="energy-charts">
    {units.map((unit) => {
      const rows = items.filter((item) => item.unit === unit);
      const max = Math.max(...rows.map((item) => item.annualValue), 1);
      return <section className="energy-chart" key={unit}>
        <h3>Jahreswerte in {unit}</h3>
        <div className="energy-chart__rows">
          {rows.map((item) => <div className="energy-chart__row"
            key={`${item.energyCarrier}-${item.direction}-${item.unit}`}>
            <span>{carrier(item.energyCarrier)} · {direction(item.direction)}</span>
            <div className="energy-chart__track" aria-hidden="true">
              <span style={{ width: `${Math.max(2, item.annualValue / max * 100)}%` }} />
            </div>
            <strong>{number(item.annualValue)} {item.unit}</strong>
          </div>)}
        </div>
      </section>;
    })}
  </div>;
}

export function EnergySummaryTable({ items }: { items: EnergySummaryItem[] }) {
  return <div className="cockpit-table-wrap"><table className="cockpit-table">
    <thead><tr><th>Energieträger</th><th>Richtung</th><th>Jahreswert</th>
      <th>Einheit</th><th>Zählpunkte</th></tr></thead>
    <tbody>{items.map((item) => <tr
      key={`${item.energyCarrier}-${item.direction}-${item.unit}`}>
      <td>{carrier(item.energyCarrier)}</td><td>{direction(item.direction)}</td>
      <td>{number(item.annualValue)}</td><td>{item.unit}</td>
      <td>{number(item.meterCount)}</td>
    </tr>)}</tbody>
  </table></div>;
}

export interface QualityAction {
  problem: string; count: number; impact: string; action: string; to: string;
}
export function DataQualityActionList({ items }: { items: QualityAction[] }) {
  return <div className="cockpit-table-wrap"><table className="cockpit-table action-table">
    <thead><tr><th>Problem</th><th>Anzahl</th><th>Auswirkung</th><th>Aktion</th></tr></thead>
    <tbody>{items.map((item) => <tr key={item.problem}>
      <td>{item.problem}</td><td><strong>{number(item.count)}</strong></td>
      <td>{item.impact}</td><td><Link to={item.to}>{item.action}</Link></td>
    </tr>)}</tbody>
  </table></div>;
}

export function ReadinessCard({ item }: { item: PortfolioReadinessSummary }) {
  const percentage = item.percentage;
  return <article className="readiness-card">
    <header><div><h3>{product(item.product)}</h3>
      <span className={`readiness-badge readiness-badge--${item.status.toLowerCase()}`}>
        {percentage == null ? "Noch nicht bewertet" : readinessStatus(item.status)}</span></div>
      <strong>{percentage == null ? "Nicht bewertet" : `${percentage} %`}</strong></header>
    <div className="readiness-progress" role="progressbar"
      aria-valuemin={0} aria-valuemax={100} aria-valuenow={percentage ?? 0}>
      <span style={{ width: `${percentage ?? 0}%` }} /></div>
    <p className="readiness-card__note">{percentage == null
      ? "Die Voraussetzungen wurden noch nicht bewertet."
      : readinessMessage(item.status)}</p>
    <dl><div><dt>Vorbereitet</dt><dd>{number(item.readyScopeCount)}</dd></div>
      <div><dt>Blockiert</dt><dd>{number(item.blockedScopeCount)}</dd></div></dl>
    <BlockerList blockers={item.topBlockers} />
    {item.recommendations.length > 0 && <div className="readiness-card__recommendations">
      <strong>Empfehlung</strong><ul>{item.recommendations.slice(0, 2)
        .map((text) => <li key={text}>{text}</li>)}</ul></div>}
    <Link className="cockpit-action" to={item.detailPath}>Betroffene Bereiche anzeigen</Link>
  </article>;
}

export function BlockerList({ blockers }: { blockers: string[] }) {
  if (blockers.length === 0) return <p className="readiness-card__clear">
    Keine zentralen Blocker in der aktuellen Bewertung.</p>;
  return <div className="blocker-list"><strong>Wichtigste Blocker</strong>
    <ul>{blockers.slice(0, 3).map((blocker) => <li key={blocker}>{blocker}</li>)}</ul>
  </div>;
}

export function ImportQualityOverview({ data }: { data: ImportQualityProduct }) {
  return <div className="cockpit-table-wrap"><table className="cockpit-table">
    <thead><tr><th>Datei</th><th>Zeitpunkt</th><th>Status</th><th>Probleme</th>
      <th>Warnungen</th><th>Commit</th></tr></thead>
    <tbody>{data.latestImports.length === 0
      ? <tr><td colSpan={6}>Noch keine Importe vorhanden.</td></tr>
      : data.latestImports.map((item) => <tr key={item.importId}>
        <td>{item.fileName ?? "Nicht verfügbar"}</td><td>{date(item.createdAt)}</td>
        <td>{importStatus(item.status)}</td><td>{number(item.issueCount)}</td>
        <td>{number(item.warningCount)}</td>
        <td>{item.committedAt ? date(item.committedAt) : "Nicht verfügbar"}</td>
      </tr>)}</tbody>
  </table></div>;
}

export function LoadingDashboardSkeleton() {
  return <div className="dashboard-skeleton" aria-label="Dashboard wird geladen">
    {Array.from({ length: 8 }, (_, index) =>
      <span key={index} className="dashboard-skeleton__card" />)}
  </div>;
}
export function EmptyDashboardState({ message }: { message: string }) {
  return <div className="dashboard-state"><strong>Noch keine Daten</strong><p>{message}</p></div>;
}
export function ErrorDashboardState({ retry }: { retry: () => void }) {
  return <div className="dashboard-state dashboard-state--error"><strong>
    Dashboard-Daten konnten nicht geladen werden.</strong>
    <p>Bitte versuchen Sie es erneut. Technische Details werden nicht angezeigt.</p>
    <button type="button" onClick={retry}>Erneut laden</button></div>;
}

const product = (value: string) => ({
  BuildingBenchmark: "Building Benchmark", EnergyBenchmark: "Energy Benchmark",
  NormalizedLoadProfile: "Normalisiertes Lastprofil",
  NormalizedGenerationProfile: "Normalisiertes Erzeugungsprofil",
}[value] ?? value);
const readinessStatus = (value: string) => ({
  Ready: "Voraussetzungen erfüllt", ReadyWithWarnings: "Teilweise bereit",
  PartiallyReady: "Teilweise erfüllt", NotReady: "Blockiert",
}[value] ?? "Noch nicht bewertet");
const readinessMessage = (value: string) => value === "Ready"
  ? "Für die Auswertung vorbereitet; die Berechnung selbst ist nicht Bestandteil des Cockpits."
  : value === "NotReady" ? "Blockiert durch fehlende fachliche Voraussetzungen."
    : "Voraussetzungen sind teilweise erfüllt; noch keine Ergebnisberechnung.";
const date = (value: string) => new Date(value).toLocaleString("de-AT");
const importStatus = (value: string) => ({
  Successful: "Erfolgreich", Succeeded: "Erfolgreich", Completed: "Abgeschlossen",
  Committed: "Übernommen", Failed: "Fehlgeschlagen", Rejected: "Abgelehnt",
  Pending: "Ausstehend",
}[value] ?? value);
