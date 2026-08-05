import type {
  ExecuteOrderTransferInput,
  OrderTransferExecutionAudit,
  OrderTransferExecutionFailureCode,
  OrderTransferExecutionResult,
  OrderTransferExecutor,
} from "./order-transfer-executor";
import {
  validateOrderTransferExecutionInput,
} from "./order-transfer-executor";

export type OrderTransferPreviewScenario =
  | "success"
  | "partial"
  | "failure";

export type OrderTransferPreviewExecutorOptions =
  Readonly<{
    scenario?: OrderTransferPreviewScenario;
    failureCode?: OrderTransferExecutionFailureCode;
    externalOrderId?: string;
    now?: () => string;
  }>;

function createPreviewExternalOrderId(
  orderDraftId: string,
): string {
  return `PREVIEW-${orderDraftId
    .replace(/[^a-zA-Z0-9]/g, "")
    .toUpperCase()}`;
}

function createExecutionAudit(
  input: ExecuteOrderTransferInput,
  startedAt: string,
  finishedAt: string,
): OrderTransferExecutionAudit {
  return {
    commandId: input.command.commandId,
    orderDraftId: input.command.orderDraftId,
    targetSystem: input.command.targetSystem,
    requestedByUserId:
      input.command.requestedByUserId,
    requestedAt: input.command.requestedAt,
    startedAt,
    finishedAt,
  };
}

function mapValidationFailureCode(
  code:
    | "order-state-conflict"
    | "approval-invalid"
    | "target-unavailable",
): OrderTransferExecutionFailureCode {
  return code;
}

export function createOrderTransferPreviewExecutor(
  options: OrderTransferPreviewExecutorOptions = {},
): OrderTransferExecutor {
  const {
    scenario = "success",
    failureCode = "target-rejected",
    externalOrderId,
    now = () => new Date().toISOString(),
  } = options;

  return {
    async execute(
      input: ExecuteOrderTransferInput,
    ): Promise<OrderTransferExecutionResult> {
      const startedAt = now();

      const validation =
        validateOrderTransferExecutionInput(input);

      if (!validation.valid) {
        const finishedAt = now();

        return {
          status: "failed",
          code: mapValidationFailureCode(
            validation.code,
          ),
          message:
            "Aktarım öncesi son durum doğrulaması başarısız oldu.",
          retryable:
            validation.code !== "approval-invalid",
          audit: createExecutionAudit(
            input,
            startedAt,
            finishedAt,
          ),
        };
      }

      if (scenario === "failure") {
        const finishedAt = now();

        return {
          status: "failed",
          code: failureCode,
          message:
            "Önizleme senaryosu bağlı sistem aktarımını başarısız olarak sonuçlandırdı.",
          retryable:
            failureCode === "timeout" ||
            failureCode ===
              "target-unavailable" ||
            failureCode === "unknown",
          audit: createExecutionAudit(
            input,
            startedAt,
            finishedAt,
          ),
        };
      }

      if (scenario === "partial") {
        const finishedAt = now();

        return {
          status: "partially-completed",
          externalOrderId:
            externalOrderId ??
            createPreviewExternalOrderId(
              input.command.orderDraftId,
            ),
          message:
            "Sipariş hedef sistemde oluşturuldu ancak sonraki operasyon adımları tamamlanmadı.",
          completedSteps: [
            "Sipariş verileri doğrulandı",
            "Hedef sistem kaydı oluşturuldu",
          ],
          pendingSteps: [
            "Stok rezervasyonu doğrulaması",
            "Üretim planlama bildirimi",
          ],
          audit: createExecutionAudit(
            input,
            startedAt,
            finishedAt,
          ),
        };
      }

      const finishedAt = now();

      return {
        status: "completed",
        externalOrderId:
          externalOrderId ??
          createPreviewExternalOrderId(
            input.command.orderDraftId,
          ),
        message:
          "Sipariş önizleme yürütücüsü tarafından başarıyla aktarıldı.",
        audit: createExecutionAudit(
          input,
          startedAt,
          finishedAt,
        ),
      };
    },
  };
}