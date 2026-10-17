

import { readQualityDashboard } from "@/features/quality-control/server/quality-control-reader";
import { QualityDashboardView } from "@/features/quality-control/components/quality-dashboard-view";

import { FailureStatePage } from "@/components/ui/failure-state";

export default async function QualityControlPage() {
  const result = await readQualityDashboard();

  if (result.status === "failure") {
    return (
      <div className="production-execution-page">
        <header className="production-execution-header">
          <p>KALİTE KONTROL</p>
          <h1>Kalite Kontrol</h1>
        </header>
        <FailureStatePage code={"code" in result ? result.code : undefined} message={result.message} failedTitle="Kalite verisi yüklenemedi" inline />
      </div>
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
