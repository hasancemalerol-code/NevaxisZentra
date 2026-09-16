import { FactoryDecisionCenterView } from "@/features/factory-decision-center/components/factory-decision-center-view";
import { loadFactoryDecisionCenterWorkspace } from "@/features/factory-decision-center/server/factory-decision-center-reader";

export default async function FactoryDecisionCenterPage() {
  const result = await loadFactoryDecisionCenterWorkspace();

  if (result.status === "failure") {
    return (
      <main className="fact-workspace" data-testid="factory-decision-center-view">
        <header className="fact-workspace__header">
          <div>
            <p className="fact-workspace__context">FABRİKA · KARAR MERKEZİ</p>
            <h1 className="fact-workspace__title">Fabrika Karar Merkezi</h1>
          </div>
        </header>
        <div role="alert" className="state-panel error-state">{result.message}</div>
      </main>
    );
  }

  return <FactoryDecisionCenterView workspace={result.workspace} />;
}
