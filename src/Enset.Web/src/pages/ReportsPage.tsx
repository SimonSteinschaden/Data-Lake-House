import { useEffect, useState } from "react";
import { Card } from "../components/ui/Card";
import type { BuildingSummary } from "../features/buildings/types";
import { PageHeader } from "../layouts/PageHeader";
import { buildingService } from "../services/buildingService";
import {
  reportService,
  type ReportDefinition,
  type ReportInstance,
} from "../services/reportService";

const year = new Date().getFullYear();

export function ReportsPage() {
  const [definitions, setDefinitions] = useState<ReportDefinition[]>([]);
  const [reports, setReports] = useState<ReportInstance[]>([]);
  const [buildings, setBuildings] = useState<BuildingSummary[]>([]);
  const [type, setType] = useState("ObjectEnergy");
  const [buildingId, setBuildingId] = useState("");
  const [from, setFrom] = useState(`${year}-01-01`);
  const [to, setTo] = useState(`${year + 1}-01-01`);
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
  }).catch(() => setError("Reports konnten nicht geladen werden."));

  useEffect(() => { void load(); }, []);

  const create = async () => {
    if (!buildingId || !recipient.trim()) {
      setError("Objekt und Empfänger sind erforderlich.");
      return;
    }
    try {
      await reportService.create({
        type, buildingId,
        fromUtc: `${from}T00:00:00Z`,
        toUtc: `${to}T00:00:00Z`,
        recipient,
      });
      setError("");
      await load();
    } catch {
      setError("Report konnte nicht erzeugt werden.");
    }
  };

  return <section className="page-stack">
    <PageHeader title="Reports"
      description="Versionierte Ergebnisdokumente aus demselben Object Analytics Data Product." />
    <Card title="Report erzeugen">
      <div className="object-analysis__filters">
        <label>Reporttyp<select value={type} onChange={(e) => setType(e.target.value)}>
          {definitions.map((x) => <option key={x.type} value={x.type}>{x.title}</option>)}
        </select></label>
        <label>Objekt<select value={buildingId} onChange={(e) => setBuildingId(e.target.value)}>
          <option value="">Objekt auswählen</option>
          {buildings.map((x) => <option key={x.id} value={x.id}>{x.name}</option>)}
        </select></label>
        <label>Von<input type="date" value={from} onChange={(e) => setFrom(e.target.value)} /></label>
        <label>Bis<input type="date" value={to} onChange={(e) => setTo(e.target.value)} /></label>
        <label>Empfänger<input value={recipient} onChange={(e) => setRecipient(e.target.value)} /></label>
      </div>
      <button type="button" onClick={() => void create()}>Report erzeugen</button>
      {error && <p className="object-analysis__filter-error">{error}</p>}
    </Card>
    <Card title="Reportliste">
      {reports.length === 0 ? <p>Noch keine Reports vorhanden.</p> :
        <table><thead><tr><th>Report</th><th>Objekt</th><th>Periode</th>
          <th>Version</th><th>Quality</th><th>Status</th><th>Export</th></tr></thead>
          <tbody>{reports.map((report) => <tr key={report.reportId}>
            <td>{definitions.find((x) => x.type === report.type)?.title ?? report.type}</td>
            <td>{report.buildingName}</td>
            <td>{new Date(report.fromUtc).toLocaleDateString("de-AT")}–{new Date(report.toUtc).toLocaleDateString("de-AT")}</td>
            <td>{report.version}</td><td>{report.qualityLevel}</td><td>{report.releaseStatus}</td>
            <td>{["pdf", "xlsx", "json"].map((format) =>
              <button key={format} type="button"
                onClick={() => void reportService.download(report, format)}>{format.toUpperCase()}</button>)}</td>
          </tr>)}</tbody></table>}
    </Card>
  </section>;
}
