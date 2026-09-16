import { MixedProductionLinesView } from "@/features/mixed-production/components/mixed-production-lines-view";
import { loadMixedProductionLinesWorkspace } from "@/features/mixed-production/server/mixed-production-reader";

export default async function MixedProductionLinesPage() {
  const result = await loadMixedProductionLinesWorkspace();

  if (result.status === "failure") {
    return (
      <main className="production-execution-page">
        <div role="alert" className="state-panel error-state">
          <h3>Üretim hatları yüklenemedi</h3>
          <p>{result.message}</p>
        </div>
      </main>
    );
  }

  return <MixedProductionLinesView lines={result.lines} />;
}
