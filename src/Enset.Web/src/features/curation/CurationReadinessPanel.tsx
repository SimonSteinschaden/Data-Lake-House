import { useEffect, useState } from "react";
import { Link } from "react-router";
import { curationService } from "../../services/curationService";
import type { CurationReadiness, MeteringPointGoldProfile } from "./types";

export function CurationReadinessPanel({ entityType, id }: {
  entityType: "Building" | "MeteringPoint";
  id: string;
}) {
  const [data, setData] = useState<CurationReadiness>();
  const [openTasks, setOpenTasks] = useState<number>();
  const [meterProfile, setMeterProfile] = useState<MeteringPointGoldProfile>();
  useEffect(() => {
    const controller = new AbortController();
    const request = entityType === "Building"
      ? curationService.buildingReadiness(id, controller.signal)
      : curationService.meterReadiness(id, controller.signal);
    request.then(setData).catch(() => setData(undefined));
    if (entityType === "MeteringPoint")
      curationService.meterProfile(id, controller.signal).then(setMeterProfile).catch(() => setMeterProfile(undefined));
    const query = new URLSearchParams({
      entityType, status: "Open", page: "1", pageSize: "1",
      [entityType === "Building" ? "buildingId" : "meteringPointId"]: id,
    });
    curationService.tasks(query, controller.signal)
      .then((result) => setOpenTasks(result.totalCount)).catch(() => setOpenTasks(undefined));
    return () => controller.abort();
  }, [entityType, id]);
  if (!data) return null;
  const curationUrl = `/tools/data-curation?entityType=${entityType}&${
    entityType === "Building" ? "buildingId" : "meteringPointId"}=${id}`;
  return <section className="detail-section">
    <div className="section-heading"><h2>Datenreife</h2>
      <Link to={curationUrl}>Im Datenkurationscenter öffnen</Link>
    </div>
    <dl className="detail-grid">
      <div><dt>Reifegrad</dt><dd>{data.maturityLevel}</dd></div>
      <div><dt>Gold-Reife</dt><dd>{data.readinessPercent} %</dd></div>
      <div><dt>Offene Kurationsvorschläge</dt><dd>{openTasks ?? "Nicht verfügbar"}</dd></div>
    </dl>
    <progress max="100" value={data.readinessPercent}>{data.readinessPercent} %</progress>
    {meterProfile && <><h3>Profilqualität</h3><dl className="detail-grid">
      <div><dt>UsageType</dt><dd>{meterProfile.usageType ?? "Nicht angegeben"}</dd></div>
      <div><dt>Intervall</dt><dd>{meterProfile.intervalMinutes == null ? "Nicht erkannt" : `${meterProfile.intervalMinutes} Minuten`}</dd></div>
      <div><dt>Vollständigkeit</dt><dd>{meterProfile.completenessPercentage} %</dd></div>
      <div><dt>Erwartete Werte</dt><dd>{meterProfile.expectedValueCount}</dd></div>
      <div><dt>Fehlende Intervalle</dt><dd>{meterProfile.missingValueCount}</dd></div>
      <div><dt>Ungültige Werte</dt><dd>{meterProfile.invalidValueCount}</dd></div>
      <div><dt>Geschätzte Werte</dt><dd>{meterProfile.estimatedValueCount}</dd></div>
      <div><dt>Interpolierte Werte</dt><dd>{meterProfile.interpolatedValueCount}</dd></div>
    </dl></>}
    {data.blockingIssues.length > 0
      ? <><h3>Fehlt für Gold</h3><ul>{data.blockingIssues.map((issue) =>
        <li key={issue}>{issue}</li>)}</ul></>
      : <p>Keine offenen Gold-Blocker.</p>}
  </section>;
}
