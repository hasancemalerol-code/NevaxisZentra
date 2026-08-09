import { ShieldCheck } from "lucide-react";

import { readQualityDashboard } from "@/features/quality-control/server/quality-control-reader";
import { QualityDashboardView } from "@/features/quality-control/components/quality-dashboard-view";

export default async function QualityControlPage() {
  const result = await readQualityDashboard();

  if (result.status === "failure") {
    return (
      <main className="production-execution-page">
        <header className="production-execution-header">
          <p>KALİTE KONTROL</p>
          <h1>Kalite Kontrol</h1>
        </header>
        <div role="alert" className="state-panel error-state">
          <ShieldCheck aria-hidden="true" />
          <div>
            <h3>Kalite verisi yüklenemedi</h3>
            <p>{result.message}</p>
          </div>
        </div>
      </main>
    );
  }

  return (
    <QualityDashboardView
      summary={result.summary}
      plans={result.plans}
      inspections={result.inspections}
      overview={result.overview}
      openNcrs={result.openNcrs}
      activeHoldNcrs={result.activeHoldNcrs}
      correctiveActions={result.correctiveActions}
    />
  );
}
