import "server-only";

import { denyUnlessGranted } from "@/features/admin-access/server/require-current-permission";
import { parseProductionExecutionRuntimeConfig } from "@/features/production-execution/server/production-order-runtime-config";
import { resolveProductionOrderRequestContext } from "@/features/production-execution/server/production-order-request-context";

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
      message: "Fabrika Karar Merkezi görüntüleme yetkiniz yok.",
    };
  }

  const configResult = parseProductionExecutionRuntimeConfig();
  if (configResult.status !== "success") {
    return { status: "failure" as const, message: configResult.message };
  }

  const contextResult = resolveProductionOrderRequestContext(configResult.config);
  if (contextResult.status === "failure") {
    return { status: "failure" as const, message: contextResult.message };
  }

  return { status: "success" as const, context: contextResult.context };
}

export async function loadFactoryDecisionCenterWorkspace(): Promise<
  | { status: "success"; workspace: FactoryDecisionCenterWorkspaceApi }
  | { status: "failure"; message: string }
> {
  const resolved = await resolveAuthorizedContext();
  if (resolved.status === "failure") {
    return resolved;
  }

  const result = await readFactoryDecisionCenterWorkspace(resolved.context);
  if (result.status === "failure") {
    return { status: "failure", message: result.message };
  }
  return { status: "success", workspace: result.value };
}

export async function loadFactoryDecisionCaseDetail(caseId: string): Promise<
  | { status: "success"; detail: FactoryDecisionCaseDetailApi }
  | { status: "failure"; message: string }
> {
  const resolved = await resolveAuthorizedContext();
  if (resolved.status === "failure") {
    return resolved;
  }

  const result = await readFactoryDecisionCaseDetail(resolved.context, caseId);
  if (result.status === "failure") {
    return { status: "failure", message: result.message };
  }
  return { status: "success", detail: result.value };
}
