import { forbidUnlessGranted } from "@/features/admin-access/server/require-current-permission";
import { MaintenancePredictiveRiskWorkspaceView } from "@/features/maintenance/components/maintenance-predictive-risk-workspace-view";
import { readMaintenancePredictiveRiskWorkspace } from "@/features/maintenance/server/maintenance-predictive-risk-reader";

import { FailureStatePage } from "@/components/ui/failure-state";

type MaintenancePredictiveRiskPageProps = Readonly<{
  searchParams: Promise<Readonly<{ asset?: string; assessment?: string }>>;
}>;

export default async function MaintenancePredictiveRiskPage({ searchParams }: MaintenancePredictiveRiskPageProps) {
  await forbidUnlessGranted("maintenance.read");

  const params = await searchParams;
  const result = await readMaintenancePredictiveRiskWorkspace(params.asset, params.assessment);

  if (result.status === "failure") {
    return (
      <div className="production-execution-page">
        <header className="production-execution-header">
          <p>BAKIM VE ARIZA</p>
          <h1>Kestirimci Riskler</h1>
        </header>
        <FailureStatePage code={"code" in result ? result.code : undefined} message={result.message} failedTitle="Bu bilgi şu anda alınamıyor" inline />
      </div>
    );
  }

  return (
    <MaintenancePredictiveRiskWorkspaceView
      workspace={result.workspace}
      initialAssessment={result.assessment}
      initialAssetReference={params.asset ?? result.assessment?.assetReference ?? result.workspace.assets[0]?.assetReference}
      assessmentError={result.assessmentError}
    />
  );
}
