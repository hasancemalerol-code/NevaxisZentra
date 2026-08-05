import {
  canTransferOrderDraft,
  hasBlockingOrderIssues,
  type OrderDraft,
} from "../domain/order-draft";

export type OrderTransferReadinessCheckId =
  | "blocking-issues"
  | "authorized-approval"
  | "target-system"
  | "transfer-state";

export type OrderTransferReadinessCheck = Readonly<{
  id: OrderTransferReadinessCheckId;
  passed: boolean;
}>;

export type OrderTransferReadinessResult = Readonly<{
  isReady: boolean;
  passedCheckCount: number;
  totalCheckCount: number;
  checks: readonly OrderTransferReadinessCheck[];
}>;

const STARTABLE_TRANSFER_STATUSES = [
  "not-started",
  "pending",
  "failed",
] as const satisfies readonly OrderDraft["transfer"]["status"][];

export function isOrderTransferStateAvailable(
  status: OrderDraft["transfer"]["status"],
): boolean {
  return STARTABLE_TRANSFER_STATUSES.includes(
    status as (typeof STARTABLE_TRANSFER_STATUSES)[number],
  );
}

export function evaluateOrderTransferReadiness(
  orderDraft: OrderDraft,
): OrderTransferReadinessResult {
  const checks = [
    {
      id: "blocking-issues",
      passed: !hasBlockingOrderIssues(orderDraft),
    },
    {
      id: "authorized-approval",
      passed:
        orderDraft.approval.status === "approved",
    },
    {
      id: "target-system",
      passed: Boolean(
        orderDraft.transfer.targetSystem,
      ),
    },
    {
      id: "transfer-state",
      passed: isOrderTransferStateAvailable(
        orderDraft.transfer.status,
      ),
    },
  ] satisfies readonly OrderTransferReadinessCheck[];

  const passedCheckCount = checks.filter(
    (check) => check.passed,
  ).length;

  return {
    isReady: canTransferOrderDraft(orderDraft),
    passedCheckCount,
    totalCheckCount: checks.length,
    checks,
  };
}

export function getFailedOrderTransferChecks(
  orderDraft: OrderDraft,
): readonly OrderTransferReadinessCheck[] {
  return evaluateOrderTransferReadiness(
    orderDraft,
  ).checks.filter((check) => !check.passed);
}