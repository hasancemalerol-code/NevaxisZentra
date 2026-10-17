"use client";

import Link from "next/link";
import { useMemo } from "react";

import { ContextPanel } from "@/components/workspace/context-panel";
import { StatusStrip } from "@/components/workspace/status-strip";
import { WorkspaceHeader } from "@/components/workspace/workspace-header";
import { FactoryCopilotWorkspaceActions } from "@/features/factory-copilot/components/factory-copilot-workspace-actions";

import { buildWorksCaseHref } from "../application/scenario-recovery-links";
import type { ScenarioRecoveryHistoryItemApi, ScenarioRecoveryWorkspaceApi } from "../server/scenario-recovery-api-client";

type ScenarioRecoveryWorkspaceViewProps = Readonly<{
  workspace: ScenarioRecoveryWorkspaceApi;
  history: readonly ScenarioRecoveryHistoryItemApi[];
}>;

function formatNumber(value: number | undefined, digits = 0): string {
  if (value == null || Number.isNaN(value)) return "—";
  return value.toFixed(digits);
}

function phaseLabel(phase: string): string {
  const labels: Record<string, string> = {
    DRAFT: "Taslak",
    SNAPSHOT_CAPTURED: "Anlık görüntü",
    OPTIONS_GENERATED: "Seçenekler",
    COMPARED: "Karşılaştırma",
    RECOMMENDED: "Öneri hazır",
    SELECTED: "Seçildi",
    APPROVAL: "Onay bekliyor",
    APPLIED: "Uygulandı",
    OBSERVING: "Gözlem",
    VERIFIED: "Doğrulandı",
    CLOSED: "Kapatıldı",
  };
  return labels[phase] ?? phase;
}

