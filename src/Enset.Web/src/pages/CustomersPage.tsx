import { useCallback, useEffect, useState, type FormEvent } from "react";
import { Link, useNavigate, useParams, useSearchParams } from "react-router";
import {
  AdminPageHeader,
  PageState,
  Pagination,
} from "../components/admin/AdminUi";
import { errorMessage } from "../components/admin/adminFormat";
import "../components/admin/admin.css";
import { customerService } from "../services/customerService";
import { internalDataProductService } from "../services/internalDataProductService";
import type { CustomerSummaryProduct } from "../features/internalDataProducts/types";
import type {
  CustomerDetail,
  CustomerSummary,
  CustomerWriteModel,
} from "../features/customers/types";
import type { PagedResult } from "../types/paging";
import {
  ConfirmDialog,
  Dialog,
  EntityMetadataBar,
  FormField,
  concurrencyMessage,
  fieldErrors,
  formError,
  useUnsavedChanges,
} from "../features/crud/crudUi";
import { EntityAuditHistory } from "../features/crud/EntityAuditHistory";
export function CustomersPage() {
  const { customerId } = useParams();
  return customerId ? <Detail id={customerId} /> : <List />;
}
const blank = (): CustomerWriteModel => ({
  customerNumber: "",
  name: "",
  type: "Unknown",
  legalName: null,
  email: null,
  phone: null,
  countryCode: "AT",
  rowVersion: 0,
  contactPerson: null,
  street: null,
  houseNumber: null,
  postalCode: null,
  city: null,
});
const model = (x: CustomerDetail): CustomerWriteModel => ({
  customerNumber: x.customerNumber,
  name: x.name,
  type: x.type || "Unknown",
  legalName: x.legalName,
  email: x.email,
  phone: x.phone,
  countryCode: x.countryCode,
  rowVersion: x.rowVersion,
  contactPerson: x.contactPerson,
  street: x.street,
  houseNumber: x.houseNumber,
  postalCode: x.postalCode,
  city: x.city,
});
function CustomerForm({
  initial,
  id,
  onClose,
  onSaved,
  onReload,
}: {
  initial: CustomerWriteModel;
  id?: string;
  onClose: () => void;
  onSaved: (id: string) => void;
  onReload?: () => void;
}) {
  const [value, setValue] = useState(initial),
    [busy, setBusy] = useState(false),
    [error, setError] = useState(""),
    [errors, setErrors] = useState<Record<string, string>>({});
  const dirty = JSON.stringify(value) !== JSON.stringify(initial);
  useUnsavedChanges(dirty);
  const close = () => {
    if (!dirty || window.confirm("Ungespeicherte Änderungen verwerfen?"))
      onClose();
  };
  const set = (key: keyof CustomerWriteModel, text: string) =>
    setValue((x) => ({ ...x, [key]: text || null }));
  const submit = async (e: FormEvent) => {
    e.preventDefault();
    setBusy(true);
    setError("");
    setErrors({});
    try {
      const r = id
        ? await customerService.update(id, value)
        : await customerService.create(value);
      onSaved(r.id);
    } catch (v) {
      setError(formError(v));
      setErrors(fieldErrors(v));
    } finally {
      setBusy(false);
    }
  };
  return (
    <Dialog title={id ? "Kunde bearbeiten" : "Kunde anlegen"} onClose={close}>
      <form className="crud-form" onSubmit={submit}>
        {error && (
          <div className="form-error" role="alert">
            <p>{error}</p>
            {error === concurrencyMessage && (
              <div className="conflict-actions">
                <button type="button" onClick={onReload}>
                  Aktuelle Daten neu laden
                </button>
                <button type="button" onClick={onClose}>
                  Bearbeitung abbrechen
                </button>
              </div>
            )}
          </div>
        )}
        <h3 className="form-actions">Identifikation</h3>
        <FormField label="Kundennummer *" error={errors.customerNumber}>
          <input
            required
            maxLength={64}
            value={value.customerNumber}
            onChange={(e) => set("customerNumber", e.target.value)}
          />
        </FormField>
        <FormField label="Name / Organisation *" error={errors.name}>
          <input
            required
            maxLength={256}
            value={value.name}
            onChange={(e) => set("name", e.target.value)}
          />
        </FormField>
        <h3 className="form-actions">Kontakt</h3>
        <FormField label="E-Mail (optional)" error={errors.email}>
          <input
            type="email"
            maxLength={256}
            value={value.email ?? ""}
            onChange={(e) => set("email", e.target.value)}
          />
        </FormField>
        <FormField label="Telefon (optional)" error={errors.phone}>
          <input
            maxLength={64}
            value={value.phone ?? ""}
            onChange={(e) => set("phone", e.target.value)}
          />
        </FormField>
        <FormField
          label="Ansprechperson (optional)"
          error={errors.contactPerson}
        >
          <input
            maxLength={256}
            value={value.contactPerson ?? ""}
            onChange={(e) => set("contactPerson", e.target.value)}
          />
        </FormField>
        <h3 className="form-actions">Adresse</h3>
        <FormField label="Straße (optional)" error={errors.street}>
          <input
            maxLength={256}
            value={value.street ?? ""}
            onChange={(e) => set("street", e.target.value)}
          />
        </FormField>
        <FormField label="Hausnummer (optional)" error={errors.houseNumber}>
          <input
            maxLength={32}
            value={value.houseNumber ?? ""}
            onChange={(e) => set("houseNumber", e.target.value)}
          />
        </FormField>
        <FormField label="PLZ (optional)" error={errors.postalCode}>
          <input
            inputMode="text"
            maxLength={32}
            value={value.postalCode ?? ""}
            onChange={(e) => set("postalCode", e.target.value)}
          />
        </FormField>
        <FormField label="Ort (optional)" error={errors.city}>
          <input
            maxLength={128}
            value={value.city ?? ""}
            onChange={(e) => set("city", e.target.value)}
          />
        </FormField>
        <footer className="form-actions">
          <button type="button" onClick={close}>
            Abbrechen
          </button>
          <button className="primary-button" disabled={busy}>
            {busy ? "Speichert …" : "Speichern"}
          </button>
        </footer>
      </form>
    </Dialog>
  );
}
function List() {
  const nav = useNavigate(),
    [params, setParams] = useSearchParams();
  const page = Math.max(1, Number(params.get("page")) || 1);
  const [search, setSearch] = useState(params.get("search") ?? ""),
    [result, setResult] = useState<PagedResult<CustomerSummary>>(),
    [error, setError] = useState(""),
    [creating, setCreating] = useState(params.get("create") === "true");
  useEffect(() => {
    const c = new AbortController();
    customerService
      .list(
        {
          search: params.get("search") ?? undefined,
          page,
          pageSize: 50,
        },
        c.signal,
      )
      .then(setResult)
      .catch((e) => {
        if (!c.signal.aborted) setError(errorMessage(e));
      });
    return () => c.abort();
  }, [params, page]);
  const update = (key: string, v?: string) =>
    setParams((p) => {
      const n = new URLSearchParams(p);
      if (v) n.set(key, v);
      else n.delete(key);
      n.set("page", "1");
      return n;
    });
  return (
    <section className="admin-page">
      <AdminPageHeader
        title="Kunden"
        description="Organisationen, Kontaktdaten und zugeordnete Objekte"
      />
      <div className="detail-actions">
        <button className="primary-button" onClick={() => setCreating(true)}>
          Kunde anlegen
        </button>
      </div>
      <form
        className="list-toolbar"
        onSubmit={(e) => {
          e.preventDefault();
          update("search", search.trim() || undefined);
        }}
      >
        <label>
          Suche
          <input value={search} onChange={(e) => setSearch(e.target.value)} />
        </label>
        <button>Suchen</button>
      </form>
      {error ? (
        <PageState>{error}</PageState>
      ) : !result ? (
        <PageState>Daten werden geladen …</PageState>
      ) : result.items.length === 0 ? (
        <PageState>Keine Kunden gefunden.</PageState>
      ) : (
        <>
          <div className="table-wrap">
            <table className="admin-table">
              <thead>
                <tr>
                  <th>Kundennummer</th>
                  <th>Name / Organisation</th>
                  <th>PLZ</th>
                  <th>Ort</th>
                  <th>Objekte</th>
                  <th>Telefon</th>
                  <th>E-Mail</th>
                  <th></th>
                </tr>
              </thead>
              <tbody>
                {result.items.map((x) => (
                  <tr key={x.id}>
                    <td>{x.customerNumber}</td>
                    <td>{x.name}</td>
                    <td>{x.postalCode ?? "–"}</td>
                    <td>{x.city ?? "–"}</td>
                    <td>{x.buildingCount}</td>
                    <td>{x.phone ?? "–"}</td>
                    <td>{x.email ?? "–"}</td>
                    <td>
                      <Link to={`/customers/${x.id}`}>Öffnen</Link>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
          <Pagination
            page={result.page}
            totalPages={result.totalPages}
            onPage={(p) => update("page", String(p))}
          />
        </>
      )}
      {creating && (
        <CustomerForm
          initial={blank()}
          onClose={() => setCreating(false)}
          onSaved={(id) => nav(`/customers/${id}`)}
        />
      )}
    </section>
  );
}
function Detail({ id }: { id: string }) {
  const [item, setItem] = useState<CustomerDetail>(),
    [summary, setSummary] = useState<CustomerSummaryProduct>(),
    [error, setError] = useState(""),
    [editing, setEditing] = useState(false),
    [audit, setAudit] = useState(false),
    [confirm, setConfirm] = useState<"delete" | "restore">(),
    [actionError, setActionError] = useState(""),
    [busy, setBusy] = useState(false);
  const load = useCallback(
    () =>
      Promise.all([customerService.get(id), internalDataProductService.customer(id)])
        .then(([customer, product]) => { setItem(customer); setSummary(product); })
        .catch((e) => setError(errorMessage(e))),
    [id],
  );
  useEffect(() => {
    void load();
  }, [load]);
  const mutate = async () => {
    if (!item || !confirm) return;
    setBusy(true);
    setActionError("");
    try {
      if (confirm === "delete")
        await customerService.remove(id, item.rowVersion);
      else await customerService.restore(id, item.rowVersion);
      setConfirm(undefined);
      await load();
    } catch (e) {
      setActionError(formError(e));
    } finally {
      setBusy(false);
    }
  };
  if (error) return <PageState>{error}</PageState>;
  if (!item) return <PageState>Daten werden geladen …</PageState>;
  const address = [item.street, item.houseNumber].filter(Boolean).join(" ");
  return (
    <section className="admin-page">
      <Link className="back-link" to="/customers">
        ← Kunden
      </Link>
      <AdminPageHeader
        title={item.name}
        description={`Kundennummer ${item.customerNumber}`}
      />
      <div className="detail-actions">
        <button className="primary-button" onClick={() => setEditing(true)}>
          Bearbeiten
        </button>
        <button onClick={() => setAudit(true)}>Änderungsverlauf</button>
        <button
          className={item.isDeleted ? "" : "danger-button"}
          onClick={() => setConfirm(item.isDeleted ? "restore" : "delete")}
        >
          {item.isDeleted ? "Wiederherstellen" : "Deaktivieren"}
        </button>
      </div>
      <EntityMetadataBar entity={item} />
      {summary && <section className="detail-section">
        <h2>Fachliche Summary</h2>
        <dl className="detail-grid">
          <div><dt>Objekte</dt><dd>{summary.buildingCount}</dd></div>
          <div><dt>Zählpunkte</dt><dd>{summary.meterCount}</dd></div>
          <div><dt>Jahresverbrauch</dt><dd>{summary.totalAnnualConsumption ??
            "Nicht einheitenrein summierbar"}</dd></div>
          <div><dt>Jahreserzeugung</dt><dd>{summary.totalAnnualGeneration ??
            "Nicht einheitenrein summierbar"}</dd></div>
          <div><dt>Gold-Reife</dt><dd>{summary.averageMaturity.goldMaturityPercentage} %</dd></div>
          <div><dt>Offene Curation Tasks</dt><dd>{summary.openCurationTaskCount}</dd></div>
        </dl>
      </section>}
      <section className="detail-section">
        <h2>Stammdaten</h2>
        <dl className="detail-grid">
          <div>
            <dt>E-Mail</dt>
            <dd>{item.email ?? "Nicht angegeben"}</dd>
          </div>
          <div>
            <dt>Telefon</dt>
            <dd>{item.phone ?? "Nicht angegeben"}</dd>
          </div>
          <div>
            <dt>Ansprechperson</dt>
            <dd>{item.contactPerson ?? "Nicht angegeben"}</dd>
          </div>
          <div>
            <dt>PLZ</dt>
            <dd>{item.postalCode ?? "Nicht angegeben"}</dd>
          </div>
          <div>
            <dt>Ort</dt>
            <dd>{item.city ?? "Nicht angegeben"}</dd>
          </div>
          <div>
            <dt>Adresse</dt>
            <dd>{address || "Nicht angegeben"}</dd>
          </div>
        </dl>
      </section>
      <section className="detail-section">
        <div className="section-heading">
          <h2>Objekte</h2>
          <Link
            className="primary-button"
            to={`/buildings?customerId=${id}&create=true`}
          >
            Objekt anlegen
          </Link>
        </div>
        {item.buildings.length === 0 ? (
          <PageState>Keine Objekte zugeordnet.</PageState>
        ) : (
          <div className="table-wrap">
            <table className="admin-table">
              <thead>
                <tr>
                  <th>Objektname</th>
                  <th>Gebäudenummer</th>
                  <th>Nutzungsart</th>
                  <th>Zählpunkte</th>
                  <th>Datenreife</th>
                  <th></th>
                </tr>
              </thead>
              <tbody>
                {item.buildings.map((x) => (
                  <tr key={x.id}>
                    <td>{x.name}</td>
                    <td>{x.buildingNumber}</td>
                    <td>{x.usageType ?? "Nicht angegeben"}</td>
                    <td>{x.meterCount}</td>
                    <td>{x.dataMaturity}</td>
                    <td>
                      <Link to={`/buildings/${x.id}`}>Öffnen</Link>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </section>
      <section className="detail-section">
        <h2>Weitere Beziehungen</h2>
        <dl className="detail-grid">
          <div>
            <dt>Zählpunkte gesamt</dt>
            <dd>{item.meterCount}</dd>
          </div>
          <div>
            <dt>Anlagen gesamt</dt>
            <dd>{item.energySystemCount}</dd>
          </div>
        </dl>
      </section>
      {editing && (
        <CustomerForm
          initial={model(item)}
          id={id}
          onClose={() => setEditing(false)}
          onReload={async () => {
            setEditing(false);
            await load();
          }}
          onSaved={async () => {
            setEditing(false);
            await load();
          }}
        />
      )}
      {audit && (
        <EntityAuditHistory
          entityType="Customer"
          entityId={id}
          onClose={() => setAudit(false)}
        />
      )}{" "}
      {confirm && (
        <ConfirmDialog
          title={
            confirm === "delete"
              ? "Kunde deaktivieren"
              : "Kunde wiederherstellen"
          }
          confirmLabel={
            confirm === "delete" ? "Deaktivieren" : "Wiederherstellen"
          }
          busy={busy}
          error={actionError}
          onConfirm={() => void mutate()}
          onClose={() => setConfirm(undefined)}
        >
          <p>
            {confirm === "delete"
              ? `Der Kunde „${item.name}“ wird deaktiviert. Bestehende Abhängigkeiten können die Aktion blockieren.`
              : `Der Kunde „${item.name}“ wird wiederhergestellt.`}
          </p>
        </ConfirmDialog>
      )}
    </section>
  );
}
