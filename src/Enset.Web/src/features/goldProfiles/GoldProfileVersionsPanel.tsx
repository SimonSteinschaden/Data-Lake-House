import { useCallback, useEffect, useState } from "react";
import { apiGet, apiPost } from "../../api";
import { errorMessage } from "../../components/admin/adminFormat";
import { formatUiDateTime, formatUiValue } from "../../components/ui/uiFormat";

interface Version {
  id: string;
  versionNumber: number;
  validFromUtc: string;
  validToUtc: string | null;
  createdByUserId: string;
  snapshotHash: string;
  isCurrent: boolean;
  releaseStatus: string;
  rowVersion: number;
}

export function GoldProfileVersionsPanel({
  entityType,
  entityId,
  onChanged,
}: {
  entityType: "Building" | "MeteringPoint";
  entityId: string;
  onChanged?: () => void | Promise<void>;
}) {
  const [items, setItems] = useState<Version[]>([]);
  const [error, setError] = useState("");
  const load = useCallback(() =>
    apiGet<Version[]>(`/api/v1/gold-profiles/${entityType}/${entityId}/versions`)
      .then(setItems).catch((requestError) => setError(errorMessage(requestError))),
  [entityType, entityId]);

  useEffect(() => { void load(); }, [load]);

  const create = async () => {
    try {
      await apiPost(`/api/v1/gold-profiles/${entityType}/${entityId}/create-version`);
      await load();
      await onChanged?.();
    } catch (requestError) {
      setError(errorMessage(requestError));
    }
  };
  const change = async (version: Version, action: "release" | "revoke") => {
    try {
      await apiPost(
        `/api/v1/gold-profiles/${entityType}/${entityId}/versions/${version.id}/${action}?rowVersion=${version.rowVersion}`,
        action === "revoke" ? "Fachlich zurückgezogen" : undefined,
      );
      await load();
      await onChanged?.();
    } catch (requestError) {
      setError(errorMessage(requestError));
    }
  };

  return <section className="detail-section">
    <div className="section-heading">
      <h2>Gold-Profil-Versionen</h2>
      <button onClick={() => void create()}>Neue Version erstellen</button>
    </div>
    {error && <p className="form-error">{error}</p>}
    {items.length === 0
      ? <p>Noch keine Profilversion vorhanden.</p>
      : <div className="table-wrap"><table className="admin-table">
        <thead><tr><th>Version</th><th>Status</th><th>Gültig ab</th>
          <th>Erstellt von</th><th>Datenstand</th><th></th></tr></thead>
        <tbody>{items.map((version) => <tr key={version.id}>
          <td>{version.versionNumber}{version.isCurrent ? " · aktuell" : ""}</td>
          <td>{formatUiValue(version.releaseStatus)}</td>
          <td>{formatUiDateTime(version.validFromUtc)}</td>
          <td>{version.createdByUserId}</td>
          <td>{version.snapshotHash.slice(0, 12)}…</td>
          <td>{version.releaseStatus === "Draft" &&
            <button onClick={() => void change(version, "release")}>Freigeben</button>}
            {" "}{version.releaseStatus === "Released" &&
            <button className="danger-button" onClick={() => void change(version, "revoke")}>
              Zurückziehen
            </button>}
          </td>
        </tr>)}</tbody>
      </table></div>}
  </section>;
}
