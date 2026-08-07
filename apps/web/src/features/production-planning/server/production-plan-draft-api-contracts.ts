import "./production-plan-server-boundary";

import type { ProductionPlanDraftFieldError } from "../domain/production-plan";

export type { ProductionPlanDraftFieldError as ProductionPlanDraftFieldErrorDto };

type ProductionPlanDraftFieldErrorDto = ProductionPlanDraftFieldError;

export type ProductionPlanDraftWriteRequestBody = Readonly<{
  facilityId: string;
  referenceCode: string;
  title: string;
  notes?: string;
  origin: string;
  aiGenerated: boolean;
  confidence?: string;
  plannedStartAtUtc: string;
  plannedEndAtUtc: string;
  demands: readonly ProductionPlanDemandWriteDto[];
  risks: readonly ProductionPlanRiskWriteDto[];
  resources: ProductionPlanResourceWriteDto;
  timeWindow?: ProductionPlanTimeWindowWriteDto;
  approval?: ProductionPlanApprovalWriteDto;
  transfer?: ProductionPlanTransferWriteDto;
  sourceReferences: readonly ProductionPlanSourceReferenceWriteDto[];
  aiExplanation?: string;
}>;

export type ProductionPlanDraftUpdateRequestBody =
  ProductionPlanDraftWriteRequestBody &
  Readonly<{
    expectedVersion: number;
  }>;

export type ProductionPlanDemandWriteDto = Readonly<{
  id: string;
  productId: string;
  productName: string;
  requiredQuantity: number;
  unit: string;
  priority: string;
  orderReference?: string;
  customerId?: string;
  customerName?: string;
  productCode?: string;
  requestedDeliveryDateUtc?: string;
}>;

export type ProductionPlanRiskWriteDto = Readonly<{
  id: string;
  type: string;
  severity: string;
  title: string;
  description: string;
  blocking: boolean;
  source?: string;
}>;

export type ProductionPlanResourceWriteDto = Readonly<{
  machineId?: string;
  machineName?: string;
  moldId?: string;
  moldName?: string;
  materialId?: string;
  materialName?: string;
  operatorGroupId?: string;
  operatorGroupName?: string;
}>;

export type ProductionPlanTimeWindowWriteDto = Readonly<{
  plannedStartAtUtc?: string;
  plannedEndAtUtc?: string;
  estimatedDurationMinutes?: number;
  shiftId?: string;
  shiftName?: string;
}>;

export type ProductionPlanApprovalWriteDto = Readonly<{
  status: string;
  requiredRole?: string;
}>;

export type ProductionPlanTransferWriteDto = Readonly<{
  status: string;
  targetSystem?: string;
}>;

export type ProductionPlanSourceReferenceWriteDto = Readonly<{
  sourceType: string;
  sourceId: string;
  sourceUpdatedAtUtc?: string;
}>;

export type ProductionPlanDraftWriteSuccessResponse = Readonly<{
  id: string;
  referenceCode: string;
  status: string;
  version: number;
  createdAtUtc: string;
  updatedAtUtc: string;
}>;

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === "object" && value !== null && !Array.isArray(value);
}

export type ProductionPlanDraftWriteResult =
  | Readonly<{
      status: "success";
      value: ProductionPlanDraftWriteSuccessResponse;
    }>
  | Readonly<{
      status: "failure";
      code: string;
      message: string;
      fieldErrors?: readonly ProductionPlanDraftFieldErrorDto[];
    }>;

export function parseProductionPlanDraftWriteSuccess(
  value: unknown,
): ProductionPlanDraftWriteResult {
  if (!isRecord(value)) {
    return {
      status: "failure",
      code: "invalid-response",
      message: "Write response must be an object.",
    };
  }

  const id = typeof value.id === "string" ? value.id.trim() : "";
  const referenceCode =
    typeof value.referenceCode === "string" ? value.referenceCode.trim() : "";
  const status = typeof value.status === "string" ? value.status.trim() : "";
  const version =
    typeof value.version === "number" && Number.isFinite(value.version)
      ? value.version
      : -1;
  const createdAtUtc =
    typeof value.createdAtUtc === "string" ? value.createdAtUtc : "";
  const updatedAtUtc =
    typeof value.updatedAtUtc === "string" ? value.updatedAtUtc : "";

  if (!id || !referenceCode || !status || version < 0 || !createdAtUtc || !updatedAtUtc) {
    return {
      status: "failure",
      code: "invalid-response",
      message: "Write success response is incomplete.",
    };
  }

  return {
    status: "success",
    value: {
      id,
      referenceCode,
      status,
      version,
      createdAtUtc,
      updatedAtUtc,
    },
  };
}

export function parseProductionPlanDraftFieldErrors(
  value: unknown,
): readonly ProductionPlanDraftFieldErrorDto[] {
  if (!isRecord(value)) {
    return [];
  }

  const extensions = isRecord(value.extensions) ? value.extensions : value;
  const fieldErrorsRaw = extensions.fieldErrors;

  if (!Array.isArray(fieldErrorsRaw)) {
    return [];
  }

  const result: ProductionPlanDraftFieldErrorDto[] = [];

  for (const item of fieldErrorsRaw) {
    if (!isRecord(item)) {
      continue;
    }

    const field = typeof item.field === "string" ? item.field : "";
    const code = typeof item.code === "string" ? item.code : "";
    const message = typeof item.message === "string" ? item.message : "";

    if (field) {
      result.push({ field, code, message });
    }
  }

  return result;
}

export function parseFailureCode(value: unknown): string {
  if (!isRecord(value)) {
    return "unexpected-error";
  }

  const extensions = isRecord(value.extensions) ? value.extensions : value;
  const failureCode = extensions.failureCode ?? value.failureCode;

  return typeof failureCode === "string" && failureCode.trim()
    ? failureCode.trim()
    : "unexpected-error";
}
