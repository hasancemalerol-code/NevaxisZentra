import { IntegrationFileImportWorkbench } from "@/features/integration/components/integration-file-import-workbench";
import { denyUnlessGranted } from "@/features/admin-access/server/require-current-permission";
import { parseProductionExecutionRuntimeConfig } from "@/features/production-execution/server/production-order-runtime-config";
import { resolveProductionOrderRequestContextForIncomingRequest } from "@/features/production-execution/server/production-order-request-context";
import { listIntegrationConnectors } from "@/features/integration/server/integration-api-client";

export const dynamic = "force-dynamic";

type FileImportPageProps = Readonly<{
  params: Promise<{ connectorId: string }>;
}>;

export default async function DosyaAktarimiPage({ params }: FileImportPageProps) {
  const { connectorId } = await params;
  const authorization = await denyUnlessGranted("integration.run");
  if (authorization.status === "failure") {
    return (
      <main className="production-execution-page">
        <h1>Dosya aktarımı</h1>
        <p>Dosya aktarımı için yetkiniz yok.</p>
      </main>
    );
  }

  const configResult = parseProductionExecutionRuntimeConfig();
  if (configResult.status !== "success") {
    return <main><p>{configResult.message}</p></main>;
  }

  const contextResult = await resolveProductionOrderRequestContextForIncomingRequest(configResult.config);
  if (contextResult.status === "failure") {
    return <main><p>{contextResult.message}</p></main>;
  }

  const connectors = await listIntegrationConnectors(contextResult.context);
  const connector = connectors.find((item) => item.id === connectorId);
  if (!connector) {
    return <main><p>Bağlantı bulunamadı.</p></main>;
  }

  return (
    <main className="production-execution-page">
      <header className="production-execution-header">
        <p>ENTEGRASYONLAR</p>
        <h1>Dosya Aktarımı</h1>
      </header>
      <IntegrationFileImportWorkbench
        connectorId={connector.id}
        connectorName={connector.name}
        facilityId={contextResult.context.facilityId}
        apiBaseUrl={contextResult.context.apiBaseUrl}
      />
    </main>
  );
}
