import "./production-order-server-boundary";

import type {
  ProductionOrder,
  ProductionOrderSummary,
} from "../domain/production-order";
import {
  getProductionOrderById,
  listProductionOrders,
  listQuantityEntries,
  type ProductionOrderApiFailure,
} from "./production-order-api-client";
import type { ProductionExecutionEntry } from "../domain/production-execution-entry";
import {
  parseProductionExecutionEntryListApiResponse,
} from "./production-execution-entry-api-contracts";
import {
  resolveProductionOrderRequestContext,
  type ProductionOrderRequestContext,
} from "./production-order-request-context";
import {
  parseProductionExecutionRuntimeConfig,
} from "./production-order-runtime-config";

type ProductionOrderReadFailure =
  | ProductionOrderApiFailure
  | Readonly<{
      status: "failure";
      code: "configuration-error" | "invalid-scope" | "unauthenticated";
      message: string;
      retryable: false;
    }>;

export type ProductionOrderListResult =
  | Readonly<{ status: "success"; orders: ProductionOrderSummary[] }>
  | ProductionOrderReadFailure;

export type ProductionOrderDetailResult =
  | Readonly<{ status: "success"; order: ProductionOrder }>
  | ProductionOrderReadFailure;

export type ProductionEntryListResult =
  | Readonly<{ status: "success"; entries: ProductionExecutionEntry[] }>
  | ProductionOrderReadFailure;

type ResolvedContext =
  | Readonly<{ kind: "context"; context: ProductionOrderRequestContext }>
  | Readonly<{ kind: "failure"; failure: ProductionOrderReadFailure }>;

function resolveReadContext(): ResolvedContext {
  const configResult = parseProductionExecutionRuntimeConfig();

  if (configResult.status === "failure") {
    return {
      kind: "failure",
      failure: {
        status: "failure",
        code: "configuration-error",
        message: configResult.message,
        retryable: false,
      },
    };
  }

  if (configResult.config.mode === "development-preview") {
    return {
      kind: "failure",
      failure: {
        status: "failure",
        code: "configuration-error",
        message: "Development preview mode is not configured for production execution.",
        retryable: false,
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
        retryable: false,
      },
    };
  }

  return { kind: "context", context: contextResult.context };
}

export async function readProductionOrders(
  query?: { status?: string; search?: string; limit?: number },
): Promise<ProductionOrderListResult> {
  const resolved = resolveReadContext();

  if (resolved.kind === "failure") {
    return resolved.failure;
  }

  const { context } = resolved;

  const result = await listProductionOrders(context, {
    facilityId: context.facilityId,
    status: query?.status,
    search: query?.search,
    limit: query?.limit,
  });

  if (result.status === "failure") {
    return result;
  }

  return { status: "success", orders: result.value };
}

export async function readProductionOrderById(
  productionOrderId: string,
): Promise<ProductionOrderDetailResult> {
  const normalizedId = productionOrderId.trim();

  if (!normalizedId) {
    return {
      status: "failure",
      code: "invalid-scope",
      message: "productionOrderId is required.",
      retryable: false,
    };
  }

  const resolved = resolveReadContext();

  if (resolved.kind === "failure") {
    return resolved.failure;
  }

  const { context } = resolved;

  const result = await getProductionOrderById(
    context,
    normalizedId,
    context.facilityId,
  );

  if (result.status === "failure") {
    return result;
  }

  return { status: "success", order: result.value };
}

export async function readProductionEntries(
  productionOrderId: string,
  limit?: number,
): Promise<ProductionEntryListResult> {
  const normalizedId = productionOrderId.trim();

  if (!normalizedId) {
    return {
      status: "failure",
      code: "invalid-scope",
      message: "productionOrderId is required.",
      retryable: false,
    };
  }

  const resolved = resolveReadContext();

  if (resolved.kind === "failure") {
    return resolved.failure;
  }

  const { context } = resolved;

  const result = await listQuantityEntries(context, normalizedId, context.facilityId, limit);

  if (result.status === "failure") {
    return result;
  }

  const parsed = parseProductionExecutionEntryListApiResponse(result.value);
  if (parsed.status === "failure") {
    return {
      status: "failure",
      code: "invalid-response" as const,
      message: parsed.message,
      retryable: false,
    };
  }

  return { status: "success", entries: parsed.value };
}
