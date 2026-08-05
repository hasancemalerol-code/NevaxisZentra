import type {
  OrderDraft,
  OrderDraftStatus,
  OrderIntakeSource,
  OrderMatchConfidence,
  OrderTransferStatus,
} from "../domain/order-draft";

export type OrderDraftFilter = Readonly<{
  search?: string;
  source?: OrderIntakeSource;
  status?: OrderDraftStatus;
  confidenceLevel?: OrderMatchConfidence;
  transferStatus?: OrderTransferStatus;
  hasBlockingIssues?: boolean;
  awaitingApproval?: boolean;
}>;

export type OrderDraftSummary = Readonly<{
  totalCount: number;
  collectingCount: number;
  needsReviewCount: number;
  readyForApprovalCount: number;
  awaitingApprovalCount: number;
  approvedCount: number;
  transferredCount: number;
  transferFailedCount: number;
  blockingIssueCount: number;
  highConfidenceCount: number;
  mediumConfidenceCount: number;
  lowConfidenceCount: number;
}>;

function normalizeSearchValue(
  value: string,
): string {
  return value
    .toLocaleLowerCase("tr-TR")
    .trim();
}

function matchesOrderDraftSearch(
  orderDraft: OrderDraft,
  search: string,
): boolean {
  const normalizedSearch =
    normalizeSearchValue(search);

  if (!normalizedSearch) {
    return true;
  }

  const searchableValues = [
    orderDraft.id,
    orderDraft.title,
    orderDraft.customer.customerId,
    orderDraft.customer.customerCode,
    orderDraft.customer.customerName,
    orderDraft.customer.taxNumber,
    orderDraft.customer.email,
    orderDraft.customer.phone,
    orderDraft.notes,
    ...orderDraft.sources.flatMap((source) => [
      source.externalReferenceId,
      source.senderName,
      source.senderAddress,
      source.subject,
      source.messagePreview,
      ...(source.attachmentNames ?? []),
    ]),
    ...orderDraft.lines.flatMap((line) => [
      line.productId,
      line.productCode,
      line.productName,
      line.variant,
      line.requestedDescription,
    ]),
  ];

  return searchableValues.some(
    (value) =>
      value !== undefined &&
      normalizeSearchValue(value).includes(
        normalizedSearch,
      ),
  );
}

export function orderDraftHasBlockingIssues(
  orderDraft: OrderDraft,
): boolean {
  return orderDraft.issues.some(
    (issue) => issue.blocking,
  );
}

export function filterOrderDrafts(
  orderDrafts: readonly OrderDraft[],
  filter: OrderDraftFilter,
): readonly OrderDraft[] {
  return orderDrafts.filter((orderDraft) => {
    if (
      filter.search &&
      !matchesOrderDraftSearch(
        orderDraft,
        filter.search,
      )
    ) {
      return false;
    }

    if (
      filter.source &&
      !orderDraft.sources.some(
        (source) =>
          source.source === filter.source,
      )
    ) {
      return false;
    }

    if (
      filter.status &&
      orderDraft.status !== filter.status
    ) {
      return false;
    }

    if (
      filter.confidenceLevel &&
      orderDraft.confidenceLevel !==
        filter.confidenceLevel
    ) {
      return false;
    }

    if (
      filter.transferStatus &&
      orderDraft.transfer.status !==
        filter.transferStatus
    ) {
      return false;
    }

    if (
      filter.hasBlockingIssues !== undefined &&
      orderDraftHasBlockingIssues(orderDraft) !==
        filter.hasBlockingIssues
    ) {
      return false;
    }

    if (
      filter.awaitingApproval !== undefined
    ) {
      const isAwaitingApproval =
        orderDraft.approval.status === "pending";

      if (
        isAwaitingApproval !==
        filter.awaitingApproval
      ) {
        return false;
      }
    }

    return true;
  });
}

export function sortOrderDraftsByUpdatedAt(
  orderDrafts: readonly OrderDraft[],
): readonly OrderDraft[] {
  return [...orderDrafts].sort(
    (left, right) =>
      new Date(right.updatedAt).getTime() -
      new Date(left.updatedAt).getTime(),
  );
}

export function calculateOrderDraftSummary(
  orderDrafts: readonly OrderDraft[],
): OrderDraftSummary {
  return orderDrafts.reduce<OrderDraftSummary>(
    (summary, orderDraft) => ({
      totalCount:
        summary.totalCount + 1,

      collectingCount:
        summary.collectingCount +
        Number(orderDraft.status === "collecting"),

      needsReviewCount:
        summary.needsReviewCount +
        Number(
          orderDraft.status === "needs-review",
        ),

      readyForApprovalCount:
        summary.readyForApprovalCount +
        Number(
          orderDraft.status ===
            "ready-for-approval",
        ),

      awaitingApprovalCount:
        summary.awaitingApprovalCount +
        Number(
          orderDraft.approval.status ===
            "pending",
        ),

      approvedCount:
        summary.approvedCount +
        Number(
          orderDraft.status === "approved",
        ),

      transferredCount:
        summary.transferredCount +
        Number(
          orderDraft.status === "transferred" ||
            orderDraft.transfer.status ===
              "completed",
        ),

      transferFailedCount:
        summary.transferFailedCount +
        Number(
          orderDraft.status ===
            "transfer-failed" ||
            orderDraft.transfer.status ===
              "failed",
        ),

      blockingIssueCount:
        summary.blockingIssueCount +
        orderDraft.issues.filter(
          (issue) => issue.blocking,
        ).length,

      highConfidenceCount:
        summary.highConfidenceCount +
        Number(
          orderDraft.confidenceLevel === "high",
        ),

      mediumConfidenceCount:
        summary.mediumConfidenceCount +
        Number(
          orderDraft.confidenceLevel ===
            "medium",
        ),

      lowConfidenceCount:
        summary.lowConfidenceCount +
        Number(
          orderDraft.confidenceLevel === "low",
        ),
    }),
    {
      totalCount: 0,
      collectingCount: 0,
      needsReviewCount: 0,
      readyForApprovalCount: 0,
      awaitingApprovalCount: 0,
      approvedCount: 0,
      transferredCount: 0,
      transferFailedCount: 0,
      blockingIssueCount: 0,
      highConfidenceCount: 0,
      mediumConfidenceCount: 0,
      lowConfidenceCount: 0,
    },
  );
}