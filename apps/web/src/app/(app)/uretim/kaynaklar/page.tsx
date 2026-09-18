import { ProductionResourcesWorkspaceView } from "@/features/production-resources/components/production-resources-workspace-view";
import { loadProductionResourceWorkspace } from "@/features/production-resources/server/production-resources-reader";

export default async function ProductionResourcesPage() {
  const result = await loadProductionResourceWorkspace();

  if (result.status === "failure") {
    return (
      <main className="fact-workspace">
        <div role="alert" className="commint-error-banner" data-testid="production-resources-error">
          {result.message}
        </div>
      </main>
    );
  }

  return <ProductionResourcesWorkspaceView workspace={result.workspace} />;
}
