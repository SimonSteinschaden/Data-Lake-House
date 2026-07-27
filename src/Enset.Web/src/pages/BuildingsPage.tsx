import { useCallback, useEffect, useState } from "react";
import { Link, useNavigate, useParams, useSearchParams } from "react-router";
import { AdminPageHeader, PageState, Pagination } from "../components/admin/AdminUi";
import { displayNumber, errorMessage } from "../components/admin/adminFormat";
import "../components/admin/admin.css";
import { buildingService } from "../services/buildingService";
import type { BuildingDetail, BuildingSummary, BuildingWriteModel } from "../features/buildings/types";
import type { PagedResult } from "../types/paging";
import { BuildingForm } from "../features/buildings/BuildingForm";
import { ConfirmDialog, EntityMetadataBar, formError } from "../features/crud/crudUi";
import { EntityAuditHistory } from "../features/crud/EntityAuditHistory";
import { energySystemService } from "../services/energySystemService";
import type { EnergySystem, EnergySystemWriteModel } from "../features/energySystems/types";
import { EnergySystemForm } from "../features/energySystems/EnergySystemForm";
import { CurationReadinessPanel } from "../features/curation/CurationReadinessPanel";
import { DataProductReadinessPanel } from "../features/dataProductReadiness/DataProductReadinessPanel";
import { GoldProfileVersionsPanel } from "../features/goldProfiles/GoldProfileVersionsPanel";

const categoryLabel: Record<string, string> = {
  Apartment: "Mehrfamilienhaus", House: "Haus", Office: "Büro", Hall: "Halle",
  School: "Schule", Retail: "Handel", Industry: "Industrie", Other: "Sonstiges",
};
const useLabel: Record<string, string> = {
  Residential: "Wohnen", Commercial: "Gewerbe", Public: "Öffentlich", Mixed: "Mischnutzung",
};
const stateLabel: Record<string, string> = {
  Existing: "Bestand", Improved: "Verbessert", Planned: "Saniert (geplant)",
  Target: "Zielzustand", Unknown: "Nicht angegeben",
};
const value = (map: Record<string, string>, key?: string | null) =>
  key ? (map[key] ?? key) : "Nicht angegeben";

export function BuildingsPage() {
  const { buildingId } = useParams();
  return buildingId ? <Detail id={buildingId} /> : <List />;
}

const blank = (customerId: string | null): BuildingWriteModel => ({
  buildingNumber: "", name: "", externalIdentifier: null, customerId,
  grossFloorAreaM2: null, heatedFloorAreaM2: null, yearOfConstruction: null,
  yearOfLastMajorRenovation: null, buildingCategory: null, primaryUseType: null,
  benchmarkState: null, postalCode: null, city: null, street: null, houseNumber: null,
  latitude: null, longitude: null, rowVersion: 0,
});

