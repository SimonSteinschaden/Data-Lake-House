import { useCallback, useEffect, useState } from "react";
import { useSearchParams } from "react-router";
import { AdminPageHeader, PageState, Pagination } from "../components/admin/AdminUi";
import { errorMessage } from "../components/admin/adminFormat";
import { formatUiDateTime, formatUiValue } from "../components/ui/uiFormat";
import {
  qualityService,
  type CurationDecisionRequest,
  type MeterProfileCurationDecision,
  type MeterProfileIssue,
  type MeterQualityAssessment,
} from "../services/qualityService";
import "./CurationCenterPage.css";

const decisionTypes: CurationDecisionRequest["decisionType"][] = [
  "ConfirmAsCorrect", "CorrectValue", "MarkInvalid", "AcceptGap",
  "AddManualValue", "GenerateEstimatedValue", "MarkForObservation",
  "IgnoreWithReason", "Reopen",
];
const nonFinalDecisions = new Set(["MarkForObservation", "Reopen"]);

export function MeterIssueReviewPage() {
  const [searchParams] = useSearchParams();
  const meterId = searchParams.get("meterId");
  const [assessment, setAssessment] = useState<MeterQualityAssessment>();
  const [issues, setIssues] = useState<MeterProfileIssue[]>();
  const [page, setPage] = useState(1);
  const [totalPages, setTotalPages] = useState(1);
  const [selected, setSelected] = useState<MeterProfileIssue>();
  const [decisions, setDecisions] = useState<MeterProfileCurationDecision[]>();
  const [decisionType, setDecisionType] = useState<CurationDecisionRequest["decisionType"]>("ConfirmAsCorrect");
  const [newValue, setNewValue] = useState("");
  const [method, setMethod] = useState("");
  const [confidence, setConfidence] = useState("");
  const [reason, setReason] = useState("");
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState("");

  const load = useCallback(async () => {
    if (!meterId) return;
    try {
      const result = await qualityService.meter(meterId);
      setAssessment(result);
      if (result.currentAnalysisId) {
        const list = await qualityService.issues(meterId, result.currentAnalysisId, page, 25);
        setIssues(list.items);
        setTotalPages(list.totalPages);
      } else {
        setIssues([]);
      }
    } catch (loadError) { setError(errorMessage(loadError)); }
  }, [meterId, page]);

  // eslint-disable-next-line react-hooks/set-state-in-effect
  useEffect(() => { void load(); }, [load]);

  const open = async (issue: MeterProfileIssue) => {
    setSelected(issue);
    setDecisionType("ConfirmAsCorrect");
    setNewValue(""); setMethod(""); setConfidence(""); setReason("");
    try {
      if (meterId && assessment?.currentAnalysisId) {
        const result = await qualityService.decisions(meterId, assessment.currentAnalysisId, issue.id);
        setDecisions(result.items);
      }
    } catch (loadError) { setError(errorMessage(loadError)); }
  };

  const submit = async () => {
    if (!meterId || !assessment?.currentAnalysisId || !selected || !reason.trim()) return;
    setBusy(true); setError("");
    try {
      await qualityService.decide(meterId, assessment.currentAnalysisId, selected.id, {
        decisionType,
        previousValue: selected.originalValue,
        newValue: newValue.trim() || null,
        generatedValueMethod: decisionType === "GenerateEstimatedValue" ? method.trim() || null : null,
        confidencePercent: decisionType === "GenerateEstimatedValue" && confidence
          ? Number(confidence) : null,
        reason: reason.trim(),
        isFinal: !nonFinalDecisions.has(decisionType),
      });
      setSelected(undefined);
      await load();
    } catch (submitError) { setError(errorMessage(submitError)); }
    finally { setBusy(false); }
  };

  if (!meterId) return <section className="admin-page">
    <AdminPageHeader title="Datenprüfung – Zählpunktprobleme"
      description="Diese Ansicht wird aus dem Zählpunktdetail heraus aufgerufen." />
    <PageState>Kein Zählpunkt ausgewählt. Bitte über die Profilqualität eines Zählpunkts öffnen.</PageState>
  </section>;

  const needsNewValue = decisionType === "CorrectValue" || decisionType === "AddManualValue" ||
    decisionType === "GenerateEstimatedValue";

  return <section className="admin-page curation-page">
    <AdminPageHeader title="Datenprüfung – Zählpunktprobleme"
      description="Offene Profilprobleme fachlich entscheiden, begründen und revisionssicher festhalten." />
    {error && <p className="form-error">{error}</p>}
    <div className="curation-layout">
      <section>
        <div className="section-heading"><h2>Offene Probleme</h2>
          <span>{assessment?.openIssueCount ?? 0}</span></div>
        {!issues ? <PageState>Probleme werden geladen …</PageState>
          : issues.length === 0 ? <PageState>Keine offenen Probleme für diesen Zählpunkt.</PageState>
          : <><ul className="curation-task-list">{issues.map((issue) =>
            <li key={issue.id}><button onClick={() => void open(issue)}
              className={selected?.id === issue.id ? "is-selected" : ""}>
              <span><strong>{issue.code}</strong>
                <small>{formatUiValue(issue.category)} · {formatUiValue(issue.resolutionStatus)}</small>
              </span><b>{formatUiValue(issue.severity)}</b>
            </button></li>)}</ul>
            <Pagination page={page} totalPages={totalPages} onPage={setPage} /></>}
      </section>
      <section className="curation-detail">
        <h2>Einzelprüfung</h2>
        {!selected ? <PageState>Ein offenes Problem auswählen.</PageState> : <>
          <div className="comparison">
            <article><small>Originalwert</small>
              <strong>{selected.originalValue ?? "Nicht vorhanden"}</strong>
              <span>Quelle: Profilanalyse</span></article>
            <article><small>Erwarteter Wert</small>
              <strong>{selected.expectedValue ?? "Nicht verfügbar"}</strong>
              <span className="confidence">{formatUiValue(selected.severity)}</span></article>
          </div>
          <div className="reasoning"><h3>Technische Analyse</h3><p>{selected.message}</p>
            {selected.technicalDetails && <small>{selected.technicalDetails}</small>}
            <small>Kategorie {formatUiValue(selected.category)} · erkannt {formatUiDateTime(selected.createdAtUtc)}</small>
          </div>
          <div className="customize-fields">
            <label>Entscheidung
              <select value={decisionType}
                onChange={(event) => setDecisionType(event.target.value as CurationDecisionRequest["decisionType"])}>
                {decisionTypes.map((type) => <option key={type} value={type}>{formatUiValue(type)}</option>)}
              </select>
            </label>
            {needsNewValue && <label>Neuer beziehungsweise Ersatzwert
              <input value={newValue} onChange={(event) => setNewValue(event.target.value)} /></label>}
            {decisionType === "GenerateEstimatedValue" && <>
              <label>Erzeugungsmethode
                <input value={method} onChange={(event) => setMethod(event.target.value)} /></label>
              <label>Confidence in %
                <input type="number" min="0" max="100" value={confidence}
                  onChange={(event) => setConfidence(event.target.value)} /></label>
            </>}
            <label>Begründung<textarea value={reason}
              onChange={(event) => setReason(event.target.value)} /></label>
          </div>
          <div className="curation-actions">
            <button className="primary-button" disabled={busy || !reason.trim()}
              onClick={() => void submit()}>Entscheidung speichern</button>
          </div>
          {decisions && decisions.length > 0 && <>
            <h3>Bisherige Entscheidungen</h3>
            <ul>{decisions.map((decision) => <li key={decision.id}>
              {formatUiDateTime(decision.decidedAtUtc)} · {formatUiValue(decision.decisionType)}
              {" "}· {decision.reason} · {decision.decidedByDisplayNameSnapshot ?? "Unbekannt"}
            </li>)}</ul>
          </>}
        </>}
      </section>
    </div>
  </section>;
}
