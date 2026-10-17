

import { ScenarioRecoveryCreatePanel } from "@/features/scenario-recovery/components/scenario-recovery-create-panel";
import { ScenarioRecoveryWorkspaceView } from "@/features/scenario-recovery/components/scenario-recovery-workspace-view";
import { readScenarioRecoveryPage } from "@/features/scenario-recovery/server/scenario-recovery-reader";
import "@/features/scenario-recovery/scenario-recovery-workspace.css";

import { FailureStatePage } from "@/components/ui/failure-state";
import { PageHeader } from "@/components/ui/page";
import { StatePage, StatePageLink } from "@/components/ui/state-page";

type ScenarioRecoveryPageProps = Readonly<{
  searchParams: Promise<Record<string, string | string[] | undefined>>;
}>;

function readParam(value: string | string[] | undefined): string | undefined {
  return typeof value === "string" ? value : undefined;
}

export default async function ScenarioRecoveryPage({ searchParams }: ScenarioRecoveryPageProps) {
  const params = await searchParams;
  const result = await readScenarioRecoveryPage({
    runId: readParam(params.runId),
    lineId: readParam(params.lineId),
    entryPoint: readParam(params.entryPoint),
    orderReference: readParam(params.orderReference),
    constraintStationCode: readParam(params.constraintStationCode),
  });

  if (result.status === "empty") {
    return (
      <div className="commint-page" data-testid="scenario-recovery-empty">
        <PageHeader
          eyebrow="Üretim Planlama"
          title="Senaryo Kurtarma"
          description="Plan aksamalarında alternatif kurtarma seçeneklerini karşılaştırın."
        />
        <StatePage
          kind="empty"
          inline
          title="Henüz kurtarma senaryosu yok"
          description="Canlı Plan ekranında bir plan için kurtarma simülasyonu başlatıldığında senaryolar burada listelenir."
          actions={<StatePageLink href="/planlama" primary>Canlı Plana Git</StatePageLink>}
        />
      </div>
    );
  }

  if (result.status === "forbidden") {
    return (
      <div className="commint-page">
        <div data-testid="scenario-recovery-forbidden"><FailureStatePage code={"code" in result ? result.code : undefined} message={result.message} failedTitle="Senaryo kurtarma şu anda alınamıyor" backHref="/dashboard" backLabel="Ana sayfaya dön" /></div>
      </div>
    );
  }

  if (result.status === "failure") {
    return (
      <div className="commint-page">
        <div data-testid="scenario-recovery-error"><FailureStatePage code={"code" in result ? result.code : undefined} message={result.message} failedTitle="Senaryo kurtarma şu anda alınamıyor" backHref="/dashboard" backLabel="Ana sayfaya dön" /></div>
      </div>
    );
  }

  if (result.status === "ready") {
    return (
      <div className="commint-page">
        <ScenarioRecoveryCreatePanel
          lineId={result.lineId}
          entryPoint={result.entryPoint}
          orderReference={result.orderReference}
          constraintStationCode={result.constraintStationCode}
          canSimulate={result.canSimulate}
          history={result.history}
        />
      </div>
    );
  }

  return (
    <div className="fact-workspace">
      <ScenarioRecoveryWorkspaceView workspace={result.workspace} history={result.history} />
    </div>
  );
}
