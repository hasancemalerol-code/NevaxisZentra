import type { OrderDraft } from "../domain/order-draft";

export type OrderTransferActorRole =
  | "sales-representative"
  | "sales-and-order-manager"
  | "planning-manager"
  | "plant-manager"
  | "system-administrator";

export type OrderTransferAuthorizationInput =
  Readonly<{
    role: OrderTransferActorRole;
    userId: string;
    tenantId: string;
    orderDraft: OrderDraft;
  }>;

export type OrderTransferAuthorizationFailureReason =
  | "user-missing"
  | "tenant-mismatch"
  | "role-not-allowed"
  | "target-system-not-allowed";

export type OrderTransferAuthorizationResult =
  | Readonly<{
      authorized: true;
      userId: string;
      role: OrderTransferActorRole;
    }>
  | Readonly<{
      authorized: false;
      reason: OrderTransferAuthorizationFailureReason;
    }>;

type OrderTransferTargetSystem = NonNullable<
  OrderDraft["transfer"]["targetSystem"]
>;

const ALLOWED_TRANSFER_ROLES:
  readonly OrderTransferActorRole[] = [
    "sales-and-order-manager",
    "planning-manager",
    "plant-manager",
    "system-administrator",
  ];

const TARGET_SYSTEM_ALLOWED_ROLES:
  Readonly<
    Record<
      OrderTransferTargetSystem,
      readonly OrderTransferActorRole[]
    >
  > = {
    erp: [
      "sales-and-order-manager",
      "plant-manager",
      "system-administrator",
    ],
    mes: [
      "planning-manager",
      "plant-manager",
      "system-administrator",
    ],
    crevafactos: [
      "sales-and-order-manager",
      "planning-manager",
      "plant-manager",
      "system-administrator",
    ],
  };

function isRoleAllowed(
  role: OrderTransferActorRole,
): boolean {
  return ALLOWED_TRANSFER_ROLES.includes(role);
}

function isRoleAllowedForTarget(
  role: OrderTransferActorRole,
  targetSystem: OrderTransferTargetSystem,
): boolean {
  return TARGET_SYSTEM_ALLOWED_ROLES[
    targetSystem
  ].includes(role);
}

export function authorizeOrderTransfer({
  role,
  userId,
  tenantId,
  orderDraft,
}: OrderTransferAuthorizationInput): OrderTransferAuthorizationResult {
  const normalizedUserId = userId.trim();

  if (!normalizedUserId) {
    return {
      authorized: false,
      reason: "user-missing",
    };
  }

  if (orderDraft.tenantId !== tenantId) {
    return {
      authorized: false,
      reason: "tenant-mismatch",
    };
  }

  if (!isRoleAllowed(role)) {
    return {
      authorized: false,
      reason: "role-not-allowed",
    };
  }

  const targetSystem =
    orderDraft.transfer.targetSystem;

  if (
    targetSystem &&
    !isRoleAllowedForTarget(
      role,
      targetSystem,
    )
  ) {
    return {
      authorized: false,
      reason: "target-system-not-allowed",
    };
  }

  return {
    authorized: true,
    userId: normalizedUserId,
    role,
  };
}

export function canRoleTransferOrderToTarget(
  role: OrderTransferActorRole,
  targetSystem: OrderTransferTargetSystem,
): boolean {
  return (
    isRoleAllowed(role) &&
    isRoleAllowedForTarget(
      role,
      targetSystem,
    )
  );
}