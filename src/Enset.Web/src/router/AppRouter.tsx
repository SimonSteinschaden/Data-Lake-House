import { Navigate, Route, Routes } from "react-router";
import { MainLayout } from "../layouts/MainLayout";
import { AnalyticsPage } from "../pages/AnalyticsPage";
import { BuildingsPage } from "../pages/BuildingsPage";
import { CustomersPage } from "../pages/CustomersPage";
import { DashboardPage } from "../pages/DashboardPage";
import { ImportPage } from "../pages/ImportPage";
import { NotFoundPage } from "../pages/NotFoundPage";
import { SettingsPage } from "../pages/SettingsPage";
import { DataProductsPage } from "../pages/DataProductsPage";
import { DataProductDetailPage } from "../pages/DataProductDetailPage";
import { DataQualityWarningsPage } from "../pages/DataQualityWarningsPage";
import { BuildingEnergyPage } from "../pages/BuildingEnergyPage";
import { MetersPage } from "../pages/MetersPage";
import { PlaceholderPage } from "../pages/PlaceholderPage";
import { CurationCenterPage } from "../pages/CurationCenterPage";

export function AppRouter() {
  return (
    <Routes>
      <Route element={<MainLayout />}>
        <Route index element={<DashboardPage />} />

        <Route path="imports" element={<ImportPage />} />

        <Route path="customers" element={<CustomersPage />} />
        <Route
          path="customers/:customerId"
          element={<CustomersPage />}
        />

        <Route path="buildings" element={<BuildingsPage />} />
        <Route
          path="buildings/:buildingId"
          element={<BuildingsPage />}
        />
        <Route
          path="buildings/:buildingId/energy"
          element={<BuildingEnergyPage />}
        />

        <Route path="metering-points">
          <Route
            index
            element={
              <PlaceholderPage
                title="Zählpunkte"
                description="Zählpunkte und ihre fachlichen Zuordnungen verwalten."
              />
            }
          />
        </Route>

        <Route path="meters" element={<MetersPage />} />
        <Route
          path="meters/:meterId"
          element={<MetersPage />}
        />

        <Route path="documents">
          <Route
            index
            element={
              <PlaceholderPage
                title="Dokumente"
                description="Dokumente anzeigen und fachlichen Objekten zuordnen."
              />
            }
          />
        </Route>

        <Route path="analysis/object" element={<AnalyticsPage />} />
        <Route
          path="analysis"
          element={<Navigate to="/analysis/object" replace />}
        />
        <Route
          path="analytics"
          element={<Navigate to="/analysis/object" replace />}
        />

        <Route path="reports">
          <Route
            index
            element={
              <PlaceholderPage
                title="Reports"
                description="Berichte erzeugen, anzeigen und exportieren."
              />
            }
          />
        </Route>

        <Route
          path="data-products"
          element={<DataProductsPage />}
        />
        <Route
          path="data-products/:id"
          element={<DataProductDetailPage />}
        />

        <Route path="tools">
          <Route
            path="data-quality"
            element={
              <PlaceholderPage
                title="Datenqualität"
                description="Datenqualität, Validierungsstatus und offene Probleme prüfen."
              />
            }
          />
          <Route
            path="data-quality/warnings"
            element={<DataQualityWarningsPage />}
          />
          <Route path="curation" element={<CurationCenterPage />} />
          <Route
            path="assignments"
            element={
              <PlaceholderPage
                title="Zuordnungen"
                description="Beziehungen zwischen Kunden, Gebäuden, Zählpunkten und Zählern verwalten."
              />
            }
          />
        </Route>

        <Route path="admin">
          <Route
            path="users"
            element={
              <PlaceholderPage
                title="Benutzer"
                description="Benutzer und Berechtigungen verwalten."
              />
            }
          />
          <Route
            path="system"
            element={
              <PlaceholderPage
                title="System"
                description="Systemstatus und technische Informationen anzeigen."
              />
            }
          />
        </Route>

        <Route path="settings" element={<SettingsPage />} />
      </Route>

      <Route path="*" element={<NotFoundPage />} />
    </Routes>
  );
}