function List() {
  const navigate = useNavigate();
  const [params, setParams] = useSearchParams();
  const page = Math.max(1, Number(params.get("page")) || 1);
  const includeDeleted = params.get("includeDeleted") === "true";
  const [search, setSearch] = useState(params.get("search") ?? "");
  const [result, setResult] = useState<PagedResult<BuildingSummary>>();
  const [error, setError] = useState("");
  const [creating, setCreating] = useState(params.get("create") === "true");
  useEffect(() => {
    const controller = new AbortController();
    buildingService.list({
      search: params.get("search") ?? undefined,
      customerId: params.get("customerId") ?? undefined,
      includeDeleted, page, pageSize: 50,
    }, controller.signal).then(setResult).catch((e) => {
      if (!controller.signal.aborted) setError(errorMessage(e));
    });
    return () => controller.abort();
  }, [params, page, includeDeleted]);
  const update = (values: Record<string, string | undefined>) =>
    setParams((current) => {
      const next = new URLSearchParams(current);
      Object.entries(values).forEach(([key, entry]) => {
        if (entry) next.set(key, entry); else next.delete(key);
      });
      return next;
    });
  return <section className="admin-page">
    <AdminPageHeader title="Objekte" description="Gebäudestammdaten, Zuordnungen und Datenreife" />
    <div className="detail-actions">
      <button className="primary-button" onClick={() => setCreating(true)}>Objekt anlegen</button>
    </div>
    <form className="list-toolbar" onSubmit={(event) => {
      event.preventDefault(); update({ search: search.trim(), page: "1" });
    }}>
      <label>Suche<input value={search} onChange={(event) => setSearch(event.target.value)} /></label>
      <label><input type="checkbox" checked={includeDeleted} onChange={(event) =>
        update({ includeDeleted: event.target.checked ? "true" : undefined, page: "1" })} />
        Deaktivierte anzeigen
      </label>
      <button>Suchen</button>
    </form>
    {error ? <PageState>{error}</PageState> : !result
      ? <PageState>Daten werden geladen …</PageState>
      : result.items.length === 0 ? <PageState>Keine Objekte gefunden.</PageState>
      : <><div className="table-wrap"><table className="admin-table">
        <thead><tr><th>Gebäudenummer</th><th>Objektname</th><th>Gebäudetyp</th>
          <th>Nutzungstyp</th><th>Kunde</th><th>Zählpunkte</th><th>Gebäudezustand</th>
          <th>Datenreife / Gold-Reife</th><th></th></tr></thead>
        <tbody>{result.items.map((item) => <tr key={item.id}>
          <td>{item.buildingNumber}</td><td>{item.name}</td>
          <td>{value(categoryLabel, item.buildingCategory)}</td>
          <td>{value(useLabel, item.primaryUseType)}</td>
          <td>{item.customerName
            ? <>{item.customerNumber} · {item.customerName}</> : "Nicht zugeordnet"}</td>
          <td>{displayNumber(item.meterCount)}</td>
          <td>{value(stateLabel, item.benchmarkState)}</td>
          <td>{item.dataMaturity} · {item.goldReadinessPercent} %</td>
          <td><Link className="table-link" to={`/buildings/${item.id}`}>Öffnen</Link></td>
        </tr>)}</tbody>
      </table></div>
      <Pagination page={result.page} totalPages={result.totalPages}
        onPage={(nextPage) => update({ page: String(nextPage) })} /></>}
    {creating && <BuildingForm initial={blank(params.get("customerId"))}
      onClose={() => setCreating(false)} onSaved={(id) => navigate(`/buildings/${id}`)} />}
  </section>;
}

