import { useCallback, useEffect, useState } from "react";
import { Link, useNavigate, useParams, useSearchParams } from "react-router";
import { AdminPageHeader, PageState, Pagination } from "../components/admin/AdminUi";
import { displayDate, displayNumber, errorMessage } from "../components/admin/adminFormat";
import "../components/admin/admin.css";
import { meterService } from "../services/meterService";
import type { MeterDetail, MeterSummary, MeterWriteModel } from "../features/meters/types";
import type { PagedResult } from "../types/paging";
import { MeterForm } from "../features/meters/MeterForm";
import { ConfirmDialog, EntityMetadataBar, formError } from "../features/crud/crudUi";
import { EntityAuditHistory } from "../features/crud/EntityAuditHistory";
import { meterReadingService } from "../services/meterReadingService";
import type { MeterReading, MeterReadingWriteModel } from "../features/meterReadings/types";
import { MeterReadingForm } from "../features/meterReadings/MeterReadingForm";
import { CurationReadinessPanel } from "../features/curation/CurationReadinessPanel";
import { MeterQualityPanel } from "../features/curation/MeterQualityPanel";
import { DataProductReadinessPanel } from "../features/dataProductReadiness/DataProductReadinessPanel";
import { GoldProfileVersionsPanel } from "../features/goldProfiles/GoldProfileVersionsPanel";
import { internalDataProductService } from "../services/internalDataProductService";
import type { MeterSummaryProduct } from "../features/internalDataProducts/types";

const mediumLabel: Record<string, string> = {
  Electricity: "Strom", Gas: "Gas", Heat: "Wärme", Cooling: "Kälte", Water: "Wasser",
  Steam: "Dampf", Hydrogen: "Wasserstoff", Other: "Sonstiges",
};
const directionLabel: Record<string, string> = {
  Consumption: "Consumer", Production: "Producer", Bidirectional: "Prosumer",
};
const label = (values: Record<string, string>, key: string) => values[key] ?? key;
const annualOrigin = (origin: string | null) =>
  origin === "CalculatedFromReadings" ? "Aus vollständigem Messzeitraum berechnet"
    : origin === "Manual" ? "Manuell erfasst" : "Nicht verfügbar";

export function MetersPage() {
  const { meterId } = useParams();
  return meterId ? <Detail id={meterId} /> : <List />;
}

const blank = (buildingId: string): MeterWriteModel => ({
  meterNumber: "", name: "", buildingId, medium: "", quantity: "Energy", unit: "",
  direction: "", type: "Physical", energySystemId: null, rowVersion: 0,
  description: null, externalIdentifier: null, annualValue: null,
});

