import "./quality-control-server-boundary";

import {
  getProductionOrderTraceability,
  getQualityCorrectiveActionAnalytics,
  getQualityHoldAnalytics,
  getQualityInspectionInternalSummary,
  getQualityNcrAnalytics,
  getQualityReleaseIntelligence,
  getQualityReportingOverview,
  listQualityFailureBreakdown,
  listQualityReportingTrends,
  listQualityNcrs,
  getQualityControlPlan,
  getQualityInspection,
  getQualityNcr,
  getQualityWorkspaceSummary,
  listQualityControlPlans,
  listQualityInspections,
  type QualityNcrListFilters,
  type QualityReportingFilters,
} from "./quality-control-api-client";
import { parseProductionExecutionRuntimeConfig } from "../../production-execution/server/production-order-runtime-config";
import { resolveProductionOrderRequestContextForIncomingRequest } from "../../production-execution/server/production-order-request-context";
import type {
  ProductionOrderTraceability,
  QualityControlPlanDetail,
  QualityControlPlanSummary,
  QualityCorrectiveActionAnalytics,
  QualityFailureBreakdownItem,
  QualityHoldAnalytics,
  QualityInspectionDetail,
  QualityInspectionInternalSummary,
  QualityInspectionSummary,
  QualityNcrAnalytics,
  QualityNcrDetail,
  QualityNcrSummary,
  QualityReleaseIntelligence,
  QualityReportingOverview,
  QualityReportingTrendPeriod,
  QualityTrendPeriod,
  QualityWorkspaceSummary,
} from "../domain/quality-control";

type ReadFailure = Readonly<{
  status: "failure";
  message: string;
}>;

export type QualityWorkspaceResult =
  | Readonly<{
      status: "success";
      summary: QualityWorkspaceSummary;
      plans: readonly QualityControlPlanSummary[];
      inspections: readonly QualityInspectionSummary[];
    }>
  | ReadFailure;

export type QualityDashboardCorrectiveAction = Readonly<{
  id: string;
  title: string;
  ncrId: string;
  ncrNumber: string;
  ownerSubjectId?: string;
  dueAtUtc?: string;
  status: string;
}>;

export type QualityDashboardResult =
  | Readonly<{
      status: "success";
      summary: QualityWorkspaceSummary;
      plans: readonly QualityControlPlanSummary[];
      inspections: readonly QualityInspectionSummary[];
      overview: QualityReportingOverview | null;
      openNcrs: readonly QualityNcrSummary[];
      activeHoldNcrs: readonly QualityNcrSummary[];
      correctiveActions: readonly QualityDashboardCorrectiveAction[];
    }>
  | ReadFailure;

export async function readQualityWorkspace(): Promise<QualityWorkspaceResult> {
  const configResult = parseProductionExecutionRuntimeConfig();
  if (configResult.status !== "success") {
    return { status: "failure", message: configResult.message };
  }

  const contextResult = await resolveProductionOrderRequestContextForIncomingRequest(configResult.config);
  if (contextResult.status === "failure") {
    return { status: "failure", message: contextResult.message };
  }

  const context = contextResult.context;
  const [summaryResult, plansResult, inspectionsResult] = await Promise.all([
    getQualityWorkspaceSummary(context),
    listQualityControlPlans(context),
    listQualityInspections(context),
  ]);

  if (summaryResult.status === "failure") {
    return { status: "failure", message: summaryResult.message };
  }

  if (plansResult.status === "failure") {
    return { status: "failure", message: plansResult.message };
  }

  if (inspectionsResult.status === "failure") {
    return { status: "failure", message: inspectionsResult.message };
  }

  return {
    status: "success",
    summary: summaryResult.value,
    plans: plansResult.value,
    inspections: inspectionsResult.value,
  };
}

