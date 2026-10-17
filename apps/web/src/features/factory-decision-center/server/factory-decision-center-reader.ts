import "server-only";

import { denyUnlessGranted } from "@/features/admin-access/server/require-current-permission";
import { parseProductionExecutionRuntimeConfig } from "@/features/production-execution/server/production-order-runtime-config";
import { resolveProductionOrderRequestContextForIncomingRequest } from "@/features/production-execution/server/production-order-request-context";

import {
  readFactoryDecisionCaseDetail,
  readFactoryDecisionCenterWorkspace,
  type FactoryDecisionCaseDetailApi,
  type FactoryDecisionCenterWorkspaceApi,
} from "./factory-decision-center-api-client";

async function resolveAuthorizedContext(permission = "factory-case.read") {
  const authorization = await denyUnlessGranted(permission);
  if (authorization.status === "failure") {
    return {
      status: "failure" as const,
      code: authorization.code === "authorization-denied" ? "permission-denied" : "source-unavailable",
      message: authorization.code === "authorization-denied" ? "Fabrika Karar Merkezi görüntüleme yetkiniz yok." : authorization.message,
    };
  }

  const configResult = parseProductionExecutionRuntimeConfig();
  if (configResult.status !== "success") {
    return { status: "failure" as const, code: "configuration-error", message: configResult.message };
  }

  const contextResult = await resolveProductionOrderRequestContextForIncomingRequest(configResult.config);
  if (contextResult.status === "failure") {
    return { status: "failure" as const, code: contextResult.code, message: contextResult.message };
  }

  return { status: "success" as const, context: contextResult.context };
}

export async function loadFactoryDecisionCenterWorkspace(): Promise<
  | { status: "success"; workspace: FactoryDecisionCenterWorkspaceApi }
  | { status: "failure"; code?: string; message: string }
> {
  const resolved = await resolveAuthorizedContext();
  if (resolved.status === "failure") {
    return resolved;
  }

  const result = await readFactoryDecisionCenterWorkspace(resolved.context);
  if (result.status === "failure") {
    return { status: "failure", code: result.code, message: result.message };
  }
  return { status: "success", workspace: result.value };
}

export async function loadFactoryDecisionCaseDetail(caseId: string): Promise<
  | { status: "success"; detail: FactoryDecisionCaseDetailApi }
  | { status: "failure"; code?: string; message: string }
> {
  const resolved = await resolveAuthorizedContext();
  if (resolved.status === "failure") {
    return resolved;
  }

  const result = await readFactoryDecisionCaseDetail(resolved.context, caseId);
  if (result.status === "failure") {
    return { status: "failure", code: result.code, message: result.message };
  }
  return { status: "success", detail: result.value };
}
