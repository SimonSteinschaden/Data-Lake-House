import { useEffect, useState } from "react";
import { apiGet } from "../../api";
import { PageState } from "../../components/admin/AdminUi";
import { formatUiValue } from "../../components/ui/uiFormat";

interface Requirement {
  requirementId: string;
  name: string;
  weight: number;
  isBlocking: boolean;
  guidance: string;
}

interface Result {
  dataProductType: string;
  percentage: number;
  status: string;
  minimumMaturity: string;
  missingRequirements: Requirement[];
  blockingRequirements: Requirement[];
  warnings: string[];
  profileVersions: { profileType: string; version: number }[];
}

export function DataProductReadinessPanel({
  scopeType,
  scopeId,
}: {
  scopeType: "Building" | "MeteringPoint";
  scopeId: string;
}) {
  const [data, setData] = useState<Result[]>();

  useEffect(() => {
    const controller = new AbortController();
    apiGet<Result[]>(`/api/v1/data-product-readiness/${scopeType}/${scopeId}`, {
      signal: controller.signal,
    }).then(setData).catch(() => setData([]));
    return () => controller.abort();
  }, [scopeType, scopeId]);

  return <section className="detail-section">
    <h2>Datenprodukt-Reife</h2>
    {!data
      ? <PageState>Reife wird ermittelt …</PageState>
      : data.length === 0
        ? <PageState>Keine Reifebewertung verfügbar.</PageState>
        : <div className="readiness-list">{data.map((item) =>
          <article key={item.dataProductType}>
            <div className="section-heading">
              <strong>{item.dataProductType}</strong>
              <b>{item.percentage} % · {formatUiValue(item.status)}</b>
            </div>
            <progress max="100" value={item.percentage}>{item.percentage}%</progress>
            <small>Mindest-Reifegrad: {formatUiValue(item.minimumMaturity)}
              {item.profileVersions[0] ? ` · Profilversion ${item.profileVersions[0].version}` : ""}
            </small>
            {item.blockingRequirements.length > 0 && <>
              <h3>Blockiert durch</h3>
              <ul>{item.blockingRequirements.map((requirement) =>
                <li key={requirement.requirementId}>{requirement.name}: {requirement.guidance}</li>)}
              </ul>
            </>}
            {item.blockingRequirements.length === 0 && item.missingRequirements.length > 0 &&
              <ul>{item.missingRequirements.map((requirement) =>
                <li key={requirement.requirementId}>{requirement.name}: {requirement.guidance}</li>)}
              </ul>}
          </article>)}
        </div>}
  </section>;
}
