import { CalendarClock } from "lucide-react";

import {
  fetchCurrentEffectiveAccess,
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

export default async function MaintenancePlanningPage() {
  await forbidUnlessGranted("maintenance.read");
  const access = await fetchCurrentEffectiveAccess();

  const result = await readMaintenancePlanning();

  if (result.status === "failure") {
    return (
      <main className="production-execution-page maintenance-page">
        <header className="maintenance-page-header production-execution-header">
          <div className="maintenance-page-header__intro">
            <p>BAKIM &amp; ARIZA</p>
            <h1>Bakım Planlama</h1>
          </div>
        </header>
        <MaintenanceWorkspaceNav active="planning" />
        <div role="alert" className="maintenance-error-panel">
          <CalendarClock aria-hidden="true" />
          <div>
            <h3>Planlama verisi yüklenemedi</h3>
            <p>{result.message}</p>
          </div>
        </div>
      </main>
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
