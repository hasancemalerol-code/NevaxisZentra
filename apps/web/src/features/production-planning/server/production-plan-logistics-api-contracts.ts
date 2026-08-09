import "./production-plan-server-boundary";

export type PlanningLogisticsConstraintApiDto = Readonly<{
  id: string;
  constraintType: string;
  status: string;
  severity: string;
  requiredByUtc?: string;
  calculatedLatestStartUtc?: string;
  calculatedLatestCompletionUtc?: string;
  bufferMinutes?: number;
  slackMinutes?: number;
  blocking: boolean;
  riskLevel: string;
  reason: string;
  sourceReference: string;
  sourceType: string;
  sourceRecordId?: string;
  sourceExportCaseId?: string;
  sourceShipmentId?: string;
}>;

export type PlanningLogisticsEvaluationApiDto = Readonly<{
  evaluationId: string;
  productionPlanId: string;
  exportCaseId?: string;
  exportCaseNumber?: string;
  shipmentId?: string;
  shipmentReference?: string;
  sourceOrderReference?: string;
  pickupDeadlineUtc?: string;
  departureDeadlineUtc?: string;
  latestProductionCompletionUtc: string;
  plannedProductionCompletionUtc: string;
  slackMinutes: number;
  riskLevel: string;
  blocking: boolean;
  blockingReason?: string;
  sourceVersion: number;
  evaluatedAtUtc: string;
  version: number;
  constraints: readonly PlanningLogisticsConstraintApiDto[];
  linkageStatus: string;
}>;

export function parsePlanningLogisticsEvaluationApiDto(
  value: unknown,
): PlanningLogisticsEvaluationApiDto | null {
  if (!value || typeof value !== "object") {
    return null;
  }

  const record = value as Record<string, unknown>;
  const productionPlanId = readString(record.productionPlanId);
  const evaluationId = readString(record.evaluationId);
  const latestProductionCompletionUtc = readString(
    record.latestProductionCompletionUtc,
  );
  const plannedProductionCompletionUtc = readString(
    record.plannedProductionCompletionUtc,
  );
  const evaluatedAtUtc = readString(record.evaluatedAtUtc);
  const linkageStatus = readString(record.linkageStatus);

  if (
    !productionPlanId ||
    !evaluationId ||
    !latestProductionCompletionUtc ||
    !plannedProductionCompletionUtc ||
    !evaluatedAtUtc ||
    !linkageStatus
  ) {
    return null;
  }

  return {
    evaluationId,
    productionPlanId,
    exportCaseId: readOptionalString(record.exportCaseId),
    exportCaseNumber: readOptionalString(record.exportCaseNumber),
    shipmentId: readOptionalString(record.shipmentId),
    shipmentReference: readOptionalString(record.shipmentReference),
    sourceOrderReference: readOptionalString(record.sourceOrderReference),
    pickupDeadlineUtc: readOptionalString(record.pickupDeadlineUtc),
    departureDeadlineUtc: readOptionalString(record.departureDeadlineUtc),
    latestProductionCompletionUtc,
    plannedProductionCompletionUtc,
    slackMinutes: readNumber(record.slackMinutes) ?? 0,
    riskLevel: readString(record.riskLevel) ?? "none",
    blocking: record.blocking === true,
    blockingReason: readOptionalString(record.blockingReason),
    sourceVersion: readNumber(record.sourceVersion) ?? 0,
    evaluatedAtUtc,
    version: readNumber(record.version) ?? 1,
    constraints: Array.isArray(record.constraints)
      ? record.constraints
          .map(parsePlanningLogisticsConstraintApiDto)
          .filter(
            (
              constraint,
            ): constraint is PlanningLogisticsConstraintApiDto =>
              constraint !== null,
          )
      : [],
    linkageStatus,
  };
}

function parsePlanningLogisticsConstraintApiDto(
  value: unknown,
): PlanningLogisticsConstraintApiDto | null {
  if (!value || typeof value !== "object") {
    return null;
  }

  const record = value as Record<string, unknown>;
  const id = readString(record.id);
  const constraintType = readString(record.constraintType);
  const status = readString(record.status);
  const severity = readString(record.severity);
  const riskLevel = readString(record.riskLevel);
  const reason = readString(record.reason);
  const sourceReference = readString(record.sourceReference);
  const sourceType = readString(record.sourceType);

  if (
    !id ||
    !constraintType ||
    !status ||
    !severity ||
    !riskLevel ||
    !reason ||
    !sourceReference ||
    !sourceType
  ) {
    return null;
  }

  return {
    id,
    constraintType,
    status,
    severity,
    requiredByUtc: readOptionalString(record.requiredByUtc),
    calculatedLatestStartUtc: readOptionalString(
      record.calculatedLatestStartUtc,
    ),
    calculatedLatestCompletionUtc: readOptionalString(
      record.calculatedLatestCompletionUtc,
    ),
    bufferMinutes: readOptionalNumber(record.bufferMinutes),
    slackMinutes: readOptionalNumber(record.slackMinutes),
    blocking: record.blocking === true,
    riskLevel,
    reason,
    sourceReference,
    sourceType,
    sourceRecordId: readOptionalString(record.sourceRecordId),
    sourceExportCaseId: readOptionalString(record.sourceExportCaseId),
    sourceShipmentId: readOptionalString(record.sourceShipmentId),
  };
}

function readString(value: unknown): string | null {
  return typeof value === "string" && value.trim().length > 0
    ? value.trim()
    : null;
}

function readOptionalString(value: unknown): string | undefined {
  const parsed = readString(value);
  return parsed ?? undefined;
}

function readNumber(value: unknown): number | null {
  return typeof value === "number" && Number.isFinite(value)
    ? value
    : null;
}

function readOptionalNumber(value: unknown): number | undefined {
  const parsed = readNumber(value);
  return parsed ?? undefined;
}