function Detail({ id }: { id: string }) {
  const [item, setItem] = useState<BuildingDetail>();
  const [systems, setSystems] = useState<EnergySystem[]>([]);
  const [error, setError] = useState("");
  const [editing, setEditing] = useState(false);
  const [audit, setAudit] = useState(false);
  const [confirm, setConfirm] = useState<"delete" | "restore">();
  const [systemForm, setSystemForm] = useState<EnergySystem>();
  const [newSystem, setNewSystem] = useState(false);
  const [busy, setBusy] = useState(false);
  const [actionError, setActionError] = useState("");
  const load = useCallback(async () => {
    try {
      const [building, allSystems] = await Promise.all([
        buildingService.get(id), energySystemService.list(),
      ]);
      setItem(building);
      setSystems(allSystems.items.filter((x) => x.buildingId === id));
    } catch (loadError) { setError(errorMessage(loadError)); }
  }, [id]);
  // The callback also serves explicit reload actions after mutations.
  // eslint-disable-next-line react-hooks/set-state-in-effect
  useEffect(() => { void load(); }, [load]);
  if (error) return <section className="admin-page"><PageState>{error}</PageState></section>;
  if (!item) return <section className="admin-page"><PageState>Daten werden geladen …</PageState></section>;

  const primaryCustomer = item.customers.find((x) => x.isPrimary) ?? item.customers[0];
  const model: BuildingWriteModel = {
    buildingNumber: item.buildingNumber, name: item.name,
    externalIdentifier: item.externalIdentifier, customerId: primaryCustomer?.customerId ?? null,
    grossFloorAreaM2: item.grossFloorAreaM2, heatedFloorAreaM2: item.heatedFloorAreaM2,
    yearOfConstruction: item.yearOfConstruction,
    yearOfLastMajorRenovation: item.yearOfLastMajorRenovation,
    buildingCategory: item.buildingCategory, primaryUseType: item.primaryUseType,
    benchmarkState: item.benchmarkState === "Unknown" ? null : item.benchmarkState,
    postalCode: item.postalCode, city: item.city, street: item.street,
    houseNumber: item.houseNumber, latitude: item.latitude, longitude: item.longitude,
    rowVersion: item.rowVersion,
  };
  const blankSystem: EnergySystemWriteModel = {
    energySystemNumber: "", name: "", type: "", buildingId: id, ratedPowerKw: null,
    commissionedAt: null, decommissionedAt: null, rowVersion: 0,
  };
  const mutate = async () => {
    if (!confirm) return;
    setBusy(true); setActionError("");
    try {
      if (confirm === "delete") await buildingService.remove(id, item.rowVersion);
      else await buildingService.restore(id, item.rowVersion);
      setConfirm(undefined); await load();
    } catch (mutationError) { setActionError(formError(mutationError)); }
    finally { setBusy(false); }
  };
  const address = [item.street, item.houseNumber].filter(Boolean).join(" ");
  return <section className="admin-page">
    <Link className="back-link" to="/buildings">← Objekte</Link>
    <AdminPageHeader title={item.name} description={`Gebäudenummer ${item.buildingNumber}`} />
    {primaryCustomer && <p><Link to={`/customers/${primaryCustomer.customerId}`}>
      {primaryCustomer.customerNumber} · {primaryCustomer.customerName}</Link></p>}
    <div className="detail-actions">
      <button className="primary-button" onClick={() => setEditing(true)}>Bearbeiten</button>
      <button onClick={() => setAudit(true)}>Änderungsverlauf</button>
      <button className={item.isDeleted ? "" : "danger-button"}
        onClick={() => setConfirm(item.isDeleted ? "restore" : "delete")}>
        {item.isDeleted ? "Wiederherstellen" : "Deaktivieren"}
      </button>
    </div>
    <EntityMetadataBar entity={item} />
    <section className="detail-section"><h2>Stammdaten</h2><dl className="detail-grid">
      <div><dt>Gebäudetyp</dt><dd>{value(categoryLabel, item.buildingCategory)}</dd></div>
      <div><dt>Nutzungstyp</dt><dd>{value(useLabel, item.primaryUseType)}</dd></div>
      <div><dt>Gebäudezustand</dt><dd>{value(stateLabel, item.benchmarkState)}</dd></div>
      <div><dt>Bruttogrundfläche</dt><dd>{item.grossFloorAreaM2 ?? "Nicht angegeben"} m²</dd></div>
      <div><dt>Beheizte Fläche</dt><dd>{item.heatedFloorAreaM2 ?? "Nicht angegeben"} m²</dd></div>
      <div><dt>Baujahr</dt><dd>{item.yearOfConstruction ?? "Nicht angegeben"}</dd></div>
      <div><dt>Renovierungsjahr</dt><dd>{item.yearOfLastMajorRenovation ?? "Nicht angegeben"}</dd></div>
      <div><dt>PLZ</dt><dd>{item.postalCode ?? "Nicht angegeben"}</dd></div>
      <div><dt>Ort</dt><dd>{item.city ?? "Nicht angegeben"}</dd></div>
      <div><dt>Adresse</dt><dd>{address || "Nicht angegeben"}</dd></div>
      <div><dt>Externe ID</dt><dd>{item.externalIdentifier ?? "Nicht angegeben"}</dd></div>
    </dl></section>
    <CurationReadinessPanel entityType="Building" id={id} />
    <GoldProfileVersionsPanel entityType="Building" entityId={id} />
    <DataProductReadinessPanel scopeType="Building" scopeId={id} />
    <section className="detail-section"><div className="section-heading"><h2>Zählpunkte</h2>
      <Link className="primary-button" to={`/meters?buildingId=${id}&create=true`}>Zählpunkt anlegen</Link>
    </div>{item.meters.length === 0 ? <PageState>Keine Zählpunkte zugeordnet.</PageState>
      : <div className="table-wrap"><table className="admin-table"><thead><tr>
        <th>Zählpunktnummer</th><th>Energieträger</th><th>Richtung</th><th>Einheit</th>
        <th>Datenreife</th><th></th></tr></thead><tbody>{item.meters.map((meter) => <tr key={meter.id}>
          <td>{meter.meterNumber}</td><td>{meter.medium}</td><td>{meter.direction}</td>
          <td>{meter.unit}</td><td>{meter.dataMaturity}</td>
          <td><Link to={`/meters/${meter.id}`}>Öffnen</Link></td>
        </tr>)}</tbody></table></div>}</section>
    <section className="detail-section"><div className="section-heading"><h2>Anlagen</h2>
      <button className="primary-button" onClick={() => setNewSystem(true)}>Anlage anlegen</button>
    </div>{systems.length === 0 ? <PageState>Keine Anlagen zugeordnet.</PageState>
      : <div className="table-wrap"><table className="admin-table"><thead><tr>
        <th>Bezeichnung</th><th>Anlagentyp</th><th>Leistung</th><th>Betriebszustand</th><th></th>
      </tr></thead><tbody>{systems.map((system) => <tr key={system.id}>
        <td>{system.name}</td><td>{system.type}</td>
        <td>{system.ratedPowerKw == null ? "Nicht angegeben" : `${system.ratedPowerKw} kW`}</td>
        <td>{system.decommissionedAt ? "Außer Betrieb" : system.commissionedAt ? "In Betrieb" : "Nicht angegeben"}</td>
        <td><button onClick={() => setSystemForm(system)}>Öffnen</button></td>
      </tr>)}</tbody></table></div>}</section>
    <p><Link className="table-link" to={`/buildings/${id}/energy`}>Objektanalyse öffnen</Link></p>
    {editing && <BuildingForm initial={model} entityId={id} onClose={() => setEditing(false)}
      onReload={async () => { setEditing(false); await load(); }}
      onSaved={async () => { setEditing(false); await load(); }} />}
    {audit && <EntityAuditHistory entityType="Building" entityId={id} onClose={() => setAudit(false)} />}
    {confirm && <ConfirmDialog title={confirm === "delete" ? "Objekt deaktivieren" : "Objekt wiederherstellen"}
      confirmLabel={confirm === "delete" ? "Deaktivieren" : "Wiederherstellen"}
      busy={busy} error={actionError} onConfirm={() => void mutate()} onClose={() => setConfirm(undefined)}>
      <p>Das Objekt „{item.name}“ wird {confirm === "delete"
        ? "deaktiviert. Zugeordnete Zählpunkte oder Anlagen können die Aktion blockieren."
        : "wiederhergestellt."}</p>
    </ConfirmDialog>}
    {newSystem && <EnergySystemForm initial={blankSystem} onClose={() => setNewSystem(false)}
      onSaved={async () => { setNewSystem(false); await load(); }} />}
    {systemForm && <EnergySystemForm entityId={systemForm.id} initial={{
      energySystemNumber: systemForm.energySystemNumber, name: systemForm.name,
      type: systemForm.type, buildingId: id, ratedPowerKw: systemForm.ratedPowerKw,
      commissionedAt: systemForm.commissionedAt, decommissionedAt: systemForm.decommissionedAt,
      rowVersion: systemForm.rowVersion,
    }} onClose={() => setSystemForm(undefined)}
      onSaved={async () => { setSystemForm(undefined); await load(); }} />}
  </section>;
}
