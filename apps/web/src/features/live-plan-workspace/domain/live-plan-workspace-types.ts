import type { AdaptivePlanningWorkspaceApi, ReplanEvaluationResultApi } from "@/features/adaptive-production-planning/server/adaptive-planning-api-client";
import type { ProductionPlan } from "@/features/production-planning/domain/production-plan";
import type { ProductionPlanSummary } from "@/features/production-planning/application/production-plan-query";

export type LivePlanWorkspaceTab =
  | "summary"
  | "live"
  | "scenarios"
  | "changes"
  | "history"
  | "risks"
  | "timeline";

export type LivePlanWorkspaceSelection = Readonly<{
  productionPlanId?: string;
  lineId?: string;
  tab: LivePlanWorkspaceTab;
  search?: string;
}>;

export type LivePlanLineContext = Readonly<{
  lineId: string;
  lineCode: string;
  lineName: string;
  constraintSummary?: string;
  throughputGapPercent?: number;
}>;

export type LivePlanWorkspacePayload = Readonly<{
  selection: LivePlanWorkspaceSelection;
  summary: ProductionPlanSummary;
  productionPlans: readonly ProductionPlan[];
  adaptiveWorkspace: AdaptivePlanningWorkspaceApi | null;
  replanEvaluation: ReplanEvaluationResultApi | null;
  lineContexts: readonly LivePlanLineContext[];
  selectedProductionPlan: ProductionPlan | null;
  canReadAdaptivePlanning: boolean;
}>;
