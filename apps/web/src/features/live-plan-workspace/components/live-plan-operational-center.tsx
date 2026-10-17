"use client";

import Link from "next/link";
import { usePathname, useRouter } from "next/navigation";

import { AdaptivePlanningWorkspaceClient } from "@/features/adaptive-production-planning/components/adaptive-planning-workspace-client";
import { ProductionPlanFilteredWorkspace } from "@/features/production-planning/components/production-plan-filtered-workspace";
import { buildScenarioRecoveryHref } from "@/features/scenario-recovery/application/scenario-recovery-links";

import { buildLivePlanWorkspaceHref } from "../application/live-plan-workspace-selection";
import type { LivePlanWorkspacePayload, LivePlanWorkspaceTab } from "../domain/live-plan-workspace-types";

type LivePlanOperationalCenterProps = Readonly<{
  workspace: LivePlanWorkspacePayload;
  tabsHost?: HTMLElement | null;
}>;

export function LivePlanOperationalCenter({ workspace, tabsHost }: LivePlanOperationalCenterProps) {
  const router = useRouter();
  const pathname = usePathname();
  const { adaptiveWorkspace, replanEvaluation, selection, productionPlans, canReadAdaptivePlanning, lineContexts } = workspace;

  function handleTabChange(tab: LivePlanWorkspaceTab) {
    const href = buildLivePlanWorkspaceHref(pathname, { ...selection, tab });
    router.replace(href, { scroll: false });
  }

  return (
    <section className="live-plan-workspace__panel" data-testid="live-plan-operational-center">
      <div className="live-plan-workspace__panel-header">
        <h2>Canlı Plan</h2>
        {productionPlans.length > 0 && (selection.lineId ?? lineContexts[0]?.lineId) ? (
          <Link
            href={buildScenarioRecoveryHref({
              lineId: selection.lineId ?? lineContexts[0]?.lineId,
              entryPoint: "PLAN",
              orderReference: adaptiveWorkspace?.risks.deadlineRisks[0]?.orderId,
            })}
            className="fact-link-button"
            data-testid="live-plan-simulate-recovery-link"
          >
            Kurtarma simülasyonu
          </Link>
        ) : null}
      </div>
      <div className="live-plan-workspace__panel-body">
        {canReadAdaptivePlanning && adaptiveWorkspace ? (
          <AdaptivePlanningWorkspaceClient
            workspace={adaptiveWorkspace}
            replanEvaluation={replanEvaluation}
            initialTab={selection.tab}
            onTabChange={handleTabChange}
            hideCreatePlanAction
            tabsHost={tabsHost}
          />
        ) : (
          <ProductionPlanFilteredWorkspace productionPlans={productionPlans} />
        )}
      </div>
    </section>
  );
}
