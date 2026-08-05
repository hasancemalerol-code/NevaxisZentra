import {
  createOrderTransferAuditRecord,
  type OrderTransferAuditRecord,
} from "./order-transfer-audit-record";
import type {
  OrderTransferAuditRepository,
  SaveOrderTransferAuditRecordResult,
} from "./order-transfer-audit-repository";
import type { OrderTransferCommand } from "./order-transfer-command";
import type {
  OrderTransferExecutionResult,
  OrderTransferExecutor,
} from "./order-transfer-executor";
import type { OrderDraft } from "../domain/order-draft";

export type ExecuteAndAuditOrderTransferInput =
  Readonly<{
    command: OrderTransferCommand;
    currentOrderDraft: OrderDraft;
    recordedAt?: string;
  }>;

export type ExecuteAndAuditOrderTransferSuccess =
  Readonly<{
    success: true;
    execution: OrderTransferExecutionResult;
    auditRecord: OrderTransferAuditRecord;
    auditSaveResult: Extract<
      SaveOrderTransferAuditRecordResult,
      Readonly<{ success: true }>
    >;
  }>;

export type ExecuteAndAuditOrderTransferFailure =
  Readonly<{
    success: false;
    reason:
      | "execution-failed"
      | "audit-save-failed";
    execution: OrderTransferExecutionResult;
    auditRecord: OrderTransferAuditRecord;
    auditSaveResult:
      SaveOrderTransferAuditRecordResult;
  }>;

export type ExecuteAndAuditOrderTransferResult =
  | ExecuteAndAuditOrderTransferSuccess
  | ExecuteAndAuditOrderTransferFailure;

export type OrderTransferService =
  Readonly<{
    executeAndAudit(
      input: ExecuteAndAuditOrderTransferInput,
    ): Promise<ExecuteAndAuditOrderTransferResult>;
  }>;

type CreateOrderTransferServiceInput =
  Readonly<{
    executor: OrderTransferExecutor;
    auditRepository: OrderTransferAuditRepository;
    now?: () => string;
  }>;

export function createOrderTransferService({
  executor,
  auditRepository,
  now = () => new Date().toISOString(),
}: CreateOrderTransferServiceInput): OrderTransferService {
  return {
    async executeAndAudit(
      input: ExecuteAndAuditOrderTransferInput,
    ): Promise<ExecuteAndAuditOrderTransferResult> {
      const execution = await executor.execute({
        command: input.command,
        currentOrderDraft:
          input.currentOrderDraft,
      });

      const auditRecord =
        createOrderTransferAuditRecord({
          command: input.command,
          result: execution,
          recordedAt:
            input.recordedAt ?? now(),
        });

      const auditSaveResult =
        await auditRepository.save(
          auditRecord,
        );

      if (!auditSaveResult.success) {
        return {
          success: false,
          reason: "audit-save-failed",
          execution,
          auditRecord,
          auditSaveResult,
        };
      }

      if (execution.status === "failed") {
        return {
          success: false,
          reason: "execution-failed",
          execution,
          auditRecord,
          auditSaveResult,
        };
      }

      return {
        success: true,
        execution,
        auditRecord,
        auditSaveResult,
      };
    },
  };
}