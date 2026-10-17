import { forbidUnlessGranted } from "@/features/admin-access/server/require-current-permission";


import { fetchEffectiveAccessCapabilities, hasGrantedPermission } from "@/features/admin-access/server/admin-access-effective-access";
import { MaintenanceDashboardView } from "@/features/maintenance/components/maintenance-dashboard-view";
import { readMaintenanceConditionOverviewSnapshot } from "@/features/maintenance/server/maintenance-condition-reader";
import { readMaintenanceDashboard } from "@/features/maintenance/server/maintenance-reader";

import { FailureStatePage } from "@/components/ui/failure-state";

export default async function MaintenancePage() {
  await forbidUnlessGranted("maintenance.read");
  const access = await fetchEffectiveAccessCapabilities();

  const [result, conditionResult] = await Promise.all([
    readMaintenanceDashboard(),
    readMaintenanceConditionOverviewSnapshot(),
  ]);

  if (result.status === "failure") {
    return (
      <div className="production-execution-page">
        <header className="production-execution-header">
          <p>BAKIM VE ARIZA</p>
          <h1>Bakım ve Arıza</h1>
        </header>
        <FailureStatePage code={"code" in result ? result.code : undefined} message={result.message} failedTitle="Bakım verisi yüklenemedi" inline />
      </div>
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
