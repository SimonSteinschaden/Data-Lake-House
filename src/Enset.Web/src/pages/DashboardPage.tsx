import { useCallback, useEffect, useState } from "react";
import { Link } from "react-router";
import { PageHeader } from "../layouts/PageHeader";
import {
  DashboardKpiCard,
  DashboardSection,
  DataQualityActionList,
  EmptyDashboardState,
  EnergySummaryChart,
  EnergySummaryTable,
  ErrorDashboardState,
  ImportQualityOverview,
  LoadingDashboardSkeleton,
  ReadinessCard,
  type QualityAction,
} from "../features/internalDataProducts/DashboardCockpit";
import { carrier, direction, number } from
  "../features/internalDataProducts/dashboardFormat";
import { internalDataProductService } from "../services/internalDataProductService";
import type {
  ImportQualityProduct,
  PortfolioSummaryProduct,
} from "../features/internalDataProducts/types";
import "./DashboardPage.css";

export function DashboardPage() {
  const [portfolio, setPortfolio] = useState<PortfolioSummaryProduct>();
  const [imports, setImports] = useState<ImportQualityProduct>();
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(false);
  const [reload, setReload] = useState(0);

  const retry = useCallback(() => setReload((value) => value + 1), []);
  useEffect(() => {
    const controller = new AbortController();
    // eslint-disable-next-line react-hooks/set-state-in-effect
    setLoading(true);
    setError(false);
    Promise.all([
      internalDataProductService.portfolio(controller.signal),
      internalDataProductService.importQuality(controller.signal),
    ]).then(([portfolioResult, importResult]) => {
      setPortfolio(portfolioResult);
      setImports(importResult);
    }).catch(() => {
      if (!controller.signal.aborted) setError(true);
    }).finally(() => {
      if (!controller.signal.aborted) setLoading(false);
    });
    return () => controller.abort();
  }, [reload]);

  return <section className="management-dashboard">
    <PageHeader title="Energie- und Datenqualitätscockpit"
      description="Portfolio bewerten, Handlungsbedarf erkennen und fachliche Voraussetzungen priorisieren." />
    {loading && <LoadingDashboardSkeleton />}
    {!loading && error && <ErrorDashboardState retry={retry} />}
    {!loading && !error && portfolio && imports &&
      <DashboardContent portfolio={portfolio} imports={imports} />}
  </section>;
}

