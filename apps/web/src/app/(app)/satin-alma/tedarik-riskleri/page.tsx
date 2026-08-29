import Link from "next/link";
import { Truck } from "lucide-react";

import { forbidUnlessGranted } from "@/features/admin-access/server/require-current-permission";
import { ProcurementDeliveryRiskWorkspaceView } from "@/features/procurement-delivery-risk/components/procurement-delivery-risk-workspace-view";
import { readProcurementDeliveryRiskWorkspace } from "@/features/procurement-delivery-risk/server/procurement-delivery-risk-reader";
import "@/features/quality-control/components/quality-workspace.css";

type ProcurementDeliveryRiskPageProps = Readonly<{
  searchParams: Promise<
    Readonly<{
      assessment?: string;
      supplierReference?: string;
      status?: string;
      purchaseOrderReference?: string;
      sourceFreshness?: string;
      page?: string;
      pageSize?: string;
    }>
  >;
}>;

export default async function ProcurementDeliveryRiskPage({
  searchParams,
}: ProcurementDeliveryRiskPageProps) {
  await forbidUnlessGranted("erp.read");

  const params = await searchParams;
  const page = params.page ? Number.parseInt(params.page, 10) : undefined;
  const pageSize = params.pageSize ? Number.parseInt(params.pageSize, 10) : undefined;

  const result = await readProcurementDeliveryRiskWorkspace(params.assessment, {
    supplierReference: params.supplierReference,
    status: params.status,
    purchaseOrderReference: params.purchaseOrderReference,
    sourceFreshness: params.sourceFreshness,
    page: Number.isFinite(page) ? page : undefined,
    pageSize: Number.isFinite(pageSize) ? pageSize : undefined,
  });

  if (result.status === "failure") {
    return (
      <main className="material-registry-page">
        <header className="material-registry-header">
          <div className="material-registry-header__intro">
            <p className="material-registry-header__eyebrow">SATIN ALMA</p>
            <h1>Tedarik Riskleri</h1>
          </div>
        </header>
        <div role="alert" className="material-registry-error" data-testid="procurement-delivery-risk-server-error">
          <Truck aria-hidden="true" />
          <div>
            <h3>Tedarik risk verisi yüklenemedi</h3>
            <p>{result.message}</p>
          </div>
        </div>
      </main>
    );
  }

  return (
    <main className="material-registry-page">
      <header className="material-registry-header">
        <div className="material-registry-header__intro">
          <p className="material-registry-header__eyebrow">SATIN ALMA</p>
          <h1>Tedarik Riskleri</h1>
          <p className="material-registry-header__subtitle">
            ERP satın alma siparişleri, taahhüt tarihleri ve açık miktarlara dayalı teslimat risk değerlendirmeleri.
          </p>
        </div>
        <div className="material-registry-header__actions">
          <Link href="/erp-entegrasyonlari/satin-alma" className="material-registry-header__link">
            <Truck aria-hidden="true" />
            ERP satın alma kayıtları
          </Link>
        </div>
      </header>

      <ProcurementDeliveryRiskWorkspaceView
        workspace={result.workspace}
        initialAssessment={result.assessment}
        assessmentError={result.assessmentError}
        history={result.history}
        historyError={result.historyError}
      />
    </main>
  );
}
