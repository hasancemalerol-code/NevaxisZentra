import "server-only";

import { parseProductionExecutionRuntimeConfig } from "@/features/production-execution/server/production-order-runtime-config";
import { resolveProductionOrderRequestContext } from "@/features/production-execution/server/production-order-request-context";
import {
  getProductionDowntimeReporting,
  getProductionReportingOverview,
  getProductionScrapAnalytics,
  listProductionShiftReporting,
} from "@/features/production-reporting/server/production-reporting-api-client";
import { readProductionResourceWorkspace } from "@/features/production-resources/server/production-resources-api-client";

import type { AppsOperationalAnalyticsModel } from "../domain/apps-operational-analytics-types";
import { EMPTY_APPS_OPERATIONAL_ANALYTICS } from "../domain/apps-operational-analytics-types";
import { buildAppsOperationalAnalytics } from "./apps-operational-analytics-builder";

export type AppsOperationalOverviewModel = AppsOperationalAnalyticsModel;

export async function readAppsOperationalOverview(): Promise<AppsOperationalOverviewModel> {
  const configResult = parseProductionExecutionRuntimeConfig();
  if (configResult.status !== "success") {
    return EMPTY_APPS_OPERATIONAL_ANALYTICS;
  }

  const contextResult = resolveProductionOrderRequestContext(configResult.config);
  if (contextResult.status === "failure") {
    return EMPTY_APPS_OPERATIONAL_ANALYTICS;
  }

  const context = contextResult.context;
  const [overviewResult, resourcesResult, shiftsResult, scrapResult, downtimeResult] = await Promise.all([
    getProductionReportingOverview(context),
    readProductionResourceWorkspace(context),
    listProductionShiftReporting(context),
    getProductionScrapAnalytics(context),
    getProductionDowntimeReporting(context),
  ]);

  const overview = overviewResult.status === "success" ? overviewResult.value : null;
  const resources =
    resourcesResult.status === "success" ? resourcesResult.value.resources : [];
  const shifts = shiftsResult.status === "success" ? shiftsResult.value : [];
  const scrap = scrapResult.status === "success" ? scrapResult.value : null;
  const downtime = downtimeResult.status === "success" ? downtimeResult.value : null;

  return buildAppsOperationalAnalytics({
    overview,
    shifts,
    resources,
    scrapRatePercent: scrap?.scrapRatePercent ?? null,
    scrapQuantity: scrap?.scrapQuantity ?? null,
    goodQuantityFromScrap: scrap?.goodQuantity ?? null,
    downtimeActiveCount: downtime?.activeDowntimeCount ?? null,
    downtimeTotalMinutes: downtime?.totalDowntimeMinutes ?? null,
  });
}
