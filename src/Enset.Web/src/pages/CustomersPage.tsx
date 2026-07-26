import { useEffect, useState, type FormEvent } from "react";
import { Link, useParams, useSearchParams } from "react-router";
import { AdminPageHeader, PageState, Pagination, StatusBadge } from "../components/admin/AdminUi";
import { displayNumber, errorMessage } from "../components/admin/adminFormat";
import "../components/admin/admin.css";
import { customerService } from "../services/customerService";
import type { CustomerDetail, CustomerSummary } from "../features/customers/types";
import type { PagedResult } from "../types/paging";

export function CustomersPage() {
  const { customerId } = useParams();
  return customerId ? <CustomerDetailView id={customerId} /> : <CustomerList />;
}

function CustomerList() {
  const [params, setParams] = useSearchParams();
  const page = Math.max(1, Number(params.get("page")) || 1);
  const [search, setSearch] = useState(params.get("search") ?? "");
  const [result, setResult] = useState<PagedResult<CustomerSummary>>();
  const [error, setError] = useState("");
  useEffect(() => {
    const controller = new AbortController();
    customerService.list({ search: params.get("search") ?? undefined, isActive: params.has("isActive") ? params.get("isActive") === "true" : undefined, page, pageSize: 50, sortBy: params.get("sortBy") ?? "name", sortDirection: params.get("sortDirection") === "desc" ? "desc" : "asc" }, controller.signal)
      .then(setResult).catch(e => { if (!controller.signal.aborted) setError(errorMessage(e)); });
    return () => controller.abort();
  }, [params, page]);
  const update = (values: Record<string, string | undefined>) => setParams(current => { const next = new URLSearchParams(current); Object.entries(values).forEach(([key, value]) => value === undefined || value === "" ? next.delete(key) : next.set(key, value)); return next; });
  const submit = (event: FormEvent) => { event.preventDefault(); update({ search: search.trim() || undefined, page: "1" }); };
  return <section className="admin-page"><AdminPageHeader title="Kunden" description="Interne ENSET-Kundenverwaltung" />
    <form className="list-toolbar" onSubmit={submit}><label>Suche<input value={search} onChange={e => setSearch(e.target.value)} /></label><label>Status<select value={params.get("isActive") ?? ""} onChange={e => update({ isActive: e.target.value || undefined, page: "1" })}><option value="">Alle</option><option value="true">Aktiv</option><option value="false">Inaktiv</option></select></label><label>Sortierung<select value={params.get("sortBy") ?? "name"} onChange={e => update({ sortBy: e.target.value, page: "1" })}><option value="name">Name</option><option value="customerNumber">Kundennummer</option></select></label><button type="submit">Suchen</button></form>
    {error ? <PageState>{error}</PageState> : !result ? <PageState>Daten werden geladen …</PageState> : result.items.length === 0 ? <PageState>Keine Kunden gefunden.</PageState> : <><div className="table-wrap"><table className="admin-table"><thead><tr><th>Kundennummer</th><th>Name</th><th>Typ</th><th>Gebäude</th><th>Status</th><th></th></tr></thead><tbody>{result.items.map(x => <tr key={x.id}><td>{x.customerNumber}</td><td>{x.name}</td><td>{x.type}</td><td>{displayNumber(x.buildingCount)}</td><td><StatusBadge active={x.isActive} /></td><td><Link className="table-link" to={`/customers/${encodeURIComponent(x.id)}`}>Öffnen</Link></td></tr>)}</tbody></table></div><Pagination page={result.page} totalPages={result.totalPages} onPage={value => update({ page: String(value) })} /></>}
  </section>;
}

function CustomerDetailView({ id }: { id: string }) {
  const [item, setItem] = useState<CustomerDetail>(); const [error, setError] = useState("");
  useEffect(() => { const controller = new AbortController(); customerService.get(id, controller.signal).then(setItem).catch(e => { if (!controller.signal.aborted) setError(errorMessage(e)); }); return () => controller.abort(); }, [id]);
  return <section className="admin-page"><Link className="back-link" to="/customers">← Kunden</Link><AdminPageHeader title={item?.name ?? "Kundendetail"} description={item?.customerNumber ?? ""} />{error ? <PageState>{error}</PageState> : !item ? <PageState>Daten werden geladen …</PageState> : <><dl className="detail-grid"><div><dt>Typ</dt><dd>{item.type}</dd></div><div><dt>E-Mail</dt><dd>{item.email ?? "–"}</dd></div><div><dt>Telefon</dt><dd>{item.phone ?? "–"}</dd></div><div><dt>Ort</dt><dd>{[item.postalCode, item.city].filter(Boolean).join(" ") || "–"}</dd></div></dl><h2>Gebäude</h2>{item.buildings.length === 0 ? <PageState>Keine Gebäude zugeordnet.</PageState> : <div className="table-wrap"><table className="admin-table"><thead><tr><th>Nummer</th><th>Name</th><th>Rolle</th><th></th></tr></thead><tbody>{item.buildings.map(x => <tr key={x.id}><td>{x.buildingNumber}</td><td>{x.name}</td><td>{x.role}</td><td><Link className="table-link" to={`/buildings/${encodeURIComponent(x.id)}`}>Öffnen</Link></td></tr>)}</tbody></table></div>}</>}</section>;
}
