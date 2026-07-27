import { useCallback, useEffect, useState, type FormEvent } from "react";
import { Link, useNavigate, useParams, useSearchParams } from "react-router";
import { AdminPageHeader, PageState, Pagination, StatusBadge } from "../components/admin/AdminUi";
import { displayNumber, errorMessage } from "../components/admin/adminFormat";
import "../components/admin/admin.css";
import { customerService } from "../services/customerService";
import type { CustomerDetail, CustomerSummary, CustomerWriteModel } from "../features/customers/types";
import type { PagedResult } from "../types/paging";
import { ConfirmDialog, Dialog, EntityMetadataBar, FormField, fieldErrors, formError, useUnsavedChanges } from "../features/crud/crudUi";
import { EntityAuditHistory } from "../features/crud/EntityAuditHistory";

export function CustomersPage() {
  const { customerId } = useParams();
  return customerId ? <CustomerDetailView id={customerId} /> : <CustomerList />;
}

const blankCustomer = (): CustomerWriteModel => ({
  customerNumber: "", name: "", type: "", legalName: null, email: null,
  phone: null, countryCode: "AT", rowVersion: 0,
});
const customerModel = (item: CustomerDetail): CustomerWriteModel => ({
  customerNumber: item.customerNumber, name: item.name, type: item.type,
  legalName: item.legalName, email: item.email, phone: item.phone,
  countryCode: item.countryCode, rowVersion: item.rowVersion,
});

function CustomerForm({ initial, entityId, title, onClose, onSaved }: {
  initial: CustomerWriteModel; entityId?: string; title: string; onClose: () => void; onSaved: (id: string) => void;
}) {
  const [model, setModel] = useState(initial); const [busy, setBusy] = useState(false);
  const [error, setError] = useState(""); const [errors, setErrors] = useState<Record<string, string>>({});
  const dirty = JSON.stringify(model) !== JSON.stringify(initial); useUnsavedChanges(dirty);
  const close = () => { if (!dirty || window.confirm("Ungespeicherte Änderungen verwerfen?")) onClose(); };
  const set = (key: keyof CustomerWriteModel, value: string) => setModel(current => ({ ...current, [key]: value || null }));
  const submit = async (event: FormEvent) => {
    event.preventDefault(); setBusy(true); setError(""); setErrors({});
    try {
      const result = entityId
        ? await customerService.update(entityId, model)
        : await customerService.create(model);
      onSaved(result.id);
    } catch (value) { setError(formError(value)); setErrors(fieldErrors(value)); } finally { setBusy(false); }
  };
  return <Dialog title={title} onClose={close}><form className="crud-form" onSubmit={submit}>
    {error && <p className="form-error" role="alert">{error}</p>}
    <FormField label="Kundennummer" error={errors.customerNumber}><input required value={model.customerNumber} onChange={e => set("customerNumber", e.target.value)} /></FormField>
    <FormField label="Name" error={errors.name}><input required value={model.name} onChange={e => set("name", e.target.value)} /></FormField>
    <FormField label="Typ" error={errors.type}><input required value={model.type} onChange={e => set("type", e.target.value)} /></FormField>
    <FormField label="Rechtlicher Name" error={errors.legalName}><input value={model.legalName ?? ""} onChange={e => set("legalName", e.target.value)} /></FormField>
    <FormField label="E-Mail" error={errors.email}><input type="email" value={model.email ?? ""} onChange={e => set("email", e.target.value)} /></FormField>
    <FormField label="Telefon" error={errors.phone}><input value={model.phone ?? ""} onChange={e => set("phone", e.target.value)} /></FormField>
    <FormField label="Land" error={errors.countryCode}><input required maxLength={2} value={model.countryCode} onChange={e => set("countryCode", e.target.value.toUpperCase())} /></FormField>
    <footer className="form-actions"><button type="button" onClick={close}>Abbrechen</button><button className="primary-button" disabled={busy}>{busy ? "Speichert …" : "Speichern"}</button></footer>
  </form></Dialog>;
}

