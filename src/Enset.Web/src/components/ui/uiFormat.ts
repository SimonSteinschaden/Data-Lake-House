const uiLabels: Record<string, string> = {
  Active: "Aktiv",
  Inactive: "Inaktiv",
  Draft: "Entwurf",
  Released: "Freigegeben",
  Revoked: "Zurückgezogen",
  Ready: "Bereit",
  ReadyWithWarnings: "Bereit mit Hinweisen",
  PartiallyReady: "Teilweise bereit",
  NotReady: "Nicht bereit",
  Completed: "Abgeschlossen",
  Committed: "Übernommen",
  Failed: "Fehlgeschlagen",
  Cancelled: "Abgebrochen",
  Interrupted: "Unterbrochen",
  Warning: "Warnung",
  Information: "Information",
  Blocking: "Blockierend",
  Gold: "Gold",
  Silver: "Silber",
  Bronze: "Bronze",
  Unknown: "Unbekannt",
  Created: "Erstellt",
  Updated: "Aktualisiert",
  Deleted: "Gelöscht",
  Restored: "Wiederhergestellt",
  Accepted: "Angenommen",
  Rejected: "Abgelehnt",
  Suitable: "Geeignet",
  NotSuitable: "Nicht geeignet",
  Customer: "Kunde",
  Building: "Gebäude",
  Meter: "Zähler",
  MeteringPoint: "Zähler",
  EnergySystem: "Energiesystem",
};

export function formatUiValue(value?: string | null, fallback = "–"): string {
  if (!value) return fallback;
  return uiLabels[value] ?? value;
}

export function formatUiDateTime(value?: string | null): string {
  if (!value) return "–";
  return new Intl.DateTimeFormat("de-AT", {
    dateStyle: "medium",
    timeStyle: "short",
  }).format(new Date(value));
}

export function formatUiNumber(value: number): string {
  return new Intl.NumberFormat("de-AT").format(value);
}
