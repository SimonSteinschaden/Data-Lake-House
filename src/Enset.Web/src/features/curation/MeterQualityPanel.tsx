import { useCallback, useEffect, useState } from "react";
import { Link } from "react-router";
import { errorMessage } from "../../components/admin/adminFormat";
import { formatUiDateTime, formatUiValue } from "../../components/ui/uiFormat";
import {
  qualityService,
  type MeterProfileAnalysis,
  type MeterProfileIssue,
  type MeterQualityAssessment,
} from "../../services/qualityService";

export function MeterQualityPanel({ meterId }: { meterId: string }) {
  const [assessment, setAssessment] = useState<MeterQualityAssessment>();
  const [analysis, setAnalysis] = useState<MeterProfileAnalysis>();
  const [issues, setIssues] = useState<MeterProfileIssue[]>();
  const [showStart, setShowStart] = useState(false);
  const [from, setFrom] = useState("");
  const [to, setTo] = useState("");
  const [version, setVersion] = useState("1.0");
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState("");

  const load = useCallback(async () => {
    try {
      const result = await qualityService.meter(meterId);
      setAssessment(result);
      if (result.currentAnalysisId) {
        setAnalysis(await qualityService.analysis(meterId, result.currentAnalysisId));
      } else {
        setAnalysis(undefined);
      }
    } catch (loadError) { setError(errorMessage(loadError)); }
  }, [meterId]);

  // eslint-disable-next-line react-hooks/set-state-in-effect
  useEffect(() => { void load(); }, [load]);

  const loadIssues = async () => {
    if (!assessment?.currentAnalysisId) return;
    try {
      const result = await qualityService.issues(meterId, assessment.currentAnalysisId, 1, 50);
      setIssues(result.items);
    } catch (loadError) { setError(errorMessage(loadError)); }
  };

  const start = async () => {
    if (!from || !to) return;
    setBusy(true); setError("");
    try {
      await qualityService.startAnalysis(
        meterId, new Date(from).toISOString(), new Date(to).toISOString(), version);
      setShowStart(false);
      setIssues(undefined);
      await load();
    } catch (startError) { setError(errorMessage(startError)); }
    finally { setBusy(false); }
  };

  if (!assessment) return null;

  return <section className="detail-section">
    <div className="section-heading"><h2>Profilqualität</h2>
      <div className="detail-actions">
        <button onClick={() => setShowStart(true)}>
          {assessment.currentAnalysisId ? "Analyse erneut starten" : "Analyse starten"}
        </button>
        {assessment.currentAnalysisId &&
          <button onClick={() => void loadIssues()}>Offene Probleme anzeigen</button>}
        <Link className="table-link" to={`/tools/data-review/meter-issues?meterId=${meterId}`}>
          Datenprüfung öffnen
        </Link>
      </div>
    </div>
    {error && <p className="form-error">{error}</p>}
    <dl className="detail-grid">
      <div><dt>Qualitätsstatus</dt><dd>
        <span className={`quality-badge quality-badge--${assessment.qualityLevel.toLowerCase()}`}>
          {assessment.qualityLevel}</span></dd></div>
      <div><dt>Analysezustand</dt><dd>{formatUiValue(assessment.profileAnalysisStatus)}</dd></div>
      <div><dt>Analysierter Zeitraum</dt><dd>{analysis
        ? `${formatUiDateTime(analysis.periodFromUtc)} – ${formatUiDateTime(analysis.periodToUtc)}`
        : "Nicht verfügbar"}</dd></div>
      <div><dt>Analyseversion</dt><dd>{assessment.analysisVersion ?? "Nicht verfügbar"}</dd></div>
      <div><dt>Messintervall</dt><dd>{analysis?.detectedIntervalSeconds
        ? `${analysis.detectedIntervalSeconds / 60} Minuten` : "Nicht erkannt"}</dd></div>
      <div><dt>Vollständigkeit</dt><dd>{assessment.completenessPercentage == null
        ? "Nicht bestimmbar" : `${assessment.completenessPercentage} %`}</dd></div>
      <div><dt>Anzahl Lücken</dt><dd>{analysis?.gapCount ?? "Nicht verfügbar"}</dd></div>
      <div><dt>Anzahl Anomalien</dt><dd>{assessment.openAnomalyCount}</dd></div>
      <div><dt>Blockierende Probleme</dt><dd>{assessment.blockingIssueCount}</dd></div>
      <div><dt>Warnungen</dt><dd>{assessment.warnings.length === 0
        ? "Keine" : assessment.warnings.join(", ")}</dd></div>
      <div><dt>Letzte Analyse</dt><dd>{formatUiDateTime(assessment.lastAnalyzedAtUtc)}</dd></div>
      <div><dt>Bestätigende Person</dt><dd>{analysis?.executedByDisplayNameSnapshot ?? "Nicht verfügbar"}</dd></div>
      <div><dt>Nächste Aktion</dt><dd>{assessment.nextActions[0] ?? "Keine"}</dd></div>
    </dl>
    {assessment.blockingReasons.length > 0 && <><h3>Blockierende Gründe</h3>
      <ul>{assessment.blockingReasons.map((reason) => <li key={reason}>{reason}</li>)}</ul></>}
    {issues && <><h3>Offene Probleme</h3>
      {issues.length === 0 ? <p>Keine offenen Probleme.</p> : <div className="table-wrap">
        <table className="admin-table"><thead><tr>
          <th>Code</th><th>Kategorie</th><th>Schweregrad</th><th>Nachricht</th><th>Status</th>
        </tr></thead><tbody>{issues.map((issue) => <tr key={issue.id}>
          <td>{issue.code}</td><td>{formatUiValue(issue.category)}</td>
          <td>{formatUiValue(issue.severity)}</td>
          <td>{issue.message}</td><td>{formatUiValue(issue.resolutionStatus)}</td>
        </tr>)}</tbody></table></div>}</>}
    {showStart && <div className="quality-detail-popover">
      <h3>Analysezeitraum festlegen</h3>
      <label>Von{" "}<input type="datetime-local" value={from}
        onChange={(event) => setFrom(event.target.value)} /></label>{" "}
      <label>Bis{" "}<input type="datetime-local" value={to}
        onChange={(event) => setTo(event.target.value)} /></label>{" "}
      <label>Analyseversion{" "}<input value={version}
        onChange={(event) => setVersion(event.target.value)} /></label>
      <div className="detail-actions">
        <button className="primary-button" disabled={busy || !from || !to}
          onClick={() => void start()}>Starten</button>
        <button onClick={() => setShowStart(false)}>Abbrechen</button>
      </div>
    </div>}
  </section>;
}
