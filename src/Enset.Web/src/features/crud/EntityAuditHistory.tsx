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

export function EntityAuditHistory({
  entityType,
  entityId,
  onClose,
}: {
  entityType: string;
  entityId: string;
  onClose: () => void;
}) {
  const [items, setItems] = useState<AuditHistoryItem[] | null>(null);
  const [error, setError] = useState("");

  useEffect(() => {
    const controller = new AbortController();

    auditHistoryService
      .get(entityType, entityId, controller.signal)
      .then((result) => {
        if (controller.signal.aborted) return;

        // Unterstützt sowohl [] als auch { items: [] }.
        const historyItems: AuditHistoryItem[] = Array.isArray(result)
          ? result
          : Array.isArray(result?.items)
            ? result.items
            : [];

        const visibleItems = historyItems.filter(
          (item) => !item.fieldName || !hidden.has(item.fieldName),
        );

        setItems(visibleItems);
      })
      .catch((value: unknown) => {
        if (controller.signal.aborted) return;

        setItems([]);
        setError(formError(value));
      });

    return () => controller.abort();
  }, [entityId, entityType]);

  return (
    <Dialog title="Änderungsverlauf" onClose={onClose}>
      <div className="dialog-content">
        {error ? (
          <p className="form-error" role="alert">
            {error}
          </p>
        ) : items === null ? (
          <p>Änderungen werden geladen …</p>
        ) : items.length === 0 ? (
          <p>Für dieses Objekt liegt noch keine Änderungshistorie vor.</p>
        ) : (
          <ol className="audit-list">
            {items.map((item, index) => (
              <li
                key={`${item.changedAtUtc}-${item.fieldName ?? item.changeType}-${index}`}
              >
                <strong>
                  {item.fieldName
                    ? fields[item.fieldName] ?? item.fieldName
                    : item.changeType}
                </strong>

                {item.fieldName && (
                  <div>
                    {item.oldValue ?? "–"} → {item.newValue ?? "–"}
                  </div>
                )}

                <small>
                  {new Date(item.changedAtUtc).toLocaleString("de-AT")}
                  {" · "}
                  {item.changedByDisplayName}
                  {" · "}
                  {item.source}
                </small>

                {item.reason && <small>Grund: {item.reason}</small>}
              </li>
            ))}
          </ol>
        )}
      </div>
    </Dialog>
  );
}
