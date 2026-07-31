import { useEffect, useState } from "react";
import { Link } from "react-router";
import { formatUiValue } from "../components/ui/uiFormat";
import {
  dataQualityService,
  type QualityDashboard,
} from "../services/dataQualityService";
import "../features/dataProducts/dataProducts.css";

export function DataQualityPage() {
  const [data, setData] = useState<QualityDashboard>();
  const [error, setError] = useState("");

  useEffect(() => {
    const controller = new AbortController();
    dataQualityService.dashboard(controller.signal)
      .then(setData)
      .catch((requestError: Error) => {
        if (!controller.signal.aborted) setError(requestError.message);
      });
    return () => controller.abort();
  }, []);

  return <main>
    <h1>Datenqualität</h1>
    <p>Zustand, Auswirkung und passende Arbeitsabläufe der vorhandenen Daten.</p>
    {error && <p className="error">{error}</p>}
    {data && <>
      <section className="kpi-grid">
        <article className="kpi"><span>Kunden vollständig</span><strong>{data.customerCompleteness} %</strong></article>
        <article className="kpi"><span>Gebäude vollständig</span><strong>{data.buildingCompleteness} %</strong></article>
        <article className="kpi"><span>Zähler vollständig</span><strong>{data.meterCompleteness} %</strong></article>
        <article className="kpi"><span>Offene Importprüfhinweise</span><strong>{data.openImportIssues}</strong></article>
        <article className="kpi"><span>Offene Datenprüfungen</span><strong>{data.openDataReviews}</strong></article>
      </section>
      <h2>Qualitätsstufen</h2>
      <div className="kpi-grid">{data.qualityLevels.map((item) =>
        <article className="kpi" key={item.level}>
          <span>{formatUiValue(item.level)}</span><strong>{item.count}</strong>
        </article>)}
      </div>
      <h2>Eignung</h2>
      <div className="table-responsive"><table>
        <thead><tr><th>Anwendungsfall</th><th>Geeignet</th><th>Nicht geeignet</th></tr></thead>
        <tbody>{data.suitability.map((item) => <tr key={item.useCase}>
          <td>{item.useCase}</td><td>{item.suitable}</td><td>{item.notSuitable}</td>
        </tr>)}</tbody>
      </table></div>
      <h2>Qualitätsprobleme</h2>
      <div className="table-responsive"><table>
        <thead><tr><th>Problem</th><th>Schweregrad</th><th>Anzahl</th><th>Trend</th>
          <th>Kunden</th><th>Gebäude</th><th>Zähler</th><th>Aktion</th></tr></thead>
        <tbody>{data.metrics.map((item) => <tr key={item.code}>
          <td><strong>{item.name}</strong><br /><small>{item.description}</small></td>
          <td>{formatUiValue(item.severity)}</td><td>{item.count}</td><td>{item.trend}</td>
          <td>{item.affectedCustomers}</td><td>{item.affectedBuildings}</td>
          <td>{item.affectedMeters}</td><td><Link to={item.actionUrl}>{item.action}</Link></td>
        </tr>)}</tbody>
      </table></div>
    </>}
  </main>;
}