function List() {
  const navigate = useNavigate();
  const [params, setParams] = useSearchParams();
  const page = Math.max(1, Number(params.get("page")) || 1);
  const [search, setSearch] = useState(params.get("search") ?? "");
  const [result, setResult] = useState<PagedResult<MeterSummary>>();
  const [error, setError] = useState("");
  const [creating, setCreating] = useState(params.get("create") === "true");
  useEffect(() => {
    const controller = new AbortController();
    meterService.list({
      search: params.get("search") ?? undefined,
      buildingId: params.get("buildingId") ?? undefined,
      page, pageSize: 50,
    }, controller.signal).then(setResult).catch((loadError) => {
      if (!controller.signal.aborted) setError(errorMessage(loadError));
    });
    return () => controller.abort();
  }, [params, page]);
  const update = (values: Record<string, string | undefined>) =>
    setParams((current) => {
      const next = new URLSearchParams(current);
      Object.entries(values).forEach(([key, value]) => {
        if (value) next.set(key, value); else next.delete(key);
      });
      return next;
    });
  return <section className="admin-page">
    <AdminPageHeader title="Zähler" description="Zählerstammdaten, Messprofile und Datenreife" />
    <div className="detail-actions">
      <button className="primary-button" onClick={() => setCreating(true)}>Zähler anlegen</button>
    </div>
    <form className="list-toolbar" onSubmit={(event) => {
      event.preventDefault(); update({ search: search.trim(), page: "1" });
    }}>
      <label>Suche<input value={search} onChange={(event) => setSearch(event.target.value)} /></label>
      <button>Suchen</button>
    </form>
    {error ? <PageState>{error}</PageState> : !result
      ? <PageState>Daten werden geladen …</PageState>
      : result.items.length === 0 ? <PageState>Keine Zähler gefunden.</PageState>
      : <><div className="table-wrap"><table className="admin-table"><thead><tr>
        <th>Zählpunktnummer</th><th>Interne ID</th><th>Gebäude</th><th>Kunde</th>
        <th>Energieträger</th><th>Richtung</th><th>Jahreswert</th><th>Einheit</th>
        <th>Messwerte / Zeitraum</th><th>Datenreife / Gold-Reife</th><th></th>
      </tr></thead><tbody>{result.items.map((item) => <tr key={item.id}>
        <td>{item.meterNumber}</td><td>{item.name}</td>
        <td>{item.buildingId
          ? <Link to={`/buildings/${item.buildingId}`}>{item.buildingNumber} · {item.buildingName}</Link>
          : "Nicht zugeordnet"}</td>
        <td>{item.customerName ? `${item.customerNumber} · ${item.customerName}` : "Nicht zugeordnet"}</td>
        <td>{label(mediumLabel, item.medium)}</td><td>{label(directionLabel, item.direction)}</td>
        <td>{item.annualValue == null ? "–" : `${item.annualValue} (${annualOrigin(item.annualValueOrigin)})`}</td>
        <td>{item.unit}</td><td>{displayNumber(item.readingCount)} · {displayDate(item.firstReadingAt)}
          {" – "}{displayDate(item.lastReadingAt)}</td>
        <td>{item.dataMaturity} · {item.goldReadinessPercent} %</td>
        <td><Link to={`/meters/${item.id}`}>Öffnen</Link></td>
      </tr>)}</tbody></table></div>
      <Pagination page={result.page} totalPages={result.totalPages}
        onPage={(nextPage) => update({ page: String(nextPage) })} /></>}
    {creating && <MeterForm initial={blank(params.get("buildingId") ?? "")}
      onClose={() => setCreating(false)} onSaved={(id) => navigate(`/meters/${id}`)} />}
  </section>;
}

