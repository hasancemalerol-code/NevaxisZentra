

import { ErpWorkspaceView } from "@/features/erp/components/erp-workspace-view";
import { readErpWorkspace } from "@/features/erp/server/erp-reader";

import { ErpFailurePage } from "./erp-failure-page";

type ErpWorkspacePageProps = Readonly<{
  activeHref: string;
}>;

export async function ErpWorkspacePage({ activeHref }: ErpWorkspacePageProps) {
  const result = await readErpWorkspace();

  if (result.status === "failure") {
    return (
      <ErpFailurePage
        title="ERP Entegrasyonları"
        description="ERP bağlantılarınızı, veri akışını ve kontrollü ERP işlemlerini tek merkezden yönetin."
        listHref={activeHref}
        listLabel="Bağlantılara Dön"
        failedTitle="ERP verisi şu anda alınamıyor"
        code={"code" in result ? result.code : undefined}
        message={result.message}
      />
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
