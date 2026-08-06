import type {
  ProductionPlan,
  ProductionPlanConfidence,
  ProductionPlanOrigin,
  ProductionPlanPriority,
  ProductionPlanStatus,
  ProductionPlanTargetSystem,
} from "../domain/production-plan";
import {
  hasBlockingProductionPlanRisks,
  resolveProductionPlanHighestPriority,
} from "../domain/production-plan";
import type {
  ProductionPlanRepository,
  ProductionPlanRepositoryListResult,
  ProductionPlanRepositoryScope,
} from "./production-plan-repository";

export type ProductionPlanFilter =
  Readonly<{
    search?: string;
    status?: ProductionPlanStatus;
    origin?: ProductionPlanOrigin;
    priority?: ProductionPlanPriority;
    confidence?: ProductionPlanConfidence;
    targetSystem?: ProductionPlanTargetSystem;
    facilityId?: string;
    blockingOnly?: boolean;
    approvalPendingOnly?: boolean;
    transferableOnly?: boolean;
  }>;

export type ProductionPlanSummary =
  Readonly<{
    total: number;
    needsReview: number;
    waitingForApproval: number;
    approved: number;
    scheduled: number;
    inProgress: number;
    blockingRisk: number;
    transferable: number;
    urgent: number;
    aiGenerated: number;
  }>;

function normalizeSearchValue(
  value: string | undefined,
): string {
  return value
    ?.trim()
    .toLocaleLowerCase("tr-TR") ?? "";
}

function getProductionPlanSearchText(
  productionPlan: ProductionPlan,
): string {
  const demandText = productionPlan.demands
    .flatMap((demand) => [
      demand.orderReference,
      demand.customerName,
      demand.productCode,
      demand.productName,
    ])
    .filter(Boolean)
    .join(" ");

  const listSummaryText = [
    productionPlan.listSummary
      ?.primaryProductCode,
    productionPlan.listSummary
      ?.primaryProductName,
  ]
    .filter(Boolean)
    .join(" ");

  const resourceText = [
    productionPlan.resourceAssignment.machineName,
    productionPlan.resourceAssignment.moldName,
    productionPlan.resourceAssignment.materialName,
    productionPlan.resourceAssignment.operatorGroupName,
  ]
    .filter(Boolean)
    .join(" ");

  return normalizeSearchValue(
    [
      productionPlan.id,
      productionPlan.title,
      productionPlan.notes,
      productionPlan.aiExplanation,
      demandText,
      listSummaryText,
      resourceText,
    ]
      .filter(Boolean)
      .join(" "),
  );
}

function getHighestProductionPlanPriority(
  productionPlan: ProductionPlan,
): ProductionPlanPriority {
  return resolveProductionPlanHighestPriority(
    productionPlan,
  );
}

function hasBlockingRisk(
  productionPlan: ProductionPlan,
): boolean {
  return hasBlockingProductionPlanRisks(
    productionPlan,
  );
}

function isWaitingForApproval(
  productionPlan: ProductionPlan,
): boolean {
  return (
    productionPlan.status ===
      "ready-for-approval" &&
    productionPlan.approval.status ===
      "pending"
  );
}

function isTransferable(
  productionPlan: ProductionPlan,
): boolean {
  return (
    productionPlan.status === "approved" &&
    productionPlan.approval.status ===
      "approved" &&
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
    ) &&
    !hasBlockingRisk(productionPlan)
  );
}