export function ScenarioRecoveryWorkspaceView({ workspace, history }: ScenarioRecoveryWorkspaceViewProps) {
  const comparison = workspace.comparison;
  const recommended = comparison?.recommendation.recommendedOptionLabel;
  const selected = workspace.selectedOptionLabel ?? recommended;

  const constraintRows = useMemo(() => {
    if (!comparison) return [];
    return [
      { label: "Mevcut darboğaz", value: comparison.predictedConstraintBefore ?? "—" },
      { label: "Öneri sonrası", value: comparison.predictedConstraintAfterRecommended ?? "—" },
    ];
  }, [comparison]);

  const problemRows = [
    { label: "Problem", value: workspace.problem.summary },
    { label: "Hat", value: workspace.lineCode },
    { label: "Emir", value: workspace.problem.orderReference ?? "—" },
    { label: "Darboğaz", value: workspace.problem.constraintStationCode ?? "—" },
    { label: "Baseline GOOD/h", value: formatNumber(workspace.problem.baselineGoodThroughputPerHour) },
    { label: "Hedef GOOD/h", value: formatNumber(workspace.problem.targetGoodThroughputPerHour) },
  ];

  const objectiveRows = workspace.objectives.map((objective) => ({
    label: objective.label,
    value: objective.isHardConstraint ? "Sert kısıt" : "Hedef",
  }));

  return (
    <div className="scenario-recovery-workspace" data-testid="scenario-recovery-workspace">
      <WorkspaceHeader
        context="SENARYO / KURTARMA"
        title={workspace.problem.summary}
        meta={`${workspace.lineCode} · ${phaseLabel(workspace.currentPhase)} · ${workspace.executionState}`}
        actions={<FactoryCopilotWorkspaceActions />}
      />

      <StatusStrip
        aria-label="Senaryo durumu"
        items={[
          { label: "Faz", value: phaseLabel(workspace.currentPhase) },
          { label: "Durum", value: workspace.executionState },
          { label: "Giriş", value: workspace.entryPoint },
          { label: "Öneri", value: recommended ?? "—" },
          { label: "Seçim", value: selected ?? "—" },
          { label: "Onay", value: workspace.hasPendingApproval ? "Bekliyor" : "—" },
        ]}
      />

      <div className="scenario-recovery-workspace__grid">
        <aside className="scenario-recovery-workspace__left" data-testid="scenario-recovery-constraints">
          <section className="fact-section">
            <header className="fact-section__header">
              <h2 className="fact-section__title">Problem / Hedef</h2>
            </header>
            <div className="fact-section__body">
              <ContextPanel title="Operasyonel soru" rows={problemRows} />
              <ContextPanel title="Hedefler" rows={objectiveRows} />
            </div>
          </section>

          <section className="fact-section">
            <header className="fact-section__header">
              <h2 className="fact-section__title">Kısıtlar / Varsayımlar</h2>
            </header>
            <div className="fact-section__body">
              <ContextPanel title="Darboğaz hareketi" rows={constraintRows} />
              {workspace.snapshot ? (
                <ContextPanel
                  title="Anlık görüntü"
                  rows={[
                    { label: "Veri kalitesi", value: workspace.snapshot.dataQuality },
                    { label: "Yakalandı", value: new Date(workspace.snapshot.capturedAtUtc).toLocaleString("tr-TR") },
                    ...workspace.snapshot.assumptions.map((assumption, index) => ({
                      label: `Varsayım ${index + 1}`,
                      value: assumption,
                    })),
                  ]}
                />
              ) : null}
            </div>
          </section>
        </aside>

        <div className="scenario-recovery-workspace__center" data-testid="scenario-recovery-comparison">
          <section className="fact-section">
            <header className="fact-section__header">
              <h2 className="fact-section__title">Baseline + Seçenek A/B/C</h2>
            </header>
            <div className="fact-section__body">
              {comparison ? (
                <div className="fact-scenario-compare">
                  {comparison.options.map((option) => (
                    <article
                      key={option.optionLabel}
                      className="fact-scenario-compare__column"
                      data-testid={`scenario-option-${option.optionLabel}`}
                      data-selected={option.optionLabel === selected ? "true" : "false"}
                      data-recommended={option.optionLabel === recommended ? "true" : "false"}
                    >
                      <header>
                        <strong>{option.optionLabel}</strong>
                        <p>{option.title}</p>
                      </header>
                      <dl className="scenario-recovery-metric-list">
                        <div>
                          <dt>GOOD/h</dt>
                          <dd className="fact-numeric">{formatNumber(option.simulation.predictedGoodThroughputPerHour)}</dd>
                        </div>
                        <div>
                          <dt>Δ GOOD/h</dt>
                          <dd className="fact-numeric">{formatNumber(option.simulation.deltaGoodThroughputPerHour, 1)}</dd>
                        </div>
                        <div>
                          <dt>WIP</dt>
                          <dd className="fact-numeric">{formatNumber(option.simulation.predictedWipAverage, 1)}</dd>
                        </div>
                        <div>
                          <dt>Risk</dt>
                          <dd className="fact-numeric">{formatNumber(option.simulation.operationalRiskScore, 2)}</dd>
                        </div>
                        <div>
                          <dt>Uygulanabilirlik</dt>
                          <dd>{option.feasibilityState}</dd>
                        </div>
                        <div>
                          <dt>Darboğaz</dt>
                          <dd>
                            {option.simulation.constraintBefore?.stationCode ?? "—"}
                            {" → "}
                            {option.simulation.constraintAfter?.stationCode ?? "—"}
                          </dd>
                        </div>
                      </dl>
                    </article>
                  ))}
                </div>
              ) : (
                <p>Seçenek karşılaştırması henüz üretilmedi.</p>
              )}
            </div>
          </section>
        </div>

        <aside className="scenario-recovery-workspace__right" data-testid="scenario-recovery-decision">
          <section className="fact-section">
            <header className="fact-section__header">
              <h2 className="fact-section__title">Öneri / Karar</h2>
            </header>
            <div className="fact-section__body">
              {comparison ? (
                <>
                  <ContextPanel
                    title="Zentra önerisi"
                    rows={[
                      { label: "Seçenek", value: recommended ?? "—" },
                      ...comparison.recommendation.scoreComponents.map((component, index) => ({
                        label: `Skor ${index + 1}`,
                        value: component,
                      })),
                    ]}
                  />
                  {comparison.tradeOffs.map((tradeOff) => (
                    <ContextPanel
                      key={tradeOff.optionLabel}
                      title={`${tradeOff.optionLabel} trade-off`}
                      rows={[
                        { label: "Artılar", value: tradeOff.advantages.join("; ") || "—" },
                        { label: "Eksiler", value: tradeOff.disadvantages.join("; ") || "—" },
                        { label: "Hesaplanan", value: tradeOff.computedFacts.join("; ") || "—" },
                      ]}
                    />
                  ))}
                </>
              ) : null}

              {workspace.recoveryPlan ? (
                <ContextPanel
                  title="Kurtarma planı"
                  rows={[
                    { label: "Seçilen", value: workspace.recoveryPlan.selectedOptionLabel },
                    { label: "Mod", value: workspace.recoveryPlan.executionMode },
                    { label: "Gözlem penceresi", value: `${workspace.recoveryPlan.observationWindowMinutes} dk` },
                  ]}
                />
              ) : null}

              {workspace.verification ? (
                <ContextPanel
                  title="Doğrulama"
                  rows={[
                    { label: "Sonuç", value: workspace.verification.outcome },
                    { label: "Tahmin GOOD/h", value: formatNumber(workspace.verification.predictedGoodThroughputPerHour, 1) },
                    { label: "Gerçek GOOD/h", value: formatNumber(workspace.verification.actual?.goodThroughputPerHour, 1) },
                    { label: "Tahmin hatası", value: formatNumber(workspace.verification.throughputPredictionError, 1) },
                  ]}
                />
              ) : null}

              {workspace.digitalWorkCaseId ? (
                <p>
                  <Link href={buildWorksCaseHref(workspace.digitalWorkCaseId)}>
                    Bağlı operasyonel iş
                  </Link>
                </p>
              ) : null}
            </div>
          </section>
        </aside>
      </div>

      {history.length > 0 ? (
        <section className="fact-section" data-testid="scenario-recovery-history">
          <header className="fact-section__header">
            <h2 className="fact-section__title">Senaryo geçmişi</h2>
          </header>
          <div className="fact-section__body scenario-recovery-history-table-wrap" tabIndex={0} role="region" aria-label="Senaryo geçmişi (kaydırılabilir)">
            <table aria-label="Senaryo geçmişi" className="scenario-recovery-history-table">
              <thead>
                <tr>
                  <th>Problem</th>
                  <th>Seçim</th>
                  <th>Faz</th>
                  <th>Doğrulama</th>
                </tr>
              </thead>
              <tbody>
                {history.map((item) => (
                  <tr key={item.runId}>
                    <td>{item.problemSummary}</td>
                    <td>{item.selectedOptionLabel || "—"}</td>
                    <td>{phaseLabel(item.phase)}</td>
                    <td>{item.verificationOutcome ?? "—"}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </section>
      ) : null}
    </div>
  );
}
