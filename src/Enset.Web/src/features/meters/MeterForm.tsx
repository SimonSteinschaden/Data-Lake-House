import { useCallback, useEffect, useMemo, useState, type FormEvent } from "react";
import { Link } from "react-router";
import { meterService } from "../../services/meterService";
import { buildingService } from "../../services/buildingService";
import type { BuildingSummary } from "../buildings/types";
import type { MeterWriteModel } from "./types";
import {
  meterDirectionOptions,
  meterMediumOptions,
  meterUnitOptions,
} from "../../components/ui/enumOptions";
import {
  Dialog, FormField, concurrencyMessage, fieldErrors, formError, useUnsavedChanges,
} from "../crud/crudUi";

export function MeterForm({ initial, entityId, onClose, onSaved, onReload }: {
  initial: MeterWriteModel; entityId?: string; onClose: () => void;
  onSaved: (id: string) => void; onReload?: () => void;
}) {
  const [model, setModel] = useState(initial);
  const [buildings, setBuildings] = useState<BuildingSummary[]>([]);
  const [buildingSearch, setBuildingSearch] = useState("");
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState("");
  const [errors, setErrors] = useState<Record<string, string>>({});
  const dirty = JSON.stringify(model) !== JSON.stringify(initial);
  useUnsavedChanges(dirty);
  const loadBuildings = useCallback(() => {
    buildingService.list({ pageSize: 200 }).then((x) => setBuildings(x.items)).catch(() => setBuildings([]));
  }, []);
  useEffect(() => { loadBuildings(); }, [loadBuildings]);
  const visibleBuildings = useMemo(() => {
    const search = buildingSearch.trim().toLocaleLowerCase();
    return search ? buildings.filter((x) =>
      `${x.buildingNumber} ${x.name} ${x.customerName ?? ""}`.toLocaleLowerCase().includes(search))
      : buildings;
  }, [buildingSearch, buildings]);
  const close = () => {
    if (!dirty || window.confirm("Ungespeicherte Änderungen verwerfen?")) onClose();
  };
  const set = (key: keyof MeterWriteModel, value: string | number | null) =>
    setModel((current) => ({ ...current, [key]: value }));
  const submit = async (event: FormEvent) => {
    event.preventDefault(); setBusy(true); setError(""); setErrors({});
    const request = { ...model, name: model.name.trim() || model.meterNumber.trim() };
    try {
      const result = entityId ? await meterService.update(entityId, request) : await meterService.create(request);
      onSaved(result.id);
    } catch (value) { setError(formError(value)); setErrors(fieldErrors(value)); }
    finally { setBusy(false); }
  };
  return <Dialog title={entityId ? "Zähler bearbeiten" : "Zähler anlegen"} onClose={close}>
    <form className="crud-form" onSubmit={submit}>
      {error && <div className="form-error" role="alert"><p>{error}</p>
        {error === concurrencyMessage && <div className="conflict-actions">
          <button type="button" onClick={onReload}>Aktuelle Daten neu laden</button>
          <button type="button" onClick={onClose}>Bearbeitung abbrechen</button>
        </div>}
      </div>}
      <FormField label="Zählpunktnummer *" error={errors.meterNumber}>
        <input required maxLength={128} value={model.meterNumber}
          onChange={(e) => set("meterNumber", e.target.value)} />
      </FormField>
      <FormField label="Interne ID / Bezeichnung (optional)" error={errors.name}>
        <input maxLength={256} value={model.name} onChange={(e) => set("name", e.target.value)} />
      </FormField>
      <h3 className="form-actions">Objektzuordnung</h3>
      <FormField label="Objekt suchen">
        <input value={buildingSearch} onChange={(e) => setBuildingSearch(e.target.value)}
          placeholder="Objektname, Gebäudenummer oder Kunde" />
      </FormField>
      <FormField label="Zugehöriges Objekt *" error={errors.buildingId}>
        <select required value={model.buildingId} onChange={(e) => set("buildingId", e.target.value)}>
          <option value="">Objekt auswählen</option>
          {visibleBuildings.map((x) => <option key={x.id} value={x.id}>
            {x.buildingNumber} · {x.name}{x.customerName ? ` · ${x.customerName}` : ""}
          </option>)}
        </select>
      </FormField>
      <div className="form-actions">
        <Link to="/buildings?create=true" target="_blank">Objekt anlegen</Link>
        <button type="button" onClick={loadBuildings}>Objektliste aktualisieren</button>
      </div>
      <FormField label="Energieträger *" error={errors.medium}>
        <select required value={model.medium} onChange={(e) => set("medium", e.target.value)}>
          <option value="">Auswählen</option>
          {meterMediumOptions.map((option) =>
            <option key={option.value} value={option.value}>{option.label}</option>)}
        </select>
      </FormField>
      <FormField label="Einheit *" error={errors.unit}>
        <select required value={model.unit} onChange={(e) => set("unit", e.target.value)}>
          <option value="">Auswählen</option>
          {meterUnitOptions.map((option) =>
            <option key={option.value} value={option.value}>{option.label}</option>)}
        </select>
      </FormField>
      <FormField label="Richtung *" error={errors.direction}>
        <select required value={model.direction} onChange={(e) => set("direction", e.target.value)}>
          <option value="">Auswählen</option>
          {meterDirectionOptions.map((option) =>
            <option key={option.value} value={option.value}>{option.label}</option>)}
        </select>
      </FormField>
      <FormField label="Jahreswert (optional)" error={errors.annualValue}>
        <input type="number" min="0" step="any" value={model.annualValue ?? ""}
          onChange={(e) => set("annualValue", e.target.value === "" ? null : Number(e.target.value))} />
      </FormField>
      <FormField label="Beschreibung (optional)" error={errors.description}>
        <textarea maxLength={1000} value={model.description ?? ""}
          onChange={(e) => set("description", e.target.value || null)} />
      </FormField>
      <FormField label="Externe Referenz (optional)" error={errors.externalIdentifier}>
        <input maxLength={256} value={model.externalIdentifier ?? ""}
          onChange={(e) => set("externalIdentifier", e.target.value || null)} />
      </FormField>
      <footer className="form-actions"><button type="button" onClick={close}>Abbrechen</button>
        <button className="primary-button" disabled={busy}>{busy ? "Speichert …" : "Speichern"}</button>
      </footer>
    </form>
  </Dialog>;
}
