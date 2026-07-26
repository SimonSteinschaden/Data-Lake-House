import type { ReactNode } from "react";

export function AdminPageHeader({ title, description }: { title: string; description: string }) {
  return <header className="admin-header"><h1>{title}</h1><p>{description}</p></header>;
}

export function StatusBadge({ active }: { active: boolean }) {
  return <span className={`status-badge ${active ? "status-badge--active" : "status-badge--inactive"}`}>{active ? "Aktiv" : "Inaktiv"}</span>;
}

export function PageState({ children }: { children: ReactNode }) {
  return <div className="page-state">{children}</div>;
}

export function Pagination({ page, totalPages, onPage }: { page: number; totalPages: number; onPage: (page: number) => void }) {
  return <nav className="pagination" aria-label="Seitennavigation"><button disabled={page <= 1} onClick={() => onPage(page - 1)}>Zurück</button><span>Seite {page} von {Math.max(1, totalPages)}</span><button disabled={totalPages === 0 || page >= totalPages} onClick={() => onPage(page + 1)}>Weiter</button></nav>;
}