export async function readQualityDashboard(): Promise<QualityDashboardResult> {
  const workspaceResult = await readQualityWorkspace();
  if (workspaceResult.status === "failure") {
    return workspaceResult;
  }

  const [reportingResult, ncrListResult, holdNcrListResult] = await Promise.all([
    readQualityReportingOverview(),
    readQualityNcrList(),
    readQualityNcrList({ holdActive: true }),
  ]);

  const openNcrs =
    ncrListResult.status === "success"
      ? [...ncrListResult.ncrs]
          .filter(
            (ncr) =>
              ncr.status === "open"
              || ncr.status === "under-review"
              || ncr.status === "action-required",
          )
          .sort((a, b) => b.updatedAtUtc.localeCompare(a.updatedAtUtc))
          .slice(0, 5)
      : [];

  const activeHoldNcrs =
    holdNcrListResult.status === "success"
      ? [...holdNcrListResult.ncrs]
          .sort((a, b) => b.updatedAtUtc.localeCompare(a.updatedAtUtc))
          .slice(0, 5)
      : [];

  const correctiveActionCandidates =
    ncrListResult.status === "success"
      ? [...ncrListResult.ncrs]
          .filter(
            (ncr) =>
              ncr.status === "action-required"
              || ncr.status === "open"
              || ncr.status === "under-review",
          )
          .sort((a, b) => b.updatedAtUtc.localeCompare(a.updatedAtUtc))
          .slice(0, 5)
      : [];

  const correctiveActions: QualityDashboardCorrectiveAction[] = [];
  if (correctiveActionCandidates.length > 0) {
    const contextResult = await resolveQualityReadContext();
    if (contextResult.status === "success") {
      const detailResults = await Promise.all(
        correctiveActionCandidates.map((ncr) =>
          getQualityNcr(contextResult.context, ncr.id),
        ),
      );

      for (const [index, detailResult] of detailResults.entries()) {
        if (detailResult.status !== "success") {
          continue;
        }

        const ncr = correctiveActionCandidates[index]!;
        for (const action of detailResult.value.correctiveActions) {
          if (
            action.status === "open"
            || action.status === "in-progress"
            || action.status === "pending"
          ) {
            correctiveActions.push({
              id: action.id,
              title: action.title,
              ncrId: ncr.id,
              ncrNumber: ncr.ncrNumber,
              ownerSubjectId: action.ownerSubjectId,
              dueAtUtc: action.dueAtUtc,
              status: action.status,
            });
          }
        }
      }
    }
  }

  correctiveActions.sort((a, b) => {
    const aDue = a.dueAtUtc ? Date.parse(a.dueAtUtc) : Number.MAX_SAFE_INTEGER;
    const bDue = b.dueAtUtc ? Date.parse(b.dueAtUtc) : Number.MAX_SAFE_INTEGER;
    return aDue - bDue;
  });

  return {
    status: "success",
    summary: workspaceResult.summary,
    plans: workspaceResult.plans,
    inspections: workspaceResult.inspections,
    overview: reportingResult.status === "success" ? reportingResult.overview : null,
    openNcrs,
    activeHoldNcrs,
    correctiveActions: correctiveActions.slice(0, 5),
  };
}

export type QualityInspectionDetailResult =
  | Readonly<{ status: "success"; inspection: QualityInspectionDetail }>
  | ReadFailure;

export async function readQualityInspectionById(
  inspectionId: string,
): Promise<QualityInspectionDetailResult> {
  const configResult = parseProductionExecutionRuntimeConfig();
  if (configResult.status !== "success") {
    return { status: "failure", message: configResult.message };
  }

  const contextResult = await resolveProductionOrderRequestContextForIncomingRequest(configResult.config);
  if (contextResult.status === "failure") {
    return { status: "failure", message: contextResult.message };
  }

  const result = await getQualityInspection(contextResult.context, inspectionId);
  if (result.status === "failure") {
    return { status: "failure", message: result.message };
  }

  return { status: "success", inspection: result.value };
}

export type QualityControlPlanDetailResult =
  | Readonly<{ status: "success"; plan: QualityControlPlanDetail }>
  | ReadFailure;

