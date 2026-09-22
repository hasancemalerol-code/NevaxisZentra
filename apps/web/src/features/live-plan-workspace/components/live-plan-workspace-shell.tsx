"use client";

import Link from "next/link";

import { FactoryContextBreadcrumb } from "@/components/workspace/factory-context-breadcrumb";
import { StatusStrip } from "@/components/workspace/status-strip";
import { WorkspaceHeader } from "@/components/workspace/workspace-header";

import type { LivePlanWorkspacePayload } from "../domain/live-plan-workspace-types";
import { FactoryCopilotWorkspaceActions } from "@/features/factory-copilot/components/factory-copilot-workspace-actions";
import { LivePlanNavigator } from "./live-plan-navigator";
import { LivePlanOperationalCenter } from "./live-plan-operational-center";
import { LivePlanSchedulingInspector } from "./live-plan-scheduling-inspector";

import "../live-plan-workspace.css";

type LivePlanWorkspaceShellProps = Readonly<{
  workspace: LivePlanWorkspacePayload;
}>;

export function LivePlanWorkspaceShell({ workspace }: LivePlanWorkspaceShellProps) {
  const { summary, adaptiveWorkspace, selectedProductionPlan } = workspace;

  return (
    <main className="fact-workspace live-plan-workspace" data-testid="live-plan-workspace">
      <WorkspaceHeader
        breadcrumb={
          <FactoryContextBreadcrumb
            segments={[
              { label: "Plan", href: "/planlama" },
              ...(selectedProductionPlan
                ? [{ label: selectedProductionPlan.referenceCode }]
                : []),
            ]}
          />
        }
        context="PLAN"
        meta="Canlı plan · kısıtlar · yeniden planlama"
        title="Bugün ne üretilecek ve nasıl sıralanacak?"
        actions={
          <FactoryCopilotWorkspaceActions>
            <Link href="/planlama/yeni" className="fact-link-button">
              Yeni Plan
            </Link>
          </FactoryCopilotWorkspaceActions>
        }
      />

      <StatusStrip
        aria-label="Planlama özet"
        items={[
          { label: "Toplam plan", value: summary.total },
          { label: "İnceleme", value: summary.needsReview },
          { label: "Onay bekleyen", value: summary.waitingForApproval },
          { label: "Termin riski", value: adaptiveWorkspace?.metrics.ordersAtRisk ?? summary.urgent },
          { label: "Aktarıma hazır", value: summary.transferable },
        ]}
      />

      <div className="live-plan-workspace__layout">
        <LivePlanNavigator workspace={workspace} />
        <LivePlanOperationalCenter workspace={workspace} />
        <LivePlanSchedulingInspector workspace={workspace} />
      </div>
    </main>
  );
}
