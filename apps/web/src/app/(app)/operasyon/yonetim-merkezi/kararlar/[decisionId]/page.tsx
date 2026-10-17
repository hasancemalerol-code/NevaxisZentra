import { isNotFoundFailureCode } from "@/lib/api/api-failure-classifier";
import { RecordFailureState, StatePage, StatePageLink } from "@/components/ui/state-page";

import { denyUnlessGranted } from "@/features/admin-access/server/require-current-permission";
import { FactoryDecisionDetailView } from "@/features/production-manager/components/factory-decision-detail-view";
import { readProductionManagerDecisionDetail } from "@/features/production-manager/server/production-manager-reader";
import "@/features/commercial-intelligence/components/commercial-intelligence-workspace.css";

export default async function FactoryDecisionDetailPage({
  params,
}: Readonly<{
  params: Promise<{ decisionId: string }>;
}>) {
  const authorization = await denyUnlessGranted("production-manager.read");
  if (authorization.status === "failure") {
    return (
      <StatePage
        kind={authorization.code === "authorization-denied" ? "forbidden" : "unavailable"}
        title={authorization.code === "authorization-denied" ? "Bu karara erişim yetkiniz yok" : "Yetki doğrulanamadı"}
        description={
          authorization.code === "authorization-denied"
            ? "Hesabınızın rolü bu karar kaydını görüntülemeye izin vermiyor. Erişim gerekiyorsa yöneticinizle iletişime geçin."
            : "Yetki doğrulaması şu anda yapılamıyor. Lütfen biraz sonra yeniden deneyin."
        }
        actions={<StatePageLink href="/operasyon/yonetim-merkezi" primary>Yönetim Merkezi’ne dön</StatePageLink>}
      />
    );
  }

  const { decisionId } = await params;
  const result = await readProductionManagerDecisionDetail(decisionId);
  if (result.status === "failure") {
    return (
      <RecordFailureState
        notFound={isNotFoundFailureCode("code" in result ? result.code : undefined)}
        entityLabel="karar kaydı"
        failedTitle="Karar yüklenemedi"
        message={result.message}
        backHref="/operasyon/yonetim-merkezi"
        backLabel="Yönetim Merkezi’ne dön"
      />
    );
  }

  return <FactoryDecisionDetailView decision={result.decision} />;
}
