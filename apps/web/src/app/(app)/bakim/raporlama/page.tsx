

import { forbidUnlessGranted } from "@/features/admin-access/server/require-current-permission";
import { MaintenanceReportingView } from "@/features/maintenance/components/maintenance-reporting-view";
import { MaintenanceWorkspaceNav } from "@/features/maintenance/components/maintenance-workspace-nav";
import "@/features/maintenance/components/maintenance-workspace.css";
import { readMaintenanceReporting } from "@/features/maintenance/server/maintenance-reader";

import { FailureStatePage } from "@/components/ui/failure-state";

export default async function MaintenanceReportingPage() {
  await forbidUnlessGranted("maintenance.read");

  const result = await readMaintenanceReporting();

  if (result.status === "failure") {
    return (
      <div className="production-execution-page maintenance-page">
        <header className="maintenance-page-header production-execution-header">
          <div className="maintenance-page-header__intro">
            <p>BAKIM VE ARIZA</p>
            <h1>Raporlama</h1>
          </div>
        </header>
        <MaintenanceWorkspaceNav active="reporting" />
        <FailureStatePage code={"code" in result ? result.code : undefined} message={result.message} failedTitle="Raporlama verisi yüklenemedi" inline />
      </div>
    );
  }

  return (
    <MaintenanceReportingView
      overview={result.overview}
      report={result.report}
      failureBreakdown={result.failureBreakdown}
      range={result.range}
    />
  );
}