export async function readQualityControlPlanById(
  planId: string,
): Promise<QualityControlPlanDetailResult> {
  const configResult = parseProductionExecutionRuntimeConfig();
  if (configResult.status !== "success") {
    return { status: "failure", message: configResult.message };
  }

  const contextResult = await resolveProductionOrderRequestContextForIncomingRequest(configResult.config);
  if (contextResult.status === "failure") {
    return { status: "failure", message: contextResult.message };
  }

  const result = await getQualityControlPlan(contextResult.context, planId);
  if (result.status === "failure") {
    return { status: "failure", message: result.message };
  }

  return { status: "success", plan: result.value };
}

export type QualityNcrListResult =
  | Readonly<{ status: "success"; ncrs: readonly QualityNcrSummary[] }>
  | ReadFailure;

export async function readQualityNcrList(
  filters?: QualityNcrListFilters,
): Promise<QualityNcrListResult> {
  const configResult = parseProductionExecutionRuntimeConfig();
  if (configResult.status !== "success") {
    return { status: "failure", message: configResult.message };
  }

  const contextResult = await resolveProductionOrderRequestContextForIncomingRequest(configResult.config);
  if (contextResult.status === "failure") {
    return { status: "failure", message: contextResult.message };
  }

  const result = await listQualityNcrs(contextResult.context, filters);
  if (result.status === "failure") {
    return { status: "failure", message: result.message };
  }

  return { status: "success", ncrs: result.value };
}

export type QualityNcrDetailResult =
  | Readonly<{ status: "success"; ncr: QualityNcrDetail }>
  | ReadFailure;

export async function readQualityNcrById(ncrId: string): Promise<QualityNcrDetailResult> {
  const configResult = parseProductionExecutionRuntimeConfig();
  if (configResult.status !== "success") {
    return { status: "failure", message: configResult.message };
  }

  const contextResult = await resolveProductionOrderRequestContextForIncomingRequest(configResult.config);
  if (contextResult.status === "failure") {
    return { status: "failure", message: contextResult.message };
  }

  const result = await getQualityNcr(contextResult.context, ncrId);
  if (result.status === "failure") {
    return { status: "failure", message: result.message };
  }

  return { status: "success", ncr: result.value };
}

async function resolveQualityReadContext() {
  const configResult = parseProductionExecutionRuntimeConfig();
  if (configResult.status !== "success") {
    return { status: "failure" as const, message: configResult.message };
  }

  const contextResult = await resolveProductionOrderRequestContextForIncomingRequest(configResult.config);
  if (contextResult.status === "failure") {
    return { status: "failure" as const, message: contextResult.message };
  }

  return { status: "success" as const, context: contextResult.context };
}

export type QualityReportingOverviewResult =
  | Readonly<{ status: "success"; overview: QualityReportingOverview }>
  | ReadFailure;

export async function readQualityReportingOverview(
  filters?: QualityReportingFilters,
): Promise<QualityReportingOverviewResult> {
  const contextResult = await resolveQualityReadContext();
  if (contextResult.status === "failure") {
    return { status: "failure", message: contextResult.message };
  }

  const result = await getQualityReportingOverview(contextResult.context, filters);
  if (result.status === "failure") {
    return { status: "failure", message: result.message };
  }

  return { status: "success", overview: result.value };
}

export type QualityReportingPageResult =
  | Readonly<{
      status: "success";
      overview: QualityReportingOverview;
      trends: readonly QualityTrendPeriod[];
      failures: readonly QualityFailureBreakdownItem[];
      ncrAnalytics: QualityNcrAnalytics;
      holdAnalytics: QualityHoldAnalytics;
      correctiveActionAnalytics: QualityCorrectiveActionAnalytics;
    }>
  | ReadFailure;

