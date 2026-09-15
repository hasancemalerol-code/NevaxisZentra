import { denyUnlessGranted } from "@/features/admin-access/server/require-current-permission";
import { denyUnlessPresentationModeAccess } from "@/features/presentation-mode/server/require-presentation-mode-access";
import { parseProductionExecutionRuntimeConfig } from "@/features/production-execution/server/production-order-runtime-config";
import { resolveProductionOrderRequestContext } from "@/features/production-execution/server/production-order-request-context";

import { getGoldenDemoP2BDigitalFactoryTeam } from "./golden-demo-p2b-api-client";

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
      message: "Dijital fabrika ekibi görüntüleme yetkiniz yok.",
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

export async function readGoldenDemoP2BDigitalFactoryTeam() {
  const resolved = await resolveAuthorizedContext();
  if (resolved.status === "failure") {
    return resolved;
  }

  const snapshot = await getGoldenDemoP2BDigitalFactoryTeam(resolved.context);
  if (snapshot.status === "failure") {
    return { status: "failure" as const, code: snapshot.code, message: snapshot.message };
  }

  return { status: "success" as const, team: snapshot.value };
}
