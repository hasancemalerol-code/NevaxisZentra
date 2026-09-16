import { MixedProductionLinesView } from "@/features/mixed-production/components/mixed-production-lines-view";
import { loadMixedProductionLinesWorkspace } from "@/features/mixed-production/server/mixed-production-reader";

export default async function MixedProductionLinesPage() {
  const result = await loadMixedProductionLinesWorkspace();

  if (result.status === "failure") {
    return (
      <main className="fact-workspace" data-testid="mixed-production-lines-view">
        <header className="fact-workspace__header">
          <div>
            <p className="fact-workspace__context">ÜRETİM · KARMA HATLAR</p>
            <h1 className="fact-workspace__title">Üretim Hatları</h1>
          </div>
        </header>
        <div role="alert" className="state-panel error-state">
          <h3>Üretim hatları yüklenemedi</h3>
          <p>{result.message}</p>
        </div>
      </main>
    );
  }

  return <MixedProductionLinesView lines={result.lines} />;
}
