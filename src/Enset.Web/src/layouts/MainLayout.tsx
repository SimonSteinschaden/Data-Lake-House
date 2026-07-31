import { Outlet } from "react-router";
import { MainNavigation } from "../components/navigation/MainNavigation";
import { PageBreadcrumbs } from "../components/navigation/PageBreadcrumbs";
import "./MainLayout.css";

export function MainLayout() {
  return (
    <div className="main-layout">
      <header className="main-layout__header">
        <div className="main-layout__brand">
          <span className="main-layout__brand-name">ENSET</span>
          <span className="main-layout__brand-product">Datenplattform</span>
        </div>

        <div className="main-layout__header-actions">
          <button className="main-layout__user" type="button" aria-label="Benutzermenü">
            <span className="main-layout__avatar">MB</span>
            <span className="main-layout__user-label">MVP-Benutzer</span>
          </button>
        </div>
      </header>

      <aside className="main-layout__sidebar">
        <MainNavigation />
      </aside>

      <main className="main-layout__content">
        <PageBreadcrumbs />
        <Outlet />
      </main>
    </div>
  );
}
