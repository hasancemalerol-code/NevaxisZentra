"use client";

import { StatusStrip } from "@/components/workspace/status-strip";

import type { ThroughputOptimizerWorkspaceApi } from "../server/throughput-optimizer-api-client";

const PHASES = [
  "OBSERVE",
  "DIAGNOSE",
  "SIMULATE",
  "DECIDE",
  "APPROVAL",
  "EXECUTE",
  "OBSERVE_RESULT",
  "VERIFY",
  "CLOSED",
] as const;

type ThroughputOptimizerPanelProps = Readonly<{
  workspace: ThroughputOptimizerWorkspaceApi;
}>;

export function ThroughputOptimizerPanel({ workspace }: ThroughputOptimizerPanelProps) {
  const observation = workspace.observation;
  const improvement = workspace.improvement;

  return (
    <div className="fact-workspace" data-testid="throughput-optimizer-panel">
      <StatusStrip
        aria-label="Verim optimizasyon durumu"
        items={[
          { label: "Faz", value: workspace.currentPhase },
          { label: "Vaka", value: workspace.caseStatus },
          { label: "Gap", value: observation ? `${observation.gapPerHour.toFixed(0)} GOOD/saat` : "—" },
          { label: "Kısıt", value: observation?.currentConstraintStationCode ?? "—" },
          { label: "Onay", value: workspace.hasPendingApproval ? "BEKLİYOR" : "—" },
        ]}
      />

      <section className="fact-section" aria-label="Worker fazı">
        <header className="fact-section__header">
          <h2 className="fact-section__title">Worker fazı</h2>
        </header>
        <div className="fact-section__body" data-testid="throughput-optimizer-phases">
          {PHASES.map((phase) => (
            <span
              key={phase}
              className={phase === workspace.currentPhase ? "fact-workspace__meta" : "fact-workspace__meta"}
              style={{ opacity: phase === workspace.currentPhase ? 1 : 0.5, marginRight: "0.75rem" }}
            >
              {phase}
            </span>
          ))}
        </div>
      </section>

      {workspace.diagnosis ? (
        <section className="fact-section" aria-label="Teşhis">
          <header className="fact-section__header">
            <h2 className="fact-section__title">Teşhis</h2>
          </header>
          <div className="fact-section__body">
            <p className="fact-workspace__meta">{workspace.diagnosis.rootCauseSummary}</p>
            <p className="fact-workspace__meta">
              Kısıt: {workspace.diagnosis.currentConstraintStationCode} · {workspace.diagnosis.constraintType}
            </p>
            {workspace.modelIntelligencePath ? (
              <p className="fact-workspace__meta" data-testid="model-transparency">
                Intelligence path: {workspace.modelIntelligencePath}
              </p>
            ) : null}
          </div>
        </section>
      ) : null}

      {workspace.selection ? (
        <section className="fact-section" aria-label="Seçilen müdahale">
          <header className="fact-section__header">
            <h2 className="fact-section__title">Seçilen müdahale</h2>
          </header>
          <div className="fact-section__body" data-testid="selected-intervention">
            <p className="fact-workspace__meta">{workspace.selection.selected.title}</p>
            <p className="fact-workspace__meta">
              Tahmin: +{workspace.selection.selected.deltaGoodThroughputPerHour.toFixed(0)} GOOD/saat
            </p>
          </div>
        </section>
      ) : null}

      {improvement ? (
        <section className="fact-section" aria-label="Önce / Sonra">
          <header className="fact-section__header">
            <h2 className="fact-section__title">Önce / Sonra</h2>
          </header>
          <div className="fact-section__body" data-testid="before-after-comparison">
            <p className="fact-workspace__meta">
              ÖNCE: {improvement.baseline.goodThroughputPerHour.toFixed(0)} GOOD/saat · REJECT {improvement.baseline.rejectCount}
            </p>
            {improvement.actual ? (
              <p className="fact-workspace__meta">
                SONRA (ölçülen): {improvement.actual.goodThroughputPerHour.toFixed(0)} GOOD/saat · REJECT {improvement.actual.rejectCount}
              </p>
            ) : (
              <p className="fact-workspace__meta">SONRA: gözlem devam ediyor</p>
            )}
            <p className="fact-workspace__meta">Doğrulama: {improvement.verificationOutcome || "BEKLİYOR"}</p>
          </div>
        </section>
      ) : null}

      {workspace.isVerifiedClosed ? (
        <p className="fact-workspace__meta" data-testid="verified-closed-banner" role="status">
          VERIFIED CLOSED — throughput outcome doğrulandı.
        </p>
      ) : null}
    </div>
  );
}
