"use client";

import { usePathname, useRouter } from "next/navigation";

import { AdaptivePlanningWorkspaceClient } from "@/features/adaptive-production-planning/components/adaptive-planning-workspace-client";
import { ProductionPlanFilteredWorkspace } from "@/features/production-planning/components/production-plan-filtered-workspace";

import { buildLivePlanWorkspaceHref } from "../application/live-plan-workspace-selection";
import type { LivePlanWorkspacePayload, LivePlanWorkspaceTab } from "../domain/live-plan-workspace-types";

type LivePlanOperationalCenterProps = Readonly<{
  workspace: LivePlanWorkspacePayload;
}>;

export function LivePlanOperationalCenter({ workspace }: LivePlanOperationalCenterProps) {
  const router = useRouter();
  const pathname = usePathname();
  const { adaptiveWorkspace, replanEvaluation, selection, productionPlans, canReadAdaptivePlanning } = workspace;

  function handleTabChange(tab: LivePlanWorkspaceTab) {
    const href = buildLivePlanWorkspaceHref(pathname, { ...selection, tab });
    router.replace(href, { scroll: false });
  }

  return (
    <section className="live-plan-workspace__panel" data-testid="live-plan-operational-center">
      <div className="live-plan-workspace__panel-header">
        <h2>Canlı Plan</h2>
      </div>
      <div className="live-plan-workspace__panel-body">
        {canReadAdaptivePlanning && adaptiveWorkspace ? (
          <AdaptivePlanningWorkspaceClient
            workspace={adaptiveWorkspace}
            replanEvaluation={replanEvaluation}
            initialTab={selection.tab}
            onTabChange={handleTabChange}
          />
        ) : (
          <ProductionPlanFilteredWorkspace productionPlans={productionPlans} />
        )}
      </div>
    </section>
  );
}
