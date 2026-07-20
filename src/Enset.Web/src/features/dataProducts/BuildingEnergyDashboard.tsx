import type { ProductVersion } from "./types";
const labels: Record<string, string> = { BUILDING_TOTAL_CONSUMPTION: "Gesamtverbrauch", BUILDING_BASE_LOAD: "Grundlast", BUILDING_PEAK_LOAD: "Spitzenlast", NUMBER_OF_METERS: "Anzahl Zähler", DATA_QUALITY: "Datenqualität" };
export function BuildingEnergyDashboard({ version }: { version: ProductVersion }) {
  const format = (value?: number, unit?: string) => value == null ? "–" : `${new Intl.NumberFormat("de-AT", { maximumFractionDigits: 2 }).format(value)} ${unit ?? ""}`;
  return <section><h2>Building Energy Profile</h2><div className="kpi-grid">{Object.entries(labels).map(([key, label]) => { const value = version.values.find(v => v.key === key); return <article className="kpi" key={key}><span>{label}</span><strong>{format(value?.numericValue, value?.unit)}</strong></article>; })}</div><p>Version {version.version} · {new Intl.DateTimeFormat("de-AT", { dateStyle: "medium", timeStyle: "short" }).format(new Date(version.generatedAt))} · {version.generationStatus}</p>{version.warnings.map(w => <p className="warning" key={w}>{w}</p>)}</section>;
}