export async function readQualityReportingPage(
  period: QualityReportingTrendPeriod = "week",
  filters?: QualityReportingFilters,
): Promise<QualityReportingPageResult> {
  const contextResult = await resolveQualityReadContext();
  if (contextResult.status === "failure") {
    return { status: "failure", message: contextResult.message };
  }

  const context = contextResult.context;
  const [
    overviewResult,
    trendsResult,
    failuresResult,
    ncrAnalyticsResult,
    holdAnalyticsResult,
    correctiveActionAnalyticsResult,
  ] = await Promise.all([
    getQualityReportingOverview(context, filters),
    listQualityReportingTrends(context, period, filters),
    listQualityFailureBreakdown(context, filters),
    getQualityNcrAnalytics(context, filters),
    getQualityHoldAnalytics(context, filters),
    getQualityCorrectiveActionAnalytics(context, filters),
  ]);

  if (overviewResult.status === "failure") {
    return { status: "failure", message: overviewResult.message };
  }

  if (trendsResult.status === "failure") {
    return { status: "failure", message: trendsResult.message };
  }

  if (failuresResult.status === "failure") {
    return { status: "failure", message: failuresResult.message };
  }

  if (ncrAnalyticsResult.status === "failure") {
    return { status: "failure", message: ncrAnalyticsResult.message };
  }

  if (holdAnalyticsResult.status === "failure") {
    return { status: "failure", message: holdAnalyticsResult.message };
  }

  if (correctiveActionAnalyticsResult.status === "failure") {
    return { status: "failure", message: correctiveActionAnalyticsResult.message };
  }

  return {
    status: "success",
    overview: overviewResult.value,
    trends: trendsResult.value,
    failures: failuresResult.value,
    ncrAnalytics: ncrAnalyticsResult.value,
    holdAnalytics: holdAnalyticsResult.value,
    correctiveActionAnalytics: correctiveActionAnalyticsResult.value,
  };
}

export type QualityInspectionSummaryResult =
  | Readonly<{
      status: "success";
      summary: QualityInspectionInternalSummary;
      releaseIntelligence: QualityReleaseIntelligence;
    }>
  | ReadFailure;

export async function readInspectionSummary(
  inspectionId: string,
): Promise<QualityInspectionSummaryResult> {
  const contextResult = await resolveQualityReadContext();
  if (contextResult.status === "failure") {
    return { status: "failure", message: contextResult.message };
  }

  const context = contextResult.context;
  const [summaryResult, intelligenceResult] = await Promise.all([
    getQualityInspectionInternalSummary(context, inspectionId),
    getQualityReleaseIntelligence(context, inspectionId),
  ]);

  if (summaryResult.status === "failure") {
    return { status: "failure", message: summaryResult.message };
  }

  if (intelligenceResult.status === "failure") {
    return { status: "failure", message: intelligenceResult.message };
  }

  return {
    status: "success",
    summary: summaryResult.value,
    releaseIntelligence: intelligenceResult.value,
  };
}

export type ProductionOrderTraceabilityResult =
  | Readonly<{ status: "success"; traceability: ProductionOrderTraceability }>
  | ReadFailure;

export async function readProductionOrderTraceability(
  productionOrderId: string,
): Promise<ProductionOrderTraceabilityResult> {
  const contextResult = await resolveQualityReadContext();
  if (contextResult.status === "failure") {
    return { status: "failure", message: contextResult.message };
  }

  const result = await getProductionOrderTraceability(contextResult.context, productionOrderId);
  if (result.status === "failure") {
    return { status: "failure", message: result.message };
  }

  return { status: "success", traceability: result.value };
}

export type QualityReleaseIntelligenceResult =
  | Readonly<{ status: "success"; releaseIntelligence: QualityReleaseIntelligence }>
  | ReadFailure;

export async function readReleaseIntelligence(
  inspectionId: string,
): Promise<QualityReleaseIntelligenceResult> {
  const contextResult = await resolveQualityReadContext();
  if (contextResult.status === "failure") {
    return { status: "failure", message: contextResult.message };
  }

  const result = await getQualityReleaseIntelligence(contextResult.context, inspectionId);
  if (result.status === "failure") {
    return { status: "failure", message: result.message };
  }

  return { status: "success", releaseIntelligence: result.value };
}
