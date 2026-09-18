"use client";

import Link from "next/link";

import { DecisionCard } from "@/components/workspace/decision-card";
import {
  formatAdaptivePercent,
  getAutonomyModeLabel,
  getReplanActionLabel,
} from "@/features/adaptive-production-planning/domain/adaptive-planning-labels";

import type { LivePlanWorkspacePayload } from "../domain/live-plan-workspace-types";

type LivePlanSchedulingInspectorProps = Readonly<{
  workspace: LivePlanWorkspacePayload;
}>;

export function LivePlanSchedulingInspector({ workspace }: LivePlanSchedulingInspectorProps) {
  const { adaptiveWorkspace, replanEvaluation, lineContexts, selection, selectedProductionPlan } = workspace;
  const metrics = adaptiveWorkspace?.metrics;
  const policy = adaptiveWorkspace?.policy;
  const selectedLine = lineContexts.find((line) => line.lineId === selection.lineId);

  return (
    <aside
      className="live-plan-workspace__panel live-plan-workspace__panel--inspector"
      data-testid="live-plan-scheduling-inspector"
    >
      <div className="live-plan-workspace__panel-header">
        <h2>Planlama Zekâsı</h2>
      </div>
      <div className="live-plan-workspace__panel-body">
        {selectedProductionPlan ? (
          <section className="live-plan-inspector__section">
            <h3>Seçili plan</h3>
            <p className="text-sm font-medium">{selectedProductionPlan.referenceCode}</p>
            <p className="text-xs text-[color:var(--fact-text-secondary)]">{selectedProductionPlan.title}</p>
            <Link href={`/planlama/${selectedProductionPlan.id}`} className="fact-link-button">
              Plan detayına git
            </Link>
          </section>
        ) : null}

        {metrics ? (
          <section className="live-plan-inspector__section">
            <h3>Canlı metrikler</h3>
            <div className="live-plan-inspector__metric">
              <span>Plan uyumu</span>
              <strong>{formatAdaptivePercent(metrics.planAdherencePercent)}</strong>
            </div>
            <div className="live-plan-inspector__metric">
              <span>Termin riski</span>
              <strong>{metrics.ordersAtRisk}</strong>
            </div>
            <div className="live-plan-inspector__metric">
              <span>Bugünkü replan</span>
              <strong>{metrics.replansToday}</strong>
            </div>
          </section>
        ) : null}

        {policy ? (
          <section className="live-plan-inspector__section">
            <h3>Otonomi</h3>
            <div className="live-plan-inspector__metric">
              <span>Mod</span>
              <strong>{getAutonomyModeLabel(policy.autonomyMode)}</strong>
            </div>
            <div className="live-plan-inspector__metric">
              <span>Dondurma penceresi</span>
              <strong>{policy.frozenWindowMinutes} dk</strong>
            </div>
          </section>
        ) : null}

        {replanEvaluation ? (
          <DecisionCard
            title="Yeniden planlama değerlendirmesi"
            signal={{ label: "Öneri", value: getReplanActionLabel(replanEvaluation.recommendedAction) }}
            cause={{ label: "Gerekçe", value: replanEvaluation.rationale }}
            impact={{
              label: "Koruma",
              value: replanEvaluation.frozenWindowBlocked
                ? "Dondurma penceresi engellendi"
                : replanEvaluation.nervousnessBlocked
                  ? "Sinirlilik koruması aktif"
                  : "Uygulanabilir",
            }}
          />
        ) : null}

        {selectedLine ? (
          <section className="live-plan-inspector__section">
            <h3>Hat kısıtı</h3>
            <p className="text-sm">{selectedLine.constraintSummary ?? "Aktif kısıt yok"}</p>
            {selectedLine.throughputGapPercent !== undefined ? (
              <p className="text-xs text-[color:var(--fact-text-secondary)]">
                Throughput sapması: {selectedLine.throughputGapPercent}%
              </p>
            ) : null}
            <Link
              href={`/fabrika?hat=${encodeURIComponent(selectedLine.lineId)}`}
              className="fact-link-button"
            >
              Fabrikada görüntüle
            </Link>
          </section>
        ) : null}

        {(adaptiveWorkspace?.risks.deadlineRisks.length ?? 0) > 0 ? (
          <section className="live-plan-inspector__section">
            <h3>Termin riskleri</h3>
            <ul className="text-xs">
              {adaptiveWorkspace?.risks.deadlineRisks.slice(0, 4).map((risk) => (
                <li key={`${risk.orderId}-${risk.riskLevel}`}>
                  {risk.orderId} · {risk.riskLevel}
                </li>
              ))}
            </ul>
          </section>
        ) : null}
      </div>
    </aside>
  );
}
