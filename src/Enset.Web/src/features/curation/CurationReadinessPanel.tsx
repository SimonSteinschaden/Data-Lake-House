import { useEffect, useState } from "react";
import { Link } from "react-router";
import { curationService } from "../../services/curationService";
import type { CurationReadiness, MeteringPointGoldProfile } from "./types";
import type { BuildingGoldAssessment } from "../internalDataProducts/types";

export function CurationReadinessPanel({ entityType, id, buildingAssessment }: {
  entityType: "Building" | "MeteringPoint";
  id: string;
  buildingAssessment?: BuildingGoldAssessment;
}) {
  const [data, setData] = useState<CurationReadiness>();
  const [openTasks, setOpenTasks] = useState<number>();
  const [meterProfile, setMeterProfile] = useState<MeteringPointGoldProfile>();
  useEffect(() => {
    const controller = new AbortController();
    if (entityType === "MeteringPoint") {
      curationService.meterReadiness(id, controller.signal)
        .then(setData).catch(() => setData(undefined));
      curationService.meterProfile(id, controller.signal).then(setMeterProfile).catch(() => setMeterProfile(undefined));
    }
    const query = new URLSearchParams({
      entityType, status: "Open", page: "1", pageSize: "1",
      [entityType === "Building" ? "buildingId" : "meteringPointId"]: id,
    });
    curationService.tasks(query, controller.signal)
      .then((result) => setOpenTasks(result.totalCount)).catch(() => setOpenTasks(undefined));
    return () => controller.abort();
  }, [entityType, id]);
  if (entityType === "MeteringPoint" && !data) return null;
  if (entityType === "Building" && !buildingAssessment) return null;
  const maturityLevel = buildingAssessment?.maturityLevel ?? data!.maturityLevel;
  const completenessPercent = buildingAssessment?.goldCompletenessPercentage ?? data!.readinessPercent;
  const confirmationPercent = buildingAssessment?.goldConfirmationPercentage ?? data!.confirmationPercent;
  const curationUrl = `/tools/data-curation?entityType=${entityType}&${
    entityType === "Building" ? "buildingId" : "meteringPointId"}=${id}`;
  return <section className="detail-section">
    <div className="section-heading"><h2>Datenreife</h2>
      <Link to={curationUrl}>Im Datenkurationscenter öffnen</Link>
    </div>
    <dl className="detail-grid">
      <div><dt>Reifegrad</dt><dd><span
        className={`quality-badge quality-badge--${maturityLevel.toLowerCase()}`}>
        {maturityLevel}</span></dd></div>
      <div><dt>Gold-Vollständigkeit</dt><dd>{completenessPercent} %
        {buildingAssessment && <> · {buildingAssessment.goldPresentFieldCount} von{" "}
          {buildingAssessment.goldRequiredFieldCount} Werten vorhanden</>}</dd></div>
      {buildingAssessment && <div><dt>Fachlich bestätigt</dt><dd>{confirmationPercent} %
        {" "}· {buildingAssessment.goldConfirmedFieldCount} von{" "}
        {buildingAssessment.goldRequiredFieldCount} Werten bestätigt</dd></div>}
      <div><dt>Offene Kurationsvorschläge</dt><dd>{openTasks ?? "Nicht verfügbar"}</dd></div>
    </dl>
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
    {buildingAssessment ? <>
      {buildingAssessment.missingReasons.length > 0
        ? <><h3>Fehlt für Gold</h3><ul>{buildingAssessment.missingReasons.map((issue) =>
          <li key={issue}>{issue}</li>)}</ul></>
        : <p>Alle Gold-relevanten Stammdaten sind vollständig.</p>}
      {buildingAssessment.confirmationReasons.length > 0 &&
        <><h3>Noch fachlich zu bestätigen</h3>
          <ul>{buildingAssessment.goldFieldStates
            .filter((item) => item.state === "PresentUnconfirmed")
            .map((item) => <li key={item.fieldName}>{item.unfulfilledReason}{" "}
              <Link to={`/tools/data-curation?entityType=Building&entityId=${id}&fieldName=${item.fieldName}&returnTo=${encodeURIComponent(`/buildings/${id}`)}`}>
                In der Datenprüfung bestätigen
              </Link>
            </li>)}</ul></>}
    </> : data!.blockingIssues.length > 0
      ? <><h3>Fehlt für Gold</h3><ul>{data!.blockingIssues.map((issue) =>
        <li key={issue}>{issue}</li>)}</ul></>
      : <p>Alle Gold-relevanten Stammdaten sind vollständig.</p>}
  </section>;
}
