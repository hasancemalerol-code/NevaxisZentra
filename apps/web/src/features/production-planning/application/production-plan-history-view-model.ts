import type {
  ProductionPlan,
  ProductionPlanOrigin,
} from "../domain/production-plan";

export type ProductionPlanHistoryItemType =
  | "created"
  | "updated"
  | "approval"
  | "transfer";

export type ProductionPlanHistoryViewItem =
  Readonly<{
    id: string;
    type: ProductionPlanHistoryItemType;
    title: string;
    description: string;
    occurredAt: string | null;
  }>;

export type ProductionPlanHistoryViewModel =
  Readonly<{
    sourceType: ProductionPlanOrigin;
    sourceReference: string | null;
    createdAt: string | null;
    updatedAt: string | null;
    isAiGenerated: boolean;
    items: readonly ProductionPlanHistoryViewItem[];
    hasHistory: boolean;
  }>;

function normalizeOptionalText(
  value: string | null | undefined,
): string | null {
  const normalizedValue = value?.trim();

  return normalizedValue || null;
}

function createHistoryItem(
  id: string,
  type: ProductionPlanHistoryItemType,
  title: string,
  description: string,
  occurredAt: string | null | undefined,
): ProductionPlanHistoryViewItem {
  return {
    id,
    type,
    title,
    description,
    occurredAt: normalizeOptionalText(
      occurredAt,
    ),
  };
}

function getApprovalOccurredAt(
  productionPlan: ProductionPlan,
): string | null {
  return (
    normalizeOptionalText(
      productionPlan.approval.approvedAt,
    ) ??
    normalizeOptionalText(
      productionPlan.approval.rejectedAt,
    ) ??
    normalizeOptionalText(
      productionPlan.approval.requestedAt,
    )
  );
}

function getTransferOccurredAt(
  productionPlan: ProductionPlan,
): string | null {
  return (
    normalizeOptionalText(
      productionPlan.transfer.completedAt,
    ) ??
    normalizeOptionalText(
      productionPlan.transfer.startedAt,
    )
  );
}

export function createProductionPlanHistoryViewModel(
  productionPlan: ProductionPlan,
): ProductionPlanHistoryViewModel {
  const items: ProductionPlanHistoryViewItem[] =
    [];

  const createdAt = normalizeOptionalText(
    productionPlan.createdAt,
  );

  const updatedAt = normalizeOptionalText(
    productionPlan.updatedAt,
  );

  const sourceReference =
    productionPlan.sourceReferences[0] ??
    null;

  if (createdAt) {
    items.push(
      createHistoryItem(
        `${productionPlan.id}-created`,
        "created",
        "Üretim planı oluşturuldu",
        "Plan ilk kez çalışma alanına kaydedildi.",
        createdAt,
      ),
    );
  }

  if (
    updatedAt &&
    updatedAt !== createdAt
  ) {
    items.push(
      createHistoryItem(
        `${productionPlan.id}-updated`,
        "updated",
        "Üretim planı güncellendi",
        "Plan kapsamı veya kaynak bilgileri güncellendi.",
        updatedAt,
      ),
    );
  }

  const approvalOccurredAt =
    getApprovalOccurredAt(productionPlan);

  if (approvalOccurredAt) {
    items.push(
      createHistoryItem(
        `${productionPlan.id}-approval`,
        "approval",
        "Onay süreci güncellendi",
        `Güncel onay durumu: ${productionPlan.approval.status}.`,
        approvalOccurredAt,
      ),
    );
  }

  const transferOccurredAt =
    getTransferOccurredAt(productionPlan);

  if (transferOccurredAt) {
    items.push(
      createHistoryItem(
        `${productionPlan.id}-transfer`,
        "transfer",
        "Aktarım süreci güncellendi",
        `Güncel aktarım durumu: ${productionPlan.transfer.status}.`,
        transferOccurredAt,
      ),
    );
  }

  const sortedItems = [...items].sort(
    (left, right) => {
      const leftTime = left.occurredAt
        ? new Date(left.occurredAt).getTime()
        : 0;

      const rightTime = right.occurredAt
        ? new Date(right.occurredAt).getTime()
        : 0;

      return rightTime - leftTime;
    },
  );

  return {
    sourceType: productionPlan.origin,
    sourceReference: sourceReference
      ? `${sourceReference.sourceType}: ${sourceReference.sourceId}`
      : null,
    createdAt,
    updatedAt,
    isAiGenerated:
      productionPlan.aiGenerated,
    items: sortedItems,
    hasHistory: sortedItems.length > 0,
  };
}