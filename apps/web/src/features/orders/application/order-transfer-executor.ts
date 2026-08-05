import type { OrderTransferCommand } from "./order-transfer-command";
import type { OrderDraft } from "../domain/order-draft";

export type OrderTransferExecutionStatus =
  | "completed"
  | "partially-completed"
  | "failed";

export type OrderTransferExecutionFailureCode =
  | "order-not-found"
  | "order-state-conflict"
  | "approval-invalid"
  | "authorization-denied"
  | "target-unavailable"
  | "target-rejected"
  | "timeout"
  | "unknown";

export type OrderTransferExecutionAudit = Readonly<{
  commandId: string;
  orderDraftId: string;
  targetSystem: NonNullable<
    OrderDraft["transfer"]["targetSystem"]
  >;
  requestedByUserId: string;
  requestedAt: string;
  startedAt: string;
  finishedAt: string;
}>;

export type OrderTransferExecutionSuccess = Readonly<{
  status: "completed";
  externalOrderId: string;
  message: string;
  audit: OrderTransferExecutionAudit;
}>;

export type OrderTransferExecutionPartial = Readonly<{
  status: "partially-completed";
  externalOrderId?: string;
  message: string;
  completedSteps: readonly string[];
  pendingSteps: readonly string[];
  audit: OrderTransferExecutionAudit;
}>;

export type OrderTransferExecutionFailure = Readonly<{
  status: "failed";
  code: OrderTransferExecutionFailureCode;
  message: string;
  retryable: boolean;
  audit: OrderTransferExecutionAudit;
}>;

export type OrderTransferExecutionResult =
  | OrderTransferExecutionSuccess
  | OrderTransferExecutionPartial
  | OrderTransferExecutionFailure;

export type ExecuteOrderTransferInput = Readonly<{
  command: OrderTransferCommand;
  currentOrderDraft: OrderDraft;
}>;

export interface OrderTransferExecutor {
  execute(
    input: ExecuteOrderTransferInput,
  ): Promise<OrderTransferExecutionResult>;
}

export function hasOrderTransferStateConflict(
  command: OrderTransferCommand,
  currentOrderDraft: OrderDraft,
): boolean {
  return (
    command.orderDraftId !== currentOrderDraft.id ||
    command.expectedUpdatedAt !==
      currentOrderDraft.updatedAt
  );
}

export function validateOrderTransferExecutionInput(
  input: ExecuteOrderTransferInput,
):
  | Readonly<{ valid: true }>
  | Readonly<{
      valid: false;
      code:
        | "order-state-conflict"
        | "approval-invalid"
        | "target-unavailable";
    }> {
  const { command, currentOrderDraft } = input;

  if (
    hasOrderTransferStateConflict(
      command,
      currentOrderDraft,
    )
  ) {
    return {
      valid: false,
      code: "order-state-conflict",
    };
  }

  if (
    currentOrderDraft.approval.status !==
      "approved" ||
    !currentOrderDraft.approval.approvedAt ||
    !currentOrderDraft.approval.approvedBy
  ) {
    return {
      valid: false,
      code: "approval-invalid",
    };
  }

  if (
    !currentOrderDraft.transfer.targetSystem ||
    currentOrderDraft.transfer.targetSystem !==
      command.targetSystem
  ) {
    return {
      valid: false,
      code: "target-unavailable",
    };
  }

  return {
    valid: true,
  };
}