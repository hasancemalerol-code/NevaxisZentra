import { forbidUnlessGranted } from "@/features/admin-access/server/require-current-permission";
import { MaintenancePredictiveRiskWorkspaceView } from "@/features/maintenance/components/maintenance-predictive-risk-workspace-view";
import { readMaintenancePredictiveRiskWorkspace } from "@/features/maintenance/server/maintenance-predictive-risk-reader";

type MaintenancePredictiveRiskPageProps = Readonly<{
  searchParams: Promise<Readonly<{ asset?: string; assessment?: string }>>;
}>;

export default async function MaintenancePredictiveRiskPage({ searchParams }: MaintenancePredictiveRiskPageProps) {
  await forbidUnlessGranted("maintenance.read");

  const params = await searchParams;
  const result = await readMaintenancePredictiveRiskWorkspace(params.asset, params.assessment);

  if (result.status === "failure") {
    return (
      <main className="production-execution-page">
        <header className="production-execution-header">
          <p>BAKIM &amp; ARIZA</p>
          <h1>Kestirimci Bakım Riskleri</h1>
        </header>
        <div role="alert" className="state-panel error-state">
          <p>{result.message}</p>
        </div>
      </main>
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
