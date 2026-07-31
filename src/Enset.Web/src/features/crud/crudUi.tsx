/* eslint-disable react-refresh/only-export-components */
import { useEffect, type ReactNode } from "react";
import { ApiError } from "../../api/apiError";
import type { EntityMetadata } from "./types";
import "./crud.css";

export const concurrencyMessage =
  "Der Datensatz wurde zwischenzeitlich von einer anderen Person geändert.";

export function fieldErrors(error: unknown): Record<string, string> {
  if (!(error instanceof ApiError)) return {};
  return Object.fromEntries(
    Object.entries(error.problem?.errors ?? {}).map(([key, values]) => [
      key.charAt(0).toLowerCase() + key.slice(1),
      values.join(" "),
    ]),
  );
}

export function formError(error: unknown): string {
  if (error instanceof ApiError && error.status === 409) return concurrencyMessage;
  if (error instanceof ApiError) {
    const labels: Record<string, string> = {
      PostalCode: "PLZ",
      BuildingNumber: "Gebäudenummer",
      Name: "Gebäudename",
      CustomerId: "Kundenzuordnung",
      BuildingCategory: "Gebäudetyp",
      PrimaryUseType: "Nutzungstyp",
      BuildingState: "Gebäudezustand",
    };
    const details = Object.entries(error.problem?.errors ?? {})
      .flatMap(([field, messages]) => messages.map(message =>
        `${labels[field] ?? field}: ${message}`));
    const summary = error.problem?.detail ?? error.problem?.title ?? error.message;
    return details.length > 0 ? `${summary} ${details.join(" ")}` : summary;
  }
  return error instanceof Error ? error.message : "Die Aktion ist fehlgeschlagen.";
}

export function provenanceLabel(value: string): string {
  const labels: Record<string, string> = {
    Import: "Importiert",
    Imported: "Importiert",
    Manual: "Manuell erstellt",
    ManuallyCreated: "Manuell erstellt",
    EditedAfterImport: "Nach Import bearbeitet",
    System: "Systemgeneriert",
    SystemGenerated: "Systemgeneriert",
  };
  return labels[value] ?? value;
}

export function EntityMetadataBar({ entity }: { entity: EntityMetadata }) {
  const changedAt = entity.updatedAtUtc ?? entity.createdAtUtc;
  const changedBy = entity.updatedByUserId ?? entity.createdByUserId;
  return (
    <div className="entity-meta">
      <span>Herkunft: <strong>{provenanceLabel(entity.dataOrigin)}</strong></span>
      <span>Zuletzt geändert: <strong>{new Date(changedAt).toLocaleString("de-AT")}</strong></span>
      <span>Bearbeiter: <strong>{changedBy ?? "System"}</strong></span>
      {entity.isDeleted && <span className="entity-inactive">Deaktiviert</span>}
    </div>
  );
}

export function FormField({
  label, error, children, goldRelevant = false,
}: {
  label: string;
  error?: string;
  children: ReactNode;
  goldRelevant?: boolean;
}) {
  return (
    <label className={`crud-field${goldRelevant ? " crud-field--gold" : ""}`}>
      <span className="crud-field__label">
        {label}
        {goldRelevant && <span className="crud-field__gold-badge"
          title="Dieses optionale Feld fließt in die fachliche Gold-Bewertung ein.">
          Gold-relevant
        </span>}
      </span>
      {children}
      {error && <small role="alert">{error}</small>}
    </label>
  );
}

export function Dialog({
  title, children, onClose,
}: { title: string; children: ReactNode; onClose: () => void }) {
  useEffect(() => {
    const close = (event: KeyboardEvent) => event.key === "Escape" && onClose();
    document.addEventListener("keydown", close);
    return () => document.removeEventListener("keydown", close);
  }, [onClose]);
  return (
    <div className="dialog-backdrop" role="presentation" onMouseDown={e => e.target === e.currentTarget && onClose()}>
      <section className="crud-dialog" role="dialog" aria-modal="true" aria-labelledby="dialog-title">
        <header><h2 id="dialog-title">{title}</h2><button type="button" className="icon-button" onClick={onClose} aria-label="Dialog schließen">×</button></header>
        {children}
      </section>
    </div>
  );
}

export function ConfirmDialog({
  title, children, confirmLabel, busy, error, onConfirm, onClose,
}: {
  title: string; children: ReactNode; confirmLabel: string; busy?: boolean;
  error?: string; onConfirm: () => void; onClose: () => void;
}) {
  return (
    <Dialog title={title} onClose={onClose}>
      <div className="dialog-content">{children}{error && <p className="form-error" role="alert">{error}</p>}</div>
      <footer className="form-actions"><button type="button" onClick={onClose} disabled={busy}>Abbrechen</button><button type="button" className="danger-button" onClick={onConfirm} disabled={busy}>{busy ? "Wird ausgeführt …" : confirmLabel}</button></footer>
    </Dialog>
  );
}

export function useUnsavedChanges(dirty: boolean) {
  useEffect(() => {
    const protect = (event: BeforeUnloadEvent) => {
      if (!dirty) return;
      event.preventDefault();
    };
    window.addEventListener("beforeunload", protect);
    return () => window.removeEventListener("beforeunload", protect);
  }, [dirty]);
}
