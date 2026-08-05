import type {
  OrderTransferExecutionFailure,
  OrderTransferExecutionFailureCode,
} from "./order-transfer-executor";

export type OrderTransferRetryDecision =
  | Readonly<{
      retryAllowed: true;
      nextAttemptNumber: number;
      delayMilliseconds: number;
      reason:
        | "temporary-target-failure"
        | "timeout"
        | "unknown-transient-failure";
    }>
  | Readonly<{
      retryAllowed: false;
      reason:
        | "maximum-attempts-reached"
        | "non-retryable-failure"
        | "manual-review-required";
    }>;

export type EvaluateOrderTransferRetryInput =
  Readonly<{
    failure: OrderTransferExecutionFailure;
    completedAttemptCount: number;
  }>;

const MAXIMUM_RETRY_ATTEMPTS = 3;

const RETRYABLE_FAILURE_CODES:
  readonly OrderTransferExecutionFailureCode[] = [
    "target-unavailable",
    "timeout",
    "unknown",
  ];

const MANUAL_REVIEW_FAILURE_CODES:
  readonly OrderTransferExecutionFailureCode[] = [
    "order-state-conflict",
    "approval-invalid",
    "authorization-denied",
    "target-rejected",
    "order-not-found",
  ];

function calculateRetryDelayMilliseconds(
  nextAttemptNumber: number,
): number {
  const baseDelayMilliseconds = 2_000;

  return (
    baseDelayMilliseconds *
    2 ** Math.max(nextAttemptNumber - 1, 0)
  );
}

function getRetryReason(
  code: OrderTransferExecutionFailureCode,
):
  | "temporary-target-failure"
  | "timeout"
  | "unknown-transient-failure" {
  switch (code) {
    case "target-unavailable":
      return "temporary-target-failure";

    case "timeout":
      return "timeout";

    case "unknown":
    default:
      return "unknown-transient-failure";
  }
}

export function evaluateOrderTransferRetry({
  failure,
  completedAttemptCount,
}: EvaluateOrderTransferRetryInput): OrderTransferRetryDecision {
  const normalizedAttemptCount = Math.max(
    Math.trunc(completedAttemptCount),
    0,
  );

  if (
    normalizedAttemptCount >=
    MAXIMUM_RETRY_ATTEMPTS
  ) {
    return {
      retryAllowed: false,
      reason: "maximum-attempts-reached",
    };
  }

  if (
    MANUAL_REVIEW_FAILURE_CODES.includes(
      failure.code,
    )
  ) {
    return {
      retryAllowed: false,
      reason: "manual-review-required",
    };
  }

  if (
    !failure.retryable ||
    !RETRYABLE_FAILURE_CODES.includes(
      failure.code,
    )
  ) {
    return {
      retryAllowed: false,
      reason: "non-retryable-failure",
    };
  }

  const nextAttemptNumber =
    normalizedAttemptCount + 1;

  return {
    retryAllowed: true,
    nextAttemptNumber,
    delayMilliseconds:
      calculateRetryDelayMilliseconds(
        nextAttemptNumber,
      ),
    reason: getRetryReason(failure.code),
  };
}

export function canRetryOrderTransferFailure(
  failure: OrderTransferExecutionFailure,
  completedAttemptCount: number,
): boolean {
  return evaluateOrderTransferRetry({
    failure,
    completedAttemptCount,
  }).retryAllowed;
}

export function getMaximumOrderTransferRetryAttempts(): number {
  return MAXIMUM_RETRY_ATTEMPTS;
}