import "./production-order-server-boundary";

import type {
  ProductionFollowUpDetail,
  ProductionFollowUpWorkspacePayload,
} from "../domain/production-follow-up";
import {
  getProductionFollowUpDetail,
  getProductionFollowUpWorkspace,
} from "./production-follow-up-api-client";
import {
  parseProductionExecutionRuntimeConfig,
} from "./production-order-runtime-config";
import {
  resolveProductionOrderRequestContext,
  type ProductionOrderRequestContext,
} from "./production-order-request-context";

export type ProductionFollowUpWorkspaceResult =
  | Readonly<{
      status: "success";
      workspace: ProductionFollowUpWorkspacePayload;
      exception?: ProductionFollowUpDetail;
      exceptionError?: string;
    }>
  | Readonly<{ status: "failure"; message: string }>;

function resolveReadContext():
  | Readonly<{ status: "success"; context: ProductionOrderRequestContext }>
  | Readonly<{ status: "failure"; message: string }> {
  const configResult = parseProductionExecutionRuntimeConfig();
  if (configResult.status === "failure") {
    return { status: "failure", message: configResult.message };
  }
  if (configResult.config.mode === "development-preview") {
    return {
      status: "failure",
      message: "Development preview mode is not configured for production follow-up.",
    };
  }
  const contextResult = resolveProductionOrderRequestContext(configResult.config);
  if (contextResult.status === "failure") {
    return { status: "failure", message: contextResult.message };
  }
  return { status: "success", context: contextResult.context };
}

export async function readProductionFollowUpWorkspace(
  exceptionId?: string,
): Promise<ProductionFollowUpWorkspaceResult> {
  const contextResult = resolveReadContext();
  if (contextResult.status === "failure") {
    return { status: "failure", message: contextResult.message };
  }

  const workspaceResult = await getProductionFollowUpWorkspace(contextResult.context);
  if (workspaceResult.status === "failure") {
    return { status: "failure", message: workspaceResult.message };
  }

  const selectedExceptionId =
    exceptionId ?? workspaceResult.value.exceptions[0]?.id;
  let exception: ProductionFollowUpDetail | undefined;
  let exceptionError: string | undefined;

  if (selectedExceptionId) {
    const detailResult = await getProductionFollowUpDetail(contextResult.context, selectedExceptionId);
    if (detailResult.status === "success") {
      exception = detailResult.value;
    } else {
      exceptionError = detailResult.message;
    }
  }

  return {
    status: "success",
    workspace: workspaceResult.value,
    exception,
    exceptionError,
  };
}
