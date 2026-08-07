import "./production-plan-server-boundary";

import type {
  ProductionPlan,
  ProductionPlanApproval,
  ProductionPlanApprovalStatus,
  ProductionPlanConfidence,
  ProductionPlanDemand,
  ProductionPlanOrigin,
  ProductionPlanPriority,
  ProductionPlanResourceAssignment,
  ProductionPlanRisk,
  ProductionPlanRiskSeverity,
  ProductionPlanRiskType,
  ProductionPlanSourceReference,
  ProductionPlanStatus,
  ProductionPlanTargetSystem,
  ProductionPlanTimeWindow,
  ProductionPlanTransfer,
} from "../domain/production-plan";
import type {
  ProductionPlanDetailApiDto,
  ProductionPlanCoreApiDto,
  ProductionPlanSummaryApiDto,
  ApiValidationFailure,
  ApiValidationResult,
} from "./production-plan-api-contracts";

const PRODUCTION_PLAN_STATUSES =
  new Set<ProductionPlanStatus>([
    "draft",
    "needs-review",
    "ready-for-approval",
    "approved",
    "rejected",
    "scheduled",
    "in-progress",
    "completed",
    "cancelled",
  ]);

const PRODUCTION_PLAN_ORIGINS =
  new Set<ProductionPlanOrigin>([
    "approved-order",
    "erp-production-request",
    "manual",
    "ai-recommendation",
  ]);

const PRODUCTION_PLAN_PRIORITIES =
  new Set<ProductionPlanPriority>([
    "low",
    "normal",
    "high",
    "urgent",
  ]);

const PRODUCTION_PLAN_CONFIDENCES =
  new Set<ProductionPlanConfidence>([
    "low",
    "medium",
    "high",
  ]);

const PRODUCTION_PLAN_RISK_TYPES =
  new Set<ProductionPlanRiskType>([
    "capacity-shortage",
    "machine-conflict",
    "mold-conflict",
    "material-shortage",
    "operator-shortage",
    "delivery-risk",
    "quality-risk",
    "maintenance-risk",
    "data-missing",
    "data-conflict",
  ]);

const PRODUCTION_PLAN_RISK_SEVERITIES =
  new Set<ProductionPlanRiskSeverity>([
    "information",
    "warning",
    "critical",
  ]);

const PRODUCTION_PLAN_APPROVAL_STATUSES =
  new Set<ProductionPlanApprovalStatus>([
    "not-required",
    "pending",
    "approved",
    "rejected",
    "expired",
  ]);

const PRODUCTION_PLAN_TRANSFER_STATUSES =
  new Set<ProductionPlanTransfer["status"]>([
    "not-started",
    "pending",
    "in-progress",
    "completed",
    "failed",
    "uncertain",
  ]);

const PRODUCTION_PLAN_TARGET_SYSTEMS =
  new Set<ProductionPlanTargetSystem>([
    "erp",
    "mes",
    "crevafactos",
  ]);

const PRODUCTION_PLAN_SOURCE_TYPES =
  new Set<
    ProductionPlanSourceReference["sourceType"]
  >([
    "order-draft",
    "erp-order",
    "customer-order",
    "manual-request",
  ]);

function createValidationFailure(
  message: string,
): ApiValidationFailure {
  return {
    status: "failure",
    code: "invalid-response",
    message,
  };
}

