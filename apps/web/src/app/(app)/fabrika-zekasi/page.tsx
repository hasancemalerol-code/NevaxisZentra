import { FactoryIntelligenceView } from "@/features/factory-intelligence/components/factory-intelligence-view";
import { loadFactoryIntelligenceWorkspace } from "@/features/factory-intelligence/server/factory-intelligence-reader";

export default async function FactoryIntelligencePage() {
  const result = await loadFactoryIntelligenceWorkspace();

  if (result.status === "failure") {
    return (
      <main className="fact-workspace" data-testid="factory-intelligence-view">
        <header className="fact-workspace__header">
          <div>
            <p className="fact-workspace__context">FABRİKA · ZEKÂ</p>
            <h1 className="fact-workspace__title">Fabrika Zekâsı</h1>
          </div>
        </header>
        <div role="alert" className="state-panel error-state">{result.message}</div>
      </main>
    );
  }

  return <FactoryIntelligenceView overview={result.overview} risks={result.risks} />;
}
