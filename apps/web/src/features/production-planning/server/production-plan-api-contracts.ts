import "./production-plan-server-boundary";

export type ApiValidationFailure =
  Readonly<{
    status: "failure";
    code: "invalid-response";
    message: string;
  }>;

export type ApiValidationResult<T> =
  | Readonly<{
      status: "success";
      value: T;
    }>
  | ApiValidationFailure;

export type ProductionPlanCoreApiDto =
  Readonly<{
    id: string;
    referenceCode: string;
    title: string;
    facilityId: string;
    status: string;
    origin: string;
    aiGenerated: boolean;
    confidence: string;
    plannedStartAtUtc: string;
    plannedEndAtUtc: string;
    approvalStatus: string;
    transferStatus: string;
    transferTargetSystem?: string;
    updatedAtUtc: string;
  }>;

export type ProductionPlanSummaryApiDto =
  ProductionPlanCoreApiDto &
  Readonly<{
    demandCount: number;
    primaryRequiredQuantity?: number;
    primaryUnit?: string;
    primaryProductCode?: string;
    primaryProductName?: string;
    highestDemandPriority?: string;
    riskCount: number;
    criticalRiskCount: number;
    blockingRiskCount: number;
    hasBlockingRisk: boolean;
    hasMachineAssignment: boolean;
    hasMoldAssignment: boolean;
    hasMaterialAssignment: boolean;
    hasOperatorGroupAssignment: boolean;
    shiftName?: string;
    earliestRequestedDeliveryDateUtc?: string;
  }>;

export type ProductionPlanListApiResponse =
  Readonly<{
    facilityId: string;
    count: number;
    items: readonly ProductionPlanSummaryApiDto[];
  }>;

