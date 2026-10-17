

import { forbidUnlessGranted } from "@/features/admin-access/server/require-current-permission";
import { MaintenanceConditionView } from "@/features/maintenance/components/maintenance-condition-view";
import { readMaintenanceConditionWorkspace } from "@/features/maintenance/server/maintenance-condition-reader";

import { FailureStatePage } from "@/components/ui/failure-state";

type MaintenanceConditionPageProps = Readonly<{
  searchParams: Promise<Readonly<{ asset?: string }>>;
}>;

export default async function MaintenanceConditionPage({ searchParams }: MaintenanceConditionPageProps) {
  await forbidUnlessGranted("maintenance.read");

  const params = await searchParams;
  const result = await readMaintenanceConditionWorkspace(params.asset);

  if (result.status === "failure") {
    return (
      <div className="production-execution-page">
        <header className="production-execution-header">
          <p>BAKIM VE ARIZA</p>
          <h1>Durum Zekâsı</h1>
        </header>
        <FailureStatePage code={"code" in result ? result.code : undefined} message={result.message} failedTitle="Durum zekâsı verisi yüklenemedi" inline />
      </div>
    );
  }

  return (
    <MaintenanceConditionView
      workspace={result.workspace}
      initialProfile={result.profile}
      initialAssetReference={params.asset ?? result.workspace.assets[0]?.assetReference}
      riskWorkspace={result.riskWorkspace}
      riskAssessment={result.riskAssessment}
      riskError={result.riskError}
      aiDraft={result.aiDraft}
      aiDraftError={result.aiDraftError}
      canManageMaintenance={result.canManageMaintenance}
    />
  );
}
