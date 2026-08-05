import {
  authorizeOrderTransfer,
  type OrderTransferActorRole,
  type OrderTransferAuthorizationFailureReason,
} from "./order-transfer-authorization";
import type { OrderTransferCommand } from "./order-transfer-command";
import {
  validateOrderTransferExecutionInput,
  type ExecuteOrderTransferInput,
} from "./order-transfer-executor";
import type { OrderDraft } from "../domain/order-draft";

export type OrderTransferServerActor =
  Readonly<{
    userId: string;
    role: OrderTransferActorRole;
    tenantId: string;
  }>;

export type OrderTransferServerCommandInput =
  Readonly<{
    command: OrderTransferCommand;
    currentOrderDraft: OrderDraft;
    actor: OrderTransferServerActor;
  }>;

export type OrderTransferServerCommand =
  Readonly<{
    command: OrderTransferCommand;
    currentOrderDraft: OrderDraft;
    actor: OrderTransferServerActor;
    idempotencyKey: string;
    authorizedAt: string;
  }>;

export type OrderTransferServerCommandFailureReason =
  | "authorization-failed"
  | "execution-input-invalid"
  | "command-actor-mismatch";

export type OrderTransferServerCommandResult =
  | Readonly<{
      success: true;
      serverCommand: OrderTransferServerCommand;
    }>
  | Readonly<{
      success: false;
      reason: OrderTransferServerCommandFailureReason;
      authorizationReason?:
        OrderTransferAuthorizationFailureReason;
      executionValidationCode?:
        | "order-state-conflict"
        | "approval-invalid"
        | "target-unavailable";
    }>;

function normalizeIdempotencyToken(
  value: string,
): string {
  return value
    .trim()
    .toLowerCase()
    .replace(/[^a-z0-9]+/g, "-")
    .replace(/^-+|-+$/g, "");
}

export function createOrderTransferIdempotencyKey(
  command: OrderTransferCommand,
): string {
  return [
    "order-transfer",
    normalizeIdempotencyToken(
      command.orderDraftId,
    ),
    normalizeIdempotencyToken(
      command.commandId,
    ),
    normalizeIdempotencyToken(
      command.targetSystem,
    ),
  ].join(":");
}

export function prepareOrderTransferServerCommand({
  command,
  currentOrderDraft,
  actor,
}: OrderTransferServerCommandInput): OrderTransferServerCommandResult {
  if (
    command.requestedByUserId !== actor.userId
  ) {
    return {
      success: false,
      reason: "command-actor-mismatch",
    };
  }

  const authorization =
    authorizeOrderTransfer({
      role: actor.role,
      userId: actor.userId,
      tenantId: actor.tenantId,
      orderDraft: currentOrderDraft,
    });

  if (!authorization.authorized) {
    return {
      success: false,
      reason: "authorization-failed",
      authorizationReason:
        authorization.reason,
    };
  }

  const executionInput:
    ExecuteOrderTransferInput = {
      command,
      currentOrderDraft,
    };

  const validation =
    validateOrderTransferExecutionInput(
      executionInput,
    );

  if (!validation.valid) {
    return {
      success: false,
      reason: "execution-input-invalid",
      executionValidationCode:
        validation.code,
    };
  }

  return {
    success: true,
    serverCommand: {
      command,
      currentOrderDraft,
      actor,
      idempotencyKey:
        createOrderTransferIdempotencyKey(
          command,
        ),
      authorizedAt:
        new Date().toISOString(),
    },
  };
}

export function hasMatchingOrderTransferIdempotencyKey(
  left: OrderTransferServerCommand,
  right: OrderTransferServerCommand,
): boolean {
  return (
    left.idempotencyKey ===
    right.idempotencyKey
  );
}