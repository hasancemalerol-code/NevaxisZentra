import "server-only";

import { denyUnlessGranted } from "@/features/admin-access/server/require-current-permission";
import { parseProductionExecutionRuntimeConfig } from "@/features/production-execution/server/production-order-runtime-config";
import { resolveProductionOrderRequestContextForIncomingRequest } from "@/features/production-execution/server/production-order-request-context";

import {
  readCurrentRealFactoryPilot,
  readRealFactoryPilotWorkspace,
  type RealFactoryPilotWorkspace,
} from "./real-factory-pilot-api-client";

export type RealFactoryPilotReadResult =
  | {
      status: "success";
      /** null = nothing was ever initialized for this facility (truthful empty state). */
      workspace: RealFactoryPilotWorkspace | null;
      syntheticInitializationAvailable: boolean;
    }
  | { status: "failure"; message: string };

async function resolveContext(permission = "real-factory-pilot.read") {
  const authorization = await denyUnlessGranted(permission);
  if (authorization.status === "failure") {
    return { status: "failure" as const, message: authorization.code === "authorization-denied" ? "Gerçek fabrika pilot görüntüleme yetkiniz yok." : authorization.message };
  }

  const configResult = parseProductionExecutionRuntimeConfig();
  if (configResult.status !== "success") {
    return { status: "failure" as const, message: configResult.message };
  }

  const contextResult = await resolveProductionOrderRequestContextForIncomingRequest(configResult.config);
  if (contextResult.status === "failure") {
    return { status: "failure" as const, message: contextResult.message };
  }

  return { status: "success" as const, context: contextResult.context };
}

/**
 * Page-load read. It is strictly read-only: viewing the page must never create connectors, runs, fixtures or any
 * other synthetic data in a customer tenant. Initialization is the separate, explicit
 * initializeRealFactoryPilotAction (POST, manage permission, audited, Development/Demo only).
 */
export async function readRealFactoryPilotWorkspaceState(
  pilotRunId?: string,
): Promise<RealFactoryPilotReadResult> {
  const resolved = await resolveContext("real-factory-pilot.read");
  if (resolved.status === "failure") {
    return resolved;
  }

  if (pilotRunId) {
    const run = await readRealFactoryPilotWorkspace(resolved.context, pilotRunId);
    if (run.status === "failure") {
      return { status: "failure", message: run.message };
    }

    return { status: "success", workspace: run.value, syntheticInitializationAvailable: false };
  }

  const current = await readCurrentRealFactoryPilot(resolved.context);
  if (current.status === "failure") {
    return { status: "failure", message: current.message };
  }

  return {
    status: "success",
    workspace: current.value.workspace,
    syntheticInitializationAvailable: current.value.syntheticInitializationAvailable,
  };
}
