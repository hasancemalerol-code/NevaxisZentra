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
  startDowntime,
  endDowntime,
  startSetupSession,
  completeSetupSession,
  recordManualCycle,
  raiseException,
  acknowledgeException,
  resolveException,
  recoverDowntime,
  recoverSetupSession,
  assignShift,
  reassignShift,
  assignOperator,
  releaseOperator,
  createHandover,
  assignException,
  unassignException,
  type ProductionOrderWriteResult,
} from "./production-order-api-client";
import {
  resolveProductionOrderRequestContextForIncomingRequest,
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

async function resolveWriteContext(): Promise< ResolvedWriteContext> {
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

  const contextResult = await resolveProductionOrderRequestContextForIncomingRequest(
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
  const resolved = await resolveWriteContext();

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
  const resolved = await resolveWriteContext();

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
  const resolved = await resolveWriteContext();

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
  const resolved = await resolveWriteContext();

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
  const resolved = await resolveWriteContext();

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
  const resolved = await resolveWriteContext();

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
  const resolved = await resolveWriteContext();

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

export async function startDowntimeAction(
  productionOrderId: string,
  expectedVersion: number,
  reasonCode: string,
  comment?: string,
): Promise<ProductionOrderActionResult> {
  const resolved = await resolveWriteContext();
  if (resolved.kind === "failure") return resolved.failure;
  if (!reasonCode.trim()) {
    return { status: "failure", code: "invalid-request", message: "Duruş nedeni zorunludur." };
  }
  const result = await startDowntime(resolved.context, productionOrderId, expectedVersion, reasonCode.trim(), comment?.trim() || undefined);
  return mapWriteResult(result);
}

export async function endDowntimeAction(
  productionOrderId: string,
  downtimeId: string,
  expectedVersion: number,
): Promise<ProductionOrderActionResult> {
  const resolved = await resolveWriteContext();
  if (resolved.kind === "failure") return resolved.failure;
  const result = await endDowntime(resolved.context, productionOrderId, downtimeId, expectedVersion);
  return mapWriteResult(result);
}

export async function startSetupSessionAction(
  productionOrderId: string,
  expectedVersion: number,
  setupType: string,
  comment?: string,
): Promise<ProductionOrderActionResult> {
  const resolved = await resolveWriteContext();
  if (resolved.kind === "failure") return resolved.failure;
  if (!setupType.trim()) {
    return { status: "failure", code: "invalid-request", message: "Setup türü zorunludur." };
  }
  const result = await startSetupSession(resolved.context, productionOrderId, expectedVersion, setupType.trim(), comment?.trim() || undefined);
  return mapWriteResult(result);
}

export async function completeSetupSessionAction(
  productionOrderId: string,
  setupId: string,
  expectedVersion: number,
): Promise<ProductionOrderActionResult> {
  const resolved = await resolveWriteContext();
  if (resolved.kind === "failure") return resolved.failure;
  const result = await completeSetupSession(resolved.context, productionOrderId, setupId, expectedVersion);
  return mapWriteResult(result);
}

export async function recordManualCycleAction(
  productionOrderId: string,
  expectedVersion: number,
  cycleDurationMilliseconds: number,
  quantityProduced?: number,
  comment?: string,
): Promise<ProductionOrderActionResult> {
  const resolved = await resolveWriteContext();
  if (resolved.kind === "failure") return resolved.failure;
  if (cycleDurationMilliseconds <= 0) {
    return { status: "failure", code: "invalid-request", message: "Çevrim süresi sıfırdan büyük olmalıdır." };
  }
  if (cycleDurationMilliseconds > 86_400_000) {
    return { status: "failure", code: "invalid-request", message: "Çevrim süresi 24 saati aşamaz." };
  }
  if (quantityProduced !== undefined && quantityProduced < 0) {
    return { status: "failure", code: "invalid-request", message: "Miktar negatif olamaz." };
  }
  const result = await recordManualCycle(resolved.context, productionOrderId, expectedVersion, cycleDurationMilliseconds, quantityProduced, comment?.trim() || undefined);
  return mapWriteResult(result);
}

// ── 4L: Exception management ──────────────────────────────────────────────────

export async function raiseExceptionAction(
  productionOrderId: string,
  expectedVersion: number,
  exceptionType: string,
  severity: string,
  message: string,
): Promise<ProductionOrderActionResult> {
  const resolved = await resolveWriteContext();
  if (resolved.kind === "failure") return resolved.failure;
  if (!exceptionType.trim()) {
    return { status: "failure", code: "invalid-request", message: "İstisna türü zorunludur." };
  }
  if (!severity.trim()) {
    return { status: "failure", code: "invalid-request", message: "Önem derecesi zorunludur." };
  }
  if (!message.trim()) {
    return { status: "failure", code: "invalid-request", message: "İstisna mesajı zorunludur." };
  }
  const result = await raiseException(resolved.context, productionOrderId, expectedVersion, exceptionType.trim(), severity.trim(), message.trim());
  return mapWriteResult(result);
}

export async function acknowledgeExceptionAction(
  productionOrderId: string,
  exceptionId: string,
  expectedVersion: number,
): Promise<ProductionOrderActionResult> {
  const resolved = await resolveWriteContext();
  if (resolved.kind === "failure") return resolved.failure;
  if (!exceptionId.trim()) {
    return { status: "failure", code: "invalid-request", message: "İstisna ID zorunludur." };
  }
  const result = await acknowledgeException(resolved.context, productionOrderId, exceptionId.trim(), expectedVersion);
  return mapWriteResult(result);
}

export async function resolveExceptionAction(
  productionOrderId: string,
  exceptionId: string,
  expectedVersion: number,
  resolutionNote: string,
): Promise<ProductionOrderActionResult> {
  const resolved = await resolveWriteContext();
  if (resolved.kind === "failure") return resolved.failure;
  if (!exceptionId.trim()) {
    return { status: "failure", code: "invalid-request", message: "İstisna ID zorunludur." };
  }
  if (!resolutionNote.trim()) {
    return { status: "failure", code: "invalid-request", message: "Çözüm notu zorunludur." };
  }
  const result = await resolveException(resolved.context, productionOrderId, exceptionId.trim(), expectedVersion, resolutionNote.trim());
  return mapWriteResult(result);
}

// ── 4L: Session recovery ──────────────────────────────────────────────────────

export async function recoverDowntimeAction(
  productionOrderId: string,
  downtimeId: string,
  expectedVersion: number,
  resolution: string,
  resumeProduction: boolean,
): Promise<ProductionOrderActionResult> {
  const resolved = await resolveWriteContext();
  if (resolved.kind === "failure") return resolved.failure;
  if (!downtimeId.trim()) {
    return { status: "failure", code: "invalid-request", message: "Duruş ID zorunludur." };
  }
  if (!resolution.trim()) {
    return { status: "failure", code: "invalid-request", message: "Çözüm açıklaması zorunludur." };
  }
  const result = await recoverDowntime(resolved.context, productionOrderId, downtimeId.trim(), expectedVersion, resolution.trim(), resumeProduction);
  return mapWriteResult(result);
}

export async function recoverSetupSessionAction(
  productionOrderId: string,
  setupId: string,
  expectedVersion: number,
  resolution: string,
): Promise<ProductionOrderActionResult> {
  const resolved = await resolveWriteContext();
  if (resolved.kind === "failure") return resolved.failure;
  if (!setupId.trim()) {
    return { status: "failure", code: "invalid-request", message: "Setup ID zorunludur." };
  }
  if (!resolution.trim()) {
    return { status: "failure", code: "invalid-request", message: "Çözüm açıklaması zorunludur." };
  }
  const result = await recoverSetupSession(resolved.context, productionOrderId, setupId.trim(), expectedVersion, resolution.trim());
  return mapWriteResult(result);
}

// ── 5L: Shift Assignment ────────────────────────────────────────────────────────

export async function assignShiftAction(
  productionOrderId: string,
  expectedVersion: number,
  productionShiftId: string,
  shiftDate: string,
): Promise<ProductionOrderActionResult> {
  const resolved = await resolveWriteContext();
  if (resolved.kind === "failure") return resolved.failure;
  if (!productionShiftId.trim()) {
    return { status: "failure", code: "invalid-request", message: "Vardiya ID zorunludur." };
  }
  if (!shiftDate.trim()) {
    return { status: "failure", code: "invalid-request", message: "Vardiya tarihi zorunludur." };
  }
  const result = await assignShift(resolved.context, productionOrderId, expectedVersion, productionShiftId.trim(), shiftDate.trim());
  return mapWriteResult(result);
}

export async function reassignShiftAction(
  productionOrderId: string,
  expectedVersion: number,
  productionShiftId: string,
  shiftDate: string,
  reason: string,
): Promise<ProductionOrderActionResult> {
  const resolved = await resolveWriteContext();
  if (resolved.kind === "failure") return resolved.failure;
  if (!productionShiftId.trim()) {
    return { status: "failure", code: "invalid-request", message: "Vardiya ID zorunludur." };
  }
  if (!shiftDate.trim()) {
    return { status: "failure", code: "invalid-request", message: "Vardiya tarihi zorunludur." };
  }
  if (!reason.trim()) {
    return { status: "failure", code: "invalid-request", message: "Değişiklik nedeni zorunludur." };
  }
  const result = await reassignShift(resolved.context, productionOrderId, expectedVersion, productionShiftId.trim(), shiftDate.trim(), reason.trim());
  return mapWriteResult(result);
}

// ── 5L: Operator Assignment ─────────────────────────────────────────────────────

export async function assignOperatorAction(
  productionOrderId: string,
  expectedVersion: number,
  subjectId: string,
  assignmentRole: string,
): Promise<ProductionOrderActionResult> {
  const resolved = await resolveWriteContext();
  if (resolved.kind === "failure") return resolved.failure;
  if (!subjectId.trim()) {
    return { status: "failure", code: "invalid-request", message: "Operatör ID zorunludur." };
  }
  if (!assignmentRole.trim()) {
    return { status: "failure", code: "invalid-request", message: "Görev rolü zorunludur." };
  }
  const result = await assignOperator(resolved.context, productionOrderId, expectedVersion, subjectId.trim(), assignmentRole.trim());
  return mapWriteResult(result);
}

export async function releaseOperatorAction(
  productionOrderId: string,
  assignmentId: string,
  expectedVersion: number,
  reason: string,
): Promise<ProductionOrderActionResult> {
  const resolved = await resolveWriteContext();
  if (resolved.kind === "failure") return resolved.failure;
  if (!assignmentId.trim()) {
    return { status: "failure", code: "invalid-request", message: "Atama ID zorunludur." };
  }
  if (!reason.trim()) {
    return { status: "failure", code: "invalid-request", message: "Görevden çıkarma nedeni zorunludur." };
  }
  const result = await releaseOperator(resolved.context, productionOrderId, assignmentId.trim(), expectedVersion, reason.trim());
  return mapWriteResult(result);
}

// ── 5L: Shift Handover ──────────────────────────────────────────────────────────

export async function createHandoverAction(
  productionOrderId: string,
  expectedVersion: number,
  note: string,
  openIssuesSummary?: string,
): Promise<ProductionOrderActionResult> {
  const resolved = await resolveWriteContext();
  if (resolved.kind === "failure") return resolved.failure;
  if (!note.trim()) {
    return { status: "failure", code: "invalid-request", message: "Devir notu zorunludur." };
  }
  const result = await createHandover(resolved.context, productionOrderId, expectedVersion, note.trim(), openIssuesSummary?.trim());
  return mapWriteResult(result);
}

// ── 5L: Exception Routing ───────────────────────────────────────────────────────

export async function assignExceptionAction(
  productionOrderId: string,
  exceptionId: string,
  expectedVersion: number,
  subjectId: string,
): Promise<ProductionOrderActionResult> {
  const resolved = await resolveWriteContext();
  if (resolved.kind === "failure") return resolved.failure;
  if (!exceptionId.trim()) {
    return { status: "failure", code: "invalid-request", message: "İstisna ID zorunludur." };
  }
  if (!subjectId.trim()) {
    return { status: "failure", code: "invalid-request", message: "Operatör ID zorunludur." };
  }
  const result = await assignException(resolved.context, productionOrderId, exceptionId.trim(), expectedVersion, subjectId.trim());
  return mapWriteResult(result);
}

export async function unassignExceptionAction(
  productionOrderId: string,
  exceptionId: string,
  expectedVersion: number,
): Promise<ProductionOrderActionResult> {
  const resolved = await resolveWriteContext();
  if (resolved.kind === "failure") return resolved.failure;
  if (!exceptionId.trim()) {
    return { status: "failure", code: "invalid-request", message: "İstisna ID zorunludur." };
  }
  const result = await unassignException(resolved.context, productionOrderId, exceptionId.trim(), expectedVersion);
  return mapWriteResult(result);
}
