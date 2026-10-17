"use client";

import Link from "next/link";
import { useState } from "react";

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
  // Sub-navigation tabs render in a full-width row above the three panels (portal target), so they never wrap in the narrow centre column.
  const [tabsHost, setTabsHost] = useState<HTMLElement | null>(null);
  const usesTabs = workspace.canReadAdaptivePlanning && Boolean(adaptiveWorkspace);

  return (
    <div className="fact-workspace live-plan-workspace" data-testid="live-plan-workspace">
      <WorkspaceHeader
        breadcrumb={
          selectedProductionPlan ? (
            <FactoryContextBreadcrumb
              segments={[
                { label: "Plan", href: "/planlama" },
                { label: selectedProductionPlan.referenceCode },
              ]}
            />
          ) : undefined
        }
        context="Üretim Planlama"
        meta="Canlı plan · kısıtlar · yeniden planlama"
        title="Canlı Üretim Planı"
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
          {
            label: "Termin riski",
            // No plan means no deadline can be at risk: show "—", never a count that no plan backs.
            value: summary.total === 0 ? "—" : adaptiveWorkspace?.metrics.ordersAtRisk ?? summary.urgent,
          },
          { label: "Aktarıma hazır", value: summary.transferable },
        ]}
      />

      {usesTabs ? <div className="live-plan-workspace__tabs-row" ref={setTabsHost} /> : null}

      <div className="live-plan-workspace__layout">
        <LivePlanNavigator workspace={workspace} />
        <LivePlanOperationalCenter workspace={workspace} tabsHost={usesTabs ? tabsHost : undefined} />
        <LivePlanSchedulingInspector workspace={workspace} />
      </div>
    </div>
  );
}
