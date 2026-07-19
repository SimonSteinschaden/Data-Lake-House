import { Outlet } from "react-router";
import { MainNavigation } from "../components/navigation/MainNavigation";
import "./MainLayout.css";

export function MainLayout() {
  return (
    <div className="main-layout">
      <header className="main-layout__header">
        <div className="main-layout__brand">
          <span className="main-layout__brand-name">ENSET</span>
          <span className="main-layout__brand-product">
            Data Platform
          </span>
        </div>

        <div className="main-layout__user">
          <span className="main-layout__user-label">
            MVP-Benutzer
          </span>
        </div>
      </header>

      <aside className="main-layout__sidebar">
        <MainNavigation />
      </aside>

      <main className="main-layout__content">
        <Outlet />
      </main>
    </div>
  );
}