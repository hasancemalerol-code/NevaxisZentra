"use server";

import "./production-order-server-boundary";

import {
  completeProductionOrder,
  pauseProductionOrder,
  resumeProductionOrder,
  startProductionOrder,
  recordGoodProduction,
  recordScrap,
  reverseEntry,
  type ProductionOrderWriteResult,
} from "./production-order-api-client";
import {
  resolveProductionOrderRequestContext,
  type ProductionOrderRequestContext,
} from "./production-order-request-context";
import {
  parseProductionExecutionRuntimeConfig,
} from "./production-order-runtime-config";

export type ProductionOrderActionResult = Readonly<{
  status: "success" | "failure";
  code?: string;
  message?: string;
  id?: string;
  referenceCode?: string;
  orderStatus?: string;
  version?: number;
}>;

type ResolvedWriteContext =
  | Readonly<{ kind: "context"; context: ProductionOrderRequestContext }>
  | Readonly<{ kind: "failure"; failure: ProductionOrderActionResult }>;

function resolveWriteContext(): ResolvedWriteContext {
  const configResult = parseProductionExecutionRuntimeConfig();

  if (configResult.status === "failure") {
    return {
      kind: "failure",
      failure: {
        status: "failure",
        code: "configuration-error",
        message: configResult.message,
      },
    };
  }

  if (configResult.config.mode === "development-preview") {
    return {
      kind: "failure",
      failure: {
        status: "failure",
        code: "preview-writes-blocked",
        message: "Production order writes are blocked in preview mode.",
      },
    };
  }

  const contextResult = resolveProductionOrderRequestContext(
    configResult.config,
  );

  if (contextResult.status === "failure") {
    return {
      kind: "failure",
      failure: {
        status: "failure",
        code: contextResult.code,
        message: contextResult.message,
      },
    };
  }

  return { kind: "context", context: contextResult.context };
}

function mapWriteResult(result: ProductionOrderWriteResult): ProductionOrderActionResult {
  return {
    status: result.status,
    code: result.code,
    message: result.message,
    id: result.id,
    referenceCode: result.referenceCode,
    orderStatus: result.orderStatus,
    version: result.version,
  };
}

export async function startProductionOrderAction(
  productionOrderId: string,
  expectedVersion: number,
): Promise<ProductionOrderActionResult> {
  const resolved = resolveWriteContext();

  if (resolved.kind === "failure") {
    return resolved.failure;
  }

  const result = await startProductionOrder(
    resolved.context,
    productionOrderId,
    expectedVersion,
  );

  return mapWriteResult(result);
}

export async function pauseProductionOrderAction(
  productionOrderId: string,
  expectedVersion: number,
  reason: string,
): Promise<ProductionOrderActionResult> {
  const resolved = resolveWriteContext();

  if (resolved.kind === "failure") {
    return resolved.failure;
  }

  if (!reason.trim()) {
    return {
      status: "failure",
      code: "invalid-request",
      message: "Pause reason is required.",
    };
  }

  const result = await pauseProductionOrder(
    resolved.context,
    productionOrderId,
    expectedVersion,
    reason.trim(),
  );

  return mapWriteResult(result);
}

export async function resumeProductionOrderAction(
  productionOrderId: string,
  expectedVersion: number,
): Promise<ProductionOrderActionResult> {
  const resolved = resolveWriteContext();

  if (resolved.kind === "failure") {
    return resolved.failure;
  }

  const result = await resumeProductionOrder(
    resolved.context,
    productionOrderId,
    expectedVersion,
  );

  return mapWriteResult(result);
}

export async function completeProductionOrderAction(
  productionOrderId: string,
  expectedVersion: number,
  options?: { allowShortCompletion?: boolean; completionReason?: string },
): Promise<ProductionOrderActionResult> {
  const resolved = resolveWriteContext();

  if (resolved.kind === "failure") {
    return resolved.failure;
  }

  const result = await completeProductionOrder(
    resolved.context,
    productionOrderId,
    expectedVersion,
    options,
  );

  return mapWriteResult(result);
}

export async function recordGoodProductionAction(
  productionOrderId: string,
  expectedVersion: number,
  quantity: number,
  comment?: string,
): Promise<ProductionOrderActionResult> {
  const resolved = resolveWriteContext();

  if (resolved.kind === "failure") {
    return resolved.failure;
  }

  if (quantity <= 0) {
    return { status: "failure", code: "invalid-quantity", message: "Miktar sıfırdan büyük olmalıdır." };
  }

  const result = await recordGoodProduction(
    resolved.context,
    productionOrderId,
    expectedVersion,
    quantity,
    comment,
  );

  return mapWriteResult(result);
}

export async function recordScrapAction(
  productionOrderId: string,
  expectedVersion: number,
  quantity: number,
  reasonCode: string,
  comment?: string,
): Promise<ProductionOrderActionResult> {
  const resolved = resolveWriteContext();

  if (resolved.kind === "failure") {
    return resolved.failure;
  }

  if (quantity <= 0) {
    return { status: "failure", code: "invalid-quantity", message: "Miktar sıfırdan büyük olmalıdır." };
  }

  if (!reasonCode.trim()) {
    return { status: "failure", code: "invalid-scrap-reason", message: "Fire nedeni zorunludur." };
  }

  const result = await recordScrap(
    resolved.context,
    productionOrderId,
    expectedVersion,
    quantity,
    reasonCode.trim(),
    comment,
  );

  return mapWriteResult(result);
}

export async function reverseEntryAction(
  productionOrderId: string,
  entryId: string,
  expectedVersion: number,
  reason: string,
): Promise<ProductionOrderActionResult> {
  const resolved = resolveWriteContext();

  if (resolved.kind === "failure") {
    return resolved.failure;
  }

  if (!reason.trim()) {
    return { status: "failure", code: "invalid-request", message: "İptal nedeni zorunludur." };
  }

  const result = await reverseEntry(
    resolved.context,
    productionOrderId,
    entryId,
    expectedVersion,
    reason.trim(),
  );

  return mapWriteResult(result);
}
