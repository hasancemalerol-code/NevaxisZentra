"use server";

import "./factory-copilot-server-boundary";

import { denyUnlessGranted } from "@/features/admin-access/server/require-current-permission";
import { parseProductionExecutionRuntimeConfig } from "@/features/production-execution/server/production-order-runtime-config";
import { resolveProductionOrderRequestContext } from "@/features/production-execution/server/production-order-request-context";

import type {
  FactoryCopilotContextInput,
  FactoryCopilotQueryRequest,
  FactoryCopilotQueryResult,
  FactoryCopilotSuggestedQuestionsResult,
} from "../domain/factory-copilot-types";
import {
  fetchFactoryCopilotQuery,
  fetchFactoryCopilotSuggestedQuestions,
} from "./factory-copilot-api-client";

type ActionFailure = Readonly<{ status: "failure"; message: string }>;

async function resolveContext(): Promise<
  | Readonly<{ status: "success"; context: import("@/features/production-execution/server/production-order-request-context").ProductionOrderRequestContext }>
  | ActionFailure
> {
  const configResult = parseProductionExecutionRuntimeConfig();
  if (configResult.status !== "success") {
    return { status: "failure", message: configResult.message };
  }

  const contextResult = resolveProductionOrderRequestContext(configResult.config);
  if (contextResult.status === "failure") {
    return { status: "failure", message: contextResult.message };
  }

  return { status: "success", context: contextResult.context };
}

export async function queryFactoryCopilotAction(
  request: FactoryCopilotQueryRequest,
): Promise<FactoryCopilotQueryResult | ActionFailure> {
  const permission = await denyUnlessGranted("factory-copilot.query");
  if (permission.status === "failure") {
    return permission;
  }

  const resolved = await resolveContext();
  if (resolved.status === "failure") {
    return resolved;
  }

  try {
    return await fetchFactoryCopilotQuery(resolved.context, request);
  } catch (error) {
    return {
      status: "failure",
      message: error instanceof Error ? error.message : "Copilot sorgusu başarısız.",
    };
  }
}

export async function loadFactoryCopilotSuggestedQuestionsAction(
  context: FactoryCopilotContextInput,
): Promise<FactoryCopilotSuggestedQuestionsResult | ActionFailure> {
  const permission = await denyUnlessGranted("factory-copilot.read");
  if (permission.status === "failure") {
    return permission;
  }

  const resolved = await resolveContext();
  if (resolved.status === "failure") {
    return resolved;
  }

  try {
    return await fetchFactoryCopilotSuggestedQuestions(resolved.context, context);
  } catch (error) {
    return {
      status: "failure",
      message: error instanceof Error ? error.message : "Önerilen sorular yüklenemedi.",
    };
  }
}
