import { useEffect, useState } from "react";
import { Link } from "react-router";
import { curationService } from "../../services/curationService";
import type { CurationReadiness, MeteringPointGoldProfile } from "./types";
import type { BuildingGoldAssessment } from "../internalDataProducts/types";
import { qualityService, type OperationalBuildingQuality } from "../../services/qualityService";
import { formatUiValue } from "../../components/ui/uiFormat";

export function CurationReadinessPanel({ entityType, id, buildingAssessment }: {
  entityType: "Building" | "MeteringPoint";
  id: string;
  buildingAssessment?: BuildingGoldAssessment;
}) {
  const [data, setData] = useState<CurationReadiness>();
  const [openTasks, setOpenTasks] = useState<number>();
  const [meterProfile, setMeterProfile] = useState<MeteringPointGoldProfile>();
  const [operational, setOperational] = useState<OperationalBuildingQuality>();
  const [showDetails, setShowDetails] = useState(false);
  useEffect(() => {
    const controller = new AbortController();
    if (entityType === "MeteringPoint") {
      curationService.meterReadiness(id, controller.signal)
        .then(setData).catch(() => setData(undefined));
      curationService.meterProfile(id, controller.signal).then(setMeterProfile).catch(() => setMeterProfile(undefined));
    }
    if (entityType === "Building") {
      qualityService.building(id, controller.signal)
        .then(setOperational).catch(() => setOperational(undefined));
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
  const maturityLevel = operational?.assessment.overallQualityLevel ??
    buildingAssessment?.maturityLevel ?? data!.maturityLevel;
  const completenessPercent = operational?.assessment.goldProgress.percentage ??
    buildingAssessment?.goldCompletenessPercentage ?? data!.readinessPercent;
  const confirmationPercent = buildingAssessment?.goldConfirmationPercentage ?? data!.confirmationPercent;
  const curationUrl = `/tools/data-curation?entityType=${entityType}&${
    entityType === "Building" ? "buildingId" : "meteringPointId"}=${id}`;
  const requirementTotal = operational
    ? operational.assessment.goldProgress.goldCount +
      operational.assessment.goldProgress.silverCount +
      operational.assessment.goldProgress.bronzeCount
    : 0;
  const weakChildren = operational ? [
    ...operational.assessment.meterQualities
      .filter((scope) => scope.level !== "Gold")
      .map((scope) => `Zählpunkt ${scope.scopeId} · ${scope.level}`),
    ...operational.assessment.energySystemQualities
      .filter((scope) => scope.level !== "Gold")
      .map((scope) => `Anlage ${scope.scopeId} · ${scope.level}`),
  ] : [];
  return <section className="detail-section">
    <div className="section-heading"><h2>Datenqualität</h2>
      <Link to={curationUrl}>Im Datenkurationscenter öffnen</Link>
    </div>
    <dl className="detail-grid">
      <div><dt>Qualitätsstatus</dt><dd>
        <button type="button" className="quality-badge-button"
          onClick={() => setShowDetails((value) => !value)}
          aria-expanded={showDetails}>
          <span className={`quality-badge quality-badge--${maturityLevel.toLowerCase()}`}>
            {maturityLevel}</span>
          <span className="quality-badge-info" aria-hidden="true">ⓘ</span>
        </button>
      </dd></div>
      <div><dt>Fortschritt zum Goldstandard</dt><dd>{completenessPercent} %
        {operational
          ? <> · {operational.assessment.goldProgress.goldCount} von{" "}
            {requirementTotal} Kernanforderungen vollständig</>
          : buildingAssessment && <> · {buildingAssessment.goldPresentFieldCount} von{" "}
            {buildingAssessment.goldRequiredFieldCount} Werten vorhanden</>}</dd></div>
      {buildingAssessment && !operational && <div><dt>Fachlich bestätigt</dt><dd>{confirmationPercent} %
        {" "}· {buildingAssessment.goldConfirmedFieldCount} von{" "}
        {buildingAssessment.goldRequiredFieldCount} Werten bestätigt</dd></div>}
      <div><dt>Offene Kurationsvorschläge</dt><dd>{openTasks ?? "Nicht verfügbar"}</dd></div>
      {operational && <><div><dt>Statusverteilung</dt><dd>
        {operational.assessment.goldProgress.goldCount} Gold ·{" "}
        {operational.assessment.goldProgress.silverCount} Silver ·{" "}
        {operational.assessment.goldProgress.bronzeCount} Bronze
      </dd></div>
      <div><dt>Zählpunkte vollständig erfasst</dt><dd>
        {operational.assessment.meterInventoryComplete ? "Ja" : "Nein"}</dd></div>
      <div><dt>Anlagen vollständig erfasst</dt><dd>
        {operational.assessment.energySystemInventoryComplete ? "Ja" : "Nein"}</dd></div>
      <div><dt>Keine relevanten Anlagen bestätigt</dt><dd>
        {operational.noRelevantEnergySystemsConfirmed ? "Ja" : "Nein"}</dd></div>
      <div><dt>Inventarstatus</dt><dd>
        {operational.inventoryDeclarationStatus}</dd></div>
      <div><dt>Jahresverbrauch</dt><dd>
        {formatUiValue(operational.assessment.annualConsumptionStatus)}</dd></div>
      <div><dt>Jahresproduktion</dt><dd>
        {formatUiValue(operational.assessment.annualProductionStatus)}</dd></div></>}
    </dl>
    {operational && showDetails && <div className="quality-detail-popover">
      <h3>Warum dieser Status?</h3>
      {operational.assessment.blockingReasons.length === 0
        ? <p>Keine blockierenden Gründe.</p>
        : <ul>{operational.assessment.blockingReasons.map((reason) =>
          <li key={reason}>{reason}</li>)}</ul>}
      {operational.assessment.warnings.length > 0 && <><h4>Hinweise</h4>
        <ul>{operational.assessment.warnings.map((warning) =>
          <li key={warning}>{warning}</li>)}</ul></>}
      {weakChildren.length > 0 && <><h4>Untergeordnete Zählpunkte und Anlagen unter Gold</h4>
        <ul>{weakChildren.map((entry) => <li key={entry}>{entry}</li>)}</ul></>}
      {operational.nextActions.length > 0 && <><h4>Nächster Schritt</h4>
        <ul>{operational.nextActions.map((action) =>
          <li key={action}>{action}</li>)}</ul></>}
    </div>}
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
