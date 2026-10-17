import { FactoryIntelligenceView } from "@/features/factory-intelligence/components/factory-intelligence-view";
import { loadFactoryIntelligenceWorkspace } from "@/features/factory-intelligence/server/factory-intelligence-reader";

import { FailureStatePage } from "@/components/ui/failure-state";

export default async function FactoryIntelligencePage() {
  const result = await loadFactoryIntelligenceWorkspace();

  if (result.status === "failure") {
    return (
      <div className="fact-workspace" data-testid="factory-intelligence-view">
        <header className="fact-workspace__header">
          <div>
            <p className="fact-workspace__context">FABRİKA ZEKÂSI</p>
            <h1 className="fact-workspace__title">Fabrika Zekâsı</h1>
          </div>
        </header>
        <FailureStatePage code={"code" in result ? result.code : undefined} message={result.message} failedTitle="Bu bilgi şu anda alınamıyor" inline />
      </div>
    );
  }

  return <FactoryIntelligenceView overview={result.overview} risks={result.risks} />;
}
