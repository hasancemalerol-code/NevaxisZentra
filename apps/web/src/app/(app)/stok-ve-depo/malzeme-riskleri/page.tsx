import Link from "next/link";
import { Package } from "lucide-react";

import { forbidUnlessGranted } from "@/features/admin-access/server/require-current-permission";
import { MaterialAvailabilityRiskWorkspaceView } from "@/features/material-availability-risk/components/material-availability-risk-workspace-view";
import { readMaterialAvailabilityRiskWorkspace } from "@/features/material-availability-risk/server/material-availability-risk-reader";
import "@/features/materials/components/material-registry.css";
import "@/features/quality-control/components/quality-workspace.css";
import "@/features/material-availability-risk/components/material-availability-risk.css";

import { FailureStatePage } from "@/components/ui/failure-state";
import { PageHeader } from "@/components/ui/page";

type MaterialAvailabilityRiskPageProps = Readonly<{
  searchParams: Promise<
    Readonly<{
      assessment?: string;
      materialId?: string;
      status?: string;
      page?: string;
      pageSize?: string;
    }>
  >;
}>;

export default async function MaterialAvailabilityRiskPage({
  searchParams,
}: MaterialAvailabilityRiskPageProps) {
  await forbidUnlessGranted("material.read");

  const params = await searchParams;
  const page = params.page ? Number.parseInt(params.page, 10) : undefined;
  const pageSize = params.pageSize ? Number.parseInt(params.pageSize, 10) : undefined;

  const result = await readMaterialAvailabilityRiskWorkspace(params.assessment, {
    materialId: params.materialId,
    status: params.status,
    page: Number.isFinite(page) ? page : undefined,
    pageSize: Number.isFinite(pageSize) ? pageSize : undefined,
  });

  if (result.status === "failure") {
    return (
      <div className="material-registry-page">
        <header className="material-registry-header">
          <div className="material-registry-header__intro">
            <p className="material-registry-header__eyebrow">STOK VE DEPO</p>
            <h1>Malzeme Riskleri</h1>
          </div>
        </header>
        <div data-testid="material-availability-risk-server-error"><FailureStatePage code={"code" in result ? result.code : undefined} message={result.message} failedTitle="Malzeme risk verisi yüklenemedi" inline /></div>
      </div>
    );
  }

  return (
    <div className="material-registry-page">
      <PageHeader
        eyebrow="Stok ve Depo"
        title="Malzeme Riskleri"
        description="ERP stok ve malzeme ihtiyaç profillerine dayalı kullanılabilirlik riski değerlendirmeleri."
        actions={(
          <Link href="/malzemeler" className="material-registry-header__link">
            <Package aria-hidden="true" />
            Malzeme kayıtları
          </Link>
        )}
      />

      <MaterialAvailabilityRiskWorkspaceView
        workspace={result.workspace}
        initialAssessment={result.assessment}
        assessmentError={result.assessmentError}
        history={result.history}
        historyError={result.historyError}
      />
    </div>
  );
}
