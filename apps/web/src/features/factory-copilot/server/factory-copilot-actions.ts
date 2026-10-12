"use server";

import "./factory-copilot-server-boundary";

import {
  AUTHENTICATION_REQUIRED_MESSAGE,
  denyUnlessGranted,
} from "@/features/admin-access/server/require-current-permission";
import { requiredRoutePermission, requiredRouteQueryPermission } from "@/lib/authorization/acceptance-role-contract";
import { parseProductionExecutionRuntimeConfig } from "@/features/production-execution/server/production-order-runtime-config";
import { resolveProductionOrderRequestContextForIncomingRequest } from "@/features/production-execution/server/production-order-request-context";

import type {
  FactoryCopilotContextInput,
  FactoryCopilotQueryRequest,
  FactoryCopilotQueryResult,
  FactoryCopilotSuggestedQuestionsResult,
} from "../domain/factory-copilot-types";
import { describeFactoryCopilotRequestFailure } from "../domain/factory-copilot-failures";
import {
  FactoryCopilotRequestError,
  fetchFactoryCopilotQuery,
  fetchFactoryCopilotSuggestedQuestions,
} from "./factory-copilot-api-client";

type ActionFailure = Readonly<{ status: "failure"; message: string }>;

const SESSION_EXPIRED_MESSAGE = "Oturumunuzun süresi doldu. Devam etmek için yeniden oturum açın.";

/** The shared permission guard answers an expired session in English; the Copilot shows it in the product language. */
function localizeFailure(failure: ActionFailure): ActionFailure {
  return failure.message === AUTHENTICATION_REQUIRED_MESSAGE
    ? { status: "failure", message: SESSION_EXPIRED_MESSAGE }
    : failure;
}

function describeCopilotError(error: unknown): string {
  if (error instanceof FactoryCopilotRequestError) {
    return describeFactoryCopilotRequestFailure(error.failure, error.backendMessage).message;
  }

  // A network failure or an unreadable response: the service could not be reached, not a "Copilot failure".
  return "Copilot servisine şu an ulaşılamıyor. Biraz sonra tekrar deneyin.";
}

async function resolveContext(): Promise<
  | Readonly<{ status: "success"; context: import("@/features/production-execution/server/production-order-request-context").ProductionOrderRequestContext }>
  | ActionFailure
> {
  const configResult = parseProductionExecutionRuntimeConfig();
  if (configResult.status !== "success") {
    return { status: "failure", message: configResult.message };
  }

  const contextResult = await resolveProductionOrderRequestContextForIncomingRequest(configResult.config);
  if (contextResult.status === "failure") {
    return { status: "failure", message: contextResult.message };
  }

  return { status: "success", context: contextResult.context };
}

export async function queryFactoryCopilotAction(
  request: FactoryCopilotQueryRequest,
): Promise<FactoryCopilotQueryResult | ActionFailure> {
  const permission = await denyUnlessGranted(requiredRouteQueryPermission("factory-copilot"));
  if (permission.status === "failure") {
    return localizeFailure(permission);
  }

  const resolved = await resolveContext();
  if (resolved.status === "failure") {
    return localizeFailure(resolved);
  }

  try {
    return await fetchFactoryCopilotQuery(resolved.context, request);
  } catch (error) {
    return {
      status: "failure",
      message: describeCopilotError(error),
    };
  }
}

export async function loadFactoryCopilotSuggestedQuestionsAction(
  context: FactoryCopilotContextInput,
): Promise<FactoryCopilotSuggestedQuestionsResult | ActionFailure> {
  const permission = await denyUnlessGranted(requiredRoutePermission("factory-copilot"));
  if (permission.status === "failure") {
    return localizeFailure(permission);
  }

  const resolved = await resolveContext();
  if (resolved.status === "failure") {
    return localizeFailure(resolved);
  }

  try {
    return await fetchFactoryCopilotSuggestedQuestions(resolved.context, context);
  } catch (error) {
    return {
      status: "failure",
      message: describeCopilotError(error),
    };
  }
}
