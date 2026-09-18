import "server-only";

import { readAdaptivePlanningWorkspaceDetail } from "@/features/adaptive-production-planning/server/adaptive-planning-reader";
import { loadMixedProductionLinesWorkspace } from "@/features/mixed-production/server/mixed-production-reader";
import { calculateProductionPlanSummary } from "@/features/production-planning/application/production-plan-query";
import { readProductionPlanningWorkspace } from "@/features/production-planning/server/production-plan-reader";
import { loadThroughputIntelligenceWorkspace } from "@/features/throughput-intelligence/server/throughput-intelligence-reader";

import { readLivePlanWorkspaceSelection } from "../application/live-plan-workspace-selection";
import type { LivePlanLineContext, LivePlanWorkspacePayload } from "../domain/live-plan-workspace-types";

export type LivePlanWorkspaceResult =
  | Readonly<{ status: "success"; workspace: LivePlanWorkspacePayload }>
  | Readonly<{ status: "failure"; message: string }>;

function filterProductionPlans(
  plans: LivePlanWorkspacePayload["productionPlans"],
  search: string | undefined,
) {
  if (!search) return plans;
  const needle = search.toLocaleLowerCase("tr-TR");
  return plans.filter((plan) => {
    const haystack = [
      plan.referenceCode,
      plan.title,
      plan.status,
      ...plan.demands.map((d) => d.orderReference ?? ""),
      ...plan.demands.map((d) => d.productName),
    ]
      .join(" ")
      .toLocaleLowerCase("tr-TR");
    return haystack.includes(needle);
  });
}

export async function readLivePlanWorkspace(
  searchParams?: Readonly<Record<string, string | string[] | undefined>>,
): Promise<LivePlanWorkspaceResult> {
  const selection = readLivePlanWorkspaceSelection(searchParams);

  const planningResult = await readProductionPlanningWorkspace();
  if (planningResult.status === "failure") {
    return { status: "failure", message: planningResult.message };
  }

  const productionPlans = filterProductionPlans(planningResult.productionPlans, selection.search);
  const summary = calculateProductionPlanSummary(planningResult.productionPlans);
  const selectedProductionPlan =
    productionPlans.find((plan) => plan.id === selection.productionPlanId) ??
    productionPlans.find((plan) => plan.status === "scheduled" || plan.status === "in-progress") ??
    productionPlans[0] ??
    null;

  const adaptiveResult = await readAdaptivePlanningWorkspaceDetail();
  const canReadAdaptivePlanning = adaptiveResult.status === "success";
  const adaptiveWorkspace = canReadAdaptivePlanning ? adaptiveResult.workspace : null;
  const replanEvaluation = canReadAdaptivePlanning ? adaptiveResult.replanEvaluation : null;

  const linesResult = await loadMixedProductionLinesWorkspace();
  const lineContexts: LivePlanLineContext[] = [];

  if (linesResult.status === "success") {
    const throughputLineId = selection.lineId ?? linesResult.lines[0]?.lineId;
    let throughputOverview: Awaited<ReturnType<typeof loadThroughputIntelligenceWorkspace>> | null = null;
    if (throughputLineId) {
      throughputOverview = await loadThroughputIntelligenceWorkspace(throughputLineId);
    }

    for (const line of linesResult.lines.slice(0, 12)) {
      let constraintSummary: string | undefined;
      let throughputGapPercent: number | undefined;

      if (
        throughputOverview?.status === "success" &&
        throughputOverview.overview.lineId === line.lineId
      ) {
        const current = throughputOverview.overview.constraints.find((c) => c.role === "CURRENT");
        constraintSummary = current?.explanation ?? current?.stationCode;
        const required = throughputOverview.overview.productionGap.requiredGoodPerHour;
        const currentGood = throughputOverview.overview.productionGap.currentGoodPerHour;
        if (required > 0) {
          throughputGapPercent = Math.round(((currentGood - required) / required) * 100);
        }
      }

      lineContexts.push({
        lineId: line.lineId,
        lineCode: line.code,
        lineName: line.name,
        constraintSummary,
        throughputGapPercent,
      });
    }
  }

  return {
    status: "success",
    workspace: {
      selection: {
        ...selection,
        productionPlanId: selectedProductionPlan?.id,
      },
      summary,
      productionPlans,
      adaptiveWorkspace,
      replanEvaluation,
      lineContexts,
      selectedProductionPlan,
      canReadAdaptivePlanning,
    },
  };
}
