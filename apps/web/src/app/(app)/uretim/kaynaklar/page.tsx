import { ProductionResourcesWorkspaceView } from "@/features/production-resources/components/production-resources-workspace-view";
import { loadProductionResourceWorkspace } from "@/features/production-resources/server/production-resources-reader";

import { FailureStatePage } from "@/components/ui/failure-state";

export default async function ProductionResourcesPage() {
  const result = await loadProductionResourceWorkspace();

  if (result.status === "failure") {
    return (
      <div className="fact-workspace">
        <div data-testid="production-resources-error"><FailureStatePage code={"code" in result ? result.code : undefined} message={result.message} failedTitle="Bu bilgi şu anda alınamıyor" inline /></div>
      </div>
    );
  }

  return <ProductionResourcesWorkspaceView workspace={result.workspace} />;
}
