import type { VersionHistory } from "./types";
import { formatUiDateTime, formatUiValue } from "../../components/ui/uiFormat";
export function DataProductVersionHistory({ versions }: { versions: VersionHistory[] }) {
  return <section><h2>Versionshistorie</h2><table><thead><tr><th>Version</th><th>Status</th><th>Erstellt</th><th>Qualität</th></tr></thead><tbody>{versions.map(v => <tr key={v.version}><td>{v.version}</td><td>{formatUiValue(v.status)}</td><td>{formatUiDateTime(v.generatedAt)}</td><td>{formatUiValue(v.quality)}</td></tr>)}</tbody></table></section>;
}