export type ProductionPlanDemandApiDto =
  Readonly<{
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

export type ProductionPlanRiskApiDto =
  Readonly<{
    id: string;
    type: string;
    severity: string;
    title: string;
    description: string;
    blocking: boolean;
    source?: string;
  }>;

export type ProductionPlanResourceAssignmentApiDto =
  Readonly<{
    machineId?: string;
    machineName?: string;
    moldId?: string;
    moldName?: string;
    materialId?: string;
    materialName?: string;
    operatorGroupId?: string;
    operatorGroupName?: string;
  }>;

export type ProductionPlanTimeWindowApiDto =
  Readonly<{
    plannedStartAtUtc?: string;
    plannedEndAtUtc?: string;
    estimatedDurationMinutes?: number;
    shiftId?: string;
    shiftName?: string;
  }>;

export type ProductionPlanApprovalApiDto =
  Readonly<{
    status: string;
    requiredRole?: string;
    requestedAtUtc?: string;
    requestedBy?: string;
    approvedAtUtc?: string;
    approvedBy?: string;
    rejectedAtUtc?: string;
    rejectedBy?: string;
    rejectionReason?: string;
  }>;

export type ProductionPlanTransferApiDto =
  Readonly<{
    targetSystem?: string;
    status: string;
    externalPlanId?: string;
    startedAtUtc?: string;
    completedAtUtc?: string;
    errorCode?: string;
    errorMessage?: string;
  }>;

export type ProductionPlanSourceReferenceApiDto =
  Readonly<{
    sourceType: string;
    sourceId: string;
    sourceUpdatedAtUtc?: string;
  }>;

export type ProductionPlanHistoryEntryApiDto =
  Readonly<{
    id: string;
    type: string;
    title: string;
    description: string;
    occurredAtUtc?: string;
  }>;

export type ProductionPlanDetailApiDto =
  ProductionPlanCoreApiDto &
  Readonly<{
    createdAtUtc: string;
    version: number;
    notes?: string;
    createdBy: string;
    aiExplanation?: string;
    demands: readonly ProductionPlanDemandApiDto[];
    risks: readonly ProductionPlanRiskApiDto[];
    resources: ProductionPlanResourceAssignmentApiDto;
    timeWindow: ProductionPlanTimeWindowApiDto;
    approval: ProductionPlanApprovalApiDto;
    transfer: ProductionPlanTransferApiDto;
    sourceReferences: readonly ProductionPlanSourceReferenceApiDto[];
    history: readonly ProductionPlanHistoryEntryApiDto[];
  }>;

export type ProductionPlanDetailApiResponse =
  Readonly<{
    plan: ProductionPlanDetailApiDto;
  }>;

function createValidationFailure(
  message: string,
): ApiValidationFailure {
  return {
    status: "failure",
    code: "invalid-response",
    message,
  };
}

function isRecord(
  value: unknown,
): value is Record<string, unknown> {
  return (
    typeof value === "object" &&
    value !== null &&
    !Array.isArray(value)
  );
}

function readNonEmptyString(
  record: Record<string, unknown>,
  key: string,
  label: string,
): ApiValidationResult<string> {
  const value = record[key];

  if (
    typeof value !== "string" ||
    !value.trim()
  ) {
    return createValidationFailure(
      `${label} must be a non-empty string.`,
    );
  }

  return {
    status: "success",
    value: value.trim(),
  };
}

function readOptionalString(
  record: Record<string, unknown>,
  key: string,
): string | undefined {
  const value = record[key];

  if (typeof value !== "string") {
    return undefined;
  }

  const trimmedValue = value.trim();
  return trimmedValue.length > 0
    ? trimmedValue
    : undefined;
}

function readRequiredBoolean(
  record: Record<string, unknown>,
  key: string,
  label: string,
): ApiValidationResult<boolean> {
  const value = record[key];

  if (typeof value !== "boolean") {
    return createValidationFailure(
      `${label} must be a boolean.`,
    );
  }

  return {
    status: "success",
    value,
  };
}

function readRequiredNumber(
  record: Record<string, unknown>,
  key: string,
  label: string,
): ApiValidationResult<number> {
  const value = record[key];

  if (
    typeof value !== "number" ||
    !Number.isFinite(value)
  ) {
    return createValidationFailure(
      `${label} must be a finite number.`,
    );
  }

  return {
    status: "success",
    value,
  };
}

function readRequiredIsoDate(
  record: Record<string, unknown>,
  key: string,
  label: string,
): ApiValidationResult<string> {
  const valueResult = readNonEmptyString(
    record,
    key,
    label,
  );

  if (valueResult.status === "failure") {
    return valueResult;
  }

  const parsedDate = Date.parse(
    valueResult.value,
  );

  if (Number.isNaN(parsedDate)) {
    return createValidationFailure(
      `${label} must be a valid ISO date.`,
    );
  }

  return valueResult;
}

function readOptionalIsoDate(
  record: Record<string, unknown>,
  key: string,
  label: string,
): ApiValidationResult<string | undefined> {
  const value = readOptionalString(
    record,
    key,
  );

  if (!value) {
    return {
      status: "success",
      value: undefined,
    };
  }

  if (Number.isNaN(Date.parse(value))) {
    return createValidationFailure(
      `${label} must be a valid ISO date.`,
    );
  }

  return {
    status: "success",
    value,
  };
}

function readRequiredArray(
  record: Record<string, unknown>,
  key: string,
  label: string,
): ApiValidationResult<readonly Record<string, unknown>[]> {
  const value = record[key];

  if (!Array.isArray(value)) {
    return createValidationFailure(
      `${label} must be an array.`,
    );
  }

  const items = value.filter(isRecord);

  if (items.length !== value.length) {
    return createValidationFailure(
      `${label} must contain objects only.`,
    );
  }

  return {
    status: "success",
    value: items,
  };
}

function parsePlanCoreDto(
  value: Record<string, unknown>,
): ApiValidationResult<ProductionPlanCoreApiDto> {
  const id = readNonEmptyString(
    value,
    "id",
    "plan.id",
  );
  if (id.status === "failure") {
    return id;
  }

  const referenceCode = readNonEmptyString(
    value,
    "referenceCode",
    "plan.referenceCode",
  );
  if (referenceCode.status === "failure") {
    return referenceCode;
  }

  const title = readNonEmptyString(
    value,
    "title",
    "plan.title",
  );
  if (title.status === "failure") {
    return title;
  }

  const facilityId = readNonEmptyString(
    value,
    "facilityId",
    "plan.facilityId",
  );
  if (facilityId.status === "failure") {
    return facilityId;
  }

  const status = readNonEmptyString(
    value,
    "status",
    "plan.status",
  );
  if (status.status === "failure") {
    return status;
  }

  const origin = readNonEmptyString(
    value,
    "origin",
    "plan.origin",
  );
  if (origin.status === "failure") {
    return origin;
  }

  const aiGenerated = readRequiredBoolean(
    value,
    "aiGenerated",
    "plan.aiGenerated",
  );
  if (aiGenerated.status === "failure") {
    return aiGenerated;
  }

  const confidence = readNonEmptyString(
    value,
    "confidence",
    "plan.confidence",
  );
  if (confidence.status === "failure") {
    return confidence;
  }

  const plannedStartAtUtc =
    readRequiredIsoDate(
      value,
      "plannedStartAtUtc",
      "plan.plannedStartAtUtc",
    );
  if (
    plannedStartAtUtc.status === "failure"
  ) {
    return plannedStartAtUtc;
  }

  const plannedEndAtUtc = readRequiredIsoDate(
    value,
    "plannedEndAtUtc",
    "plan.plannedEndAtUtc",
  );
  if (plannedEndAtUtc.status === "failure") {
    return plannedEndAtUtc;
  }

  const approvalStatus = readNonEmptyString(
    value,
    "approvalStatus",
    "plan.approvalStatus",
  );
  if (approvalStatus.status === "failure") {
    return approvalStatus;
  }

  const transferStatus = readNonEmptyString(
    value,
    "transferStatus",
    "plan.transferStatus",
  );
  if (transferStatus.status === "failure") {
    return transferStatus;
  }

  const updatedAtUtc = readRequiredIsoDate(
    value,
    "updatedAtUtc",
    "plan.updatedAtUtc",
  );
  if (updatedAtUtc.status === "failure") {
    return updatedAtUtc;
  }

  return {
    status: "success",
    value: {
      id: id.value,
      referenceCode: referenceCode.value,
      title: title.value,
      facilityId: facilityId.value,
      status: status.value,
      origin: origin.value,
      aiGenerated: aiGenerated.value,
      confidence: confidence.value,
      plannedStartAtUtc:
        plannedStartAtUtc.value,
      plannedEndAtUtc:
        plannedEndAtUtc.value,
      approvalStatus:
        approvalStatus.value,
      transferStatus:
        transferStatus.value,
      transferTargetSystem:
        readOptionalString(
          value,
          "transferTargetSystem",
        ),
      updatedAtUtc: updatedAtUtc.value,
    },
  };
}

function parseSummaryDto(
  value: unknown,
): ApiValidationResult<ProductionPlanSummaryApiDto> {
  if (!isRecord(value)) {
    return createValidationFailure(
      "Production plan summary must be an object.",
    );
  }

  const core = parsePlanCoreDto(value);
  if (core.status === "failure") {
    return core;
  }

  const demandCount = readRequiredNumber(
    value,
    "demandCount",
    "summary.demandCount",
  );
  if (demandCount.status === "failure") {
    return demandCount;
  }

  const riskCount = readRequiredNumber(
    value,
    "riskCount",
    "summary.riskCount",
  );
  if (riskCount.status === "failure") {
    return riskCount;
  }

  const criticalRiskCount = readRequiredNumber(
    value,
    "criticalRiskCount",
    "summary.criticalRiskCount",
  );
  if (criticalRiskCount.status === "failure") {
    return criticalRiskCount;
  }

  const blockingRiskCount = readRequiredNumber(
    value,
    "blockingRiskCount",
    "summary.blockingRiskCount",
  );
  if (blockingRiskCount.status === "failure") {
    return blockingRiskCount;
  }

  const hasBlockingRisk = readRequiredBoolean(
    value,
    "hasBlockingRisk",
    "summary.hasBlockingRisk",
  );
  if (hasBlockingRisk.status === "failure") {
    return hasBlockingRisk;
  }

  const hasMachineAssignment = readRequiredBoolean(
    value,
    "hasMachineAssignment",
    "summary.hasMachineAssignment",
  );
  if (hasMachineAssignment.status === "failure") {
    return hasMachineAssignment;
  }

  const hasMoldAssignment = readRequiredBoolean(
    value,
    "hasMoldAssignment",
    "summary.hasMoldAssignment",
  );
  if (hasMoldAssignment.status === "failure") {
    return hasMoldAssignment;
  }

  const hasMaterialAssignment = readRequiredBoolean(
    value,
    "hasMaterialAssignment",
    "summary.hasMaterialAssignment",
  );
  if (hasMaterialAssignment.status === "failure") {
    return hasMaterialAssignment;
  }

  const hasOperatorGroupAssignment =
    readRequiredBoolean(
      value,
      "hasOperatorGroupAssignment",
      "summary.hasOperatorGroupAssignment",
    );
  if (
    hasOperatorGroupAssignment.status ===
    "failure"
  ) {
    return hasOperatorGroupAssignment;
  }

  const primaryRequiredQuantityValue =
    value.primaryRequiredQuantity;
  const primaryRequiredQuantity =
    typeof primaryRequiredQuantityValue ===
      "number" &&
    Number.isFinite(
      primaryRequiredQuantityValue,
    )
      ? primaryRequiredQuantityValue
      : undefined;

  const earliestRequestedDeliveryDateUtc =
    readOptionalIsoDate(
      value,
      "earliestRequestedDeliveryDateUtc",
      "summary.earliestRequestedDeliveryDateUtc",
    );
  if (
    earliestRequestedDeliveryDateUtc.status ===
    "failure"
  ) {
    return earliestRequestedDeliveryDateUtc;
  }

  return {
    status: "success",
    value: {
      ...core.value,
      demandCount: demandCount.value,
      primaryRequiredQuantity,
      primaryUnit: readOptionalString(
        value,
        "primaryUnit",
      ),
      primaryProductCode: readOptionalString(
        value,
        "primaryProductCode",
      ),
      primaryProductName: readOptionalString(
        value,
        "primaryProductName",
      ),
      highestDemandPriority:
        readOptionalString(
          value,
          "highestDemandPriority",
        ),
      riskCount: riskCount.value,
      criticalRiskCount:
        criticalRiskCount.value,
      blockingRiskCount:
        blockingRiskCount.value,
      hasBlockingRisk:
        hasBlockingRisk.value,
      hasMachineAssignment:
        hasMachineAssignment.value,
      hasMoldAssignment:
        hasMoldAssignment.value,
      hasMaterialAssignment:
        hasMaterialAssignment.value,
      hasOperatorGroupAssignment:
        hasOperatorGroupAssignment.value,
      shiftName: readOptionalString(
        value,
        "shiftName",
      ),
      earliestRequestedDeliveryDateUtc:
        earliestRequestedDeliveryDateUtc.value,
    },
  };
}

export function parseProductionPlanListApiResponse(
  value: unknown,
): ApiValidationResult<ProductionPlanListApiResponse> {
  if (!isRecord(value)) {
    return createValidationFailure(
      "Production plan list response must be an object.",
    );
  }

  const facilityId = readNonEmptyString(
    value,
    "facilityId",
    "list.facilityId",
  );
  if (facilityId.status === "failure") {
    return facilityId;
  }

  const count = readRequiredNumber(
    value,
    "count",
    "list.count",
  );
  if (count.status === "failure") {
    return count;
  }

  const itemsResult = readRequiredArray(
    value,
    "items",
    "list.items",
  );
  if (itemsResult.status === "failure") {
    return itemsResult;
  }

  const items: ProductionPlanSummaryApiDto[] =
    [];

  for (const item of itemsResult.value) {
    const parsedItem = parseSummaryDto(item);

    if (parsedItem.status === "failure") {
      return parsedItem;
    }

    items.push(parsedItem.value);
  }

  return {
    status: "success",
    value: {
      facilityId: facilityId.value,
      count: count.value,
      items,
    },
  };
}

function parseDemandDto(
  value: Record<string, unknown>,
): ApiValidationResult<ProductionPlanDemandApiDto> {
  const id = readNonEmptyString(
    value,
    "id",
    "demand.id",
  );
  if (id.status === "failure") {
    return id;
  }

  const productId = readNonEmptyString(
    value,
    "productId",
    "demand.productId",
  );
  if (productId.status === "failure") {
    return productId;
  }

  const productName = readNonEmptyString(
    value,
    "productName",
    "demand.productName",
  );
  if (productName.status === "failure") {
    return productName;
  }

  const requiredQuantity =
    readRequiredNumber(
      value,
      "requiredQuantity",
      "demand.requiredQuantity",
    );
  if (
    requiredQuantity.status === "failure"
  ) {
    return requiredQuantity;
  }

  const unit = readNonEmptyString(
    value,
    "unit",
    "demand.unit",
  );
  if (unit.status === "failure") {
    return unit;
  }

  const priority = readNonEmptyString(
    value,
    "priority",
    "demand.priority",
  );
  if (priority.status === "failure") {
    return priority;
  }

  const requestedDeliveryDateUtc =
    readOptionalIsoDate(
      value,
      "requestedDeliveryDateUtc",
      "demand.requestedDeliveryDateUtc",
    );
  if (
    requestedDeliveryDateUtc.status ===
    "failure"
  ) {
    return requestedDeliveryDateUtc;
  }

  return {
    status: "success",
    value: {
      id: id.value,
      productId: productId.value,
      productName: productName.value,
      requiredQuantity:
        requiredQuantity.value,
      unit: unit.value,
      priority: priority.value,
      orderReference: readOptionalString(
        value,
        "orderReference",
      ),
      customerId: readOptionalString(
        value,
        "customerId",
      ),
      customerName: readOptionalString(
        value,
        "customerName",
      ),
      productCode: readOptionalString(
        value,
        "productCode",
      ),
      requestedDeliveryDateUtc:
        requestedDeliveryDateUtc.value,
    },
  };
}

function parseRiskDto(
  value: Record<string, unknown>,
): ApiValidationResult<ProductionPlanRiskApiDto> {
  const id = readNonEmptyString(
    value,
    "id",
    "risk.id",
  );
  if (id.status === "failure") {
    return id;
  }

  const type = readNonEmptyString(
    value,
    "type",
    "risk.type",
  );
  if (type.status === "failure") {
    return type;
  }

  const severity = readNonEmptyString(
    value,
    "severity",
    "risk.severity",
  );
  if (severity.status === "failure") {
    return severity;
  }

  const title = readNonEmptyString(
    value,
    "title",
    "risk.title",
  );
  if (title.status === "failure") {
    return title;
  }

  const description = readNonEmptyString(
    value,
    "description",
    "risk.description",
  );
  if (description.status === "failure") {
    return description;
  }

  const blocking = readRequiredBoolean(
    value,
    "blocking",
    "risk.blocking",
  );
  if (blocking.status === "failure") {
    return blocking;
  }

  return {
    status: "success",
    value: {
      id: id.value,
      type: type.value,
      severity: severity.value,
      title: title.value,
      description: description.value,
      blocking: blocking.value,
      source: readOptionalString(
        value,
        "source",
      ),
    },
  };
}

function parseResourceAssignmentDto(
  value: unknown,
): ApiValidationResult<ProductionPlanResourceAssignmentApiDto> {
  if (!isRecord(value)) {
    return createValidationFailure(
      "resources must be an object.",
    );
  }

  return {
    status: "success",
    value: {
      machineId: readOptionalString(
        value,
        "machineId",
      ),
      machineName: readOptionalString(
        value,
        "machineName",
      ),
      moldId: readOptionalString(
        value,
        "moldId",
      ),
      moldName: readOptionalString(
        value,
        "moldName",
      ),
      materialId: readOptionalString(
        value,
        "materialId",
      ),
      materialName: readOptionalString(
        value,
        "materialName",
      ),
      operatorGroupId: readOptionalString(
        value,
        "operatorGroupId",
      ),
      operatorGroupName: readOptionalString(
        value,
        "operatorGroupName",
      ),
    },
  };
}

function parseTimeWindowDto(
  value: unknown,
): ApiValidationResult<ProductionPlanTimeWindowApiDto> {
  if (!isRecord(value)) {
    return createValidationFailure(
      "timeWindow must be an object.",
    );
  }

  const plannedStartAtUtc =
    readOptionalIsoDate(
      value,
      "plannedStartAtUtc",
      "timeWindow.plannedStartAtUtc",
    );
  if (
    plannedStartAtUtc.status === "failure"
  ) {
    return plannedStartAtUtc;
  }

  const plannedEndAtUtc = readOptionalIsoDate(
    value,
    "plannedEndAtUtc",
    "timeWindow.plannedEndAtUtc",
  );
  if (plannedEndAtUtc.status === "failure") {
    return plannedEndAtUtc;
  }

  const estimatedDurationMinutes =
    value.estimatedDurationMinutes;

  return {
    status: "success",
    value: {
      plannedStartAtUtc:
        plannedStartAtUtc.value,
      plannedEndAtUtc:
        plannedEndAtUtc.value,
      estimatedDurationMinutes:
        typeof estimatedDurationMinutes ===
          "number" &&
        Number.isFinite(
          estimatedDurationMinutes,
        )
          ? estimatedDurationMinutes
          : undefined,
      shiftId: readOptionalString(
        value,
        "shiftId",
      ),
      shiftName: readOptionalString(
        value,
        "shiftName",
      ),
    },
  };
}

function parseApprovalDto(
  value: unknown,
): ApiValidationResult<ProductionPlanApprovalApiDto> {
  if (!isRecord(value)) {
    return createValidationFailure(
      "approval must be an object.",
    );
  }

  const status = readNonEmptyString(
    value,
    "status",
    "approval.status",
  );
  if (status.status === "failure") {
    return status;
  }

  const requestedAtUtc = readOptionalIsoDate(
    value,
    "requestedAtUtc",
    "approval.requestedAtUtc",
  );
  if (requestedAtUtc.status === "failure") {
    return requestedAtUtc;
  }

  const approvedAtUtc = readOptionalIsoDate(
    value,
    "approvedAtUtc",
    "approval.approvedAtUtc",
  );
  if (approvedAtUtc.status === "failure") {
    return approvedAtUtc;
  }

  const rejectedAtUtc = readOptionalIsoDate(
    value,
    "rejectedAtUtc",
    "approval.rejectedAtUtc",
  );
  if (rejectedAtUtc.status === "failure") {
    return rejectedAtUtc;
  }

  return {
    status: "success",
    value: {
      status: status.value,
      requiredRole: readOptionalString(
        value,
        "requiredRole",
      ),
      requestedAtUtc: requestedAtUtc.value,
      requestedBy: readOptionalString(
        value,
        "requestedBy",
      ),
      approvedAtUtc: approvedAtUtc.value,
      approvedBy: readOptionalString(
        value,
        "approvedBy",
      ),
      rejectedAtUtc: rejectedAtUtc.value,
      rejectedBy: readOptionalString(
        value,
        "rejectedBy",
      ),
      rejectionReason: readOptionalString(
        value,
        "rejectionReason",
      ),
    },
  };
}

function parseTransferDto(
  value: unknown,
): ApiValidationResult<ProductionPlanTransferApiDto> {
  if (!isRecord(value)) {
    return createValidationFailure(
      "transfer must be an object.",
    );
  }

  const status = readNonEmptyString(
    value,
    "status",
    "transfer.status",
  );
  if (status.status === "failure") {
    return status;
  }

  const startedAtUtc = readOptionalIsoDate(
    value,
    "startedAtUtc",
    "transfer.startedAtUtc",
  );
  if (startedAtUtc.status === "failure") {
    return startedAtUtc;
  }

  const completedAtUtc = readOptionalIsoDate(
    value,
    "completedAtUtc",
    "transfer.completedAtUtc",
  );
  if (completedAtUtc.status === "failure") {
    return completedAtUtc;
  }

  return {
    status: "success",
    value: {
      targetSystem: readOptionalString(
        value,
        "targetSystem",
      ),
      status: status.value,
      externalPlanId: readOptionalString(
        value,
        "externalPlanId",
      ),
      startedAtUtc: startedAtUtc.value,
      completedAtUtc: completedAtUtc.value,
      errorCode: readOptionalString(
        value,
        "errorCode",
      ),
      errorMessage: readOptionalString(
        value,
        "errorMessage",
      ),
    },
  };
}

function parseSourceReferenceDto(
  value: Record<string, unknown>,
): ApiValidationResult<ProductionPlanSourceReferenceApiDto> {
  const sourceType = readNonEmptyString(
    value,
    "sourceType",
    "sourceReference.sourceType",
  );
  if (sourceType.status === "failure") {
    return sourceType;
  }

  const sourceId = readNonEmptyString(
    value,
    "sourceId",
    "sourceReference.sourceId",
  );
  if (sourceId.status === "failure") {
    return sourceId;
  }

  const sourceUpdatedAtUtc =
    readOptionalIsoDate(
      value,
      "sourceUpdatedAtUtc",
      "sourceReference.sourceUpdatedAtUtc",
    );
  if (
    sourceUpdatedAtUtc.status === "failure"
  ) {
    return sourceUpdatedAtUtc;
  }

  return {
    status: "success",
    value: {
      sourceType: sourceType.value,
      sourceId: sourceId.value,
      sourceUpdatedAtUtc:
        sourceUpdatedAtUtc.value,
    },
  };
}

function parseHistoryEntryDto(
  value: Record<string, unknown>,
): ApiValidationResult<ProductionPlanHistoryEntryApiDto> {
  const id = readNonEmptyString(
    value,
    "id",
    "history.id",
  );
  if (id.status === "failure") {
    return id;
  }

  const type = readNonEmptyString(
    value,
    "type",
    "history.type",
  );
  if (type.status === "failure") {
    return type;
  }

  const title = readNonEmptyString(
    value,
    "title",
    "history.title",
  );
  if (title.status === "failure") {
    return title;
  }

  const description = readNonEmptyString(
    value,
    "description",
    "history.description",
  );
  if (description.status === "failure") {
    return description;
  }

  const occurredAtUtc = readOptionalIsoDate(
    value,
    "occurredAtUtc",
    "history.occurredAtUtc",
  );
  if (occurredAtUtc.status === "failure") {
    return occurredAtUtc;
  }

  return {
    status: "success",
    value: {
      id: id.value,
      type: type.value,
      title: title.value,
      description: description.value,
      occurredAtUtc: occurredAtUtc.value,
    },
  };
}

function parseDetailDto(
  value: unknown,
): ApiValidationResult<ProductionPlanDetailApiDto> {
  if (!isRecord(value)) {
    return createValidationFailure(
      "Production plan detail must be an object.",
    );
  }

  const core = parsePlanCoreDto(value);
  if (core.status === "failure") {
    return core;
  }

  const createdAtUtc = readRequiredIsoDate(
    value,
    "createdAtUtc",
    "detail.createdAtUtc",
  );
  if (createdAtUtc.status === "failure") {
    return createdAtUtc;
  }

  const version = readRequiredNumber(
    value,
    "version",
    "detail.version",
  );
  if (version.status === "failure") {
    return version;
  }

  const createdBy = readNonEmptyString(
    value,
    "createdBy",
    "detail.createdBy",
  );
  if (createdBy.status === "failure") {
    return createdBy;
  }

  const demandsResult = readRequiredArray(
    value,
    "demands",
    "detail.demands",
  );
  if (demandsResult.status === "failure") {
    return demandsResult;
  }

  const demands: ProductionPlanDemandApiDto[] =
    [];
  for (const demand of demandsResult.value) {
    const parsedDemand =
      parseDemandDto(demand);
    if (parsedDemand.status === "failure") {
      return parsedDemand;
    }
    demands.push(parsedDemand.value);
  }

  const risksResult = readRequiredArray(
    value,
    "risks",
    "detail.risks",
  );
  if (risksResult.status === "failure") {
    return risksResult;
  }

  const risks: ProductionPlanRiskApiDto[] =
    [];
  for (const risk of risksResult.value) {
    const parsedRisk = parseRiskDto(risk);
    if (parsedRisk.status === "failure") {
      return parsedRisk;
    }
    risks.push(parsedRisk.value);
  }

  const resources = parseResourceAssignmentDto(
    value.resources,
  );
  if (resources.status === "failure") {
    return resources;
  }

  const timeWindow = parseTimeWindowDto(
    value.timeWindow,
  );
  if (timeWindow.status === "failure") {
    return timeWindow;
  }

  const approval = parseApprovalDto(
    value.approval,
  );
  if (approval.status === "failure") {
    return approval;
  }

  const transfer = parseTransferDto(
    value.transfer,
  );
  if (transfer.status === "failure") {
    return transfer;
  }

  const sourceReferencesResult =
    readRequiredArray(
      value,
      "sourceReferences",
      "detail.sourceReferences",
    );
  if (
    sourceReferencesResult.status ===
    "failure"
  ) {
    return sourceReferencesResult;
  }

  const sourceReferences: ProductionPlanSourceReferenceApiDto[] =
    [];
  for (const sourceReference of sourceReferencesResult.value) {
    const parsedSourceReference =
      parseSourceReferenceDto(
        sourceReference,
      );
    if (
      parsedSourceReference.status ===
      "failure"
    ) {
      return parsedSourceReference;
    }
    sourceReferences.push(
      parsedSourceReference.value,
    );
  }

  const historyResult = readRequiredArray(
    value,
    "history",
    "detail.history",
  );
  if (historyResult.status === "failure") {
    return historyResult;
  }

  const history: ProductionPlanHistoryEntryApiDto[] =
    [];
  for (const historyEntry of historyResult.value) {
    const parsedHistoryEntry =
      parseHistoryEntryDto(historyEntry);
    if (
      parsedHistoryEntry.status ===
      "failure"
    ) {
      return parsedHistoryEntry;
    }
    history.push(parsedHistoryEntry.value);
  }

  return {
    status: "success",
    value: {
      ...core.value,
      createdAtUtc: createdAtUtc.value,
      version: version.value,
      notes: readOptionalString(
        value,
        "notes",
      ),
      createdBy: createdBy.value,
      aiExplanation: readOptionalString(
        value,
        "aiExplanation",
      ),
      demands,
      risks,
      resources: resources.value,
      timeWindow: timeWindow.value,
      approval: approval.value,
      transfer: transfer.value,
      sourceReferences,
      history,
    },
  };
}

export function parseProductionPlanDetailApiResponse(
  value: unknown,
): ApiValidationResult<ProductionPlanDetailApiResponse> {
  if (!isRecord(value)) {
    return createValidationFailure(
      "Production plan detail response must be an object.",
    );
  }

  const plan = parseDetailDto(value.plan);

  if (plan.status === "failure") {
    return plan;
  }

  return {
    status: "success",
    value: {
      plan: plan.value,
    },
  };
}

export function normalizeProductionPlanListQueryValue(
  value: string | undefined,
): string | undefined {
  const normalizedValue =
    value?.trim() ?? "";

  return normalizedValue.length > 0
    ? normalizedValue
    : undefined;
}

export function normalizeProductionPlanListLimit(
  value: number | undefined,
): number | undefined {
  if (value === undefined) {
    return undefined;
  }

  if (
    !Number.isFinite(value) ||
    value <= 0
  ) {
    return undefined;
  }

  return Math.trunc(value);
}
