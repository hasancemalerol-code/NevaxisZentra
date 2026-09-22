import "./bulk-data-onboarding-server-boundary";

import { denyUnlessGranted } from "@/features/admin-access/server/require-current-permission";
import { requiredRoutePermission } from "@/lib/authorization/acceptance-role-contract";
import { parseProductionExecutionRuntimeConfig } from "@/features/production-execution/server/production-order-runtime-config";
import { resolveProductionOrderRequestContext } from "@/features/production-execution/server/production-order-request-context";

import type { BulkImportJobWorkspaceApi } from "./bulk-data-onboarding-api-client";

export async function readBulkDataOnboardingWorkspace(
  jobId?: string,
): Promise<
  | { status: "success"; workspace: BulkImportJobWorkspaceApi | null; facilityId: string }
  | { status: "failure"; message: string }
> {
  const authorization = await denyUnlessGranted(requiredRoutePermission("veri-aktarim"));
  if (authorization.status === "failure") {
    return { status: "failure", message: "Veri aktarım merkezi görüntüleme yetkiniz yok." };
  }

  const configResult = parseProductionExecutionRuntimeConfig();
  if (configResult.status !== "success") {
    return { status: "failure", message: configResult.message };
  }

  const contextResult = resolveProductionOrderRequestContext(configResult.config);
  if (contextResult.status === "failure") {
    return { status: "failure", message: contextResult.message };
  }

  if (!jobId) {
    return {
      status: "success",
      facilityId: contextResult.context.facilityId,
      workspace: null,
    };
  }

  const response = await fetch(
    `${contextResult.context.apiBaseUrl}/api/bulk-data-onboarding/jobs/${jobId}?facilityId=${contextResult.context.facilityId}`,
    {
      headers: contextResult.context.requestHeaders,
      cache: "no-store",
    },
  );

  if (!response.ok) {
    return {
      status: "success",
      facilityId: contextResult.context.facilityId,
      workspace: null,
    };
  }

  const workspace = (await response.json()) as BulkImportJobWorkspaceApi;
  return {
    status: "success",
    facilityId: contextResult.context.facilityId,
    workspace,
  };
}
