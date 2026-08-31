import "./dashboard-server-boundary";

import {
  hasIncomingSessionCookie,
  readIncomingRequestCookieHeader,
} from "@/features/saas-identity/server/authenticated-server-fetch";
import { resolveSaasIdentityRuntimeConfig } from "@/features/saas-identity/server/saas-identity-runtime-config";

import { resolveAiTaskRequestContext } from "../../ai-task-center/server/ai-task-request-context";
import type { ProductionOrderRequestContext } from "../../production-execution/server/production-order-request-context";

const DEVELOPMENT_PILOT_FACILITY_ID = "bb000001-0005-4000-8000-000000000005";

export type DashboardRequestContext =
  | Readonly<{ mode: "session"; facilityId: string }>
  | Readonly<{ mode: "development"; context: ProductionOrderRequestContext }>;

export type DashboardRequestContextResult =
  | Readonly<{ status: "success"; context: DashboardRequestContext }>
  | Readonly<{ status: "failure"; message: string; code: string }>;

function resolveFacilityId(sessionMode: boolean): string {
  const configured =
    process.env.CREVAFACTOS_DEV_FACILITY_ID?.trim()
    ?? process.env.CREVAFACTOS_FACILITY_ID?.trim()
    ?? "";

  if (configured) {
    return configured;
  }

  if (sessionMode && process.env.NODE_ENV === "development") {
    return DEVELOPMENT_PILOT_FACILITY_ID;
  }

  return "";
}

export async function resolveDashboardRequestContext(): Promise<DashboardRequestContextResult> {
  const cookieHeader = await readIncomingRequestCookieHeader();
  if (hasIncomingSessionCookie(cookieHeader)) {
    const config = resolveSaasIdentityRuntimeConfig();
    const facilityId = resolveFacilityId(true);
    if (config.status === "success" && facilityId) {
      return {
        status: "success",
        context: { mode: "session", facilityId },
      };
    }
  }

  const developmentContext = resolveAiTaskRequestContext();
  if (developmentContext.status === "failure") {
    return {
      status: "failure",
      message: developmentContext.message,
      code: developmentContext.code,
    };
  }

  return {
    status: "success",
    context: { mode: "development", context: developmentContext.context },
  };
}