function Detail({ id }: { id: string }) {
  const [item, setItem] = useState<MeterDetail>();
  const [summary, setSummary] = useState<MeterSummaryProduct>();
  const [readings, setReadings] = useState<PagedResult<MeterReading>>();
  const [error, setError] = useState("");
  const [editing, setEditing] = useState(false);
  const [audit, setAudit] = useState(false);
  const [confirm, setConfirm] = useState<"delete" | "restore">();
  const [readingForm, setReadingForm] = useState<MeterReading | null>();
  const [busy, setBusy] = useState(false);
  const [actionError, setActionError] = useState("");
  const [from, setFrom] = useState("");
  const [to, setTo] = useState("");
  const [page, setPage] = useState(1);
  const load = useCallback(async () => {
    try {
      const [meter, values, product] = await Promise.all([
        meterService.get(id), meterReadingService.list(id, from, to, page),
        internalDataProductService.meter(id),
      ]);
      setItem(meter); setReadings(values); setSummary(product);
    } catch (loadError) { setError(errorMessage(loadError)); }
  }, [id, from, to, page]);
  // eslint-disable-next-line react-hooks/set-state-in-effect
  useEffect(() => { void load(); }, [load]);
  if (error) return <PageState>{error}</PageState>;
  if (!item || !readings) return <PageState>Daten werden geladen …</PageState>;
  const model: MeterWriteModel = {
    meterNumber: item.meterNumber, name: item.name, buildingId: item.buildingId ?? "",
    medium: item.medium, quantity: item.quantity, unit: item.unit, direction: item.direction,
    type: item.type, energySystemId: null, rowVersion: item.rowVersion,
    description: item.description, externalIdentifier: item.externalIdentifier,
    annualValue: item.annualValueOrigin === "Manual" ? item.annualValue : null,
  };
  const blankReading: MeterReadingWriteModel = {
    meterId: id, timestamp: new Date().toISOString(), value: 0,
    readingType: "Instantaneous", qualityFlag: "Measured", intervalSeconds: null, rowVersion: 0, reason: null,
  };
  const mutate = async () => {
    if (!confirm) return;
    setBusy(true); setActionError("");
    try {
      if (confirm === "delete") await meterService.remove(id, item.rowVersion);
      else await meterService.restore(id, item.rowVersion);
      setConfirm(undefined); await load();
    } catch (mutationError) { setActionError(formError(mutationError)); }
    finally { setBusy(false); }
  };
  const uploadUrl = `/imports?meterId=${encodeURIComponent(id)}&meterNumber=${encodeURIComponent(item.meterNumber)}`;
  return <section className="admin-page">
    <Link className="back-link" to="/meters">← Zähler</Link>
    <AdminPageHeader title={item.meterNumber}
      description={`${label(mediumLabel, item.medium)} · ${label(directionLabel, item.direction)}`} />
    {item.buildingId && <p><Link to={`/buildings/${item.buildingId}`}>
      {item.buildingNumber} · {item.buildingName}</Link></p>}
    <div className="detail-actions">
      <button className="primary-button" onClick={() => setEditing(true)}>Bearbeiten</button>
      <button onClick={() => setAudit(true)}>Änderungsverlauf</button>
      <button className={item.isDeleted ? "" : "danger-button"}
        onClick={() => setConfirm(item.isDeleted ? "restore" : "delete")}>
        {item.isDeleted ? "Wiederherstellen" : "Deaktivieren"}
      </button>
    </div>
    <EntityMetadataBar entity={item} />
    {summary && <section className="detail-section"><h2>Fachliche Summary</h2>
      <dl className="detail-grid">
        <div><dt>Jahreswert</dt><dd>{summary.annualValue ?? "Nicht verfügbar"}</dd></div>
        <div><dt>Messwerte</dt><dd>{summary.readingCount}</dd></div>
        <div><dt>Vollständigkeit</dt><dd>{summary.completenessPercentage == null
          ? "Nicht bestimmbar" : `${summary.completenessPercentage} %`}</dd></div>
        <div><dt>Gold-Reife</dt><dd>{summary.maturity.goldMaturityPercentage} %</dd></div>
        <div><dt>Offene Kurationsaufgaben</dt><dd>{summary.openCurationTaskCount}</dd></div>
      </dl>
    </section>}
    <section className="detail-section"><h2>Stammdaten</h2><dl className="detail-grid">
      <div><dt>Zählpunktnummer</dt><dd>{item.meterNumber}</dd></div>
      <div><dt>Interne ID</dt><dd>{item.name}</dd></div>
      <div><dt>Objektzuordnung</dt><dd>{item.buildingNumber} · {item.buildingName}</dd></div>
      <div><dt>Kunde</dt><dd>{item.customerName
        ? `${item.customerNumber} · ${item.customerName}` : "Nicht zugeordnet"}</dd></div>
      <div><dt>Energieträger</dt><dd>{label(mediumLabel, item.medium)}</dd></div>
      <div><dt>Einheit</dt><dd>{item.unit}</dd></div>
      <div><dt>Richtung</dt><dd>{label(directionLabel, item.direction)}</dd></div>
      <div><dt>Jahreswert</dt><dd>{item.annualValue ?? "Nicht verfügbar"} · {annualOrigin(item.annualValueOrigin)}</dd></div>
      <div><dt>Messzeitraum</dt><dd>{displayDate(item.firstReadingAt)} – {displayDate(item.lastReadingAt)}</dd></div>
      <div><dt>Intervall</dt><dd>{item.intervalSeconds == null
        ? "Nicht erkannt" : `${item.intervalSeconds / 60} Minuten`}</dd></div>
      <div><dt>Anzahl Messwerte</dt><dd>{displayNumber(item.readingCount)}</dd></div>
      <div><dt>Beschreibung</dt><dd>{item.description ?? "Nicht angegeben"}</dd></div>
      <div><dt>Externe Referenz</dt><dd>{item.externalIdentifier ?? "Nicht angegeben"}</dd></div>
    </dl></section>
    <section className="detail-section"><div className="section-heading"><h2>Messprofil hochladen</h2>
      <Link className="primary-button" to={uploadUrl}>Upload starten</Link>
    </div>
      <p>CSV sowie die vorhandenen stabilen Excel-Formate werden durch die bestehende Importpipeline analysiert.</p>
      <ul><li>Mit Zeitspalte werden die Zeitstempel aus der Datei verwendet.</li>
        <li>Ohne Zeitspalte fordert der Analyseprozess Startzeit und Intervall an.</li>
        <li>Spalten, Trennzeichen, Dezimalformat, Dubletten und ungültige Werte werden vor der Übernahme geprüft.</li>
      </ul>
    </section>
    <CurationReadinessPanel entityType="MeteringPoint" id={id} />
    <MeterQualityPanel meterId={id} />
    <GoldProfileVersionsPanel entityType="MeteringPoint" entityId={id} />
    <DataProductReadinessPanel scopeType="MeteringPoint" scopeId={id} />
    <section className="detail-section"><div className="section-heading"><h2>Messwerte</h2>
      <button className="primary-button" onClick={() => setReadingForm(null)}>Messwert hinzufügen</button>
    </div><form className="list-toolbar" onSubmit={(event) => {
      event.preventDefault(); setPage(1); void load();
    }}><label>Von<input type="datetime-local" value={from} onChange={(event) => setFrom(event.target.value)} /></label>
      <label>Bis<input type="datetime-local" value={to} onChange={(event) => setTo(event.target.value)} /></label>
      <button>Filtern</button></form>
      {readings.items.length === 0 ? <PageState>Keine Messwerte vorhanden.</PageState>
        : <><div className="table-wrap"><table className="admin-table"><thead><tr>
          <th>Zeitpunkt</th><th>Wert</th><th>Qualität</th><th>Herkunft</th><th></th>
        </tr></thead><tbody>{readings.items.map((reading) => <tr key={reading.id}>
          <td>{displayDate(reading.timestamp)}</td><td>{reading.value} {item.unit}</td>
          <td>{reading.qualityFlag}</td><td>{reading.dataOrigin}</td>
          <td><button onClick={() => setReadingForm(reading)}>Bearbeiten</button></td>
        </tr>)}</tbody></table></div>
        <Pagination page={readings.page} totalPages={readings.totalPages} onPage={setPage} /></>}
    </section>
    {editing && <MeterForm initial={model} entityId={id} onClose={() => setEditing(false)}
      onReload={async () => { setEditing(false); await load(); }}
      onSaved={async () => { setEditing(false); await load(); }} />}
    {audit && <EntityAuditHistory entityType="MeteringPoint" entityId={id}
      onClose={() => setAudit(false)} />}
    {confirm && <ConfirmDialog title={confirm === "delete"
      ? "Zähler deaktivieren" : "Zähler wiederherstellen"}
      confirmLabel={confirm === "delete" ? "Deaktivieren" : "Wiederherstellen"}
      busy={busy} error={actionError} onConfirm={() => void mutate()} onClose={() => setConfirm(undefined)}>
      <p>Der Zähler „{item.meterNumber}“ wird {confirm === "delete"
        ? "deaktiviert. Vorhandene Messwerte können die Aktion blockieren." : "wiederhergestellt."}</p>
    </ConfirmDialog>}
    {readingForm !== undefined && <MeterReadingForm unit={item.unit} entityId={readingForm?.id}
      initial={readingForm ? {
        meterId: id, timestamp: readingForm.timestamp, value: readingForm.value,
        readingType: readingForm.readingType, qualityFlag: readingForm.qualityFlag,
        intervalSeconds: readingForm.intervalSeconds, rowVersion: readingForm.rowVersion, reason: null,
      } : blankReading} onClose={() => setReadingForm(undefined)}
      onSaved={async () => { setReadingForm(undefined); await load(); }} />}
  </section>;
}
