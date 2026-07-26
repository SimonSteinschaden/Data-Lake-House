import { ApiError } from "../../api";

export const displayDate = (value: string | null): string => value
  ? new Intl.DateTimeFormat("de-AT", { dateStyle: "medium", timeStyle: "short" }).format(new Date(value))
  : "–";

export const displayNumber = (value: number): string =>
  new Intl.NumberFormat("de-AT").format(value);

export const errorMessage = (error: unknown): string => {
  if (error instanceof ApiError) {
    if (error.status === 401) return "Bitte anmelden, um diese Daten aufzurufen.";
    if (error.status === 403) return "Für diesen Bereich fehlt die Berechtigung.";
    if (error.status === 404) return "Der angeforderte Datensatz wurde nicht gefunden.";
    return error.message;
  }
  return error instanceof Error ? error.message : "Die Daten konnten nicht geladen werden.";
};
