import { Route, Routes } from "react-router";
import { MainLayout } from "../layouts/MainLayout";
import { AnalyticsPage } from "../pages/AnalyticsPage";
import { BuildingsPage } from "../pages/BuildingsPage";
import { CustomersPage } from "../pages/CustomersPage";
import { DashboardPage } from "../pages/DashboardPage";
import { ImportPage } from "../pages/ImportPage";
import { NotFoundPage } from "../pages/NotFoundPage";
import { SettingsPage } from "../pages/SettingsPage";

export function AppRouter() {
  return (
    <Routes>
      <Route element={<MainLayout />}>
        <Route index element={<DashboardPage />} />
        <Route path="imports" element={<ImportPage />} />
        <Route path="customers" element={<CustomersPage />} />
        <Route path="buildings" element={<BuildingsPage />} />
        <Route path="analytics" element={<AnalyticsPage />} />
        <Route path="settings" element={<SettingsPage />} />
      </Route>

      <Route path="*" element={<NotFoundPage />} />
    </Routes>
  );
}