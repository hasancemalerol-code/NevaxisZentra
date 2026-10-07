import "./golden-demo-customs-file-server-boundary";

import { denyUnlessGranted } from "@/features/admin-access/server/require-current-permission";
import { denyUnlessPresentationModeAccess } from "@/features/presentation-mode/server/require-presentation-mode-access";
import { parseProductionExecutionRuntimeConfig } from "@/features/production-execution/server/production-order-runtime-config";
import { resolveProductionOrderRequestContextForIncomingRequest } from "@/features/production-execution/server/production-order-request-context";

import { getGoldenDemoCustomsFileWorkspace } from "./golden-demo-customs-file-api-client";

export async function readGoldenDemoCustomsFileWorkspace() {
  const presentation = await denyUnlessPresentationModeAccess();
  if (presentation.status === "failure") {
    return { status: "failure" as const, message: "Sunum modu erişimi reddedildi." };
  }

  const auth = await denyUnlessGranted("golden-demo-customs-file.read");
  if (auth.status === "failure") {
    return { status: "failure" as const, message: auth.code === "authorization-denied" ? "Gümrük dosyası altın demo erişimi reddedildi." : auth.message };
  }

  const configResult = parseProductionExecutionRuntimeConfig();
  if (configResult.status !== "success") {
    return { status: "failure" as const, message: configResult.message };
  }

  const contextResult = await resolveProductionOrderRequestContextForIncomingRequest(configResult.config);
  if (contextResult.status === "failure") {
    return { status: "failure" as const, message: contextResult.message };
  }

  const result = await getGoldenDemoCustomsFileWorkspace(contextResult.context);
  if (result.status === "failure") {
    return { status: "failure" as const, message: result.message };
  }

  return { status: "success" as const, workspace: result.value };
}
