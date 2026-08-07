import "./production-plan-server-boundary";

import { parseFailureCode } from "./production-plan-draft-api-contracts";

export { parseFailureCode };

export type ProductionPlanHandoffResponse = Readonly<{
  handoffId: string;
  productionPlanId: string;
  referenceCode: string;
  status: string;
  handoffVersion: number;
  preparedAtUtc: string;
}>;

export type ProductionPlanHandoffResult =
  | Readonly<{
      status: "success";
      value: ProductionPlanHandoffResponse;
    }>
  | Readonly<{
      status: "failure";
      code: string;
      message: string;
    }>;

export type PrepareHandoffBody = Readonly<{
  expectedVersion: number;
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

export function parseHandoffSuccess(
  value: unknown,
): ProductionPlanHandoffResult {
  if (!isRecord(value)) {
    return {
      status: "failure",
      code: "invalid-response",
      message: "Handoff response must be an object.",
    };
  }

  const handoffId =
    typeof value.handoffId === "string" ? value.handoffId.trim() : "";
  const productionPlanId =
    typeof value.productionPlanId === "string"
      ? value.productionPlanId.trim()
      : "";
  const referenceCode =
    typeof value.referenceCode === "string" ? value.referenceCode.trim() : "";
  const status =
    typeof value.status === "string" ? value.status.trim() : "";
  const handoffVersion =
    typeof value.handoffVersion === "number" ? value.handoffVersion : 0;
  const preparedAtUtc =
    typeof value.preparedAtUtc === "string" ? value.preparedAtUtc : "";

  if (!handoffId || !productionPlanId || !referenceCode || !status || !preparedAtUtc) {
    return {
      status: "failure",
      code: "invalid-response",
      message: "Handoff response is incomplete.",
    };
  }

  return {
    status: "success",
    value: {
      handoffId,
      productionPlanId,
      referenceCode,
      status,
      handoffVersion,
      preparedAtUtc,
    },
  };
}

export function parseHandoffFailure(
  value: unknown,
  fallbackMessage: string,
): ProductionPlanHandoffResult {
  return {
    status: "failure",
    code: parseFailureCode(value),
    message: extractTitle(value) ?? fallbackMessage,
  };
}
