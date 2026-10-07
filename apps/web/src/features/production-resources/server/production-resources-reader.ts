import "server-only";

import { denyUnlessGranted } from "@/features/admin-access/server/require-current-permission";
import { requiredRoutePermission } from "@/lib/authorization/acceptance-role-contract";
import { parseProductionExecutionRuntimeConfig } from "@/features/production-execution/server/production-order-runtime-config";
import { resolveProductionOrderRequestContextForIncomingRequest } from "@/features/production-execution/server/production-order-request-context";

import { readProductionResourceWorkspace, type ProductionResourceWorkspaceApi } from "./production-resources-api-client";

export async function loadProductionResourceWorkspace(): Promise<
  | { status: "success"; workspace: ProductionResourceWorkspaceApi }
  | { status: "failure"; message: string }
> {
  const authorization = await denyUnlessGranted(requiredRoutePermission("kaynaklar"));
  if (authorization.status === "failure") {
    return { status: "failure", message: authorization.code === "authorization-denied" ? "Üretim kaynakları görüntüleme yetkiniz yok." : authorization.message };
  }

  const configResult = parseProductionExecutionRuntimeConfig();
  if (configResult.status !== "success") {
    return { status: "failure", message: configResult.message };
  }

  const contextResult = await resolveProductionOrderRequestContextForIncomingRequest(configResult.config);
  if (contextResult.status === "failure") {
    return { status: "failure", message: contextResult.message };
  }

  const result = await readProductionResourceWorkspace(contextResult.context);
  if (result.status === "failure") {
    return { status: "failure", message: result.message };
  }

  return { status: "success", workspace: result.value };
}
