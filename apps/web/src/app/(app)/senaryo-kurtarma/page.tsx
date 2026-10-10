import { ShieldAlert } from "lucide-react";

import { ScenarioRecoveryCreatePanel } from "@/features/scenario-recovery/components/scenario-recovery-create-panel";
import { ScenarioRecoveryWorkspaceView } from "@/features/scenario-recovery/components/scenario-recovery-workspace-view";
import { readScenarioRecoveryPage } from "@/features/scenario-recovery/server/scenario-recovery-reader";
import "@/features/scenario-recovery/scenario-recovery-workspace.css";

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

  if (result.status === "forbidden") {
    return (
      <main className="commint-page">
        <div role="alert" className="commint-error-banner" data-testid="scenario-recovery-forbidden">
          <ShieldAlert aria-hidden="true" />
          <div>
            <h1>Senaryo Kurtarma</h1>
            <p>{result.message}</p>
          </div>
        </div>
      </main>
    );
  }

  if (result.status === "failure") {
    return (
      <main className="commint-page">
        <div role="alert" className="commint-error-banner" data-testid="scenario-recovery-error">
          <ShieldAlert aria-hidden="true" />
          <div>
            <h1>Senaryo Kurtarma</h1>
            <p>{result.message}</p>
          </div>
        </div>
      </main>
    );
  }

  if (result.status === "ready") {
    return (
      <main className="commint-page">
        <ScenarioRecoveryCreatePanel
          lineId={result.lineId}
          entryPoint={result.entryPoint}
          orderReference={result.orderReference}
          constraintStationCode={result.constraintStationCode}
          canSimulate={result.canSimulate}
          history={result.history}
        />
      </main>
    );
  }

  return (
    <main className="fact-workspace">
      <ScenarioRecoveryWorkspaceView workspace={result.workspace} history={result.history} />
    </main>
  );
}
