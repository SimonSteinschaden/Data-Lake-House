import {useEffect,useMemo,useState} from "react";
import {Link} from "react-router";
import {dataProductCatalogService,type CatalogItem} from "../services/dataProductCatalogService";
import "../features/dataProducts/dataProducts.css";
export function DataProductsPage(){
 const[items,setItems]=useState<CatalogItem[]>([]),[error,setError]=useState(""),[search,setSearch]=useState(""),[category,setCategory]=useState("");
 useEffect(()=>{dataProductCatalogService.list().then(setItems).catch(e=>setError(e.message))},[]);
 const categories=useMemo(()=>[...new Set(items.map(x=>x.metadata.category))].sort(),[items]);
 const visible=items.filter(x=>(!search||`${x.metadata.germanName} ${x.metadata.description} ${x.metadata.code}`.toLowerCase().includes(search.toLowerCase()))&&(!category||x.metadata.category===category));
 return <main><h1>Data Products</h1><p>Versionierte, reproduzierbare Ausgabemodelle auf Basis kanonischer Snapshots.</p><div className="catalog-filters"><input aria-label="Data Products suchen" placeholder="Suchen …" value={search} onChange={e=>setSearch(e.target.value)}/><select aria-label="Kategorie filtern" value={category} onChange={e=>setCategory(e.target.value)}><option value="">Alle Kategorien</option>{categories.map(x=><option key={x}>{x}</option>)}</select></div>{error&&<p className="error">{error}</p>}<div className="product-grid">{visible.map(({metadata:p,lastUpdatedUtc})=><Link className="product-card" to={`/data-products/${p.code}`} key={p.code}><small>{p.category}</small><h2>{p.germanName}</h2><p>{p.description}</p><dl><dt>Version</dt><dd>{p.version.major}.{p.version.minor}.{p.version.patch}</dd><dt>Qualität</dt><dd>{p.qualityLevel}</dd><dt>Suitability</dt><dd>{p.suitability}</dd><dt>Aktualisiert</dt><dd>{new Date(lastUpdatedUtc).toLocaleString("de-AT")}</dd></dl></Link>)}</div></main>
}
