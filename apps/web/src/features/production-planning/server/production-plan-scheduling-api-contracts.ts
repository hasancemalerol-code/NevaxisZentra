import "./production-plan-server-boundary";

import { parseFailureCode } from "./production-plan-draft-api-contracts";

export { parseFailureCode };

export type ProductionPlanScheduleSuccessResponse = Readonly<{
  id: string;
  referenceCode: string;
  previousStatus: string;
  status: string;
  version: number;
  updatedAtUtc: string;
  approvalStatus: string;
  plannedStartAtUtc: string;
  plannedEndAtUtc: string;
  scheduledAtUtc: string;
}>;

export type ProductionPlanScheduleResult =
  | Readonly<{
      status: "success";
      value: ProductionPlanScheduleSuccessResponse;
    }>
  | Readonly<{
      status: "failure";
      code: string;
      message: string;
    }>;

export type ProductionPlanScheduleRequestBody = Readonly<{
  expectedVersion: number;
  plannedStartAtUtc: string;
  plannedEndAtUtc: string;
  shiftName?: string;
  schedulingComment?: string;
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

export function parseScheduleSuccess(
  value: unknown,
): ProductionPlanScheduleResult {
  if (!isRecord(value)) {
    return {
      status: "failure",
      code: "invalid-response",
      message: "Schedule response must be an object.",
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
  const plannedStartAtUtc =
    typeof value.plannedStartAtUtc === "string" ? value.plannedStartAtUtc : "";
  const plannedEndAtUtc =
    typeof value.plannedEndAtUtc === "string" ? value.plannedEndAtUtc : "";
  const scheduledAtUtc =
    typeof value.scheduledAtUtc === "string" ? value.scheduledAtUtc : "";

  if (
    !id ||
    !referenceCode ||
    !previousStatus ||
    !status ||
    version < 0 ||
    !updatedAtUtc ||
    !approvalStatus ||
    !plannedStartAtUtc ||
    !plannedEndAtUtc ||
    !scheduledAtUtc
  ) {
    return {
      status: "failure",
      code: "invalid-response",
      message: "Schedule success response is incomplete.",
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
      plannedStartAtUtc,
      plannedEndAtUtc,
      scheduledAtUtc,
    },
  };
}

export function parseScheduleFailure(
  value: unknown,
  fallbackMessage: string,
): ProductionPlanScheduleResult {
  return {
    status: "failure",
    code: parseFailureCode(value),
    message: extractTitle(value) ?? fallbackMessage,
  };
}
