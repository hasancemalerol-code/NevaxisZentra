import "./shift-handover-server-boundary";

import {
  hasIncomingSessionCookie,
  readIncomingRequestCookieHeader,
} from "@/features/saas-identity/server/authenticated-server-fetch";
import { resolveSaasIdentityRuntimeConfig } from "@/features/saas-identity/server/saas-identity-runtime-config";
import { resolveAiTaskRequestContextFromIncomingRequest } from "@/features/ai-task-center/server/ai-task-request-context";

export type ShiftHandoverRequestContext =
  | Readonly<{ mode: "session"; facilityId: string }>
  | Readonly<{ mode: "development"; facilityId: string; headers: Record<string, string> }>;

export type ShiftHandoverRequestContextResult =
  | Readonly<{ status: "success"; context: ShiftHandoverRequestContext }>
  | Readonly<{ status: "failure"; message: string; code: string }>;

function resolveFacilityId(): string {
  return (
    process.env.NEVAXIS_FACTOS_DEV_FACILITY_ID?.trim()
    ?? process.env.NEVAXIS_FACTOS_FACILITY_ID?.trim()
    ?? ""
  );
}

export async function resolveShiftHandoverRequestContext(): Promise<ShiftHandoverRequestContextResult> {
  const cookieHeader = await readIncomingRequestCookieHeader();
  if (hasIncomingSessionCookie(cookieHeader)) {
    const config = resolveSaasIdentityRuntimeConfig();
    const facilityId = resolveFacilityId();
    if (config.status === "success" && facilityId) {
      return { status: "success", context: { mode: "session", facilityId } };
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
      facilityId: developmentContext.context.facilityId,
      headers: developmentContext.context.requestHeaders,
    },
  };
}
