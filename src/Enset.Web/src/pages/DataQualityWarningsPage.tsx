import { useEffect, useState } from "react";
import { Link, useSearchParams } from "react-router";

import { Card } from "../components/ui/Card";
import { PageHeader } from "../layouts/PageHeader";
import { analyticsService } from "../services/analyticsService";
import type {
  ManagementWarningDetail,
  ManagementWarningDetails,
} from "../features/analytics/types";

function formatDate(timestamp: string) {
  return new Intl.DateTimeFormat("de-AT", {
    dateStyle: "short",
    timeStyle: "short",
  }).format(new Date(timestamp));
}

function buildLink(label: string | null, path: string | null) {
  return path ? <Link to={path}>{label ?? "—"}</Link> : label ?? "—";
}

function entityLink(warning: ManagementWarningDetail) {
  if (warning.entityType === "Meter" && warning.meterId) {
    return buildLink(warning.entity, `/meters/${warning.meterId}`);
  }
  if (warning.entityType === "Building" && warning.buildingId) {
    return buildLink(warning.entity, `/buildings/${warning.buildingId}`);
  }
  if (warning.entityType === "Customer" && warning.entityId) {
    return buildLink(warning.entity, `/customers/${warning.entityId}`);
  }

  return warning.entity ?? "—";
}

function objectLink(label: string | null, id: string | null, pathPrefix: string) {
  return id ? buildLink(label, `${pathPrefix}/${id}`) : label ?? "—";
}

export function DataQualityWarningsPage() {
  const [searchParams, setSearchParams] = useSearchParams();
  const severity = searchParams.get("severity") || undefined;
  const currentPage = Number(searchParams.get("page") ?? "1") || 1;
  const pageSize = Number(searchParams.get("pageSize") ?? "50") || 50;
  const [data, setData] = useState<ManagementWarningDetails>();
  const [loadedRequest, setLoadedRequest] = useState("");
  const requestKey = `${currentPage}:${pageSize}:${severity ?? ""}`;
  const loading = loadedRequest !== requestKey;

  useEffect(() => {
    const controller = new AbortController();

    analyticsService
      .warningsDetails(currentPage, pageSize, severity, controller.signal)
      .then((result) => {
        if (!controller.signal.aborted) {
          setData(result);
          setLoadedRequest(requestKey);
        }
      });

    return () => controller.abort();
  }, [currentPage, pageSize, severity, requestKey]);

  const warnings = data?.warnings ?? [];
  const totalPages = data
    ? Math.max(1, Math.ceil(data.totalCount / pageSize))
    : 1;

  const updateParams = (params: Record<string, string | number | undefined>) => {
    const values = new URLSearchParams(searchParams);

    Object.entries(params).forEach(([key, value]) => {
      if (value === undefined || value === null) {
        values.delete(key);
      } else {
        values.set(key, String(value));
      }
    });

    setSearchParams(values);
  };

  return (
    <section className="management-dashboard">
      <PageHeader
        title="Datenqualitätswarnungen"
        description="Detailansicht aktiver Qualitätswarnungen mit direktem Zugriff auf betroffene Objekte."
      />

      <Card
        title="Warnungen"
        description={
          severity
            ? `Gefiltert nach Schweregrad: ${severity}`
            : "Alle aktiven Qualitätswarnungen"
        }
      >
        {loading ? (
          <p>Wird geladen …</p>
        ) : warnings.length === 0 ? (
          <p className="dashboard-empty">
            Keine Detailwarnungen verfügbar.
          </p>
        ) : (
          <>
            <div className="table-responsive">
              <table className="management-dashboard__warnings-table">
                <thead>
                  <tr>
                    <th>Warnungstyp</th>
                    <th>Schweregrad</th>
                    <th>Entität</th>
                    <th>Gebäude</th>
                    <th>Zähler</th>
                    <th>Beschreibung</th>
                    <th>Ursache</th>
                    <th>Empfohlene Maßnahme</th>
                    <th>Status</th>
                    <th>Zeitpunkt</th>
                  </tr>
                </thead>
                <tbody>
                  {warnings.map((warning) => (
                    <tr key={`${warning.code}-${warning.entityId}-${warning.meterId}-${warning.occurredAt}`}>
                      <td>{warning.warningType}</td>
                      <td>{warning.severity}</td>
                      <td>{entityLink(warning)}</td>
                      <td>
                        {objectLink(warning.building, warning.buildingId, "/buildings")}
                      </td>
                      <td>
                        {objectLink(warning.meter, warning.meterId, "/meters")}
                      </td>
                      <td>{warning.description}</td>
                      <td>{warning.cause}</td>
                      <td>{warning.recommendation}</td>
                      <td>{warning.status}</td>
                      <td>{formatDate(warning.occurredAt)}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>

            <div className="management-dashboard__pagination">
              <button
                type="button"
                disabled={currentPage <= 1}
                onClick={() => updateParams({ page: currentPage - 1 })}
              >
                Zurück
              </button>
              <span>
                Seite {currentPage} von {totalPages}
              </span>
              <button
                type="button"
                disabled={currentPage >= totalPages}
                onClick={() => updateParams({ page: currentPage + 1 })}
              >
                Weiter
              </button>
            </div>
          </>
        )}
      </Card>
    </section>
  );
}
