"use client";

import { StatusStrip } from "@/components/workspace/status-strip";
import {
  constraintTypeLabel,
  intelligencePathLabel,
  optimizerCaseStatusLabel,
  optimizerPhaseLabel,
} from "@/lib/presentation/throughput-labels";

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
    <div className="fact-workspace mlp-panel" data-testid="throughput-optimizer-panel">
      <StatusStrip
        aria-label="Verim optimizasyon durumu"
        items={[
          { label: "Aşama", value: optimizerPhaseLabel(workspace.currentPhase) },
          { label: "Vaka", value: optimizerCaseStatusLabel(workspace.caseStatus) },
          { label: "Fark", value: observation ? `${observation.gapPerHour.toFixed(0)} iyi/saat` : "—" },
          { label: "Kısıt", value: observation?.currentConstraintStationCode ?? "—" },
          { label: "İnsan Onayı", value: workspace.hasPendingApproval ? "Onay Bekliyor" : "—" },
        ]}
      />

      <section className="fact-section" aria-label="Optimizasyon aşamaları">
        <header className="fact-section__header">
          <h2 className="fact-section__title">Optimizasyon aşamaları</h2>
        </header>
        <div className="fact-section__body mlp-phases" data-testid="throughput-optimizer-phases">
          {PHASES.map((phase) => (
            <span
              key={phase}
              className="mlp-phase"
              data-current={phase === workspace.currentPhase ? "true" : "false"}
              aria-current={phase === workspace.currentPhase ? "step" : undefined}
            >
              {optimizerPhaseLabel(phase)}
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
              Kısıt: {workspace.diagnosis.currentConstraintStationCode} · {constraintTypeLabel(workspace.diagnosis.constraintType)}
            </p>
            {workspace.modelIntelligencePath ? (
              <p className="fact-workspace__meta" data-testid="model-transparency">
                Zekâ yolu: {intelligencePathLabel(workspace.modelIntelligencePath)}
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
              Tahmin: +{workspace.selection.selected.deltaGoodThroughputPerHour.toFixed(0)} iyi/saat
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
              Önce: {improvement.baseline.goodThroughputPerHour.toFixed(0)} iyi/saat · Hurda {improvement.baseline.rejectCount}
            </p>
            {improvement.actual ? (
              <p className="fact-workspace__meta">
                Sonra (ölçülen): {improvement.actual.goodThroughputPerHour.toFixed(0)} iyi/saat · Hurda {improvement.actual.rejectCount}
              </p>
            ) : (
              <p className="fact-workspace__meta">Sonra: gözlem devam ediyor</p>
            )}
            <p className="fact-workspace__meta">
              Doğrulama: {improvement.verificationOutcome ? optimizerCaseStatusLabel(improvement.verificationOutcome) : "Bekliyor"}
            </p>
          </div>
        </section>
      ) : null}

      {workspace.isVerifiedClosed ? (
        <p className="fact-workspace__meta" data-testid="verified-closed-banner" role="status">
          Doğrulanarak kapandı: verim sonucu ölçümle doğrulandı.
        </p>
      ) : null}
    </div>
  );
}
