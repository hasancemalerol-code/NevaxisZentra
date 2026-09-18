import Link from "next/link";
import { ArrowUpRight } from "lucide-react";

import { DecisionCard } from "@/components/workspace/decision-card";
import { StatusStrip } from "@/components/workspace/status-strip";
import { WorkspaceHeader } from "@/components/workspace/workspace-header";
import { AiTaskListPageContent } from "@/features/ai-task-center/components/ai-task-list-page-content";
import {
  getFactoryDecisionSeverityLabel,
  getFactoryDecisionStatusLabel,
} from "@/features/factory-decision-center/domain/factory-decision-center-labels";
import { readBugunWorkspace } from "@/features/bugun/server/bugun-workspace-queries";

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

  const statusItems: Array<{ label: string; value: string | number }> = [];
  if (workspace?.taskCount != null) {
    statusItems.push({ label: "Açık görev", value: workspace.taskCount });
  }
  if (workspace?.pendingApprovals != null) {
    statusItems.push({ label: "Bekleyen onay", value: workspace.pendingApprovals });
  }
  if (workspace?.pendingVerification != null) {
    statusItems.push({ label: "Doğrulama", value: workspace.pendingVerification });
  }
  if (workspace?.openShiftItems != null) {
    statusItems.push({ label: "Vardiya konu", value: workspace.openShiftItems });
  }
  if (workspace?.shiftLabel) {
    statusItems.push({ label: "Vardiya", value: workspace.shiftLabel });
  }

  return (
    <div data-testid="bugun-page">
      <main className="fact-workspace">
        <WorkspaceHeader
          context="BUGÜN"
          title="Bugün neye dikkat etmeliyim?"
          meta={workspace?.profileLabel ?? "Operasyonel öncelikler"}
        />

        {statusItems.length > 0 ? (
          <StatusStrip aria-label="Bugün özet" items={statusItems} />
        ) : null}

        {workspace && workspace.attentionItems.length > 0 ? (
          <section aria-label="Dikkat gerektiren konular" className="fact-section">
            <header className="fact-section__header">
              <h2 className="fact-section__title">Dikkat gerektiren</h2>
            </header>
            <div className="fact-section__body">
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
                      <ArrowUpRight aria-hidden="true" size={14} />
                    </Link>
                  </li>
                ))}
              </ul>
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
                    value: `Gap ${item.gapQuantity} · ${item.projectedQuantity}/${item.requiredQuantity}`,
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
      </main>

      <AiTaskListPageContent
        initialOriginFilter={initialOriginFilter}
        initialView={initialView}
        listPath="/"
        showCreateAction
      />
    </div>
  );
}
