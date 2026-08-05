import type {
  OrderTransferAuditOutcome,
  OrderTransferAuditRecord,
} from "./order-transfer-audit-record";

export type OrderTransferAuditQuery =
  Readonly<{
    orderDraftId?: string;
    commandId?: string;
    requestedByUserId?: string;
    outcome?: OrderTransferAuditOutcome;
    recordedFrom?: string;
    recordedTo?: string;
    limit?: number;
  }>;

export type SaveOrderTransferAuditRecordResult =
  | Readonly<{
      success: true;
      record: OrderTransferAuditRecord;
      created: boolean;
    }>
  | Readonly<{
      success: false;
      reason:
        | "duplicate-record"
        | "invalid-record"
        | "repository-unavailable"
        | "unknown";
      message: string;
      retryable: boolean;
    }>;

export interface OrderTransferAuditRepository {
  save(
    record: OrderTransferAuditRecord,
  ): Promise<SaveOrderTransferAuditRecordResult>;

  findById(
    recordId: string,
  ): Promise<OrderTransferAuditRecord | null>;

  findByCommandId(
    commandId: string,
  ): Promise<
    readonly OrderTransferAuditRecord[]
  >;

  query(
    query: OrderTransferAuditQuery,
  ): Promise<
    readonly OrderTransferAuditRecord[]
  >;
}

export function normalizeOrderTransferAuditQuery(
  query: OrderTransferAuditQuery,
): OrderTransferAuditQuery {
  const normalizedLimit =
    typeof query.limit === "number"
      ? Math.min(
          Math.max(
            Math.trunc(query.limit),
            1,
          ),
          200,
        )
      : 50;

  return {
    orderDraftId:
      query.orderDraftId?.trim() || undefined,
    commandId:
      query.commandId?.trim() || undefined,
    requestedByUserId:
      query.requestedByUserId?.trim() ||
      undefined,
    outcome: query.outcome,
    recordedFrom: query.recordedFrom,
    recordedTo: query.recordedTo,
    limit: normalizedLimit,
  };
}

export function isOrderTransferAuditQueryDateRangeValid(
  query: OrderTransferAuditQuery,
): boolean {
  if (
    !query.recordedFrom ||
    !query.recordedTo
  ) {
    return true;
  }

  const fromTime = new Date(
    query.recordedFrom,
  ).getTime();

  const toTime = new Date(
    query.recordedTo,
  ).getTime();

  return (
    Number.isFinite(fromTime) &&
    Number.isFinite(toTime) &&
    fromTime <= toTime
  );
}