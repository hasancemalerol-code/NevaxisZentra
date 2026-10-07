import { denyUnlessGranted } from "@/features/admin-access/server/require-current-permission";
import { denyUnlessPresentationModeAccess } from "@/features/presentation-mode/server/require-presentation-mode-access";
import { parseProductionExecutionRuntimeConfig } from "@/features/production-execution/server/production-order-runtime-config";
import { resolveProductionOrderRequestContextForIncomingRequest } from "@/features/production-execution/server/production-order-request-context";

import { getGoldenDemoP2PresentationStatus } from "./golden-demo-p2-api-client";

async function resolveAuthorizedContext(permission = "golden-demo-p2.read") {
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
      message: authorization.code === "authorization-denied" ? "Altın demo sunum durumu görüntüleme yetkiniz yok." : authorization.message,
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

export async function readGoldenDemoP2PresentationStatus() {
  const resolved = await resolveAuthorizedContext();
  if (resolved.status === "failure") {
    return resolved;
  }

  const status = await getGoldenDemoP2PresentationStatus(resolved.context);
  if (status.status === "failure") {
    return { status: "failure" as const, code: status.code, message: status.message };
  }

  return { status: "success" as const, presentation: status.value };
}
