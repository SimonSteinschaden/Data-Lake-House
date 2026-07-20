import { Route, Routes } from "react-router";
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
import { BuildingEnergyPage } from "../pages/BuildingEnergyPage";

export function AppRouter() {
  return (
    <Routes>
      <Route element={<MainLayout />}>
        <Route index element={<DashboardPage />} />
        <Route path="imports" element={<ImportPage />} />
        <Route path="customers" element={<CustomersPage />} />
        <Route path="buildings" element={<BuildingsPage />} />
        <Route path="analytics" element={<AnalyticsPage />} />
        <Route path="data-products" element={<DataProductsPage />} />
        <Route path="data-products/:id" element={<DataProductDetailPage />} />
        <Route path="buildings/:buildingId/energy" element={<BuildingEnergyPage />} />
        <Route path="settings" element={<SettingsPage />} />
      </Route>

      <Route path="*" element={<NotFoundPage />} />
    </Routes>
  );
}
