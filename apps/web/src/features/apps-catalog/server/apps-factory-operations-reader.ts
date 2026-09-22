import "server-only";

import { parseProductionExecutionRuntimeConfig } from "@/features/production-execution/server/production-order-runtime-config";
import { resolveProductionOrderRequestContext } from "@/features/production-execution/server/production-order-request-context";
import {
  getProductionCycleAnalytics,
  getProductionReportingOverview,
  getProductionScrapAnalytics,
  listProductionOrderPerformance,
  listProductionShiftReporting,
} from "@/features/production-reporting/server/production-reporting-api-client";
import { readProcessOeeAssessments, readProcessOeeWorkspace } from "@/features/process-oee/server/process-oee-reader";
import { readQualityReportingOverview } from "@/features/quality-control/server/quality-control-reader";
import { resolveSaasIdentitySession } from "@/features/saas-identity/server/saas-identity-session";
import type { DashboardShowcaseViewModel } from "@/features/dashboard-showcase/domain/dashboard-showcase-types";
import {
  mapDashboardShowcaseViewModel,
  type ShowcaseSourceData,
} from "@/features/dashboard-showcase/server/dashboard-showcase-mapper";

async function resolveFacilityDisplayName(): Promise<string> {
  const session = await resolveSaasIdentitySession();
  if (session.status === "authenticated") {
    const organizationLabel = session.account.organizationLabel?.trim();
    if (organizationLabel) {
      return organizationLabel;
    }
  }

  return "—";
}

export async function readAppsFactoryOperationsDashboard(): Promise<DashboardShowcaseViewModel | null> {
  const configResult = parseProductionExecutionRuntimeConfig();
  if (configResult.status !== "success") {
    return null;
  }

  const contextResult = resolveProductionOrderRequestContext(configResult.config);
  if (contextResult.status === "failure") {
    return null;
  }

  const context = contextResult.context;
  const refreshedAtUtc = new Date().toISOString();

  const [
    facilityDisplayName,
    overviewResult,
    ordersResult,
    shiftsResult,
    scrapResult,
    cyclesResult,
    oeeWorkspaceResult,
    assessmentsResult,
    qualityOverviewResult,
  ] = await Promise.all([
    resolveFacilityDisplayName(),
    getProductionReportingOverview(context),
    listProductionOrderPerformance(context),
    listProductionShiftReporting(context),
    getProductionScrapAnalytics(context),
    getProductionCycleAnalytics(context),
    readProcessOeeWorkspace(),
    readProcessOeeAssessments(),
    readQualityReportingOverview(),
  ]);

  const sortedAssessments = assessmentsResult.status === "success"
    ? [...assessmentsResult.assessments].sort(
        (left, right) => Date.parse(right.evaluatedAtUtc) - Date.parse(left.evaluatedAtUtc),
      )
    : [];

  const latestAssessment = sortedAssessments[0] ?? null;
  const previousAssessment = sortedAssessments[1] ?? null;

  const latestOeePercent = latestAssessment?.oeePercent
    ?? (oeeWorkspaceResult.status === "success" ? oeeWorkspaceResult.summary.latestOeePercent : null);

  const cycleDurationMs = cyclesResult.status === "success"
    ? cyclesResult.value.recentCycleDurationMilliseconds
      ?? cyclesResult.value.averageCycleDurationMilliseconds
      ?? null
    : null;

  const source: ShowcaseSourceData = {
    facilityDisplayName,
    developmentPilotBanner: false,
    overview: overviewResult.status === "success" ? overviewResult.value : null,
    orders: ordersResult.status === "success" ? ordersResult.value : [],
    shifts: shiftsResult.status === "success" ? shiftsResult.value : [],
    scrapRatePercent: scrapResult.status === "success"
      ? scrapResult.value.scrapRatePercent ?? null
      : null,
    cycleDurationMs,
    latestOeePercent,
    previousOeePercent: previousAssessment?.oeePercent ?? null,
    latestAssessment,
    qualityOverview: qualityOverviewResult.status === "success"
      ? qualityOverviewResult.overview
      : null,
    assessments: sortedAssessments,
  };

  return mapDashboardShowcaseViewModel(source, refreshedAtUtc);
}
