import {
  evaluateOrderTransferReadiness,
  getFailedOrderTransferChecks,
  type OrderTransferReadinessCheckId,
} from "./order-transfer-readiness";
import type { OrderDraft } from "../domain/order-draft";

export type OrderTransferCommand = Readonly<{
  commandId: string;
  orderDraftId: string;
  targetSystem: NonNullable<
    OrderDraft["transfer"]["targetSystem"]
  >;
  expectedUpdatedAt: string;
  requestedByUserId: string;
  requestedAt: string;
  approvalId: string;
}>;

export type OrderTransferCommandFailure = Readonly<{
  success: false;
  reason:
    | "transfer-not-ready"
    | "target-system-missing"
    | "approval-missing";
  failedChecks: readonly OrderTransferReadinessCheckId[];
}>;

export type OrderTransferCommandSuccess = Readonly<{
  success: true;
  command: OrderTransferCommand;
}>;

export type CreateOrderTransferCommandResult =
  | OrderTransferCommandSuccess
  | OrderTransferCommandFailure;

type CreateOrderTransferCommandInput = Readonly<{
  orderDraft: OrderDraft;
  requestedByUserId: string;
  requestedAt?: string;
  commandId?: string;
}>;

function createApprovalReference(
  orderDraftId: string,
  approvedAt: string,
  approvedBy: string,
): string {
  const normalizedTime = approvedAt.replace(
    /[^0-9]/g,
    "",
  );

  const normalizedApprover = approvedBy
    .trim()
    .toLowerCase()
    .replace(/[^a-z0-9]+/g, "-")
    .replace(/^-+|-+$/g, "");

  return [
    "order-approval",
    orderDraftId,
    normalizedApprover || "authorized-user",
    normalizedTime,
  ].join("-");
}
function createCommandId(
  orderDraftId: string,
  requestedAt: string,
): string {
  const normalizedTime = requestedAt.replace(
    /[^0-9]/g,
    "",
  );

  return `order-transfer-${orderDraftId}-${normalizedTime}`;
}

export function createOrderTransferCommand({
  orderDraft,
  requestedByUserId,
  requestedAt = new Date().toISOString(),
  commandId,
}: CreateOrderTransferCommandInput): CreateOrderTransferCommandResult {
  const readiness =
    evaluateOrderTransferReadiness(orderDraft);

  const failedChecks =
    getFailedOrderTransferChecks(orderDraft).map(
      (check) => check.id,
    );

  if (!readiness.isReady) {
    return {
      success: false,
      reason: "transfer-not-ready",
      failedChecks,
    };
  }

  if (!orderDraft.transfer.targetSystem) {
    return {
      success: false,
      reason: "target-system-missing",
      failedChecks: ["target-system"],
    };
  }

  if (
    orderDraft.approval.status !== "approved" ||
    !orderDraft.approval.approvedAt ||
    !orderDraft.approval.approvedBy
  ) {
    return {
      success: false,
      reason: "approval-missing",
      failedChecks: ["authorized-approval"],
    };
  }

  return {
    success: true,
    command: {
      commandId:
        commandId ??
        createCommandId(
          orderDraft.id,
          requestedAt,
        ),
      orderDraftId: orderDraft.id,
      targetSystem:
        orderDraft.transfer.targetSystem,
      expectedUpdatedAt:
        orderDraft.updatedAt,
      requestedByUserId,
      requestedAt,
      approvalId: createApprovalReference(
        orderDraft.id,
        orderDraft.approval.approvedAt,
        orderDraft.approval.approvedBy,
      ),
    },
  };
}