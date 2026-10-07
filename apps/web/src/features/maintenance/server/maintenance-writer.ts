"use server";

import "./maintenance-server-boundary";

import {
  convertMaintenanceRequestToWorkOrder,
  createMaintenancePartUsage,
  createMaintenanceRequest,
  createMaintenanceWorkNote,
  createMaintenanceWorkOrder,
  createPreventiveMaintenancePlan,
  createWorkOrderFromPreventiveDue,
  generatePreventiveNextDue,
} from "./maintenance-api-client";
import {
  denyUnlessAnyGranted,
  denyUnlessGranted,
} from "../../admin-access/server/require-current-permission";
import { resolveProductionOrderRequestContextForIncomingRequest } from "../../production-execution/server/production-order-request-context";
import { parseProductionExecutionRuntimeConfig } from "../../production-execution/server/production-order-runtime-config";

export type MaintenanceActionResult = Readonly<{
  status: "success" | "failure";
  code?: string;
  message?: string;
  id?: string;
  version?: number;
}>;

async function resolveWriteContext(): Promise<
  | Readonly<{ kind: "context"; context: import("../../production-execution/server/production-order-request-context").ProductionOrderRequestContext }>
  | Readonly<{ kind: "failure"; failure: MaintenanceActionResult }>> {
  const configResult = parseProductionExecutionRuntimeConfig();
  if (configResult.status === "failure") {
    return {
      kind: "failure",
      failure: { status: "failure", code: "configuration-error", message: configResult.message },
    };
  }

  const contextResult = await resolveProductionOrderRequestContextForIncomingRequest(configResult.config);
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
  const authorization = await denyUnlessAnyGranted([
    "maintenance.execute",
    "maintenance.manage",
  ]);
  if (authorization.status === "failure") {
    return authorization;
  }

  const resolved = await resolveWriteContext();
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
  const authorization = await denyUnlessAnyGranted([
    "maintenance.execute",
    "maintenance.manage",
  ]);
  if (authorization.status === "failure") {
    return authorization;
  }

  const resolved = await resolveWriteContext();
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
  const authorization = await denyUnlessAnyGranted([
    "maintenance.execute",
    "maintenance.manage",
  ]);
  if (authorization.status === "failure") {
    return authorization;
  }

  const resolved = await resolveWriteContext();
  if (resolved.kind === "failure") {
    return resolved.failure;
  }

  const result = await generatePreventiveNextDue(resolved.context, planId);
  return mapWriteResult(result);
}

function mapCreateFailure(result: MaintenanceActionResult): MaintenanceActionResult {
  if (result.status === "success") {
    return result;
  }

  if (result.code === "permission-denied") {
    return { status: "failure", code: result.code, message: "Bu işlem için yetkiniz yok." };
  }

  if (result.code === "invalid-request") {
    return {
      status: "failure",
      code: result.code,
      message: result.message ?? "Geçersiz bakım kaydı. Zorunlu alanları kontrol edin.",
    };
  }

  return {
    status: "failure",
    code: result.code,
    message: result.message ?? "Bakım kaydı oluşturulamadı.",
  };
}

export async function createMaintenanceRequestAction(input: {
  assetType: string;
  assetReference: string;
  assetName?: string;
  type: string;
  priority: string;
  title: string;
  description: string;
}): Promise<MaintenanceActionResult> {
  const authorization = await denyUnlessGranted("maintenance.manage");
  if (authorization.status === "failure") {
    return { status: "failure", code: "permission-denied", message: authorization.code === "authorization-denied" ? "Bu işlem için yetkiniz yok." : authorization.message };
  }

  const title = input.title.trim();
  const assetReference = input.assetReference.trim();
  if (!title || !assetReference) {
    return {
      status: "failure",
      code: "invalid-request",
      message: "Başlık ve varlık referansı zorunludur.",
    };
  }

  const resolved = await resolveWriteContext();
  if (resolved.kind === "failure") {
    return resolved.failure;
  }

  const result = await createMaintenanceRequest(resolved.context, {
    assetType: input.assetType,
    assetReference,
    assetName: input.assetName?.trim() || undefined,
    type: input.type,
    priority: input.priority,
    title,
    description: input.description.trim(),
    source: "manual",
  });
  return mapCreateFailure(mapWriteResult(result));
}

