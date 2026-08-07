import "./production-plan-server-boundary";

import { parseFailureCode } from "./production-plan-draft-api-contracts";

export { parseFailureCode };

export type ProductionPlanWorkflowSuccessResponse = Readonly<{
  id: string;
  referenceCode: string;
  previousStatus: string;
  status: string;
  version: number;
  updatedAtUtc: string;
  approvalStatus: string;
}>;

export type ProductionPlanWorkflowResult =
  | Readonly<{
      status: "success";
      value: ProductionPlanWorkflowSuccessResponse;
    }>
  | Readonly<{
      status: "failure";
      code: string;
      message: string;
    }>;

export type ProductionPlanSubmitForReviewRequestBody = Readonly<{
  facilityId: string;
  expectedVersion: number;
  comment?: string;
}>;

export type ProductionPlanRequestRevisionRequestBody = Readonly<{
  facilityId: string;
  expectedVersion: number;
  reason: string;
}>;

export type ProductionPlanApproveRequestBody = Readonly<{
  facilityId: string;
  expectedVersion: number;
  note?: string;
}>;

export type ProductionPlanRejectRequestBody = Readonly<{
  facilityId: string;
  expectedVersion: number;
  reason: string;
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

export function parseProductionPlanWorkflowSuccess(
  value: unknown,
): ProductionPlanWorkflowResult {
  if (!isRecord(value)) {
    return {
      status: "failure",
      code: "invalid-response",
      message: "Workflow response must be an object.",
    };
  }

  const id = typeof value.id === "string" ? value.id.trim() : "";
  const referenceCode =
    typeof value.referenceCode === "string" ? value.referenceCode.trim() : "";
  const previousStatus =
    typeof value.previousStatus === "string" ? value.previousStatus.trim() : "";
  const status = typeof value.status === "string" ? value.status.trim() : "";
  const version =
    typeof value.version === "number" && Number.isFinite(value.version)
      ? value.version
      : -1;
  const updatedAtUtc =
    typeof value.updatedAtUtc === "string" ? value.updatedAtUtc : "";
  const approvalStatus =
    typeof value.approvalStatus === "string" ? value.approvalStatus.trim() : "";

  if (
    !id ||
    !referenceCode ||
    !previousStatus ||
    !status ||
    version < 0 ||
    !updatedAtUtc ||
    !approvalStatus
  ) {
    return {
      status: "failure",
      code: "invalid-response",
      message: "Workflow success response is incomplete.",
    };
  }

  return {
    status: "success",
    value: {
      id,
      referenceCode,
      previousStatus,
      status,
      version,
      updatedAtUtc,
      approvalStatus,
    },
  };
}

export function parseWorkflowFailure(
  value: unknown,
  fallbackMessage: string,
): ProductionPlanWorkflowResult {
  return {
    status: "failure",
    code: parseFailureCode(value),
    message: extractTitle(value) ?? fallbackMessage,
  };
}
