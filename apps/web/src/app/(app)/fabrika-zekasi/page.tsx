import { FactoryIntelligenceView } from "@/features/factory-intelligence/components/factory-intelligence-view";
import { loadFactoryIntelligenceWorkspace } from "@/features/factory-intelligence/server/factory-intelligence-reader";

export default async function FactoryIntelligencePage() {
  const result = await loadFactoryIntelligenceWorkspace();

  if (result.status === "failure") {
    return (
      <main className="commint-page">
        <div className="commint-error-banner">{result.message}</div>
      </main>
    );
  }

  return <FactoryIntelligenceView overview={result.overview} risks={result.risks} />;
}
