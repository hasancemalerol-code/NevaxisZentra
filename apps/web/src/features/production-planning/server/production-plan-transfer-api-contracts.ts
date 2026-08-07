import "./production-plan-server-boundary";

import { parseFailureCode } from "./production-plan-draft-api-contracts";

export { parseFailureCode };

export type ProductionPlanTransferRequestCreatedResponse = Readonly<{
  transferRequestId: string;
  productionPlanId: string;
  targetSystem: string;
  status: string;
  requestedAtUtc: string;
}>;

export type ProductionPlanTransferExecuteResponse = Readonly<{
  transferRequestId: string;
  productionPlanId: string;
  targetSystem: string;
  status: string;
  completedAtUtc: string | null;
  externalReference: string | null;
  failureCode: string | null;
  safeFailureMessage: string | null;
  retryable: boolean;
  reconciliationRequired: boolean;
}>;

export type ProductionPlanTransferRequestResult =
  | Readonly<{
      status: "success";
      value: ProductionPlanTransferRequestCreatedResponse;
    }>
  | Readonly<{
      status: "failure";
      code: string;
      message: string;
    }>;

export type ProductionPlanTransferExecuteResult =
  | Readonly<{
      status: "success";
      value: ProductionPlanTransferExecuteResponse;
    }>
  | Readonly<{
      status: "failure";
      code: string;
      message: string;
    }>;

export type CreateTransferRequestBody = Readonly<{
  expectedVersion: number;
  targetSystem: string;
}>;

export type ExecuteTransferRequestBody = Readonly<{
  expectedPlanVersion: number;
}>;

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === "object" && value !== null && !Array.isArray(value);
}

function extractTitle(value: unknown): string | undefined {
  if (isRecord(value) && typeof value.title === "string") {
    return value.title;
  }
  return undefined;
}

export function parseCreateTransferRequestSuccess(
  value: unknown,
): ProductionPlanTransferRequestResult {
  if (!isRecord(value)) {
    return {
      status: "failure",
      code: "invalid-response",
      message: "Transfer request response must be an object.",
    };
  }

  const transferRequestId =
    typeof value.transferRequestId === "string"
      ? value.transferRequestId.trim()
      : "";
  const productionPlanId =
    typeof value.productionPlanId === "string"
      ? value.productionPlanId.trim()
      : "";
  const targetSystem =
    typeof value.targetSystem === "string" ? value.targetSystem.trim() : "";
  const status = typeof value.status === "string" ? value.status.trim() : "";
  const requestedAtUtc =
    typeof value.requestedAtUtc === "string" ? value.requestedAtUtc : "";

  if (
    !transferRequestId ||
    !productionPlanId ||
    !targetSystem ||
    !status ||
    !requestedAtUtc
  ) {
    return {
      status: "failure",
      code: "invalid-response",
      message: "Transfer request response is incomplete.",
    };
  }

  return {
    status: "success",
    value: {
      transferRequestId,
      productionPlanId,
      targetSystem,
      status,
      requestedAtUtc,
    },
  };
}

export function parseExecuteTransferSuccess(
  value: unknown,
): ProductionPlanTransferExecuteResult {
  if (!isRecord(value)) {
    return {
      status: "failure",
      code: "invalid-response",
      message: "Transfer execute response must be an object.",
    };
  }

  const transferRequestId =
    typeof value.transferRequestId === "string"
      ? value.transferRequestId.trim()
      : "";
  const productionPlanId =
    typeof value.productionPlanId === "string"
      ? value.productionPlanId.trim()
      : "";
  const targetSystem =
    typeof value.targetSystem === "string" ? value.targetSystem.trim() : "";
  const status = typeof value.status === "string" ? value.status.trim() : "";

  if (!transferRequestId || !productionPlanId || !targetSystem || !status) {
    return {
      status: "failure",
      code: "invalid-response",
      message: "Transfer execute response is incomplete.",
    };
  }

  return {
    status: "success",
    value: {
      transferRequestId,
      productionPlanId,
      targetSystem,
      status,
      completedAtUtc:
        typeof value.completedAtUtc === "string" ? value.completedAtUtc : null,
      externalReference:
        typeof value.externalReference === "string"
          ? value.externalReference
          : null,
      failureCode:
        typeof value.failureCode === "string" ? value.failureCode : null,
      safeFailureMessage:
        typeof value.safeFailureMessage === "string"
          ? value.safeFailureMessage
          : null,
      retryable: value.retryable === true,
      reconciliationRequired: value.reconciliationRequired === true,
    },
  };
}

export function parseTransferFailure(
  value: unknown,
  fallbackMessage: string,
): ProductionPlanTransferRequestResult {
  return {
    status: "failure",
    code: parseFailureCode(value),
    message: extractTitle(value) ?? fallbackMessage,
  };
}

export function parseTransferExecuteFailure(
  value: unknown,
  fallbackMessage: string,
): ProductionPlanTransferExecuteResult {
  return {
    status: "failure",
    code: parseFailureCode(value),
    message: extractTitle(value) ?? fallbackMessage,
  };
}
