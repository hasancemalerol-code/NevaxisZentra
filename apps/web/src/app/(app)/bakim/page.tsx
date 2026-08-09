import { Wrench } from "lucide-react";

import { MaintenanceDashboardView } from "@/features/maintenance/components/maintenance-dashboard-view";
import { readMaintenanceDashboard } from "@/features/maintenance/server/maintenance-reader";

export default async function MaintenancePage() {
  const result = await readMaintenanceDashboard();

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
      summary={result.summary}
      requests={result.requests}
      workOrders={result.workOrders}
      preventivePlans={result.preventivePlans}
    />
  );
}
