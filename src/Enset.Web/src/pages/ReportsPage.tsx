import { useEffect, useState } from "react";
import { Card } from "../components/ui/Card";
import { formatUiValue } from "../components/ui/uiFormat";
import type { BuildingSummary } from "../features/buildings/types";
import { PageHeader } from "../layouts/PageHeader";
import { buildingService } from "../services/buildingService";
import {
  reportService,
  type ReportDefinition,
  type ReportInstance,
} from "../services/reportService";


export function ReportsPage() {
  const [definitions, setDefinitions] = useState<ReportDefinition[]>([]);
  const [reports, setReports] = useState<ReportInstance[]>([]);
  const [buildings, setBuildings] = useState<BuildingSummary[]>([]);
  const [type, setType] = useState("ObjectEnergy");
  const [buildingId, setBuildingId] = useState("");
  const [recipient, setRecipient] = useState("");
  const [error, setError] = useState("");

  const load = () => Promise.all([
    reportService.definitions(),
    reportService.list(),
    buildingService.list({ page: 1, pageSize: 200 }),
  ]).then(([nextDefinitions, nextReports, nextBuildings]) => {
    setDefinitions(nextDefinitions);
    setReports(nextReports);
    setBuildings(nextBuildings.items);
  }).catch(() => setError("Berichte konnten nicht geladen werden."));

  useEffect(() => { void load(); }, []);

  const create = async () => {
    if (!buildingId || !recipient.trim()) {
      setError("Gebäude und Empfänger sind erforderlich.");
      return;
    }
    try {
      await reportService.create({
        type, buildingId,
        recipient
      });
      setError("");
      await load();
    } catch {
      setError("Bericht konnte nicht erzeugt werden.");
    }
  };

  const release = async (report: ReportInstance) => {
    const releasedBy = window.prompt("Freigegeben durch:");
    if (!releasedBy) return;
    try {
      await reportService.release(report.reportId, releasedBy);
      await load();
    } catch {
      setError("Bericht konnte nicht freigegeben werden.");
    }
  };

  const archive = async (report: ReportInstance) => {
    try {
      await reportService.archive(report.reportId);
      await load();
    } catch {
      setError("Bericht konnte nicht archiviert werden.");
    }
  };

  return <section className="page-stack">
    <PageHeader title="Berichte"
      description="Versionierte Ergebnisdokumente aus demselben Datenprodukt der Objektanalyse." />
    <Card title="Bericht erzeugen">
      <div className="object-analysis__filters">
        <label>Berichtstyp<select value={type} onChange={(e) => setType(e.target.value)}>
          {definitions.map((x) => <option key={x.type} value={x.type}>{x.title}</option>)}
        </select></label>
        <label>Gebäude<select value={buildingId} onChange={(e) => setBuildingId(e.target.value)}>
          <option value="">Gebäude auswählen</option>
          {buildings.map((x) => <option key={x.id} value={x.id}>{x.name}</option>)}
        </select></label>
        <label>Empfänger<input value={recipient} onChange={(e) => setRecipient(e.target.value)} /></label>
      </div>
      <button type="button" onClick={() => void create()}>Bericht erzeugen</button>
      {error && <p className="object-analysis__filter-error">{error}</p>}
    </Card>
    <Card title="Berichtsliste">
      {reports.length === 0 ? <p>Noch keine Berichte vorhanden.</p> :
        <table><thead><tr><th>Bericht</th><th>Gebäude</th><th>Zeitraum</th>
          <th>Version</th><th>Qualität</th><th>Status</th><th>Export</th><th></th></tr></thead>
          <tbody>{reports.map((report) => <tr key={report.reportId}>
            <td>{definitions.find((x) => x.type === report.type)?.title ?? report.type}</td>
            <td>{report.buildingName}</td>
            <td>{report.version}</td><td>{formatUiValue(report.qualityLevel)}</td><td>{formatUiValue(report.releaseStatus)}</td>
            <td>{["pdf", "xlsx", "json"].map((format) =>
              <button key={format} type="button"
                onClick={() => void reportService.download(report, format)}>{format.toUpperCase()}</button>)}</td>
            <td>{report.releaseStatus === "Draft" &&
              <button type="button" onClick={() => void release(report)}>Freigeben</button>}
              {" "}{report.releaseStatus === "Released" &&
              <button type="button" onClick={() => void archive(report)}>Archivieren</button>}
            </td>
          </tr>)}</tbody></table>}
    </Card>
  </section>;
}
