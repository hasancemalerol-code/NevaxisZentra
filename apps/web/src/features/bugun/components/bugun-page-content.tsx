import Link from "next/link";
import { ArrowRight } from "lucide-react";

import { DecisionCard } from "@/components/workspace/decision-card";
import type { AiTaskExtraKpi } from "@/features/ai-task-center/components/ai-task-kpi-links";
import { AiTaskListPageContent } from "@/features/ai-task-center/components/ai-task-list-page-content";
import {
  getFactoryDecisionSeverityLabel,
  getFactoryDecisionStatusLabel,
} from "@/features/factory-decision-center/domain/factory-decision-center-labels";
import { readBugunWorkspace } from "@/features/bugun/server/bugun-workspace-queries";
import { FactoryCopilotWorkspaceActions } from "@/features/factory-copilot/components/factory-copilot-workspace-actions";

type BugunPageContentProps = Readonly<{
  initialView?: string;
  initialOriginFilter?: string;
}>;

export async function BugunPageContent({
  initialView,
  initialOriginFilter,
}: BugunPageContentProps) {
  const workspaceResult = await readBugunWorkspace();

  const workspace = workspaceResult.status === "success" ? workspaceResult.workspace : null;

  // One KPI row: the Task Center owns Açık görev / Onay bekleyen / Kritik risk / Geciken görev; Today adds only what the
  // Task Center does not show, so no metric appears twice.
  const extraKpis: AiTaskExtraKpi[] = [];
  if (workspace?.pendingVerification != null) {
    extraKpis.push({ label: "Doğrulama", value: workspace.pendingVerification, detail: "Doğrulama bekleyen iş", href: "/fabrika-karar-merkezi" });
  }
  if (workspace?.openShiftItems != null) {
    extraKpis.push({ label: "Vardiya konusu", value: workspace.openShiftItems, detail: "Açık vardiya konusu", href: "/vardiyalar/acik-konular" });
  }

  const description = [workspace?.profileLabel ?? "Operasyonel öncelikler", workspace?.shiftLabel ? `Vardiya: ${workspace.shiftLabel}` : null]
    .filter(Boolean)
    .join(" · ");

  const hasRealAttention = Boolean(workspace?.attentionItems.some((item) => item.id !== "executive-command"));

  const beforeWorkspace = (
    <div className="fact-workspace bugun-sections">
      {workspace ? (
        <section aria-label="Dikkat gerektiren konular" className="fact-section">
          <header className="fact-section__header">
            <h2 className="fact-section__title">Dikkat gerektiren</h2>
          </header>
          <div className="fact-section__body">
            {hasRealAttention ? (
              <ul className="bugun-attention-list">
                {workspace.attentionItems.map((item) => (
                  <li key={item.id} data-tone={item.tone}>
                    <div>
                      <p className="bugun-attention-list__category">{item.category}</p>
                      <strong>{item.title}</strong>
                      <p>{item.summary}</p>
                    </div>
                    <Link href={item.href}>
                      Aç
                      <ArrowRight aria-hidden="true" size={14} />
                    </Link>
                  </li>
                ))}
              </ul>
            ) : (
              <p className="bugun-attention-empty">Karar, kurtarma ve vardiya konularında dikkat gerektiren kayıt yok.</p>
            )}
          </div>
        </section>
      ) : null}

      {workspace && workspace.showExecutiveStrip && workspace.decisionQueue.length > 0 ? (
        <section aria-label="Öncelikli kararlar" className="fact-section">
          <header className="fact-section__header">
            <h2 className="fact-section__title">Öncelikli kararlar</h2>
            <Link href="/fabrika-karar-merkezi">Tüm kararlar</Link>
          </header>
          <div className="fact-section__body bugun-decision-grid">
            {workspace.decisionQueue.map((item) => (
              <DecisionCard
                key={item.caseId}
                eyebrow={getFactoryDecisionSeverityLabel(item.severity)}
                title={item.orderReference}
                subtitle={item.title}
                statusLabel={getFactoryDecisionStatusLabel(item.status)}
                signal={{ label: "Sinyal", value: item.riskSummary }}
                cause={{ label: "Neden", value: item.currentBlockerCode ?? item.riskSummary }}
                impact={{
                  label: "Etki",
                  value: `Eksik ${item.gapQuantity} · ${item.projectedQuantity}/${item.requiredQuantity}`,
                }}
                recommendation={{ label: "Öneri", value: item.recommendedStrategy }}
                expectedResult={{
                  label: "Beklenen sonuç",
                  value: item.deliveryCommitmentLabel,
                }}
                actions={[
                  {
                    label: "Kararı aç",
                    href: `/fabrika-karar-merkezi/vakalar/${item.caseId}`,
                    testId: `bugun-decision-open-${item.orderReference}`,
                  },
                ]}
                testId={`bugun-decision-card-${item.orderReference}`}
              />
            ))}
          </div>
        </section>
      ) : null}
    </div>
  );

  return (
    <div className="bugun-page" data-testid="bugun-page">
      <AiTaskListPageContent
        beforeWorkspace={beforeWorkspace}
        description={description}
        extraActions={<FactoryCopilotWorkspaceActions />}
        extraKpis={extraKpis}
        eyebrow="Bugün"
        initialOriginFilter={initialOriginFilter}
        initialView={initialView}
        listPath="/"
        showCreateAction
        title="Bugün neye dikkat etmeliyim?"
      />
    </div>
  );
}
