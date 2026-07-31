import { Navigate, Route, Routes } from "react-router";
import { MainLayout } from "../layouts/MainLayout";
import { AnalyticsPage } from "../pages/AnalyticsPage";
import { BuildingsPage } from "../pages/BuildingsPage";
import { CustomersPage } from "../pages/CustomersPage";
import { DashboardPage } from "../pages/DashboardPage";
import { ImportPage } from "../pages/ImportPage";
import { ExportsPage } from "../pages/ExportsPage";
import { NotFoundPage } from "../pages/NotFoundPage";
import { SettingsPage } from "../pages/SettingsPage";
import { DataProductsPage } from "../pages/DataProductsPage";
import { DataProductDetailPage } from "../pages/DataProductDetailPage";
import { DataQualityWarningsPage } from "../pages/DataQualityWarningsPage";
import { BuildingEnergyPage } from "../pages/BuildingEnergyPage";
import { MetersPage } from "../pages/MetersPage";
import { PlaceholderPage } from "../pages/PlaceholderPage";
import { CurationCenterPage } from "../pages/CurationCenterPage";
import { MeterIssueReviewPage } from "../pages/MeterIssueReviewPage";
import { ReportsPage } from "../pages/ReportsPage";
import { DataQualityPage } from "../pages/DataQualityPage";
import { AssociationsPage } from "../pages/AssociationsPage";

export function AppRouter() {
  return (
    <Routes>
      <Route element={<MainLayout />}>
        <Route index element={<DashboardPage />} />

        <Route path="imports" element={<ImportPage />} />
        <Route path="exports" element={<ExportsPage />} />

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

        <Route path="reports" element={<ReportsPage />} />

        <Route
          path="data-products"
          element={<DataProductsPage />}
        />
        <Route
          path="data-products/:id"
          element={<DataProductDetailPage />}
        />

        <Route path="tools">
          <Route path="data-quality" element={<DataQualityPage />} />
          <Route
            path="data-quality/warnings"
            element={<DataQualityWarningsPage />}
          />
          <Route path="data-review" element={<CurationCenterPage />} />
          <Route path="data-review/meter-issues" element={<MeterIssueReviewPage />} />
          <Route path="data-curation" element={<Navigate to="/tools/data-review" replace />} />
          <Route path="curation" element={<Navigate to="/tools/data-review" replace />} />
          <Route path="assignments" element={<AssociationsPage />} />
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
