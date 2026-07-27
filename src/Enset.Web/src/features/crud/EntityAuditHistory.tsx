import { useEffect, useState } from "react";
import { auditHistoryService } from "../../services/auditHistoryService";
import type { AuditHistoryItem } from "./types";
import { Dialog, formError } from "./crudUi";

const fields: Record<string, string> = {
  Name: "Name", CustomerId: "Kundenzuordnung", BuildingId: "Gebäudezuordnung",
  Email: "E-Mail", Phone: "Telefon", Type: "Typ", Value: "Messwert",
  Timestamp: "Zeitpunkt", QualityFlag: "Qualität", IsDeleted: "Status",
};
const hidden = new Set(["UpdatedAtUtc", "UpdatedByUserId", "RowVersion", "CreatedAtUtc", "CreatedByUserId"]);

export function EntityAuditHistory({ entityType, entityId, onClose }: {
  entityType: string; entityId: string; onClose: () => void;
}) {
  const [items, setItems] = useState<AuditHistoryItem[]>();
  const [error, setError] = useState("");
  useEffect(() => {
    const controller = new AbortController();
    auditHistoryService.get(entityType, entityId, controller.signal)
      .then(result => setItems(result.items.filter(item => !item.fieldName || !hidden.has(item.fieldName))))
      .catch(value => { if (!controller.signal.aborted) setError(formError(value)); });
    return () => controller.abort();
  }, [entityId, entityType]);
  return (
    <Dialog title="Änderungsverlauf" onClose={onClose}>
      <div className="dialog-content">
        {error ? <p className="form-error" role="alert">{error}</p> :
          !items ? <p>Änderungen werden geladen …</p> :
          items.length === 0 ? <p>Keine fachlichen Änderungen vorhanden.</p> :
          <ol className="audit-list">{items.map((item, index) =>
            <li key={`${item.changedAtUtc}-${index}`}>
              <strong>{item.fieldName ? fields[item.fieldName] ?? item.fieldName : item.changeType}</strong>
              {item.fieldName && <div>{item.oldValue ?? "–"} → {item.newValue ?? "–"}</div>}
              <small>{new Date(item.changedAtUtc).toLocaleString("de-DE")} · {item.changedByUserId} · {item.source}</small>
              {item.reason && <small>Grund: {item.reason}</small>}
            </li>)}</ol>}
      </div>
    </Dialog>
  );
}
