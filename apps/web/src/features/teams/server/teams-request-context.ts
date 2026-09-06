import "./teams-server-boundary";

import {
  hasIncomingSessionCookie,
  readIncomingRequestCookieHeader,
} from "@/features/saas-identity/server/authenticated-server-fetch";
import { resolveSaasIdentityRuntimeConfig } from "@/features/saas-identity/server/saas-identity-runtime-config";

import { resolveAiTaskRequestContextFromIncomingRequest } from "../../ai-task-center/server/ai-task-request-context";
import type { ProductionOrderRequestContext } from "../../production-execution/server/production-order-request-context";

export type TeamsRequestContext =
  | Readonly<{ mode: "session"; facilityId: string }>
  | Readonly<{ mode: "development"; context: ProductionOrderRequestContext }>;

export type TeamsRequestContextResult =
  | Readonly<{ status: "success"; context: TeamsRequestContext }>
  | Readonly<{ status: "failure"; message: string; code: string }>;

function resolveFacilityId(): string {
  return (
    process.env.CREVAFACTOS_DEV_FACILITY_ID?.trim()
    ?? process.env.CREVAFACTOS_FACILITY_ID?.trim()
    ?? ""
  );
}

export async function resolveTeamsRequestContext(): Promise<TeamsRequestContextResult> {
  const cookieHeader = await readIncomingRequestCookieHeader();
  if (hasIncomingSessionCookie(cookieHeader)) {
    const config = resolveSaasIdentityRuntimeConfig();
    const facilityId = resolveFacilityId();
    if (config.status === "success" && facilityId) {
      return {
        status: "success",
        context: { mode: "session", facilityId },
      };
    }
  }

  const developmentContext = await resolveAiTaskRequestContextFromIncomingRequest();
  if (developmentContext.status === "failure") {
    return {
      status: "failure",
      message: developmentContext.message,
      code: developmentContext.code,
    };
  }

  return {
    status: "success",
    context: {
      mode: "development",
      context: developmentContext.context,
    },
  };
}
