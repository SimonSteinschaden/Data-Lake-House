import { useEffect, useState } from "react";
import { Link } from "react-router";
import { dataProductService } from "../features/dataProducts/dataProductService";
import type { DataProductSummary } from "../features/dataProducts/types";
import "../features/dataProducts/dataProducts.css";
export function DataProductsPage() {
  const [items, setItems] = useState<DataProductSummary[]>([]); const [error, setError] = useState("");
  useEffect(() => { dataProductService.list().then(setItems).catch(e => setError(e.message)); }, []);
  return <main><h1>Data Products</h1>{error && <p className="error">{error}</p>}<div className="product-grid">{items.map(p => <Link className="product-card" to={`/data-products/${p.id}`} key={p.id}><h2>{p.name}</h2><p>{p.code}</p><dl><dt>Kategorie</dt><dd>{p.category}</dd><dt>Scope</dt><dd>{p.scope}</dd><dt>Status</dt><dd>{p.status}</dd><dt>Letzte Version</dt><dd>{p.latestVersion ?? "–"}</dd></dl></Link>)}</div></main>;
}