export async function createMaintenanceWorkOrderAction(input: {
  assetType: string;
  assetReference: string;
  assetName?: string;
  type: string;
  priority: string;
  title: string;
  description: string;
}): Promise<MaintenanceActionResult> {
  const authorization = await denyUnlessGranted("maintenance.manage");
  if (authorization.status === "failure") {
    return { status: "failure", code: "permission-denied", message: authorization.code === "authorization-denied" ? "Bu işlem için yetkiniz yok." : authorization.message };
  }

  const title = input.title.trim();
  const assetReference = input.assetReference.trim();
  if (!title || !assetReference) {
    return {
      status: "failure",
      code: "invalid-request",
      message: "Başlık ve varlık referansı zorunludur.",
    };
  }

  const resolved = await resolveWriteContext();
  if (resolved.kind === "failure") {
    return resolved.failure;
  }

  const result = await createMaintenanceWorkOrder(resolved.context, {
    assetType: input.assetType,
    assetReference,
    assetName: input.assetName?.trim() || undefined,
    type: input.type,
    priority: input.priority,
    title,
    description: input.description.trim(),
    initialStatus: "draft",
  });
  return mapCreateFailure(mapWriteResult(result));
}

export async function createPreventiveMaintenancePlanAction(input: {
  code: string;
  name: string;
  assetType: string;
  assetReference: string;
  intervalType: string;
  intervalValue: number;
  nextDueAtUtc?: string;
  description?: string;
}): Promise<MaintenanceActionResult> {
  const authorization = await denyUnlessGranted("maintenance.manage");
  if (authorization.status === "failure") {
    return { status: "failure", code: "permission-denied", message: authorization.code === "authorization-denied" ? "Bu işlem için yetkiniz yok." : authorization.message };
  }

  const code = input.code.trim();
  const name = input.name.trim();
  const assetReference = input.assetReference.trim();
  if (!code || !name || !assetReference) {
    return {
      status: "failure",
      code: "invalid-request",
      message: "Plan kodu, adı ve varlık referansı zorunludur.",
    };
  }

  if (!Number.isInteger(input.intervalValue) || input.intervalValue <= 0) {
    return {
      status: "failure",
      code: "invalid-request",
      message: "Periyot değeri sıfırdan büyük bir tam sayı olmalıdır.",
    };
  }

  const resolved = await resolveWriteContext();
  if (resolved.kind === "failure") {
    return resolved.failure;
  }

  let nextDueAtUtc: string | undefined;
  if (input.nextDueAtUtc) {
    const parsed = new Date(input.nextDueAtUtc);
    if (Number.isNaN(parsed.getTime())) {
      return {
        status: "failure",
        code: "invalid-request",
        message: "Sonraki bakım tarihi geçersiz.",
      };
    }
    nextDueAtUtc = parsed.toISOString();
  }

  const result = await createPreventiveMaintenancePlan(resolved.context, {
    code,
    name,
    assetType: input.assetType,
    assetReference,
    intervalType: input.intervalType,
    intervalValue: input.intervalValue,
    nextDueAtUtc,
    status: "active",
    description: input.description?.trim() || undefined,
  });
  return mapCreateFailure(mapWriteResult(result));
}

export async function convertMaintenanceRequestAction(input: {
  requestId: string;
  expectedVersion: number;
}): Promise<MaintenanceActionResult> {
  const authorization = await denyUnlessGranted("maintenance.manage");
  if (authorization.status === "failure") {
    return { status: "failure", code: "permission-denied", message: authorization.code === "authorization-denied" ? "Bu işlem için yetkiniz yok." : authorization.message };
  }

  const resolved = await resolveWriteContext();
  if (resolved.kind === "failure") {
    return resolved.failure;
  }

  const result = await convertMaintenanceRequestToWorkOrder(resolved.context, input.requestId, {
    expectedVersion: input.expectedVersion,
  });
  return mapCreateFailure(mapWriteResult(result));
}

export async function createWorkOrderFromPreventiveDueAction(input: {
  dueId: string;
  expectedVersion: number;
}): Promise<MaintenanceActionResult> {
  const authorization = await denyUnlessAnyGranted([
    "maintenance.execute",
    "maintenance.manage",
  ]);
  if (authorization.status === "failure") {
    return authorization;
  }

  const resolved = await resolveWriteContext();
  if (resolved.kind === "failure") {
    return resolved.failure;
  }

  const result = await createWorkOrderFromPreventiveDue(resolved.context, input.dueId, {
    expectedVersion: input.expectedVersion,
  });
  return mapWriteResult(result);
}
