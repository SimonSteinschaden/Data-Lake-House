import type { ReactNode } from "react";
import { formatUiValue } from "../ui/uiFormat";

export const EmptyValueDisplay = ({ value }: { value?: ReactNode | null }) =>
  value === null || value === undefined || value === "" ? <>–</> : <>{value}</>;

export function CustomerDisplay({
  name,
  number,
}: {
  name?: string | null;
  number?: string | null;
}) {
  if (!name) return <>Nicht zugeordnet</>;
  return <span>{name}{number ? <small> · {number}</small> : null}</span>;
}

export function BuildingDisplay({
  name,
  number,
}: {
  name?: string | null;
  number?: string | null;
}) {
  if (!name && !number) return <>Nicht zugeordnet</>;
  return <>{name || number}{name && number ? <small> · {number}</small> : null}</>;
}

export function MeterDisplay({
  number,
  name,
}: {
  number?: string | null;
  name?: string | null;
}) {
  return <span>{number || "–"}{name ? <small> · {name}</small> : null}</span>;
}

export function QualityLevelBadge({ level }: { level?: string | null }) {
  return <span className={`quality-level quality-level--${level?.toLowerCase() || "missing"}`}>
    {formatUiValue(level)}
  </span>;
}

export function AnnualValueDisplay({
  value,
  unit,
  status,
  referenceYear,
}: {
  value?: number | null;
  unit?: string | null;
  status?: string | null;
  referenceYear?: number | null;
}) {
  if (status === "IncompleteYear") return <>Unvollständiges Jahr</>;
  if (status !== "CompleteYear" || value === null || value === undefined)
    return <>–</>;
  return <>{value.toLocaleString("de-AT")} {unit || ""}{referenceYear ? ` · ${referenceYear}` : ""}</>;
}

export function MeasurementPeriodDisplay({
  count,
  start,
  end,
}: {
  count?: number | null;
  start?: string | null;
  end?: string | null;
}) {
  const date = (x?: string | null) =>
    x ? new Intl.DateTimeFormat("de-AT").format(new Date(x)) : "–";
  return <>{count ?? 0} | {start || end ? `${date(start)}–${date(end)}` : "–"}</>;
}
