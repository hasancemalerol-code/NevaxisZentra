import type { OrderTransferCommand } from "./order-transfer-command";
import type {
  OrderTransferExecutionFailureCode,
  OrderTransferExecutionResult,
  OrderTransferExecutionStatus,
} from "./order-transfer-executor";
import type { OrderDraft } from "../domain/order-draft";

export type OrderTransferAuditOutcome =
  | "completed"
  | "partially-completed"
  | "failed";

export type OrderTransferAuditRecord = Readonly<{
  id: string;
  eventType: "order-transfer-executed";
  orderDraftId: string;
  commandId: string;
  approvalId: string;
  targetSystem: NonNullable<
    OrderDraft["transfer"]["targetSystem"]
  >;
  requestedByUserId: string;
  requestedAt: string;
  startedAt: string;
  finishedAt: string;
  outcome: OrderTransferAuditOutcome;
  executionStatus: OrderTransferExecutionStatus;
  externalOrderId?: string;
  failureCode?: OrderTransferExecutionFailureCode;
  retryable?: boolean;
  completedSteps?: readonly string[];
  pendingSteps?: readonly string[];
  message: string;
  recordedAt: string;
}>;

type CreateOrderTransferAuditRecordInput =
  Readonly<{
    command: OrderTransferCommand;
    result: OrderTransferExecutionResult;
    recordedAt?: string;
  }>;

function normalizeAuditToken(
  value: string,
): string {
  return value
    .trim()
    .replace(/[^a-zA-Z0-9]+/g, "-")
    .replace(/^-+|-+$/g, "")
    .toLowerCase();
}

function createAuditRecordId(
  commandId: string,
  finishedAt: string,
): string {
  const normalizedTime = finishedAt.replace(
    /[^0-9]/g,
    "",
  );

  return [
    "order-transfer-audit",
    normalizeAuditToken(commandId),
    normalizedTime,
  ].join("-");
}

export function createOrderTransferAuditRecord({
  command,
  result,
  recordedAt = new Date().toISOString(),
}: CreateOrderTransferAuditRecordInput): OrderTransferAuditRecord {
  const commonRecord = {
    id: createAuditRecordId(
      command.commandId,
      result.audit.finishedAt,
    ),
    eventType:
      "order-transfer-executed" as const,
    orderDraftId: command.orderDraftId,
    commandId: command.commandId,
    approvalId: command.approvalId,
    targetSystem: command.targetSystem,
    requestedByUserId:
      command.requestedByUserId,
    requestedAt: command.requestedAt,
    startedAt: result.audit.startedAt,
    finishedAt: result.audit.finishedAt,
    outcome: result.status,
    executionStatus: result.status,
    message: result.message,
    recordedAt,
  };

  if (result.status === "completed") {
    return {
      ...commonRecord,
      externalOrderId:
        result.externalOrderId,
    };
  }

  if (
    result.status === "partially-completed"
  ) {
    return {
      ...commonRecord,
      externalOrderId:
        result.externalOrderId,
      completedSteps:
        result.completedSteps,
      pendingSteps:
        result.pendingSteps,
    };
  }

  return {
    ...commonRecord,
    failureCode: result.code,
    retryable: result.retryable,
  };
}

export function isSuccessfulOrderTransferAuditRecord(
  record: OrderTransferAuditRecord,
): boolean {
  return record.outcome === "completed";
}

export function requiresOrderTransferFollowUp(
  record: OrderTransferAuditRecord,
): boolean {
  return (
    record.outcome ===
      "partially-completed" ||
    record.outcome === "failed"
  );
}