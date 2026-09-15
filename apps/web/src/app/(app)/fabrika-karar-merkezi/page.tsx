import { FactoryDecisionCenterView } from "@/features/factory-decision-center/components/factory-decision-center-view";
import { loadFactoryDecisionCenterWorkspace } from "@/features/factory-decision-center/server/factory-decision-center-reader";

export default async function FactoryDecisionCenterPage() {
  const result = await loadFactoryDecisionCenterWorkspace();

  if (result.status === "failure") {
    return (
      <main className="commint-page">
        <div className="commint-error-banner">{result.message}</div>
      </main>
    );
  }

  return <FactoryDecisionCenterView workspace={result.workspace} />;
}
