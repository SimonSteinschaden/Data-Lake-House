import type { VersionHistory } from "./types";
export function DataProductVersionHistory({ versions }: { versions: VersionHistory[] }) {
  return <section><h2>Versionshistorie</h2><table><thead><tr><th>Version</th><th>Status</th><th>Erstellt</th><th>Qualität</th></tr></thead><tbody>{versions.map(v => <tr key={v.version}><td>{v.version}</td><td>{v.status}</td><td>{new Intl.DateTimeFormat("de-AT", { dateStyle: "medium", timeStyle: "short" }).format(new Date(v.generatedAt))}</td><td>{v.quality}</td></tr>)}</tbody></table></section>;
}
