import { ShieldAlert } from "lucide-react";

import { forbidUnlessGranted } from "@/features/admin-access/server/require-current-permission";
import { CommercialSupplierComparisonWorkspaceView } from "@/features/commercial-intelligence/components/commercial-supplier-comparison-workspace-view";
import "@/features/commercial-intelligence/components/commercial-intelligence-workspace.css";
import { readCommercialSupplierComparisonWorkspace } from "@/features/commercial-intelligence/server/commercial-supplier-comparison-queries";

type PageProps = Readonly<{
  searchParams: Promise<Readonly<{ comparison?: string }>>;
}>;

export default async function CommercialSupplierComparisonPage({ searchParams }: PageProps) {
  await forbidUnlessGranted("commercial-intelligence.read");

  const params = await searchParams;
  const result = await readCommercialSupplierComparisonWorkspace(params.comparison ?? null);

  if (result.status === "failure") {
    const isUnauthorized =
      result.code === "permission-denied" || result.code === "unauthenticated";

    return (
      <main className="commint-page">
        <header className="commint-executive">
          <div className="commint-executive__intro">
            <p className="commint-executive__eyebrow">TİCARİ ZEKÂ</p>
            <h1>Tedarikçi Karşılaştırması</h1>
            <p className="commint-executive__subtitle">
              Karşılaştırılabilir satın alma fiyat kanıtına göre tedarikçi medyan konumlarını izleyin.
            </p>
          </div>
        </header>
        <div
          role="alert"
          className={isUnauthorized ? "commint-error-banner" : "commint-partial-error"}
        >
          <ShieldAlert aria-hidden="true" />
          <div>
            <h3>{isUnauthorized ? "Bu modüle erişim yetkiniz yok" : "Tedarikçi karşılaştırma verisi yüklenemedi"}</h3>
            <p>{result.message}</p>
          </div>
        </div>
      </main>
    );
  }

  return (
    <CommercialSupplierComparisonWorkspaceView
      workspace={result.workspace}
      canManage={result.canManage}
    />
  );
}
