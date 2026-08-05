import {
  createOrderTransferAuditMemoryRepository,
} from "./order-transfer-audit-memory-repository";
import type {
  OrderTransferAuditRecord,
} from "./order-transfer-audit-record";
import {
  createOrderTransferPreviewExecutor,
  type OrderTransferPreviewExecutorOptions,
} from "./order-transfer-preview-executor";
import {
  createOrderTransferService,
  type ExecuteAndAuditOrderTransferResult,
  type OrderTransferService,
} from "./order-transfer-service";

export type OrderTransferPreviewService =
  Readonly<{
    transferService: OrderTransferService;

    findAuditRecord(
      recordId: string,
    ): Promise<OrderTransferAuditRecord | null>;

    findAuditRecordsByCommandId(
      commandId: string,
    ): Promise<
      readonly OrderTransferAuditRecord[]
    >;
  }>;

export type CreateOrderTransferPreviewServiceInput =
  Readonly<{
    executorOptions?:
      OrderTransferPreviewExecutorOptions;

    initialAuditRecords?:
      readonly OrderTransferAuditRecord[];

    now?: () => string;
  }>;

export function createOrderTransferPreviewService({
  executorOptions,
  initialAuditRecords = [],
  now,
}: CreateOrderTransferPreviewServiceInput = {}): OrderTransferPreviewService {
  const executor =
    createOrderTransferPreviewExecutor(
      executorOptions,
    );

  const auditRepository =
    createOrderTransferAuditMemoryRepository({
      initialRecords: initialAuditRecords,
    });

  const transferService =
    createOrderTransferService({
      executor,
      auditRepository,
      now,
    });

  return {
    transferService,

    async findAuditRecord(
      recordId: string,
    ): Promise<OrderTransferAuditRecord | null> {
      return auditRepository.findById(recordId);
    },

    async findAuditRecordsByCommandId(
      commandId: string,
    ): Promise<
      readonly OrderTransferAuditRecord[]
    > {
      return auditRepository.findByCommandId(
        commandId,
      );
    },
  };
}

export async function executeOrderTransferPreview(
  service: OrderTransferPreviewService,
  input: Parameters<
    OrderTransferService["executeAndAudit"]
  >[0],
): Promise<ExecuteAndAuditOrderTransferResult> {
  return service.transferService.executeAndAudit(
    input,
  );
}