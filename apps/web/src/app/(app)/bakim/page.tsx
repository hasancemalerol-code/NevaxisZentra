import { Wrench } from "lucide-react";
import { forbidden } from "next/navigation";

import { fetchCurrentEffectiveAccess, hasGrantedPermission } from "@/features/admin-access/server/admin-access-effective-access";
import { MaintenanceDashboardView } from "@/features/maintenance/components/maintenance-dashboard-view";
import { readMaintenanceConditionOverviewSnapshot } from "@/features/maintenance/server/maintenance-condition-reader";
import { readMaintenanceDashboard } from "@/features/maintenance/server/maintenance-reader";

export default async function MaintenancePage() {
  const access = await fetchCurrentEffectiveAccess();
  if (!hasGrantedPermission(access, "maintenance.read")) {
    forbidden();
  }

  const [result, conditionResult] = await Promise.all([
    readMaintenanceDashboard(),
    readMaintenanceConditionOverviewSnapshot(),
  ]);

  if (result.status === "failure") {
    return (
      <main className="production-execution-page">
        <header className="production-execution-header">
          <p>BAKIM &amp; ARIZA</p>
          <h1>Bakım &amp; Arıza</h1>
        </header>
        <div role="alert" className="state-panel error-state">
          <Wrench aria-hidden="true" />
          <div>
            <h3>Bakım verisi yüklenemedi</h3>
            <p>{result.message}</p>
          </div>
        </div>
      </main>
    );
  }

  return (
    <MaintenanceDashboardView
      canManage={hasGrantedPermission(access, "maintenance.manage")}
      summary={result.summary}
      requests={result.requests}
      workOrders={result.workOrders}
      preventivePlans={result.preventivePlans}
      conditionWorkspace={
        conditionResult.status === "success" ? conditionResult.workspace : undefined
      }
      conditionOverviewError={
        conditionResult.status === "failure" ? conditionResult.message : undefined
      }
    />
  );
}
