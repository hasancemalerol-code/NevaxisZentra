"use server";

import "./quality-control-server-boundary";

import {
  completeQualityInspection,
  createQualityControlPlan,
  createQualityInspection,
  recordQualityInspectionResult,
  releaseQualityInspection,
  rejectQualityInspection,
  startQualityInspection,
} from "./quality-control-api-client";
import {
  resolveProductionOrderRequestContext,
} from "../../production-execution/server/production-order-request-context";
import { parseProductionExecutionRuntimeConfig } from "../../production-execution/server/production-order-runtime-config";

export type QualityActionResult = Readonly<{
  status: "success" | "failure";
  code?: string;
  message?: string;
  id?: string;
  version?: number;
}>;

function resolveWriteContext():
  | Readonly<{ kind: "context"; context: import("../../production-execution/server/production-order-request-context").ProductionOrderRequestContext }>
  | Readonly<{ kind: "failure"; failure: QualityActionResult }> {
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

function mapWriteResult(result: Awaited<ReturnType<typeof createQualityInspection>>): QualityActionResult {
  if (result.status === "failure") {
    return { status: "failure", code: result.code, message: result.message };
  }

  return {
    status: "success",
    id: result.value.id,
    version: result.value.version,
  };
}

export async function createQualityControlPlanAction(input: {
  code: string;
  name: string;
  productReference?: string;
  operationReference?: string;
  status: string;
  revision: string;
  inspectionDefinitions: Array<{
    code: string;
    name: string;
    inspectionType: string;
    sequence: number;
    unit?: string;
    nominalValue?: number;
    lowerLimit?: number;
    upperLimit?: number;
    required: boolean;
    critical: boolean;
  }>;
}): Promise<QualityActionResult> {
  const resolved = resolveWriteContext();
  if (resolved.kind === "failure") {
    return resolved.failure;
  }

  const result = await createQualityControlPlan(resolved.context, input);
  return mapWriteResult(result);
}

export async function createQualityInspectionAction(input: {
  productionOrderId: string;
  qualityControlPlanId: string;
  notes?: string;
}): Promise<QualityActionResult> {
  const resolved = resolveWriteContext();
  if (resolved.kind === "failure") {
    return resolved.failure;
  }

  const result = await createQualityInspection(resolved.context, input);
  return mapWriteResult(result);
}

export async function startQualityInspectionAction(
  inspectionId: string,
  expectedVersion: number,
): Promise<QualityActionResult> {
  const resolved = resolveWriteContext();
  if (resolved.kind === "failure") {
    return resolved.failure;
  }

  const result = await startQualityInspection(resolved.context, inspectionId, { expectedVersion });
  return mapWriteResult(result);
}

export async function recordQualityResultAction(
  inspectionId: string,
  expectedVersion: number,
  inspectionDefinitionId: string,
  measuredNumericValue?: number,
  measuredTextValue?: string,
  booleanResult?: boolean,
  comment?: string,
): Promise<QualityActionResult> {
  const resolved = resolveWriteContext();
  if (resolved.kind === "failure") {
    return resolved.failure;
  }

  const result = await recordQualityInspectionResult(resolved.context, inspectionId, {
    expectedVersion,
    inspectionDefinitionId,
    measuredNumericValue,
    measuredTextValue,
    booleanResult,
    comment,
  });
  return mapWriteResult(result);
}

export async function completeQualityInspectionAction(
  inspectionId: string,
  expectedVersion: number,
): Promise<QualityActionResult> {
  const resolved = resolveWriteContext();
  if (resolved.kind === "failure") {
    return resolved.failure;
  }

  const result = await completeQualityInspection(resolved.context, inspectionId, { expectedVersion });
  return mapWriteResult(result);
}

export async function releaseQualityInspectionAction(
  inspectionId: string,
  expectedVersion: number,
  note?: string,
): Promise<QualityActionResult> {
  const resolved = resolveWriteContext();
  if (resolved.kind === "failure") {
    return resolved.failure;
  }

  const result = await releaseQualityInspection(resolved.context, inspectionId, {
    expectedVersion,
    note,
  });
  return mapWriteResult(result);
}

export async function rejectQualityInspectionAction(
  inspectionId: string,
  expectedVersion: number,
  note?: string,
): Promise<QualityActionResult> {
  const resolved = resolveWriteContext();
  if (resolved.kind === "failure") {
    return resolved.failure;
  }

  const result = await rejectQualityInspection(resolved.context, inspectionId, {
    expectedVersion,
    note,
  });
  return mapWriteResult(result);
}