function parseIsoDate(
  value: string,
  label: string,
): ApiValidationResult<string> {
  if (!value.trim()) {
    return createValidationFailure(
      `${label} must be a non-empty ISO date.`,
    );
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

function parseEnumValue<T extends string>(
  value: string,
  allowedValues: ReadonlySet<T>,
  label: string,
): ApiValidationResult<T> {
  if (!allowedValues.has(value as T)) {
    return createValidationFailure(
      `${label} has an invalid value.`,
    );
  }

  return {
    status: "success",
    value: value as T,
  };
}

function mapApprovalFromSummary(
  approvalStatus: string,
): ApiValidationResult<ProductionPlanApproval> {
  const status = parseEnumValue(
    approvalStatus,
    PRODUCTION_PLAN_APPROVAL_STATUSES,
    "approval.status",
  );

  if (status.status === "failure") {
    return status;
  }

  return {
    status: "success",
    value: {
      status: status.value,
    },
  };
}

function mapTransferFromSummary(
  transferStatus: string,
  transferTargetSystem: string | undefined,
): ApiValidationResult<ProductionPlanTransfer> {
  const status = parseEnumValue(
    transferStatus,
    PRODUCTION_PLAN_TRANSFER_STATUSES,
    "transfer.status",
  );

  if (status.status === "failure") {
    return status;
  }

  let targetSystem:
    | ProductionPlanTargetSystem
    | undefined;

  if (transferTargetSystem) {
    const parsedTargetSystem = parseEnumValue(
      transferTargetSystem,
      PRODUCTION_PLAN_TARGET_SYSTEMS,
      "transfer.targetSystem",
    );

    if (
      parsedTargetSystem.status === "failure"
    ) {
      return parsedTargetSystem;
    }

    targetSystem = parsedTargetSystem.value;
  }

  return {
    status: "success",
    value: {
      status: status.value,
      targetSystem,
    },
  };
}

function mapSummaryCoreFields(
  summary: ProductionPlanCoreApiDto,
  timeWindowShiftName?: string,
): ApiValidationResult<
  Pick<
    ProductionPlan,
    | "id"
    | "referenceCode"
    | "facilityId"
    | "title"
    | "status"
    | "origin"
    | "confidence"
    | "aiGenerated"
    | "updatedAt"
    | "timeWindow"
    | "approval"
    | "transfer"
  >
> {
  const status = parseEnumValue(
    summary.status,
    PRODUCTION_PLAN_STATUSES,
    "summary.status",
  );
  if (status.status === "failure") {
    return status;
  }

  const origin = parseEnumValue(
    summary.origin,
    PRODUCTION_PLAN_ORIGINS,
    "summary.origin",
  );
  if (origin.status === "failure") {
    return origin;
  }

  const confidence = parseEnumValue(
    summary.confidence,
    PRODUCTION_PLAN_CONFIDENCES,
    "summary.confidence",
  );
  if (confidence.status === "failure") {
    return confidence;
  }

  const approval = mapApprovalFromSummary(
    summary.approvalStatus,
  );
  if (approval.status === "failure") {
    return approval;
  }

  const transfer = mapTransferFromSummary(
    summary.transferStatus,
    summary.transferTargetSystem,
  );
  if (transfer.status === "failure") {
    return transfer;
  }

  const timeWindow: ProductionPlanTimeWindow =
    {
      plannedStartAt:
        summary.plannedStartAtUtc,
      plannedEndAt:
        summary.plannedEndAtUtc,
      shiftName: timeWindowShiftName,
    };

  return {
    status: "success",
    value: {
      id: summary.id.toLowerCase(),
      referenceCode: summary.referenceCode,
      facilityId: summary.facilityId.toLowerCase(),
      title: summary.title,
      status: status.value,
      origin: origin.value,
      confidence: confidence.value,
      aiGenerated: summary.aiGenerated,
      updatedAt: summary.updatedAtUtc,
      timeWindow,
      approval: approval.value,
      transfer: transfer.value,
    },
  };
}

export function mapProductionPlanSummaryApiDtoToDomain(
  summary: ProductionPlanSummaryApiDto,
): ApiValidationResult<ProductionPlan> {
  const coreFields = mapSummaryCoreFields(
    summary,
    summary.shiftName,
  );

  if (coreFields.status === "failure") {
    return coreFields;
  }

  let highestDemandPriority:
    | ProductionPlanPriority
    | undefined;

  if (summary.highestDemandPriority) {
    const parsedPriority = parseEnumValue(
      summary.highestDemandPriority,
      PRODUCTION_PLAN_PRIORITIES,
      "summary.highestDemandPriority",
    );

    if (parsedPriority.status === "failure") {
      return parsedPriority;
    }

    highestDemandPriority =
      parsedPriority.value;
  }

  const resourceAssignment: ProductionPlanResourceAssignment =
    {
      machineName: summary.hasMachineAssignment
        ? "Atandı"
        : undefined,
      moldName: summary.hasMoldAssignment
        ? "Atandı"
        : undefined,
      materialName: summary.hasMaterialAssignment
        ? "Atandı"
        : undefined,
      operatorGroupName:
        summary.hasOperatorGroupAssignment
          ? "Atandı"
          : undefined,
    };

  return {
    status: "success",
    value: {
      ...coreFields.value,
      tenantId: "",
      version: 0,
      createdAt: summary.updatedAtUtc,
      createdBy: "",
      sourceReferences: [],
      demands: [],
      resourceAssignment,
      risks: [],
      notes: undefined,
      aiExplanation: undefined,
      listSummary: {
        demandCount: summary.demandCount,
        primaryRequiredQuantity:
          summary.primaryRequiredQuantity,
        primaryUnit: summary.primaryUnit,
        primaryProductCode:
          summary.primaryProductCode,
        primaryProductName:
          summary.primaryProductName,
        highestDemandPriority,
        riskCount: summary.riskCount,
        criticalRiskCount:
          summary.criticalRiskCount,
        blockingRiskCount:
          summary.blockingRiskCount,
        hasBlockingRisk:
          summary.hasBlockingRisk,
        hasMachineAssignment:
          summary.hasMachineAssignment,
        hasMoldAssignment:
          summary.hasMoldAssignment,
        hasMaterialAssignment:
          summary.hasMaterialAssignment,
        hasOperatorGroupAssignment:
          summary.hasOperatorGroupAssignment,
        shiftName: summary.shiftName,
      },
    },
  };
}

function mapDemand(
  demand: ProductionPlanDetailApiDto["demands"][number],
): ApiValidationResult<ProductionPlanDemand> {
  const priority = parseEnumValue(
    demand.priority,
    PRODUCTION_PLAN_PRIORITIES,
    "demand.priority",
  );

  if (priority.status === "failure") {
    return priority;
  }

  return {
    status: "success",
    value: {
      id: demand.id,
      orderReference: demand.orderReference,
      customerId: demand.customerId,
      customerName: demand.customerName,
      productId: demand.productId,
      productCode: demand.productCode,
      productName: demand.productName,
      requiredQuantity: demand.requiredQuantity,
      unit: demand.unit,
      requestedDeliveryDate:
        demand.requestedDeliveryDateUtc,
      priority: priority.value,
    },
  };
}

function mapRisk(
  risk: ProductionPlanDetailApiDto["risks"][number],
): ApiValidationResult<ProductionPlanRisk> {
  const type = parseEnumValue(
    risk.type,
    PRODUCTION_PLAN_RISK_TYPES,
    "risk.type",
  );
  if (type.status === "failure") {
    return type;
  }

  const severity = parseEnumValue(
    risk.severity,
    PRODUCTION_PLAN_RISK_SEVERITIES,
    "risk.severity",
  );
  if (severity.status === "failure") {
    return severity;
  }

  return {
    status: "success",
    value: {
      id: risk.id,
      type: type.value,
      severity: severity.value,
      title: risk.title,
      description: risk.description,
      blocking: risk.blocking,
      source: risk.source,
    },
  };
}

function mapResources(
  resources: ProductionPlanDetailApiDto["resources"],
): ProductionPlanResourceAssignment {
  return {
    machineId: resources.machineId,
    machineName: resources.machineName,
    moldId: resources.moldId,
    moldName: resources.moldName,
    materialId: resources.materialId,
    materialName: resources.materialName,
    operatorGroupId:
      resources.operatorGroupId,
    operatorGroupName:
      resources.operatorGroupName,
  };
}

function mapTimeWindow(
  timeWindow: ProductionPlanDetailApiDto["timeWindow"],
): ProductionPlanTimeWindow {
  return {
    plannedStartAt:
      timeWindow.plannedStartAtUtc,
    plannedEndAt:
      timeWindow.plannedEndAtUtc,
    estimatedDurationMinutes:
      timeWindow.estimatedDurationMinutes,
    shiftId: timeWindow.shiftId,
    shiftName: timeWindow.shiftName,
  };
}

function mapApproval(
  approval: ProductionPlanDetailApiDto["approval"],
): ApiValidationResult<ProductionPlanApproval> {
  const status = parseEnumValue(
    approval.status,
    PRODUCTION_PLAN_APPROVAL_STATUSES,
    "approval.status",
  );

  if (status.status === "failure") {
    return status;
  }

  return {
    status: "success",
    value: {
      status: status.value,
      requiredRole: approval.requiredRole,
      requestedAt: approval.requestedAtUtc,
      requestedBy: approval.requestedBy,
      approvedAt: approval.approvedAtUtc,
      approvedBy: approval.approvedBy,
      rejectedAt: approval.rejectedAtUtc,
      rejectedBy: approval.rejectedBy,
      rejectionReason:
        approval.rejectionReason,
    },
  };
}

function mapTransfer(
  transfer: ProductionPlanDetailApiDto["transfer"],
): ApiValidationResult<ProductionPlanTransfer> {
  const status = parseEnumValue(
    transfer.status,
    PRODUCTION_PLAN_TRANSFER_STATUSES,
    "transfer.status",
  );

  if (status.status === "failure") {
    return status;
  }

  let targetSystem:
    | ProductionPlanTargetSystem
    | undefined;

  if (transfer.targetSystem) {
    const parsedTargetSystem = parseEnumValue(
      transfer.targetSystem,
      PRODUCTION_PLAN_TARGET_SYSTEMS,
      "transfer.targetSystem",
    );

    if (
      parsedTargetSystem.status === "failure"
    ) {
      return parsedTargetSystem;
    }

    targetSystem = parsedTargetSystem.value;
  }

  return {
    status: "success",
    value: {
      targetSystem,
      status: status.value,
      externalPlanId: transfer.externalPlanId,
      startedAt: transfer.startedAtUtc,
      completedAt: transfer.completedAtUtc,
      errorCode: transfer.errorCode,
      errorMessage: transfer.errorMessage,
    },
  };
}

function mapSourceReference(
  sourceReference: ProductionPlanDetailApiDto["sourceReferences"][number],
): ApiValidationResult<ProductionPlanSourceReference> {
  const sourceType = parseEnumValue(
    sourceReference.sourceType,
    PRODUCTION_PLAN_SOURCE_TYPES,
    "sourceReference.sourceType",
  );

  if (sourceType.status === "failure") {
    return sourceType;
  }

  return {
    status: "success",
    value: {
      sourceType: sourceType.value,
      sourceId: sourceReference.sourceId,
      sourceUpdatedAt:
        sourceReference.sourceUpdatedAtUtc,
    },
  };
}

export function mapProductionPlanDetailApiDtoToDomain(
  detail: ProductionPlanDetailApiDto,
): ApiValidationResult<ProductionPlan> {
  const coreFields = mapSummaryCoreFields(
    detail,
    detail.timeWindow.shiftName,
  );

  if (coreFields.status === "failure") {
    return coreFields;
  }

  const createdAt = parseIsoDate(
    detail.createdAtUtc,
    "detail.createdAtUtc",
  );
  if (createdAt.status === "failure") {
    return createdAt;
  }

  const demands: ProductionPlanDemand[] = [];

  for (const demand of detail.demands) {
    const mappedDemand = mapDemand(demand);

    if (mappedDemand.status === "failure") {
      return mappedDemand;
    }

    demands.push(mappedDemand.value);
  }

  const risks: ProductionPlanRisk[] = [];

  for (const risk of detail.risks) {
    const mappedRisk = mapRisk(risk);

    if (mappedRisk.status === "failure") {
      return mappedRisk;
    }

    risks.push(mappedRisk.value);
  }

  const approval = mapApproval(
    detail.approval,
  );
  if (approval.status === "failure") {
    return approval;
  }

  const transfer = mapTransfer(
    detail.transfer,
  );
  if (transfer.status === "failure") {
    return transfer;
  }

  const sourceReferences: ProductionPlanSourceReference[] =
    [];

  for (const sourceReference of detail.sourceReferences) {
    const mappedSourceReference =
      mapSourceReference(sourceReference);

    if (
      mappedSourceReference.status ===
      "failure"
    ) {
      return mappedSourceReference;
    }

    sourceReferences.push(
      mappedSourceReference.value,
    );
  }

  return {
    status: "success",
    value: {
      ...coreFields.value,
      tenantId: "",
      version: detail.version,
      createdAt: createdAt.value,
      createdBy: detail.createdBy,
      sourceReferences,
      demands,
      resourceAssignment: mapResources(
        detail.resources,
      ),
      timeWindow: mapTimeWindow(
        detail.timeWindow,
      ),
      risks,
      approval: approval.value,
      transfer: transfer.value,
      aiExplanation: detail.aiExplanation,
      notes: detail.notes,
    },
  };
}
