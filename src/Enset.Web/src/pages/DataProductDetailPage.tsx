import { useEffect, useState } from "react";
import { useParams } from "react-router";
import {
  dataProductCatalogService,
  type CatalogItem,
  type ProductPreview,
} from "../services/dataProductCatalogService";
import "../features/dataProducts/dataProducts.css";

export function DataProductDetailPage() {
  const { id = "" } = useParams();
  const [item, setItem] = useState<CatalogItem>();
  const [preview, setPreview] = useState<ProductPreview>();
  const [error, setError] = useState("");

  useEffect(() => {
    Promise.all([
      dataProductCatalogService.get(id),
      dataProductCatalogService.preview(id),
    ])
      .then(([nextItem, nextPreview]) => {
        setItem(nextItem);
        setPreview(nextPreview);
      })
      .catch((requestError: Error) => setError(requestError.message));
  }, [id]);

  const metadata = item?.metadata;
  const columns = preview?.rows.length ? Object.keys(preview.rows[0]) : [];

  return <main>
    <h1>{metadata?.germanName ?? "Datenprodukt"}</h1>
    {error && <p className="error">{error}</p>}
    {metadata && <>
      <p>{metadata.description}</p>
      <dl className="metadata-grid">
        <dt>Code</dt><dd>{metadata.code}</dd>
        <dt>Version</dt><dd>{metadata.version.major}.{metadata.version.minor}.{metadata.version.patch}</dd>
        <dt>Verantwortlich</dt><dd>{metadata.owner}</dd>
        <dt>Quelle</dt><dd>{metadata.dataSource} · {metadata.snapshotVersion}</dd>
        <dt>Eingangsdaten</dt><dd>{metadata.inputs.join(", ")}</dd>
        <dt>Verwendete Produkte</dt><dd>{metadata.usedProducts.join(", ") || "Keine"}</dd>
        <dt>Zeitraum</dt><dd>{metadata.period}</dd>
        <dt>Aggregation</dt><dd>{metadata.aggregationLevel}</dd>
        <dt>Fehlende Daten</dt><dd>{metadata.missingDataBehavior}</dd>
        <dt>Datenherkunft</dt><dd>{metadata.lineage}</dd>
        <dt>API</dt><dd><code>{metadata.apiEndpoint}</code></dd>
      </dl>
      <div className="export-actions">{metadata.supportedExports.map((format) =>
        <a key={format} href={dataProductCatalogService.exportUrl(metadata.code, format)}>
          Export {format.toUpperCase()}
        </a>)}
      </div>
      <h2>Vorschau</h2>
      {preview && <div className="table-responsive"><table>
        <thead><tr>{columns.map((column) => <th key={column}>{column}</th>)}</tr></thead>
        <tbody>{preview.rows.map((row, index) => <tr key={index}>
          {columns.map((column) => <td key={column}>{String(row[column] ?? "—")}</td>)}
        </tr>)}</tbody>
      </table></div>}
      <h2>API-Schema</h2>
      <pre>{JSON.stringify(metadata.outputSchema, null, 2)}</pre>
    </>}
  </main>;
}