export function matchesProductionPlanFilter(
  productionPlan: ProductionPlan,
  filter: ProductionPlanFilter,
): boolean {
  const search = normalizeSearchValue(
    filter.search,
  );

  if (
    search &&
    !getProductionPlanSearchText(
      productionPlan,
    ).includes(search)
  ) {
    return false;
  }

  if (
    filter.status &&
    productionPlan.status !== filter.status
  ) {
    return false;
  }

  if (
    filter.origin &&
    productionPlan.origin !== filter.origin
  ) {
    return false;
  }

  if (
    filter.priority &&
    getHighestProductionPlanPriority(
      productionPlan,
    ) !== filter.priority
  ) {
    return false;
  }

  if (
    filter.confidence &&
    productionPlan.confidence !==
      filter.confidence
  ) {
    return false;
  }

  if (
    filter.targetSystem &&
    productionPlan.transfer.targetSystem !==
      filter.targetSystem
  ) {
    return false;
  }

  if (
    filter.facilityId &&
    productionPlan.facilityId !==
      filter.facilityId
  ) {
    return false;
  }

  if (
    filter.blockingOnly &&
    !hasBlockingRisk(productionPlan)
  ) {
    return false;
  }

  if (
    filter.approvalPendingOnly &&
    !isWaitingForApproval(productionPlan)
  ) {
    return false;
  }

  if (
    filter.transferableOnly &&
    !isTransferable(productionPlan)
  ) {
    return false;
  }

  return true;
}

export function filterProductionPlans(
  productionPlans:
    readonly ProductionPlan[],
  filter: ProductionPlanFilter,
): readonly ProductionPlan[] {
  return productionPlans.filter(
    (productionPlan) =>
      matchesProductionPlanFilter(
        productionPlan,
        filter,
      ),
  );
}

export function sortProductionPlans(
  productionPlans:
    readonly ProductionPlan[],
): readonly ProductionPlan[] {
  const statusWeight = {
    "needs-review": 0,
    "ready-for-approval": 1,
    approved: 2,
    scheduled: 3,
    "in-progress": 4,
    draft: 5,
    rejected: 6,
    completed: 7,
    cancelled: 8,
  } satisfies Record<
    ProductionPlanStatus,
    number
  >;

  return [...productionPlans].sort(
    (left, right) => {
      const leftBlocking =
        hasBlockingRisk(left);
      const rightBlocking =
        hasBlockingRisk(right);

      if (leftBlocking !== rightBlocking) {
        return leftBlocking ? -1 : 1;
      }

      const statusDifference =
        statusWeight[left.status] -
        statusWeight[right.status];

      if (statusDifference !== 0) {
        return statusDifference;
      }

      return (
        new Date(right.updatedAt).getTime() -
        new Date(left.updatedAt).getTime()
      );
    },
  );
}

export function queryProductionPlans(
  productionPlans:
    readonly ProductionPlan[],
  filter: ProductionPlanFilter = {},
): readonly ProductionPlan[] {
  return sortProductionPlans(
    filterProductionPlans(
      productionPlans,
      filter,
    ),
  );
}

export function calculateProductionPlanSummary(
  productionPlans:
    readonly ProductionPlan[],
): ProductionPlanSummary {
  return {
    total: productionPlans.length,

    needsReview:
      productionPlans.filter(
        (productionPlan) =>
          productionPlan.status ===
          "needs-review",
      ).length,

    waitingForApproval:
      productionPlans.filter(
        isWaitingForApproval,
      ).length,

    approved:
      productionPlans.filter(
        (productionPlan) =>
          productionPlan.status ===
          "approved",
      ).length,

    scheduled:
      productionPlans.filter(
        (productionPlan) =>
          productionPlan.status ===
          "scheduled",
      ).length,

    inProgress:
      productionPlans.filter(
        (productionPlan) =>
          productionPlan.status ===
          "in-progress",
      ).length,

    blockingRisk:
      productionPlans.filter(
        hasBlockingRisk,
      ).length,

    transferable:
      productionPlans.filter(
        isTransferable,
      ).length,

    urgent:
      productionPlans.filter(
        (productionPlan) =>
          getHighestProductionPlanPriority(
            productionPlan,
          ) === "urgent",
      ).length,

    aiGenerated:
      productionPlans.filter(
        (productionPlan) =>
          productionPlan.aiGenerated,
      ).length,
  };
}

export async function readProductionPlans(
  repository: ProductionPlanRepository,
  scope: ProductionPlanRepositoryScope,
): Promise<ProductionPlanRepositoryListResult> {
  return repository.list({
    scope,
  });
}