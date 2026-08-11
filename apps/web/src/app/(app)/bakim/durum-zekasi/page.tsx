import { Activity } from "lucide-react";

import { MaintenanceConditionView } from "@/features/maintenance/components/maintenance-condition-view";
import { readMaintenanceConditionWorkspace } from "@/features/maintenance/server/maintenance-condition-reader";

type MaintenanceConditionPageProps = Readonly<{
  searchParams: Promise<Readonly<{ asset?: string }>>;
}>;

export default async function MaintenanceConditionPage({ searchParams }: MaintenanceConditionPageProps) {
  const params = await searchParams;
  const result = await readMaintenanceConditionWorkspace(params.asset);

  if (result.status === "failure") {
    return (
      <main className="production-execution-page">
        <header className="production-execution-header">
          <p>BAKIM &amp; ARIZA</p>
          <h1>Kestirici Bakım</h1>
        </header>
        <div role="alert" className="state-panel error-state">
          <Activity aria-hidden="true" />
          <div>
            <h3>Durum zekâsı verisi yüklenemedi</h3>
            <p>{result.message}</p>
          </div>
        </div>
      </main>
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
    />
  );
}
