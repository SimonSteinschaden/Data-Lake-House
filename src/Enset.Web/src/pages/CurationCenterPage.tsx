import { useCallback, useEffect, useState } from "react";
import { useNavigate, useSearchParams } from "react-router";
import { AdminPageHeader, PageState, Pagination } from "../components/admin/AdminUi";
import { errorMessage } from "../components/admin/adminFormat";
import { formatUiDateTime, formatUiValue } from "../components/ui/uiFormat";
import type {
  CurationStatistics,
  CurationTask,
  CurationTaskDetail,
  CurationTaskPage,
} from "../features/curation/types";
import { curationService } from "../services/curationService";
import "./CurationCenterPage.css";

const fields: Record<string, string> = {
  BuildingCategory: "Gebäudetyp",
  PrimaryUseType: "Nutzungsart",
  BuildingState: "Gebäudezustand",
  Medium: "Energieträger",
  CustomerId: "Kundenzuordnung",
  BuildingId: "Gebäudezuordnung",
  HeatedAreaSquareMeters: "Beheizte Fläche",
  PostalCode: "Postleitzahl",
  Type: "Anlagentyp",
  RatedPowerKw: "Leistung",
};

export function CurationCenterPage() {
  const [searchParams] = useSearchParams();
  const navigate = useNavigate();
  const returnTo = searchParams.get("returnTo");
  const [result, setResult] = useState<CurationTaskPage>();
  const [statistics, setStatistics] = useState<CurationStatistics>();
  const [selected, setSelected] = useState<CurationTaskDetail>();
  const [error, setError] = useState("");
  const [busy, setBusy] = useState(false);
  const [customizing, setCustomizing] = useState(false);
  const [value, setValue] = useState("");
  const [reason, setReason] = useState("");
  const [page, setPage] = useState(1);
  const [entity, setEntity] = useState(searchParams.get("entityType") ?? "");
  const [field, setField] = useState(searchParams.get("fieldName") ?? "");
  const entityId = searchParams.get("entityId");
  const [confidence, setConfidence] = useState("");

  const query = useCallback(() => {
    const parameters = new URLSearchParams({ status: "Open", page: String(page), pageSize: "25" });
    if (entity) parameters.set("entityType", entity);
    if (field) parameters.set("fieldName", field);
    if (entityId) parameters.set("entityId", entityId);
    if (confidence) parameters.set("minimumConfidence", confidence);
    return parameters;
  }, [page, entity, field, confidence, entityId]);

  const load = useCallback(async () => {
    try {
      const [tasks, nextStatistics] = await Promise.all([
        curationService.tasks(query()),
        curationService.statistics(),
      ]);
      setResult(tasks);
      setStatistics(nextStatistics);
    } catch (requestError) {
      setError(errorMessage(requestError));
    }
  }, [query]);

  // eslint-disable-next-line react-hooks/set-state-in-effect
  useEffect(() => { void load(); }, [load]);

  const open = async (task: CurationTask) => {
    try {
      setSelected(await curationService.task(task.id));
      setValue(task.suggestedValue);
      setReason("");
      setCustomizing(false);
    } catch (requestError) {
      setError(errorMessage(requestError));
    }
  };

  const decide = async (action: "accept" | "reject" | "customize") => {
    if (!selected) return;
    setBusy(true);
    setError("");
    try {
      if (action === "accept") await curationService.accept(selected.task.id);
      else if (action === "reject") await curationService.reject(selected.task.id, reason);
      else await curationService.customize(selected.task.id, value, reason);
      setSelected(undefined);
      await load();
      if (returnTo) navigate(returnTo);
    } catch (requestError) {
      setError(errorMessage(requestError));
    } finally {
      setBusy(false);
    }
  };

  return <section className="admin-page curation-page">
    <AdminPageHeader title="Datenprüfung"
      description="Offene fachliche Entscheidungen prüfen, begründen und revisionssicher festhalten" />
    {error && <p className="form-error">{error}</p>}
    <div className="curation-layout">
      <section>
        <div className="section-heading"><h2>Offene Aufgaben</h2><span>{statistics?.openTasks ?? 0}</span></div>
        <div className="curation-filters">
          <select value={entity} onChange={(event) => { setEntity(event.target.value); setPage(1); }}>
            <option value="">Alle Entitäten</option><option value="Building">Gebäude</option>
            <option value="MeteringPoint">Zähler</option>
            <option value="EnergySystem">Anlage</option>
          </select>
          <input placeholder="Feld" value={field}
            onChange={(event) => { setField(event.target.value); setPage(1); }} />
          <input type="number" min="0" max="100" placeholder="Konfidenz ab %"
            value={confidence} onChange={(event) => { setConfidence(event.target.value); setPage(1); }} />
        </div>
        {!result ? <PageState>Aufgaben werden geladen …</PageState>
          : result.items.length === 0 ? <PageState>Keine offenen Kurationsaufgaben.</PageState>
            : <><ul className="curation-task-list">{result.items.map((task) =>
              <li key={task.id}><button onClick={() => void open(task)}
                className={selected?.task.id === task.id ? "is-selected" : ""}>
                <span><strong>{task.entityDisplayName}</strong>
                  <small>{fields[task.fieldName] ?? task.fieldName} · {formatUiValue(task.maturityLevel)}</small>
                </span><b>{task.confidencePercent} %</b>
              </button></li>)}</ul>
              <Pagination page={result.page} totalPages={result.totalPages} onPage={setPage} /></>}
      </section>
      <section className="curation-detail">
        <h2>Einzelprüfung</h2>
        {!selected ? <PageState>Eine offene Aufgabe auswählen.</PageState> : <>
          <div className="comparison">
            <article><small>Originalwert</small><strong>{selected.task.originalValue ?? "Nicht vorhanden"}</strong>
              <span>Quelle: {selected.task.source}</span></article>
            <article><small>Normalisierter oder vorgeschlagener Wert</small>
              <strong>{selected.task.suggestedValue}</strong>
              <span className="confidence">{selected.task.confidencePercent} % Konfidenz</span></article>
          </div>
          <div className="reasoning"><h3>Begründung</h3><p>{selected.task.reasoning}</p>
            <small>Regel {selected.task.ruleId} · Version {selected.task.ruleVersion}
              {" "}· betroffene Entität {selected.task.entityDisplayName}</small></div>
          {customizing && <div className="customize-fields">
            <label>Manueller Wert<input value={value} onChange={(event) => setValue(event.target.value)} /></label>
            <label>Begründung<textarea value={reason} onChange={(event) => setReason(event.target.value)} /></label>
          </div>}
          <div className="curation-actions">{customizing ? <>
            <button onClick={() => setCustomizing(false)}>Später entscheiden</button>
            <button className="primary-button" disabled={busy || !value.trim() || !reason.trim()}
              onClick={() => void decide("customize")}>Manuellen Wert übernehmen</button>
          </> : <>
            <button className="primary-button" disabled={busy}
              onClick={() => void decide("accept")}>Bestätigen</button>
            <button disabled={busy} onClick={() => setCustomizing(true)}>Korrigieren</button>
            <button className="danger-button" disabled={busy || !reason.trim()}
              onClick={() => void decide("reject")}>Ignorieren</button>
          </>}</div>
          {selected.decisions.length > 0 && <>
            <h3>Änderungshistorie</h3>
            <ul>{selected.decisions.map((decision) => <li key={decision.id}>
              {formatUiDateTime(decision.decidedAtUtc)} · {formatUiValue(decision.decision)}
              {" "}· {decision.reason ?? "ohne Begründung"} · vorher {decision.originalValue ?? "—"}
              {" "}· nachher {decision.newValue ?? "—"}
            </li>)}</ul>
          </>}
        </>}
      </section>
    </div>
  </section>;
}
