import { PageFrame, PageHeader } from "@/components/ui/page";
import { RecordFailureState, StatePage, StatePageLink } from "@/components/ui/state-page";
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
      <StatePage
        kind={authorization.code === "authorization-denied" ? "forbidden" : "unavailable"}
        title={authorization.code === "authorization-denied" ? "Dosya aktarımı için yetkiniz yok" : "Yetki doğrulanamadı"}
        description={
          authorization.code === "authorization-denied"
            ? "Hesabınızın rolü dosya aktarımı çalıştırmaya izin vermiyor. Erişim gerekiyorsa yöneticinizle iletişime geçin."
            : "Yetki doğrulaması şu anda yapılamıyor. Lütfen biraz sonra yeniden deneyin."
        }
        actions={<StatePageLink href="/ayarlar/entegrasyonlar" primary>Entegrasyonlara dön</StatePageLink>}
      />
    );
  }

  const configResult = parseProductionExecutionRuntimeConfig();
  if (configResult.status !== "success") {
    return (
      <RecordFailureState notFound={false} entityLabel="bağlantı" failedTitle="Dosya aktarımı açılamadı" message={configResult.message} backHref="/ayarlar/entegrasyonlar" backLabel="Entegrasyonlara dön" />
    );
  }

  const contextResult = await resolveProductionOrderRequestContextForIncomingRequest(configResult.config);
  if (contextResult.status === "failure") {
    return (
      <RecordFailureState notFound={false} entityLabel="bağlantı" failedTitle="Dosya aktarımı açılamadı" message={contextResult.message} backHref="/ayarlar/entegrasyonlar" backLabel="Entegrasyonlara dön" />
    );
  }

  const connectors = await listIntegrationConnectors(contextResult.context);
  const connector = connectors.find((item) => item.id === connectorId);
  if (!connector) {
    return (
      <PageFrame>
        <PageHeader eyebrow="Ayarlar" title="Dosya Aktarımı" description="Dosya aktarımı yapılacak bağlantı bu çalışma alanında bulunamadı." />
        <StatePage
          kind="empty"
          inline
          title="Bağlantı bulunamadı"
          description="Bağlantı mevcut değil, kaldırılmış ya da yetki alanınızın dışında olabilir."
          actions={<StatePageLink href="/ayarlar/entegrasyonlar" primary>Entegrasyonlara dön</StatePageLink>}
        />
      </PageFrame>
    );
  }

  return (
    <div className="production-execution-page">
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
    </div>
  );
}
