export type ProductionPlanStatus =
  | "draft"
  | "needs-review"
  | "ready-for-approval"
  | "approved"
  | "rejected"
  | "scheduled"
  | "in-progress"
  | "completed"
  | "cancelled";

export type ProductionPlanOrigin =
  | "approved-order"
  | "erp-production-request"
  | "manual"
  | "ai-recommendation";

export type ProductionPlanPriority =
  | "low"
  | "normal"
  | "high"
  | "urgent";

export type ProductionPlanConfidence =
  | "low"
  | "medium"
  | "high";

export type ProductionPlanRiskType =
  | "capacity-shortage"
  | "machine-conflict"
  | "mold-conflict"
  | "material-shortage"
  | "operator-shortage"
  | "delivery-risk"
  | "quality-risk"
  | "maintenance-risk"
  | "data-missing"
  | "data-conflict";

export type ProductionPlanRiskSeverity =
  | "information"
  | "warning"
  | "critical";

export type ProductionPlanApprovalStatus =
  | "not-required"
  | "pending"
  | "approved"
  | "rejected"
  | "expired";

export type ProductionPlanTargetSystem =
  | "erp"
  | "mes"
  | "crevafactos";

export type ProductionPlanSourceReference =
  Readonly<{
    sourceType:
      | "order-draft"
      | "erp-order"
      | "customer-order"
      | "manual-request";
    sourceId: string;
    sourceUpdatedAt?: string;
  }>;

export type ProductionPlanDemand =
  Readonly<{
    id: string;
    orderReference?: string;
    customerId?: string;
    customerName?: string;
    productId: string;
    productCode?: string;
    productName: string;
    requiredQuantity: number;
    unit: string;
    requestedDeliveryDate?: string;
    priority: ProductionPlanPriority;
  }>;

export type ProductionPlanResourceAssignment =
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

export type ProductionPlanTimeWindow =
  Readonly<{
    plannedStartAt?: string;
    plannedEndAt?: string;
    estimatedDurationMinutes?: number;
    shiftId?: string;
    shiftName?: string;
  }>;

export type ProductionPlanRisk =
  Readonly<{
    id: string;
    type: ProductionPlanRiskType;
    severity: ProductionPlanRiskSeverity;
    title: string;
    description: string;
    blocking: boolean;
    source?: string;
  }>;

export type ProductionPlanApproval =
  Readonly<{
    status: ProductionPlanApprovalStatus;
    requiredRole?: string;
    requestedAt?: string;
    requestedBy?: string;
    approvedAt?: string;
    approvedBy?: string;
    rejectedAt?: string;
    rejectedBy?: string;
    rejectionReason?: string;
  }>;

export type ProductionPlanTransfer =
  Readonly<{
    targetSystem?: ProductionPlanTargetSystem;
    status:
      | "not-started"
      | "pending"
      | "in-progress"
      | "completed"
      | "failed";
    externalPlanId?: string;
    startedAt?: string;
    completedAt?: string;
    errorCode?: string;
    errorMessage?: string;
  }>;

export type ProductionPlanListSummary =
  Readonly<{
    demandCount: number;
    primaryRequiredQuantity?: number;
    primaryUnit?: string;
    primaryProductCode?: string;
    primaryProductName?: string;
    highestDemandPriority?: ProductionPlanPriority;
    riskCount: number;
    criticalRiskCount: number;
    blockingRiskCount: number;
    hasBlockingRisk: boolean;
    hasMachineAssignment: boolean;
    hasMoldAssignment: boolean;
    hasMaterialAssignment: boolean;
    hasOperatorGroupAssignment: boolean;
    shiftName?: string;
  }>;

export type ProductionPlan =
  Readonly<{
    id: string;
    tenantId: string;
    facilityId?: string;
    title: string;
    status: ProductionPlanStatus;
    origin: ProductionPlanOrigin;
    confidence: ProductionPlanConfidence;
    createdAt: string;
    updatedAt: string;
    createdBy: string;
    sourceReferences:
      readonly ProductionPlanSourceReference[];
    demands: readonly ProductionPlanDemand[];
    resourceAssignment:
      ProductionPlanResourceAssignment;
    timeWindow: ProductionPlanTimeWindow;
    risks: readonly ProductionPlanRisk[];
    approval: ProductionPlanApproval;
    transfer: ProductionPlanTransfer;
    aiGenerated: boolean;
    aiExplanation?: string;
    notes?: string;
    listSummary?: ProductionPlanListSummary;
  }>;

