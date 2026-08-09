import { BarChart3 } from "lucide-react";

import { MaintenanceReportingView } from "@/features/maintenance/components/maintenance-reporting-view";
import { MaintenanceWorkspaceNav } from "@/features/maintenance/components/maintenance-workspace-nav";
import "@/features/maintenance/components/maintenance-workspace.css";
import { readMaintenanceReporting } from "@/features/maintenance/server/maintenance-reader";

export default async function MaintenanceReportingPage() {
  const result = await readMaintenanceReporting();

  if (result.status === "failure") {
    return (
      <main className="production-execution-page maintenance-page">
        <header className="maintenance-page-header production-execution-header">
          <div className="maintenance-page-header__intro">
            <p>BAKIM &amp; ARIZA</p>
            <h1>Bakım Güvenilirlik ve Analiz</h1>
          </div>
        </header>
        <MaintenanceWorkspaceNav active="reporting" />
        <div role="alert" className="maintenance-error-panel">
          <BarChart3 aria-hidden="true" />
          <div>
            <h3>Raporlama verisi yüklenemedi</h3>
            <p>{result.message}</p>
          </div>
        </div>
      </main>
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
