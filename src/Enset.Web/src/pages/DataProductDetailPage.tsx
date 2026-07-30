import {useEffect,useState} from "react";
import {useParams} from "react-router";
import {dataProductCatalogService,type CatalogItem,type ProductPreview} from "../services/dataProductCatalogService";
import "../features/dataProducts/dataProducts.css";
export function DataProductDetailPage(){
 const{id=""}=useParams();const[item,setItem]=useState<CatalogItem>(),[preview,setPreview]=useState<ProductPreview>(),[error,setError]=useState("");
 useEffect(()=>{Promise.all([dataProductCatalogService.get(id),dataProductCatalogService.preview(id)]).then(([a,b])=>{setItem(a);setPreview(b)}).catch(e=>setError(e.message))},[id]);
 const m=item?.metadata,columns=preview?.rows.length?Object.keys(preview.rows[0]):[];
 return <main><h1>{m?.germanName??"Data Product"}</h1>{error&&<p className="error">{error}</p>}{m&&<><p>{m.description}</p><dl className="metadata-grid"><dt>Code</dt><dd>{m.code}</dd><dt>Version</dt><dd>{m.version.major}.{m.version.minor}.{m.version.patch}</dd><dt>Owner</dt><dd>{m.owner}</dd><dt>Quelle</dt><dd>{m.dataSource} · {m.snapshotVersion}</dd><dt>Input</dt><dd>{m.inputs.join(", ")}</dd><dt>Verwendete Produkte</dt><dd>{m.usedProducts.join(", ")||"Keine"}</dd><dt>Zeitraum</dt><dd>{m.period}</dd><dt>Aggregation</dt><dd>{m.aggregationLevel}</dd><dt>Fehlende Daten</dt><dd>{m.missingDataBehavior}</dd><dt>Datenherkunft</dt><dd>{m.lineage}</dd><dt>API</dt><dd><code>{m.apiEndpoint}</code></dd></dl><div className="export-actions">{m.supportedExports.map(f=><a key={f} href={dataProductCatalogService.exportUrl(m.code,f)}>Export {f.toUpperCase()}</a>)}</div><h2>Vorschau</h2>{preview&&<div className="table-responsive"><table><thead><tr>{columns.map(c=><th key={c}>{c}</th>)}</tr></thead><tbody>{preview.rows.map((r,i)=><tr key={i}>{columns.map(c=><td key={c}>{String(r[c]??"—")}</td>)}</tr>)}</tbody></table></div>}<h2>API-Schema</h2><pre>{JSON.stringify(m.outputSchema,null,2)}</pre></>}</main>
}
