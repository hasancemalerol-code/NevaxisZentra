import type {
  OrderTransferCommandFailure,
  CreateOrderTransferCommandResult,
} from "../application/order-transfer-command";
import type { OrderTransferReadinessCheckId } from "../application/order-transfer-readiness";

export const ORDER_TRANSFER_COMMAND_FAILURE_LABELS = {
  "transfer-not-ready":
    "Sipariş aktarım koşullarını karşılamıyor",
  "target-system-missing":
    "Aktarım hedefi belirlenmedi",
  "approval-missing":
    "Geçerli yetkili onayı bulunamadı",
} satisfies Record<
  OrderTransferCommandFailure["reason"],
  string
>;

export const ORDER_TRANSFER_COMMAND_FAILURE_DESCRIPTIONS = {
  "transfer-not-ready":
    "Sipariş taslağındaki zorunlu kontroller tamamlanmadan bağlı sistem aktarımı başlatılamaz.",
  "target-system-missing":
    "Aktarım komutu oluşturulmadan önce ERP, MES veya FactOS hedeflerinden biri seçilmelidir.",
  "approval-missing":
    "Sipariş, geçerli ve izlenebilir bir yetkili kullanıcı onayı olmadan bağlı sisteme aktarılamaz.",
} satisfies Record<
  OrderTransferCommandFailure["reason"],
  string
>;

export const ORDER_TRANSFER_READINESS_CHECK_LABELS = {
  "blocking-issues":
    "Engelleyici veri sorunları",
  "authorized-approval":
    "Yetkili kullanıcı onayı",
  "target-system":
    "Hedef sistem seçimi",
  "transfer-state":
    "Aktarım durumu",
} satisfies Record<
  OrderTransferReadinessCheckId,
  string
>;

export function getOrderTransferCommandFailureLabel(
  reason: OrderTransferCommandFailure["reason"],
): string {
  return ORDER_TRANSFER_COMMAND_FAILURE_LABELS[
    reason
  ];
}

export function getOrderTransferCommandFailureDescription(
  reason: OrderTransferCommandFailure["reason"],
): string {
  return ORDER_TRANSFER_COMMAND_FAILURE_DESCRIPTIONS[
    reason
  ];
}

export function getOrderTransferReadinessCheckLabel(
  checkId: OrderTransferReadinessCheckId,
): string {
  return ORDER_TRANSFER_READINESS_CHECK_LABELS[
    checkId
  ];
}

export function getOrderTransferCommandResultLabel(
  result: CreateOrderTransferCommandResult,
): string {
  if (result.success) {
    return "Aktarım komutu hazır";
  }

  return getOrderTransferCommandFailureLabel(
    result.reason,
  );
}

export function getOrderTransferCommandResultDescription(
  result: CreateOrderTransferCommandResult,
): string {
  if (result.success) {
    return "Sipariş aktarım komutu güvenlik kontrollerinden geçti ve yetkili kullanıcı işlemiyle yürütülmeye hazır.";
  }

  return getOrderTransferCommandFailureDescription(
    result.reason,
  );
}