function CustomerList() {
  const navigate = useNavigate(); const [params, setParams] = useSearchParams();
  const page = Math.max(1, Number(params.get("page")) || 1); const [search, setSearch] = useState(params.get("search") ?? "");
  const [result, setResult] = useState<PagedResult<CustomerSummary>>(); const [error, setError] = useState(""); const [creating, setCreating] = useState(false);
  useEffect(() => { const controller = new AbortController(); customerService.list({ search: params.get("search") ?? undefined, isActive: params.has("isActive") ? params.get("isActive") === "true" : undefined, page, pageSize: 50, sortBy: params.get("sortBy") ?? "name", sortDirection: params.get("sortDirection") === "desc" ? "desc" : "asc" }, controller.signal).then(setResult).catch(e => { if (!controller.signal.aborted) setError(errorMessage(e)); }); return () => controller.abort(); }, [params, page]);
  const update = (values: Record<string, string | undefined>) => setParams(current => { const next = new URLSearchParams(current); Object.entries(values).forEach(([key, value]) => value ? next.set(key, value) : next.delete(key)); return next; });
  return <section className="admin-page"><AdminPageHeader title="Kunden" description="Interne ENSET-Kundenverwaltung" /><div className="detail-actions"><button className="primary-button" onClick={() => setCreating(true)}>Kunde anlegen</button></div>
    <form className="list-toolbar" onSubmit={e => { e.preventDefault(); update({ search: search.trim(), page: "1" }); }}><label>Suche<input value={search} onChange={e => setSearch(e.target.value)} /></label><label>Status<select value={params.get("isActive") ?? ""} onChange={e => update({ isActive: e.target.value, page: "1" })}><option value="">Alle</option><option value="true">Aktiv</option><option value="false">Inaktiv</option></select></label><button>Suchen</button></form>
    {error ? <PageState>{error}</PageState> : !result ? <PageState>Daten werden geladen …</PageState> : result.items.length === 0 ? <PageState>Keine Kunden gefunden.</PageState> : <><div className="table-wrap"><table className="admin-table"><thead><tr><th>Kundennummer</th><th>Name</th><th>Typ</th><th>Gebäude</th><th>Status</th><th></th></tr></thead><tbody>{result.items.map(x => <tr key={x.id}><td>{x.customerNumber}</td><td>{x.name}</td><td>{x.type}</td><td>{displayNumber(x.buildingCount)}</td><td><StatusBadge active={x.isActive} /></td><td><Link className="table-link" to={`/customers/${x.id}`}>Öffnen</Link></td></tr>)}</tbody></table></div><Pagination page={result.page} totalPages={result.totalPages} onPage={value => update({ page: String(value) })} /></>}
    {creating && <CustomerForm initial={blankCustomer()} title="Kunde anlegen" onClose={() => setCreating(false)} onSaved={id => navigate(`/customers/${id}`)} />}
  </section>;
}

function CustomerDetailView({ id }: { id: string }) {
  const [item, setItem] = useState<CustomerDetail>(); const [error, setError] = useState(""); const [editing, setEditing] = useState(false); const [audit, setAudit] = useState(false); const [confirm, setConfirm] = useState<"delete" | "restore">(); const [actionError, setActionError] = useState(""); const [busy, setBusy] = useState(false);
  const load = useCallback(() => customerService.get(id).then(setItem).catch(e => setError(errorMessage(e))), [id]);
  useEffect(() => { void load(); }, [load]);
  const mutate = async () => { if (!item || !confirm) return; setBusy(true); setActionError(""); try { if (confirm === "delete") await customerService.remove(id, item.rowVersion); else await customerService.restore(id, item.rowVersion); setConfirm(undefined); await load(); } catch (value) { setActionError(formError(value)); } finally { setBusy(false); } };
  return <section className="admin-page"><Link className="back-link" to="/customers">← Kunden</Link><AdminPageHeader title={item?.name ?? "Kundendetail"} description={item?.customerNumber ?? ""} />
    {error ? <PageState>{error}</PageState> : !item ? <PageState>Daten werden geladen …</PageState> : <><div className="detail-actions"><button className="primary-button" onClick={() => setEditing(true)}>Bearbeiten</button><button onClick={() => setAudit(true)}>Änderungsverlauf</button><button className={item.isDeleted ? "" : "danger-button"} onClick={() => setConfirm(item.isDeleted ? "restore" : "delete")}>{item.isDeleted ? "Wiederherstellen" : "Löschen"}</button></div><EntityMetadataBar entity={item} />
      <section className="detail-section"><h2>Stammdaten</h2><dl className="detail-grid"><div><dt>Typ</dt><dd>{item.type}</dd></div><div><dt>E-Mail</dt><dd>{item.email ?? "–"}</dd></div><div><dt>Telefon</dt><dd>{item.phone ?? "–"}</dd></div><div><dt>Ort</dt><dd>{[item.postalCode, item.city].filter(Boolean).join(" ") || "–"}</dd></div><div><dt>Land</dt><dd>{item.countryCode}</dd></div></dl></section>
      <section className="detail-section"><div className="section-heading"><h2>Gebäude</h2><Link className="primary-button" to={`/buildings?customerId=${id}&create=true`}>Gebäude hinzufügen</Link></div>{item.buildings.length === 0 ? <PageState>Keine Gebäude zugeordnet.</PageState> : <div className="table-wrap"><table className="admin-table"><thead><tr><th>Nummer</th><th>Name</th><th>Rolle</th><th></th></tr></thead><tbody>{item.buildings.map(x => <tr key={x.id}><td>{x.buildingNumber}</td><td>{x.name}</td><td>{x.role}</td><td><Link className="table-link" to={`/buildings/${x.id}`}>Öffnen / Bearbeiten</Link></td></tr>)}</tbody></table></div>}</section>
      {editing && <CustomerForm initial={customerModel(item)} entityId={id} title="Kunde bearbeiten" onClose={() => setEditing(false)} onSaved={async () => { setEditing(false); await load(); }} />}
      {audit && <EntityAuditHistory entityType="Customer" entityId={id} onClose={() => setAudit(false)} />}
      {confirm && <ConfirmDialog title={confirm === "delete" ? "Kunde deaktivieren" : "Kunde wiederherstellen"} confirmLabel={confirm === "delete" ? "Deaktivieren" : "Wiederherstellen"} busy={busy} error={actionError} onConfirm={() => void mutate()} onClose={() => setConfirm(undefined)}><p>{confirm === "delete" ? `Der Kunde „${item.name}“ wird deaktiviert. Zugeordnete Gebäude bleiben erhalten. Die Aktion kann über Wiederherstellen rückgängig gemacht werden.` : `Der Kunde „${item.name}“ wird wieder aktiviert.`}</p></ConfirmDialog>}
    </>}
  </section>;
}
