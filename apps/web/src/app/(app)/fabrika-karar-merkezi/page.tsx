import { FactoryDecisionCenterView } from "@/features/factory-decision-center/components/factory-decision-center-view";
import { loadFactoryDecisionCenterWorkspace } from "@/features/factory-decision-center/server/factory-decision-center-reader";
import { FailureStatePage } from "@/components/ui/failure-state";
import { deferredSurfacesEnabled } from "@/lib/runtime/deferred-surfaces";

export default async function FactoryDecisionCenterPage() {
  const result = await loadFactoryDecisionCenterWorkspace();

  if (result.status === "failure") {
    return (
      <div className="fact-workspace" data-testid="factory-decision-center-view">
        <header className="fact-workspace__header">
          <div>
            <p className="fact-workspace__context">FABRİKA · KARAR MERKEZİ</p>
            <h1 className="fact-workspace__title">Fabrika Karar Merkezi</h1>
          </div>
        </header>
        <FailureStatePage
          code={result.code}
          message={result.message}
          failedTitle="Karar merkezi şu anda yüklenemedi"
          backHref="/dashboard"
          backLabel="Ana sayfaya dön"
          inline
        />
      </div>
    );
  }

  return <FactoryDecisionCenterView workspace={result.workspace} scenarioResetEnabled={deferredSurfacesEnabled()} showWorkforce={deferredSurfacesEnabled()} />;
}
