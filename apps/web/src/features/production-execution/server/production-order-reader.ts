import "./production-order-server-boundary";

import type {
  ProductionOrder,
  ProductionOrderSummary,
} from "../domain/production-order";
import type {
  ProductionDowntimeSummary,
  ProductionSetupSessionSummary,
  ProductionCycleEntrySummary,
  ProductionRuntimeSummary,
} from "../domain/production-execution-operations";
import type {
  ProductionExecutionExceptionSummary,
  ProductionExecutionHealthSummary,
} from "../domain/production-execution-exceptions";
import type {
  ShiftAssignmentSummary,
  OperatorAssignmentSummary,
  ShiftHandoverSummary,
  ShiftExecutionSummary,
  OperatorExecutionSummary,
} from "../domain/production-execution-resources";
import {
  getProductionOrderById,
  listProductionOrders,
  listQuantityEntries,
  listDowntimes,
  listSetupSessions,
  listCycleEntries,
  getRuntimeSummary,
  listExceptions,
  getHealthSummary,
  listShiftAssignments,
  listOperatorAssignments,
  listHandovers,
  getShiftExecutionSummary,
  getOperatorExecutionSummary,
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

export type ProductionDowntimeListResult =
  | Readonly<{ status: "success"; downtimes: ProductionDowntimeSummary[] }>
  | ProductionOrderReadFailure;

export type ProductionSetupSessionListResult =
  | Readonly<{ status: "success"; sessions: ProductionSetupSessionSummary[] }>
  | ProductionOrderReadFailure;

export type ProductionCycleEntryListResult =
  | Readonly<{ status: "success"; cycles: ProductionCycleEntrySummary[] }>
  | ProductionOrderReadFailure;

export type ProductionRuntimeSummaryResult =
  | Readonly<{ status: "success"; summary: ProductionRuntimeSummary }>
  | ProductionOrderReadFailure;

export type ProductionExceptionListResult =
  | Readonly<{ status: "success"; exceptions: ProductionExecutionExceptionSummary[] }>
  | ProductionOrderReadFailure;

export type ProductionHealthSummaryResult =
  | Readonly<{ status: "success"; summary: ProductionExecutionHealthSummary }>
  | ProductionOrderReadFailure;

// 5L result types
export type ShiftAssignmentListResult =
  | Readonly<{ status: "success"; assignments: ShiftAssignmentSummary[] }>
  | ProductionOrderReadFailure;

export type OperatorAssignmentListResult =
  | Readonly<{ status: "success"; assignments: OperatorAssignmentSummary[] }>
  | ProductionOrderReadFailure;

export type HandoverListResult =
  | Readonly<{ status: "success"; handovers: ShiftHandoverSummary[] }>
  | ProductionOrderReadFailure;

export type ShiftExecutionSummaryResult =
  | Readonly<{ status: "success"; summary: ShiftExecutionSummary }>
  | ProductionOrderReadFailure;

export type OperatorExecutionSummaryResult =
  | Readonly<{ status: "success"; summary: OperatorExecutionSummary }>
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

export async function readDowntimes(
  productionOrderId: string,
  limit?: number,
): Promise<ProductionDowntimeListResult> {
  const normalizedId = productionOrderId.trim();
  if (!normalizedId) {
    return { status: "failure", code: "invalid-scope", message: "productionOrderId is required.", retryable: false };
  }
  const resolved = resolveReadContext();
  if (resolved.kind === "failure") return resolved.failure;
  const { context } = resolved;
  const result = await listDowntimes(context, normalizedId, context.facilityId, limit);
  if (result.status === "failure") return result;
  return { status: "success", downtimes: result.value };
}

export async function readSetupSessions(
  productionOrderId: string,
  limit?: number,
): Promise<ProductionSetupSessionListResult> {
  const normalizedId = productionOrderId.trim();
  if (!normalizedId) {
    return { status: "failure", code: "invalid-scope", message: "productionOrderId is required.", retryable: false };
  }
  const resolved = resolveReadContext();
  if (resolved.kind === "failure") return resolved.failure;
  const { context } = resolved;
  const result = await listSetupSessions(context, normalizedId, context.facilityId, limit);
  if (result.status === "failure") return result;
  return { status: "success", sessions: result.value };
}

export async function readCycleEntries(
  productionOrderId: string,
  limit?: number,
): Promise<ProductionCycleEntryListResult> {
  const normalizedId = productionOrderId.trim();
  if (!normalizedId) {
    return { status: "failure", code: "invalid-scope", message: "productionOrderId is required.", retryable: false };
  }
  const resolved = resolveReadContext();
  if (resolved.kind === "failure") return resolved.failure;
  const { context } = resolved;
  const result = await listCycleEntries(context, normalizedId, context.facilityId, limit);
  if (result.status === "failure") return result;
  return { status: "success", cycles: result.value };
}

export async function readRuntimeSummary(
  productionOrderId: string,
): Promise<ProductionRuntimeSummaryResult> {
  const normalizedId = productionOrderId.trim();
  if (!normalizedId) {
    return { status: "failure", code: "invalid-scope", message: "productionOrderId is required.", retryable: false };
  }
  const resolved = resolveReadContext();
  if (resolved.kind === "failure") return resolved.failure;
  const { context } = resolved;
  const result = await getRuntimeSummary(context, normalizedId, context.facilityId);
  if (result.status === "failure") return result;
  return { status: "success", summary: result.value };
}

// ── 4L: Exception + health reads ──────────────────────────────────────────────

export async function readExceptions(
  productionOrderId: string,
  limit?: number,
): Promise<ProductionExceptionListResult> {
  const normalizedId = productionOrderId.trim();
  if (!normalizedId) {
    return { status: "failure", code: "invalid-scope", message: "productionOrderId is required.", retryable: false };
  }
  const resolved = resolveReadContext();
  if (resolved.kind === "failure") return resolved.failure;
  const { context } = resolved;
  const result = await listExceptions(context, normalizedId, context.facilityId, limit);
  if (result.status === "failure") return result;
  return { status: "success", exceptions: result.value };
}

export async function readHealthSummary(
  productionOrderId: string,
): Promise<ProductionHealthSummaryResult> {
  const normalizedId = productionOrderId.trim();
  if (!normalizedId) {
    return { status: "failure", code: "invalid-scope", message: "productionOrderId is required.", retryable: false };
  }
  const resolved = resolveReadContext();
  if (resolved.kind === "failure") return resolved.failure;
  const { context } = resolved;
  const result = await getHealthSummary(context, normalizedId, context.facilityId);
  if (result.status === "failure") return result;
  return { status: "success", summary: result.value };
}

// ── 5L: Shift/Operator/Handover reads ─────────────────────────────────────────

export async function readShiftAssignments(
  productionOrderId: string,
  limit?: number,
): Promise<ShiftAssignmentListResult> {
  const normalizedId = productionOrderId.trim();
  if (!normalizedId) return { status: "failure", code: "invalid-scope", message: "productionOrderId is required.", retryable: false };
  const resolved = resolveReadContext();
  if (resolved.kind === "failure") return resolved.failure;
  const { context } = resolved;
  const result = await listShiftAssignments(context, normalizedId, context.facilityId, limit);
  if (result.status === "failure") return result;
  return { status: "success", assignments: result.value };
}

export async function readOperatorAssignments(
  productionOrderId: string,
  limit?: number,
): Promise<OperatorAssignmentListResult> {
  const normalizedId = productionOrderId.trim();
  if (!normalizedId) return { status: "failure", code: "invalid-scope", message: "productionOrderId is required.", retryable: false };
  const resolved = resolveReadContext();
  if (resolved.kind === "failure") return resolved.failure;
  const { context } = resolved;
  const result = await listOperatorAssignments(context, normalizedId, context.facilityId, limit);
  if (result.status === "failure") return result;
  return { status: "success", assignments: result.value };
}

export async function readHandovers(
  productionOrderId: string,
  limit?: number,
): Promise<HandoverListResult> {
  const normalizedId = productionOrderId.trim();
  if (!normalizedId) return { status: "failure", code: "invalid-scope", message: "productionOrderId is required.", retryable: false };
  const resolved = resolveReadContext();
  if (resolved.kind === "failure") return resolved.failure;
  const { context } = resolved;
  const result = await listHandovers(context, normalizedId, context.facilityId, limit);
  if (result.status === "failure") return result;
  return { status: "success", handovers: result.value };
}

export async function readShiftExecutionSummary(
  shiftId: string,
  date: string,
): Promise<ShiftExecutionSummaryResult> {
  const normalizedId = shiftId.trim();
  if (!normalizedId) return { status: "failure", code: "invalid-scope", message: "shiftId is required.", retryable: false };
  const resolved = resolveReadContext();
  if (resolved.kind === "failure") return resolved.failure;
  const { context } = resolved;
  const result = await getShiftExecutionSummary(context, normalizedId, context.facilityId, date);
  if (result.status === "failure") return result;
  return { status: "success", summary: result.value };
}

export async function readOperatorExecutionSummary(
  subjectId: string,
): Promise<OperatorExecutionSummaryResult> {
  const normalizedId = subjectId.trim();
  if (!normalizedId) return { status: "failure", code: "invalid-scope", message: "subjectId is required.", retryable: false };
  const resolved = resolveReadContext();
  if (resolved.kind === "failure") return resolved.failure;
  const { context } = resolved;
  const result = await getOperatorExecutionSummary(context, normalizedId, context.facilityId);
  if (result.status === "failure") return result;
  return { status: "success", summary: result.value };
}
