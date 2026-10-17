

import {
  fetchEffectiveAccessCapabilities,
  hasGrantedPermission,
} from "@/features/admin-access/server/admin-access-effective-access";
import { forbidUnlessGranted } from "@/features/admin-access/server/require-current-permission";
import { MaintenancePlanningView } from "@/features/maintenance/components/maintenance-planning-view";
import "@/features/maintenance/components/maintenance-workspace.css";
import { readMaintenancePlanning } from "@/features/maintenance/server/maintenance-reader";
import {
  createWorkOrderFromPreventiveDueAction,
  generatePreventiveNextDueAction,
} from "@/features/maintenance/server/maintenance-writer";
import { MaintenanceWorkspaceNav } from "@/features/maintenance/components/maintenance-workspace-nav";

import { FailureStatePage } from "@/components/ui/failure-state";

export default async function MaintenancePlanningPage() {
  await forbidUnlessGranted("maintenance.read");
  const access = await fetchEffectiveAccessCapabilities();

  const result = await readMaintenancePlanning();

  if (result.status === "failure") {
    return (
      <div className="production-execution-page maintenance-page">
        <header className="maintenance-page-header production-execution-header">
          <div className="maintenance-page-header__intro">
            <p>BAKIM VE ARIZA</p>
            <h1>Bakım Planlama</h1>
          </div>
        </header>
        <MaintenanceWorkspaceNav active="planning" />
        <FailureStatePage code={"code" in result ? result.code : undefined} message={result.message} failedTitle="Planlama verisi yüklenemedi" inline />
      </div>
    );
  }

  return (
    <MaintenancePlanningView
      canManage={hasGrantedPermission(access, "maintenance.manage")}
      dues={result.dues}
      plans={result.plans}
      onGenerateNextDue={generatePreventiveNextDueAction}
      onCreateWorkOrderFromDue={createWorkOrderFromPreventiveDueAction}
    />
  );
}
