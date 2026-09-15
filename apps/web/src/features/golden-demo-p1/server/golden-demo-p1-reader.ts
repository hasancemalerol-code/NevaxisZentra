import { denyUnlessGranted } from "@/features/admin-access/server/require-current-permission";
import { denyUnlessPresentationModeAccess } from "@/features/presentation-mode/server/require-presentation-mode-access";
import { parseProductionExecutionRuntimeConfig } from "@/features/production-execution/server/production-order-runtime-config";
import { resolveProductionOrderRequestContext } from "@/features/production-execution/server/production-order-request-context";

import { getGoldenDemoP1Workspace } from "./golden-demo-p1-api-client";

async function resolveAuthorizedContext(permission = "golden-demo-p1.read") {
  const presentation = await denyUnlessPresentationModeAccess();
  if (presentation.status === "failure") {
    return {
      status: "failure" as const,
      code: "permission-denied",
      message: "Sunum modu erişimi reddedildi.",
    };
  }

  const authorization = await denyUnlessGranted(permission);
  if (authorization.status === "failure") {
    return {
      status: "failure" as const,
      code: "permission-denied",
      message: "Altın demo üretim kurtarma görüntüleme yetkiniz yok.",
    };
  }

  const configResult = parseProductionExecutionRuntimeConfig();
  if (configResult.status !== "success") {
    return { status: "failure" as const, code: "configuration-error", message: configResult.message };
  }

  const contextResult = resolveProductionOrderRequestContext(configResult.config);
  if (contextResult.status === "failure") {
    return { status: "failure" as const, code: contextResult.code, message: contextResult.message };
  }

  return { status: "success" as const, context: contextResult.context };
}

export async function readGoldenDemoP1Workspace() {
  const resolved = await resolveAuthorizedContext();
  if (resolved.status === "failure") {
    return resolved;
  }

  const workspace = await getGoldenDemoP1Workspace(resolved.context);
  if (workspace.status === "failure") {
    return { status: "failure" as const, code: workspace.code, message: workspace.message };
  }

  return { status: "success" as const, workspace: workspace.value };
}
