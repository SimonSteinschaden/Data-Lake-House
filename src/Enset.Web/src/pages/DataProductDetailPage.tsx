import { useCallback, useEffect, useState } from "react";
import { useParams } from "react-router";
import { BuildingEnergyDashboard } from "../features/dataProducts/BuildingEnergyDashboard";
import { DataProductVersionHistory } from "../features/dataProducts/DataProductVersionHistory";
import { dataProductService } from "../features/dataProducts/dataProductService";
import type { Availability, DataProductSummary, ProductVersion, VersionHistory } from "../features/dataProducts/types";
import "../features/dataProducts/dataProducts.css";
const today = new Date().toISOString().slice(0, 10); const monthAgo = new Date(Date.now() - 30 * 86400000).toISOString().slice(0, 10);
export function DataProductDetailPage() {
  const { id = "" } = useParams(); const [product, setProduct] = useState<DataProductSummary>(); const [latest, setLatest] = useState<ProductVersion>(); const [versions, setVersions] = useState<VersionHistory[]>([]); const [availability, setAvailability] = useState<Availability>(); const [customerId, setCustomerId] = useState(""); const [from, setFrom] = useState(monthAgo); const [to, setTo] = useState(today); const [error, setError] = useState(""); const [busy, setBusy] = useState(false);
  const reload = useCallback(() => { dataProductService.latest(id).then(setLatest).catch(() => setLatest(undefined)); dataProductService.versions(id).then(setVersions).catch(e => setError(e.message)); }, [id]);
  useEffect(() => { dataProductService.get(id).then(setProduct).catch(e => setError(e.message)); reload(); }, [id, reload]);
  const check = async () => { setError(""); try { setAvailability(await dataProductService.availability(id, customerId, new Date(from).toISOString(), new Date(to).toISOString())); } catch (e) { setError((e as Error).message); } };
  const generate = async () => { setBusy(true); setError(""); try { await dataProductService.generate(id, customerId, new Date(from).toISOString(), new Date(to).toISOString()); await reload(); await check(); } catch (e) { setError((e as Error).message); } finally { setBusy(false); } };
  return <main><h1>{product?.name ?? "Data Product"}</h1><div className="generation-form"><label>Kunden-ID<input value={customerId} onChange={e => setCustomerId(e.target.value)} /></label><label>Von<input type="date" value={from} onChange={e => setFrom(e.target.value)} /></label><label>Bis<input type="date" value={to} onChange={e => setTo(e.target.value)} /></label><button onClick={check} disabled={!customerId}>Verfügbarkeit prüfen</button><button onClick={generate} disabled={busy || !availability?.isAvailable}>{busy ? "Generiere …" : "Generieren"}</button></div>{availability && !availability.isAvailable && <p className="warning">Fehlende Daten: {availability.missingInputs.join(", ")}</p>}{error && <p className="error">{error}</p>}{latest && <BuildingEnergyDashboard version={latest} />}<DataProductVersionHistory versions={versions} /></main>;
}
