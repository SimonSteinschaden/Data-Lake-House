import { useCallback, useEffect, useMemo, useState, type FormEvent } from "react";
import { Link } from "react-router";
import { buildingService } from "../../services/buildingService";
import { customerService } from "../../services/customerService";
import type { CustomerSummary } from "../customers/types";
import type { BuildingWriteModel } from "./types";
import {
  Dialog, FormField, concurrencyMessage, fieldErrors, formError, useUnsavedChanges,
} from "../crud/crudUi";

const categories = ["Apartment", "House", "Office", "Hall", "School", "Retail", "Industry", "Other"];
const uses = ["Residential", "Commercial", "Public", "Mixed"];

export function BuildingForm({ initial, entityId, onClose, onSaved, onReload }: {
  initial: BuildingWriteModel;
  entityId?: string;
  onClose: () => void;
  onSaved: (id: string) => void;
  onReload?: () => void;
}) {
  const [model, setModel] = useState(initial);
  const [customers, setCustomers] = useState<CustomerSummary[]>([]);
  const [customerSearch, setCustomerSearch] = useState("");
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState("");
  const [errors, setErrors] = useState<Record<string, string>>({});
  const dirty = JSON.stringify(model) !== JSON.stringify(initial);
  useUnsavedChanges(dirty);

  const loadCustomers = useCallback(() => {
    customerService.list({ pageSize: 200 }).then((x) => setCustomers(x.items)).catch(() => setCustomers([]));
  }, []);
  useEffect(() => { loadCustomers(); }, [loadCustomers]);
  const visibleCustomers = useMemo(() => {
    const query = customerSearch.trim().toLocaleLowerCase();
    return query
      ? customers.filter((x) => `${x.customerNumber} ${x.name}`.toLocaleLowerCase().includes(query))
      : customers;
  }, [customerSearch, customers]);

  const close = () => {
    if (!dirty || window.confirm("Ungespeicherte Änderungen verwerfen?")) onClose();
  };
  const text = (key: keyof BuildingWriteModel, value: string) =>
    setModel((x) => ({ ...x, [key]: value || null }));
  const number = (key: keyof BuildingWriteModel, value: string) =>
    setModel((x) => ({ ...x, [key]: value === "" ? null : Number(value) }));
  const submit = async (event: FormEvent) => {
    event.preventDefault();
    setBusy(true); setError(""); setErrors({});
    try {
      const result = entityId
        ? await buildingService.update(entityId, model)
        : await buildingService.create(model);
      onSaved(result.id);
    } catch (value) {
      setError(formError(value)); setErrors(fieldErrors(value));
    } finally { setBusy(false); }
  };

  return <Dialog title={entityId ? "Objekt bearbeiten" : "Objekt anlegen"} onClose={close}>
    <form className="crud-form" onSubmit={submit}>
      {error && <div className="form-error" role="alert"><p>{error}</p>
        {error === concurrencyMessage && <div className="conflict-actions">
          <button type="button" onClick={onReload}>Aktuelle Daten neu laden</button>
          <button type="button" onClick={onClose}>Bearbeitung abbrechen</button>
        </div>}
      </div>}
      <h3 className="form-actions">Zuordnung</h3>
      <FormField label="Kunde suchen">
        <input value={customerSearch} onChange={(e) => setCustomerSearch(e.target.value)}
          placeholder="Kundennummer oder Organisation" />
      </FormField>
      <FormField label="Kundenzuordnung *" error={errors.customerId}>
        <select required value={model.customerId ?? ""} onChange={(e) => text("customerId", e.target.value)}>
          <option value="">Kunde auswählen</option>
          {visibleCustomers.map((x) =>
            <option key={x.id} value={x.id}>{x.customerNumber} · {x.name}</option>)}
        </select>
      </FormField>
      <div className="form-actions">
        <Link to="/customers?create=true" target="_blank">Neuen Kunden anlegen</Link>
        <button type="button" onClick={loadCustomers}>Kundenliste aktualisieren</button>
      </div>
      <h3 className="form-actions">Stammdaten</h3>
      <FormField label="Gebäudenummer *" error={errors.buildingNumber}>
        <input required maxLength={64} value={model.buildingNumber}
          onChange={(e) => text("buildingNumber", e.target.value)} />
      </FormField>
      <FormField label="Objektname *" error={errors.name}>
        <input required maxLength={256} value={model.name} onChange={(e) => text("name", e.target.value)} />
      </FormField>
      <FormField label="Externe ID (optional)" error={errors.externalIdentifier}>
        <input maxLength={128} value={model.externalIdentifier ?? ""}
          onChange={(e) => text("externalIdentifier", e.target.value)} />
      </FormField>
      <FormField label="Gebäudetyp (optional)" error={errors.buildingCategory}>
        <select value={model.buildingCategory ?? ""} onChange={(e) => text("buildingCategory", e.target.value)}>
          <option value="">Nicht angegeben</option>
          {categories.map((x) => <option key={x}>{x}</option>)}
        </select>
      </FormField>
      <FormField label="Nutzungstyp (optional)" error={errors.primaryUseType}>
        <select value={model.primaryUseType ?? ""} onChange={(e) => text("primaryUseType", e.target.value)}>
          <option value="">Nicht angegeben</option>
          {uses.map((x) => <option key={x}>{x}</option>)}
        </select>
      </FormField>
      <FormField label="Gebäudezustand (optional)" error={errors.benchmarkState}>
        <select value={model.benchmarkState ?? ""} onChange={(e) => text("benchmarkState", e.target.value)}>
          <option value="">Nicht angegeben</option>
          <option value="Existing">Bestand</option>
          <option value="Improved">Verbessert</option>
          <option value="Planned">Saniert (geplant)</option>
          <option value="Target">Zielzustand</option>
        </select>
      </FormField>
      <FormField label="Bruttogrundfläche in m² (optional)" error={errors.grossFloorAreaM2}>
        <input type="number" min="0" step="any" value={model.grossFloorAreaM2 ?? ""}
          onChange={(e) => number("grossFloorAreaM2", e.target.value)} />
      </FormField>
      <FormField label="Beheizte Fläche in m² (optional)" error={errors.heatedFloorAreaM2}>
        <input type="number" min="0" step="any" value={model.heatedFloorAreaM2 ?? ""}
          onChange={(e) => number("heatedFloorAreaM2", e.target.value)} />
      </FormField>
      <FormField label="Baujahr (optional)" error={errors.yearOfConstruction}>
        <input type="number" value={model.yearOfConstruction ?? ""}
          onChange={(e) => number("yearOfConstruction", e.target.value)} />
      </FormField>
      <FormField label="Renovierungsjahr (optional)" error={errors.yearOfLastMajorRenovation}>
        <input type="number" value={model.yearOfLastMajorRenovation ?? ""}
          onChange={(e) => number("yearOfLastMajorRenovation", e.target.value)} />
      </FormField>
      <h3 className="form-actions">Adresse</h3>
      <FormField label="Straße (optional)" error={errors.street}>
        <input maxLength={256} value={model.street ?? ""} onChange={(e) => text("street", e.target.value)} />
      </FormField>
      <FormField label="Hausnummer (optional)" error={errors.houseNumber}>
        <input maxLength={32} value={model.houseNumber ?? ""}
          onChange={(e) => text("houseNumber", e.target.value)} />
      </FormField>
      <FormField label="PLZ (optional)" error={errors.postalCode}>
        <input inputMode="text" maxLength={32} value={model.postalCode ?? ""}
          onChange={(e) => text("postalCode", e.target.value)} />
      </FormField>
      <FormField label="Ort (optional)" error={errors.city}>
        <input maxLength={128} value={model.city ?? ""} onChange={(e) => text("city", e.target.value)} />
      </FormField>
      <footer className="form-actions">
        <button type="button" onClick={close}>Abbrechen</button>
        <button className="primary-button" disabled={busy}>{busy ? "Speichert …" : "Speichern"}</button>
      </footer>
    </form>
  </Dialog>;
}
