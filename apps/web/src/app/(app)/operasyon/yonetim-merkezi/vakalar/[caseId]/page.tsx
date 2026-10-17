import { isNotFoundFailureCode } from "@/lib/api/api-failure-classifier";
import { RecordFailureState, StatePage, StatePageLink } from "@/components/ui/state-page";

import { denyUnlessGranted } from "@/features/admin-access/server/require-current-permission";
import { FactoryCaseDetailView } from "@/features/factory-orchestration/components/factory-case-detail-view";
import { readFactoryCaseDetail } from "@/features/factory-orchestration/server/factory-orchestration-reader";
import { deferredSurfacesEnabled } from "@/lib/runtime/deferred-surfaces";
import "@/features/commercial-intelligence/components/commercial-intelligence-workspace.css";

export default async function FactoryCaseDetailPage({
  params,
}: Readonly<{
  params: Promise<{ caseId: string }>;
}>) {
  const authorization = await denyUnlessGranted("factory-case.read");
  if (authorization.status === "failure") {
    return (
      <StatePage
        kind={authorization.code === "authorization-denied" ? "forbidden" : "unavailable"}
        title={authorization.code === "authorization-denied" ? "Bu vakaya erişim yetkiniz yok" : "Yetki doğrulanamadı"}
        description={
          authorization.code === "authorization-denied"
            ? "Hesabınızın rolü bu fabrika vakasını görüntülemeye izin vermiyor. Erişim gerekiyorsa yöneticinizle iletişime geçin."
            : "Yetki doğrulaması şu anda yapılamıyor. Lütfen biraz sonra yeniden deneyin."
        }
        actions={<StatePageLink href="/operasyon/yonetim-merkezi" primary>Yönetim Merkezi’ne dön</StatePageLink>}
      />
    );
  }

  const { caseId } = await params;
  const result = await readFactoryCaseDetail(caseId);
  if (result.status === "failure") {
    return (
      <RecordFailureState
        notFound={isNotFoundFailureCode("code" in result ? result.code : undefined)}
        entityLabel="fabrika vakası"
        failedTitle="Fabrika vakası yüklenemedi"
        message={result.message}
        backHref="/operasyon/yonetim-merkezi"
        backLabel="Yönetim Merkezi’ne dön"
      />
    );
  }

  return <FactoryCaseDetailView detail={result.detail} showWorkforce={deferredSurfacesEnabled()} />;
}