function DashboardContent({ portfolio, imports }: {
  portfolio: PortfolioSummaryProduct; imports: ImportQualityProduct;
}) {
  if (portfolio.customerCount === 0) return <EmptyDashboardState
    message="Noch keine Kunden vorhanden. Legen Sie einen Kunden an, um das Portfolio aufzubauen." />;

  const qualityActions: QualityAction[] = [
    { problem: "Offene Kurationsaufgaben", count: portfolio.openCurationTaskCount,
      impact: "Fachliche Freigaben können blockiert sein",
      action: "Datenkurationscenter öffnen", to: "/tools/data-curation" },
    { problem: "Objekte ohne Zählpunkte", count: portfolio.buildingsWithoutMeters,
      impact: "Keine Energiezuordnung möglich",
      action: "Objekte anzeigen", to: "/buildings" },
    { problem: "Zählpunkte ohne Messwerte", count: portfolio.metersWithoutReadings,
      impact: "Keine Profilanalyse möglich",
      action: "Zählpunkte anzeigen", to: "/meters" },
    { problem: "Offene Importprobleme", count: imports.totalOpenIssues,
      impact: "Import noch nicht vollständig geklärt",
      action: "Importhistorie öffnen", to: "/imports" },
  ].filter((item) => item.count > 0);

  return <>
    <DashboardSection id="portfolio" title="Portfolio"
      description="Bestand, aktive Datensätze und freigegebene fachliche Grundlagen.">
      <div className="cockpit-kpi-grid">
        <DashboardKpiCard title="Kunden" value={number(portfolio.customerCount)}
          hint={`${number(portfolio.activeCustomerCount)} aktiv`} to="/customers" />
        <DashboardKpiCard title="Objekte" value={number(portfolio.buildingCount)}
          hint={`${number(portfolio.activeBuildingCount)} aktiv`} to="/buildings" />
        <DashboardKpiCard title="Zählpunkte" value={number(portfolio.meterCount)}
          hint={`${number(portfolio.activeMeterCount)} aktiv`} to="/meters" />
        <DashboardKpiCard title="Building-Gold-Profile"
          value={number(portfolio.releasedBuildingGoldProfileCount)}
          hint="Freigegebene fachliche Grundlagen" to="/tools/data-curation"
          tone="positive" />
        <DashboardKpiCard title="Meter-Gold-Profile"
          value={number(portfolio.releasedMeterGoldProfileCount)}
          hint="Freigegebene Messprofil-Grundlagen" to="/tools/data-curation"
          tone="positive" />
        <DashboardKpiCard title="Durchschnittliche Gold-Reife"
          value={`${Math.round(portfolio.averageMaturity.goldMaturityPercentage)} %`}
          hint="Anteil bestätigter Gold-Felder" to="/tools/data-curation" />
      </div>
      {portfolio.buildingCount === 0 && <p className="cockpit-inline-state">
        Noch keine Objekte angelegt.</p>}
      {portfolio.meterCount === 0 && <p className="cockpit-inline-state">
        Noch keine Zählpunkte vorhanden.</p>}
      {portfolio.releasedBuildingGoldProfileCount === 0 &&
        portfolio.releasedMeterGoldProfileCount === 0 &&
        <p className="cockpit-inline-state">Es wurden noch keine Gold-Profile freigegeben.</p>}
    </DashboardSection>

    <DashboardSection id="energy" title="Energie"
      description="Vorhandene Jahreswerte – strikt getrennt nach Energieträger, Richtung und Einheit.">
      {portfolio.energySummaries.length === 0
        ? <EmptyDashboardState
          message="Für das Portfolio sind noch keine belastbaren Jahreswerte vorhanden." />
        : <><div className="cockpit-kpi-grid cockpit-kpi-grid--energy">
          {portfolio.energySummaries.map((item) =>
            <DashboardKpiCard
              key={`${item.energyCarrier}-${item.direction}-${item.unit}`}
              title={`${carrier(item.energyCarrier)} · ${direction(item.direction)}`}
              value={`${number(item.annualValue)} ${item.unit}`}
              hint={`${number(item.meterCount)} zugehörige Zählpunkte`} />)}
        </div>
        <div className="cockpit-panel"><EnergySummaryChart items={portfolio.energySummaries} /></div>
        <div className="cockpit-panel"><h3>Energiedaten im Detail</h3>
          <EnergySummaryTable items={portfolio.energySummaries} /></div></>}
      {portfolio.dataQualityWarnings.map((warning) =>
        <div className="cockpit-warning" key={warning.code}>
          <strong>{warning.severity === "Warning" ? "Hinweis" : "Information"}</strong>
          <span>{warning.message}</span>
        </div>)}
    </DashboardSection>

    <DashboardSection id="quality" title="Datenqualität"
      description="Auffälligkeiten, die fachliche Auswertungen oder Freigaben verhindern können.">
      <div className="cockpit-kpi-grid">
        <DashboardKpiCard title="Offene Kurationsaufgaben"
          value={number(portfolio.openCurationTaskCount)}
          hint="Fachliche Entscheidungen erforderlich" to="/tools/data-curation"
          tone={portfolio.openCurationTaskCount > 0 ? "attention" : "positive"} />
        <DashboardKpiCard title="Fehlende Building-Gold-Profile"
          value={number(portfolio.buildingsWithoutReleasedGoldProfile)}
          hint="Keine freigegebene fachliche Grundlage" to="/buildings"
          tone={portfolio.buildingsWithoutReleasedGoldProfile > 0 ? "attention" : "positive"} />
        <DashboardKpiCard title="Fehlende Meter-Gold-Profile"
          value={number(portfolio.metersWithoutReleasedGoldProfile)}
          hint="Messprofil noch nicht fachlich freigegeben" to="/meters"
          tone={portfolio.metersWithoutReleasedGoldProfile > 0 ? "attention" : "positive"} />
        <DashboardKpiCard title="Zählpunkte ohne Messwerte"
          value={number(portfolio.metersWithoutReadings)}
          hint="Noch kein Messprofil vorhanden" to="/meters"
          tone={portfolio.metersWithoutReadings > 0 ? "attention" : "positive"} />
        <DashboardKpiCard title="Unvollständige Messprofile"
          value={number(portfolio.metersWithIncompleteProfiles)}
          hint="Profilprüfung erforderlich" to="/meters"
          tone={portfolio.metersWithIncompleteProfiles > 0 ? "attention" : "positive"} />
        <DashboardKpiCard title="Blockierende Importprobleme"
          value={number(imports.totalBlockingIssues)}
          hint="Import kann fachlich nicht abgeschlossen sein" to="/imports"
          tone={imports.totalBlockingIssues > 0 ? "attention" : "positive"} />
      </div>
      <div className="cockpit-panel"><h3>Priorisierte Handlungsfelder</h3>
        {qualityActions.length === 0
          ? <p className="cockpit-inline-state">Keine offenen zentralen Handlungsfelder.</p>
          : <DataQualityActionList items={qualityActions} />}</div>
    </DashboardSection>

    <DashboardSection id="readiness" title="Data-Product-Readiness"
      description="Bewertung der Voraussetzungen – keine Benchmark- oder Profilergebnisse.">
      {portfolio.readinessSummaries.length === 0
        ? <EmptyDashboardState message="Die Voraussetzungen wurden noch nicht bewertet." />
        : <div className="readiness-grid">{portfolio.readinessSummaries.map((item) =>
          <ReadinessCard key={item.product} item={item} />)}</div>}
    </DashboardSection>

    <DashboardSection id="imports" title="Import- und Bearbeitungsstatus"
      description="Importverlauf, offene Entscheidungen und noch zu klärende Probleme.">
      <div className="cockpit-kpi-grid">
        <DashboardKpiCard title="Letzter erfolgreicher Import"
          value={portfolio.lastSuccessfulImportAt
            ? new Date(portfolio.lastSuccessfulImportAt).toLocaleDateString("de-AT")
            : "Nicht verfügbar"} hint="Zeitpunkt der letzten erfolgreichen Übernahme"
          to="/imports" />
        <DashboardKpiCard title="Erfolgreiche Importe"
          value={number(imports.successfulImportCount)} to="/imports" tone="positive" />
        <DashboardKpiCard title="Fehlgeschlagene Importe"
          value={number(imports.failedImportCount)} to="/imports"
          tone={imports.failedImportCount > 0 ? "attention" : "positive"} />
        <DashboardKpiCard title="Offene Importprobleme"
          value={number(imports.totalOpenIssues)} to="/imports"
          tone={imports.totalOpenIssues > 0 ? "attention" : "positive"} />
        <DashboardKpiCard title="Offene Benutzerentscheidungen"
          value={number(imports.openResolutionCount)} to="/imports" />
        <DashboardKpiCard title="Offene Kurationsaufgaben"
          value={number(imports.openCurationTaskCount)} to="/tools/data-curation" />
      </div>
      <div className="cockpit-panel"><div className="cockpit-panel__heading">
        <h3>Letzte Importe</h3><Link to="/imports">Importhistorie öffnen</Link></div>
        <ImportQualityOverview data={imports} /></div>
    </DashboardSection>
  </>;
}
