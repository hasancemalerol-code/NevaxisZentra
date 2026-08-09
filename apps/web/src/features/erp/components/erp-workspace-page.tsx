import { Cable } from "lucide-react";

import { ErpWorkspaceView } from "@/features/erp/components/erp-workspace-view";
import { readErpWorkspace } from "@/features/erp/server/erp-reader";

type ErpWorkspacePageProps = Readonly<{
  activeHref: string;
}>;

export async function ErpWorkspacePage({ activeHref }: ErpWorkspacePageProps) {
  const result = await readErpWorkspace();

  if (result.status === "failure") {
    return (
      <main className="production-execution-page erp-workspace-page">
        <header className="production-execution-header">
          <p>ERP ENTEGRASYONLARI</p>
          <h1>ERP Entegrasyonları</h1>
        </header>
        <div role="alert" className="state-panel error-state">
          <Cable aria-hidden="true" />
          <div>
            <h3>ERP verisi yüklenemedi</h3>
            <p>{result.message}</p>
          </div>
        </div>
      </main>
    );
  }

  return (
    <ErpWorkspaceView
      summary={result.summary}
      connections={result.connections}
      syncRuns={result.syncRuns}
      customers={result.customers}
      products={result.products}
      activeHref={activeHref}
    />
  );
}