export function hasBlockingProductionPlanRisks(
  productionPlan: ProductionPlan,
): boolean {
  if (
    productionPlan.risks.some(
      (risk) => risk.blocking,
    )
  ) {
    return true;
  }

  return (
    productionPlan.listSummary
      ?.hasBlockingRisk ?? false
  );
}

export function resolveProductionPlanHighestPriority(
  productionPlan: ProductionPlan,
): ProductionPlanPriority {
  const priorityWeight = {
    low: 0,
    normal: 1,
    high: 2,
    urgent: 3,
  } satisfies Record<
    ProductionPlanPriority,
    number
  >;

  if (productionPlan.demands.length > 0) {
    return productionPlan.demands.reduce(
      (highestPriority, demand) =>
        priorityWeight[demand.priority] >
        priorityWeight[highestPriority]
          ? demand.priority
          : highestPriority,
      "low" as ProductionPlanPriority,
    );
  }

  return (
    productionPlan.listSummary
      ?.highestDemandPriority ?? "low"
  );
}

export function hasProductionPlanDemand(
  productionPlan: ProductionPlan,
): boolean {
  return productionPlan.demands.some(
    (demand) =>
      demand.requiredQuantity > 0 &&
      demand.productId.trim().length > 0,
  );
}

export function hasProductionPlanResourceAssignment(
  productionPlan: ProductionPlan,
): boolean {
  return Boolean(
    productionPlan.resourceAssignment.machineId &&
      productionPlan.resourceAssignment.moldId &&
      productionPlan.resourceAssignment.materialId,
  );
}

export function hasProductionPlanTimeWindow(
  productionPlan: ProductionPlan,
): boolean {
  return Boolean(
    productionPlan.timeWindow.plannedStartAt &&
      productionPlan.timeWindow.plannedEndAt,
  );
}

export function canSubmitProductionPlanForApproval(
  productionPlan: ProductionPlan,
): boolean {
  return (
    productionPlan.status === "needs-review" &&
    hasProductionPlanDemand(productionPlan) &&
    hasProductionPlanResourceAssignment(
      productionPlan,
    ) &&
    hasProductionPlanTimeWindow(
      productionPlan,
    ) &&
    !hasBlockingProductionPlanRisks(
      productionPlan,
    )
  );
}

export function canApproveProductionPlan(
  productionPlan: ProductionPlan,
): boolean {
  return (
    productionPlan.status ===
      "ready-for-approval" &&
    productionPlan.approval.status ===
      "pending" &&
    !hasBlockingProductionPlanRisks(
      productionPlan,
    )
  );
}

export function canScheduleProductionPlan(
  productionPlan: ProductionPlan,
): boolean {
  return (
    productionPlan.status === "approved" &&
    productionPlan.approval.status ===
      "approved" &&
    hasProductionPlanResourceAssignment(
      productionPlan,
    ) &&
    hasProductionPlanTimeWindow(
      productionPlan,
    ) &&
    !hasBlockingProductionPlanRisks(
      productionPlan,
    )
  );
}

export function canTransferProductionPlan(
  productionPlan: ProductionPlan,
): boolean {
  return (
    canScheduleProductionPlan(productionPlan) &&
    Boolean(
      productionPlan.transfer.targetSystem,
    ) &&
    (
      productionPlan.transfer.status ===
        "not-started" ||
      productionPlan.transfer.status ===
        "pending" ||
      productionPlan.transfer.status ===
        "failed"
    )
  );
}

export function isProductionPlanAiDraft(
  productionPlan: ProductionPlan,
): boolean {
  return (
    productionPlan.aiGenerated &&
    productionPlan.status === "draft"
  );
}