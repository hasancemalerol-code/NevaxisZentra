import "server-only";

import { denyUnlessGranted } from "@/features/admin-access/server/require-current-permission";
import { parseProductionExecutionRuntimeConfig } from "@/features/production-execution/server/production-order-runtime-config";
import { resolveProductionOrderRequestContext } from "@/features/production-execution/server/production-order-request-context";

import {
  initializeRealFactoryPilot,
  readRealFactoryPilotWorkspace,
  type RealFactoryPilotWorkspace,
} from "./real-factory-pilot-api-client";

export type RealFactoryPilotReadResult =
  | { status: "success"; workspace: RealFactoryPilotWorkspace }
  | { status: "failure"; message: string };

async function resolveContext(permission = "real-factory-pilot.read") {
  const authorization = await denyUnlessGranted(permission);
  if (authorization.status === "failure") {
    return { status: "failure" as const, message: "Gerçek fabrika pilot görüntüleme yetkiniz yok." };
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

export async function readRealFactoryPilotWorkspaceState(
  pilotRunId?: string,
): Promise<RealFactoryPilotReadResult> {
  const resolved = await resolveContext(pilotRunId ? "real-factory-pilot.read" : "real-factory-pilot.manage");
  if (resolved.status === "failure") {
    return resolved;
  }

  const apiResult = pilotRunId
    ? await readRealFactoryPilotWorkspace(resolved.context, pilotRunId)
    : await initializeRealFactoryPilot(resolved.context);

  if (apiResult.status === "failure") {
    return { status: "failure", message: apiResult.message };
  }

  return { status: "success", workspace: apiResult.value };
}
