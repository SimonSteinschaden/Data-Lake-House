import { useState, type FormEvent } from "react";
import { buildingService } from "../../services/buildingService";
import type { BuildingWriteModel } from "./types";
import { Dialog, FormField, fieldErrors, formError, useUnsavedChanges } from "../crud/crudUi";

export function BuildingForm({ initial, entityId, onClose, onSaved }: {
  initial: BuildingWriteModel; entityId?: string; onClose: () => void; onSaved: (id: string) => void;
}) {
  const [model, setModel] = useState(initial); const [busy, setBusy] = useState(false);
  const [error, setError] = useState(""); const [errors, setErrors] = useState<Record<string, string>>({});
  const dirty = JSON.stringify(model) !== JSON.stringify(initial); useUnsavedChanges(dirty);
  const close = () => { if (!dirty || window.confirm("Ungespeicherte Änderungen verwerfen?")) onClose(); };
  const text = (key: keyof BuildingWriteModel, value: string) => setModel(x => ({ ...x, [key]: value || null }));
  const number = (key: keyof BuildingWriteModel, value: string) => setModel(x => ({ ...x, [key]: value === "" ? null : Number(value) }));
  const submit = async (event: FormEvent) => { event.preventDefault(); setBusy(true); setError(""); setErrors({}); try { const result = entityId ? await buildingService.update(entityId, model) : await buildingService.create(model); onSaved(result.id); } catch (value) { setError(formError(value)); setErrors(fieldErrors(value)); } finally { setBusy(false); } };
  return <Dialog title={entityId ? "Gebäude bearbeiten" : "Gebäude anlegen"} onClose={close}><form className="crud-form" onSubmit={submit}>
    {error && <p className="form-error" role="alert">{error}</p>}
    <FormField label="Gebäudenummer" error={errors.buildingNumber}><input required value={model.buildingNumber} onChange={e => text("buildingNumber", e.target.value)} /></FormField>
    <FormField label="Name" error={errors.name}><input required value={model.name} onChange={e => text("name", e.target.value)} /></FormField>
    <FormField label="Externe ID" error={errors.externalIdentifier}><input value={model.externalIdentifier ?? ""} onChange={e => text("externalIdentifier", e.target.value)} /></FormField>
    <FormField label="Kundenzuordnung" error={errors.customerId}><input value={model.customerId ?? ""} onChange={e => text("customerId", e.target.value)} /></FormField>
    <FormField label="Bruttogrundfläche (m²)" error={errors.grossFloorAreaM2}><input type="number" step="any" value={model.grossFloorAreaM2 ?? ""} onChange={e => number("grossFloorAreaM2", e.target.value)} /></FormField>
    <FormField label="Baujahr" error={errors.yearOfConstruction}><input type="number" value={model.yearOfConstruction ?? ""} onChange={e => number("yearOfConstruction", e.target.value)} /></FormField>
    <FormField label="Breitengrad" error={errors.latitude}><input type="number" step="any" value={model.latitude ?? ""} onChange={e => number("latitude", e.target.value)} /></FormField>
    <FormField label="Längengrad" error={errors.longitude}><input type="number" step="any" value={model.longitude ?? ""} onChange={e => number("longitude", e.target.value)} /></FormField>
    <footer className="form-actions"><button type="button" onClick={close}>Abbrechen</button><button className="primary-button" disabled={busy}>{busy ? "Speichert …" : "Speichern"}</button></footer>
  </form></Dialog>;
}
