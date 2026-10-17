

import { forbidUnlessGranted } from "@/features/admin-access/server/require-current-permission";
import { CommercialSupplierComparisonWorkspaceView } from "@/features/commercial-intelligence/components/commercial-supplier-comparison-workspace-view";
import "@/features/commercial-intelligence/components/commercial-intelligence-workspace.css";
import { readCommercialSupplierComparisonWorkspace } from "@/features/commercial-intelligence/server/commercial-supplier-comparison-queries";

import { FailureStatePage } from "@/components/ui/failure-state";

type PageProps = Readonly<{
  searchParams: Promise<Readonly<{ comparison?: string }>>;
}>;

export default async function CommercialSupplierComparisonPage({ searchParams }: PageProps) {
  await forbidUnlessGranted("commercial-intelligence.read");

  const params = await searchParams;
  const result = await readCommercialSupplierComparisonWorkspace(params.comparison ?? null);

  if (result.status === "failure") {
    return (
      <div className="commint-page">
        <header className="commint-executive">
          <div className="commint-executive__intro">
            <p className="commint-executive__eyebrow">TİCARİ ZEKÂ</p>
            <h1>Tedarikçi Karşılaştırması</h1>
            <p className="commint-executive__subtitle">
              Karşılaştırılabilir satın alma fiyat kanıtına göre tedarikçi medyan konumlarını izleyin.
            </p>
          </div>
        </header>
        <FailureStatePage code={"code" in result ? result.code : undefined} message={result.message} failedTitle="Tedarikçi karşılaştırma verisi yüklenemedi" inline />
      </div>
    );
  }

  return (
    <CommercialSupplierComparisonWorkspaceView
      workspace={result.workspace}
      canManage={result.canManage}
    />
  );
}
