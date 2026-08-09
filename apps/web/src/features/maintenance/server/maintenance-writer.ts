"use server";

import "./maintenance-server-boundary";

import {
  createMaintenancePartUsage,
  createMaintenanceWorkNote,
  createWorkOrderFromPreventiveDue,
  generatePreventiveNextDue,
} from "./maintenance-api-client";
import { resolveProductionOrderRequestContext } from "../../production-execution/server/production-order-request-context";
import { parseProductionExecutionRuntimeConfig } from "../../production-execution/server/production-order-runtime-config";

export type MaintenanceActionResult = Readonly<{
  status: "success" | "failure";
  code?: string;
  message?: string;
  id?: string;
  version?: number;
}>;

function resolveWriteContext():
  | Readonly<{ kind: "context"; context: import("../../production-execution/server/production-order-request-context").ProductionOrderRequestContext }>
  | Readonly<{ kind: "failure"; failure: MaintenanceActionResult }> {
  const configResult = parseProductionExecutionRuntimeConfig();
  if (configResult.status === "failure") {
    return {
      kind: "failure",
      failure: { status: "failure", code: "configuration-error", message: configResult.message },
    };
  }

  const contextResult = resolveProductionOrderRequestContext(configResult.config);
  if (contextResult.status === "failure") {
    return {
      kind: "failure",
      failure: { status: "failure", code: contextResult.code, message: contextResult.message },
    };
  }

  return { kind: "context", context: contextResult.context };
}

function mapWriteResult(
  result: Awaited<ReturnType<typeof createMaintenanceWorkNote>>,
): MaintenanceActionResult {
  if (result.status === "failure") {
    return { status: "failure", code: result.code, message: result.message };
  }

  return {
    status: "success",
    id: result.value.id,
    version: result.value.version,
  };
}

export async function createMaintenanceWorkNoteAction(input: {
  workOrderId: string;
  noteType: string;
  text: string;
  isImportant: boolean;
}): Promise<MaintenanceActionResult> {
  const resolved = resolveWriteContext();
  if (resolved.kind === "failure") {
    return resolved.failure;
  }

  const result = await createMaintenanceWorkNote(resolved.context, input.workOrderId, {
    noteType: input.noteType,
    text: input.text,
    isImportant: input.isImportant,
  });
  return mapWriteResult(result);
}

export async function createMaintenancePartUsageAction(input: {
  workOrderId: string;
  partReference: string;
  partName?: string;
  quantity: number;
  unit: string;
  sourceSystem?: string;
  sourceReference?: string;
  estimatedUnitCost?: number;
  actualUnitCost?: number;
}): Promise<MaintenanceActionResult> {
  const resolved = resolveWriteContext();
  if (resolved.kind === "failure") {
    return resolved.failure;
  }

  const result = await createMaintenancePartUsage(resolved.context, input.workOrderId, {
    partReference: input.partReference,
    partName: input.partName,
    quantity: input.quantity,
    unit: input.unit,
    sourceSystem: input.sourceSystem,
    sourceReference: input.sourceReference,
    estimatedUnitCost: input.estimatedUnitCost,
    actualUnitCost: input.actualUnitCost,
  });
  return mapWriteResult(result);
}

export async function generatePreventiveNextDueAction(
  planId: string,
): Promise<MaintenanceActionResult> {
  const resolved = resolveWriteContext();
  if (resolved.kind === "failure") {
    return resolved.failure;
  }

  const result = await generatePreventiveNextDue(resolved.context, planId);
  return mapWriteResult(result);
}

export async function createWorkOrderFromPreventiveDueAction(input: {
  dueId: string;
  expectedVersion: number;
}): Promise<MaintenanceActionResult> {
  const resolved = resolveWriteContext();
  if (resolved.kind === "failure") {
    return resolved.failure;
  }

  const result = await createWorkOrderFromPreventiveDue(resolved.context, input.dueId, {
    expectedVersion: input.expectedVersion,
  });
  return mapWriteResult(result);
}
