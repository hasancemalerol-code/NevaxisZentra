import type { OrderTransferAuditRecord } from "./order-transfer-audit-record";
import {
  isOrderTransferAuditQueryDateRangeValid,
  normalizeOrderTransferAuditQuery,
  type OrderTransferAuditQuery,
  type OrderTransferAuditRepository,
  type SaveOrderTransferAuditRecordResult,
} from "./order-transfer-audit-repository";

type CreateOrderTransferAuditMemoryRepositoryInput =
  Readonly<{
    initialRecords?: readonly OrderTransferAuditRecord[];
  }>;

function isAuditRecordValid(
  record: OrderTransferAuditRecord,
): boolean {
  return Boolean(
    record.id &&
      record.orderDraftId &&
      record.commandId &&
      record.approvalId &&
      record.targetSystem &&
      record.requestedByUserId &&
      record.startedAt &&
      record.finishedAt &&
      record.recordedAt,
  );
}

function isWithinDateRange(
  record: OrderTransferAuditRecord,
  query: OrderTransferAuditQuery,
): boolean {
  const recordedAt = new Date(
    record.recordedAt,
  ).getTime();

  if (!Number.isFinite(recordedAt)) {
    return false;
  }

  if (query.recordedFrom) {
    const fromTime = new Date(
      query.recordedFrom,
    ).getTime();

    if (
      Number.isFinite(fromTime) &&
      recordedAt < fromTime
    ) {
      return false;
    }
  }

  if (query.recordedTo) {
    const toTime = new Date(
      query.recordedTo,
    ).getTime();

    if (
      Number.isFinite(toTime) &&
      recordedAt > toTime
    ) {
      return false;
    }
  }

  return true;
}

function matchesAuditQuery(
  record: OrderTransferAuditRecord,
  query: OrderTransferAuditQuery,
): boolean {
  if (
    query.orderDraftId &&
    record.orderDraftId !== query.orderDraftId
  ) {
    return false;
  }

  if (
    query.commandId &&
    record.commandId !== query.commandId
  ) {
    return false;
  }

  if (
    query.requestedByUserId &&
    record.requestedByUserId !==
      query.requestedByUserId
  ) {
    return false;
  }

  if (
    query.outcome &&
    record.outcome !== query.outcome
  ) {
    return false;
  }

  return isWithinDateRange(record, query);
}

export function createOrderTransferAuditMemoryRepository({
  initialRecords = [],
}: CreateOrderTransferAuditMemoryRepositoryInput = {}): OrderTransferAuditRepository {
  const records = new Map<
    string,
    OrderTransferAuditRecord
  >();

  for (const record of initialRecords) {
    if (isAuditRecordValid(record)) {
      records.set(record.id, record);
    }
  }

  return {
    async save(
      record: OrderTransferAuditRecord,
    ): Promise<SaveOrderTransferAuditRecordResult> {
      if (!isAuditRecordValid(record)) {
        return {
          success: false,
          reason: "invalid-record",
          message:
            "Aktarım audit kaydı zorunlu alanları içermiyor.",
          retryable: false,
        };
      }

      const existingRecord = records.get(
        record.id,
      );

      if (existingRecord) {
        return {
          success: true,
          record: existingRecord,
          created: false,
        };
      }

      records.set(record.id, record);

      return {
        success: true,
        record,
        created: true,
      };
    },

    async findById(
      recordId: string,
    ): Promise<OrderTransferAuditRecord | null> {
      return records.get(recordId) ?? null;
    },

    async findByCommandId(
      commandId: string,
    ): Promise<
      readonly OrderTransferAuditRecord[]
    > {
      return [...records.values()]
        .filter(
          (record) =>
            record.commandId === commandId,
        )
        .sort(
          (left, right) =>
            new Date(
              right.recordedAt,
            ).getTime() -
            new Date(
              left.recordedAt,
            ).getTime(),
        );
    },

    async query(
      query: OrderTransferAuditQuery,
    ): Promise<
      readonly OrderTransferAuditRecord[]
    > {
      const normalizedQuery =
        normalizeOrderTransferAuditQuery(query);

      if (
        !isOrderTransferAuditQueryDateRangeValid(
          normalizedQuery,
        )
      ) {
        return [];
      }

      return [...records.values()]
        .filter((record) =>
          matchesAuditQuery(
            record,
            normalizedQuery,
          ),
        )
        .sort(
          (left, right) =>
            new Date(
              right.recordedAt,
            ).getTime() -
            new Date(
              left.recordedAt,
            ).getTime(),
        )
        .slice(
          0,
          normalizedQuery.limit ?? 50,
        );
    },
  };
}