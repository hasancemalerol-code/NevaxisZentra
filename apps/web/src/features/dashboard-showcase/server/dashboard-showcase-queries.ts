import "./dashboard-showcase-server-boundary";

import { parseProductionExecutionRuntimeConfig } from "@/features/production-execution/server/production-order-runtime-config";
import { resolveProductionOrderRequestContextForIncomingRequest } from "@/features/production-execution/server/production-order-request-context";
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

import type { DashboardShowcaseViewModel } from "../domain/dashboard-showcase-types";
import { buildDevelopmentShowcaseViewModel } from "../domain/dashboard-showcase-development-dataset";
import { isLiveShowcaseSourceComplete } from "../domain/dashboard-showcase-presentation";
import { mapDashboardShowcaseViewModel } from "./dashboard-showcase-mapper";

export type DashboardShowcaseReadResult =
  | Readonly<{ status: "success"; viewModel: DashboardShowcaseViewModel }>
  | Readonly<{ status: "failure"; message: string }>;

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

function isDevelopmentPilotBanner(): boolean {
  const configResult = parseProductionExecutionRuntimeConfig();
  if (configResult.status !== "success") {
    return false;
  }

  return configResult.config.mode === "development-api"
    || configResult.config.mode === "development-preview";
}

export async function readDashboardShowcaseWorkspace(): Promise<DashboardShowcaseReadResult> {
  const configResult = parseProductionExecutionRuntimeConfig();
  if (configResult.status !== "success") {
    return {
      status: "failure",
      message: configResult.message,
    };
  }

  const contextResult = await resolveProductionOrderRequestContextForIncomingRequest(configResult.config);
  if (contextResult.status === "failure") {
    return {
      status: "failure",
      message: contextResult.message,
    };
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

  const source = {
    facilityDisplayName,
    developmentPilotBanner: isDevelopmentPilotBanner(),
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

  const isDevelopmentRuntime = configResult.config.mode === "development-api"
    || configResult.config.mode === "development-preview";

  if (isDevelopmentRuntime && !isLiveShowcaseSourceComplete(source, sortedAssessments)) {
    return {
      status: "success",
      viewModel: buildDevelopmentShowcaseViewModel(facilityDisplayName, refreshedAtUtc),
    };
  }

  const viewModel = mapDashboardShowcaseViewModel(
    source,
    refreshedAtUtc,
  );

  return {
    status: "success",
    viewModel,
  };
}
