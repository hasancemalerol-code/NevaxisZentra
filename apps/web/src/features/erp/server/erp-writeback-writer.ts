import "./erp-server-boundary";

import {
  approveErpWriteback,
  executeErpWriteback,
  rejectErpWriteback,
  submitErpWriteback,
} from "./erp-api-client";
import { parseProductionExecutionRuntimeConfig } from "../../production-execution/server/production-order-runtime-config";
import {
  resolveProductionOrderRequestContext,
  type ProductionOrderRequestContext,
} from "../../production-execution/server/production-order-request-context";

export type ErpWritebackActionResult =
  | Readonly<{ status: "success"; id: string; version: number }>
  | Readonly<{
      status: "failure";
      code: string;
      message: string;
    }>;

const PREVIEW_BLOCKED: ErpWritebackActionResult = {
  status: "failure",
  code: "preview-write-blocked",
  message: "Yazma işlemleri geliştirme önizleme modunda devre dışı bırakılmıştır.",
};

function resolveWriteContext():
  | { context: ProductionOrderRequestContext }
  | { error: ErpWritebackActionResult } {
  const configResult = parseProductionExecutionRuntimeConfig();
  if (configResult.status !== "success") {
    return {
      error: {
        status: "failure",
        code: "configuration-error",
        message: configResult.message,
      },
    };
  }

  if (configResult.config.mode === "development-preview") {
    return { error: PREVIEW_BLOCKED };
  }

  const contextResult = resolveProductionOrderRequestContext(configResult.config);
  if (contextResult.status === "failure") {
    return {
      error: {
        status: "failure",
        code: contextResult.code,
        message: contextResult.message,
      },
    };
  }

  return { context: contextResult.context };
}

function mapApiFailure(result: { code: string; message: string }): ErpWritebackActionResult {
  return {
    status: "failure",
    code: result.code,
    message: result.message,
  };
}

export async function writeSubmitErpWriteback(
  writebackId: string,
  expectedVersion: number,
): Promise<ErpWritebackActionResult> {
  const resolved = resolveWriteContext();
  if ("error" in resolved) {
    return resolved.error;
  }

  const result = await submitErpWriteback(resolved.context, writebackId, expectedVersion);
  if (result.status === "failure") {
    return mapApiFailure(result);
  }

  return { status: "success", id: result.value.id, version: result.value.version };
}

export async function writeApproveErpWriteback(
  writebackId: string,
  expectedVersion: number,
  comment?: string,
): Promise<ErpWritebackActionResult> {
  const resolved = resolveWriteContext();
  if ("error" in resolved) {
    return resolved.error;
  }

  const result = await approveErpWriteback(
    resolved.context,
    writebackId,
    expectedVersion,
    comment,
  );
  if (result.status === "failure") {
    return mapApiFailure(result);
  }

  return { status: "success", id: result.value.id, version: result.value.version };
}

export async function writeRejectErpWriteback(
  writebackId: string,
  expectedVersion: number,
  comment?: string,
): Promise<ErpWritebackActionResult> {
  const resolved = resolveWriteContext();
  if ("error" in resolved) {
    return resolved.error;
  }

  const result = await rejectErpWriteback(
    resolved.context,
    writebackId,
    expectedVersion,
    comment,
  );
  if (result.status === "failure") {
    return mapApiFailure(result);
  }

  return { status: "success", id: result.value.id, version: result.value.version };
}

export async function writeExecuteErpWriteback(
  writebackId: string,
  expectedVersion: number,
): Promise<ErpWritebackActionResult> {
  const resolved = resolveWriteContext();
  if ("error" in resolved) {
    return resolved.error;
  }

  const result = await executeErpWriteback(resolved.context, writebackId, expectedVersion);
  if (result.status === "failure") {
    return mapApiFailure(result);
  }

  return { status: "success", id: result.value.id, version: result.value.version };
}
