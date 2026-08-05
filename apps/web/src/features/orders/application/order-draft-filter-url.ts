import type { OrderDraftFilter } from "./order-draft-query";
import type {
  OrderDraftStatus,
  OrderIntakeSource,
  OrderMatchConfidence,
  OrderTransferStatus,
} from "../domain/order-draft";

const ORDER_DRAFT_STATUSES: readonly OrderDraftStatus[] = [
  "collecting",
  "needs-review",
  "ready-for-approval",
  "approved",
  "rejected",
  "transferring",
  "transferred",
  "transfer-failed",
];

const ORDER_INTAKE_SOURCES: readonly OrderIntakeSource[] = [
  "email",
  "whatsapp",
  "erp",
  "customer-portal",
  "sales-representative",
  "web-form",
  "file-upload",
];

const ORDER_CONFIDENCE_LEVELS:
  readonly OrderMatchConfidence[] = [
    "low",
    "medium",
    "high",
  ];

const ORDER_TRANSFER_STATUSES:
  readonly OrderTransferStatus[] = [
    "not-started",
    "pending",
    "in-progress",
    "completed",
    "failed",
  ];

function isIncluded<T extends string>(
  values: readonly T[],
  value: string | null,
): value is T {
  return (
    value !== null &&
    values.includes(value as T)
  );
}

function parseBoolean(
  value: string | null,
): boolean | undefined {
  if (value === "true") {
    return true;
  }

  if (value === "false") {
    return false;
  }

  return undefined;
}

export function parseOrderDraftFilterFromUrl(
  searchParams: URLSearchParams,
): OrderDraftFilter {
  const source = searchParams.get("source");
  const status = searchParams.get("status");
  const confidence =
    searchParams.get("confidence");
  const transfer =
    searchParams.get("transfer");

  return {
    search:
      searchParams.get("search")?.trim() ||
      undefined,

    source: isIncluded(
      ORDER_INTAKE_SOURCES,
      source,
    )
      ? source
      : undefined,

    status: isIncluded(
      ORDER_DRAFT_STATUSES,
      status,
    )
      ? status
      : undefined,

    confidenceLevel: isIncluded(
      ORDER_CONFIDENCE_LEVELS,
      confidence,
    )
      ? confidence
      : undefined,

    transferStatus: isIncluded(
      ORDER_TRANSFER_STATUSES,
      transfer,
    )
      ? transfer
      : undefined,

    hasBlockingIssues: parseBoolean(
      searchParams.get("blocking"),
    ),

    awaitingApproval: parseBoolean(
      searchParams.get("awaitingApproval"),
    ),
  };
}

export function createOrderDraftFilterUrl(
  filter: OrderDraftFilter,
  pathname = "/siparisler",
): string {
  const searchParams = new URLSearchParams();

  if (filter.search) {
    searchParams.set(
      "search",
      filter.search.trim(),
    );
  }

  if (filter.source) {
    searchParams.set(
      "source",
      filter.source,
    );
  }

  if (filter.status) {
    searchParams.set(
      "status",
      filter.status,
    );
  }

  if (filter.confidenceLevel) {
    searchParams.set(
      "confidence",
      filter.confidenceLevel,
    );
  }

  if (filter.transferStatus) {
    searchParams.set(
      "transfer",
      filter.transferStatus,
    );
  }

  if (
    filter.hasBlockingIssues !== undefined
  ) {
    searchParams.set(
      "blocking",
      String(filter.hasBlockingIssues),
    );
  }

  if (
    filter.awaitingApproval !== undefined
  ) {
    searchParams.set(
      "awaitingApproval",
      String(filter.awaitingApproval),
    );
  }

  const query = searchParams.toString();

  return query
    ? `${pathname}?${query}`
    : pathname;
}