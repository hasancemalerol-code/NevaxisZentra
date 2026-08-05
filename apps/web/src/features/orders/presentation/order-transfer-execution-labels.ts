import type {
  OrderTransferExecutionFailureCode,
  OrderTransferExecutionResult,
  OrderTransferExecutionStatus,
} from "../application/order-transfer-executor";

export const ORDER_TRANSFER_EXECUTION_STATUS_LABELS = {
  completed: "Aktarım tamamlandı",
  "partially-completed": "Aktarım kısmen tamamlandı",
  failed: "Aktarım başarısız",
} satisfies Record<
  OrderTransferExecutionStatus,
  string
>;

export const ORDER_TRANSFER_EXECUTION_STATUS_DESCRIPTIONS = {
  completed:
    "Sipariş bağlı sistem tarafından başarıyla kabul edildi ve aktarım işlemi tamamlandı.",
  "partially-completed":
    "Aktarımın bazı adımları tamamlandı ancak kalan işlemler için takip veya yeniden deneme gerekiyor.",
  failed:
    "Sipariş bağlı sisteme aktarılamadı. Hata nedeni incelenmeden işlem tamamlanmış sayılmaz.",
} satisfies Record<
  OrderTransferExecutionStatus,
  string
>;

export const ORDER_TRANSFER_EXECUTION_FAILURE_LABELS = {
  "order-not-found": "Sipariş bulunamadı",
  "order-state-conflict": "Sipariş durumu değişti",
  "approval-invalid": "Onay kaydı geçersiz",
  "authorization-denied": "İşlem yetkisi reddedildi",
  "target-unavailable": "Hedef sistem kullanılamıyor",
  "target-rejected": "Hedef sistem siparişi reddetti",
  timeout: "Aktarım zaman aşımına uğradı",
  unknown: "Bilinmeyen aktarım hatası",
} satisfies Record<
  OrderTransferExecutionFailureCode,
  string
>;

export const ORDER_TRANSFER_EXECUTION_FAILURE_DESCRIPTIONS = {
  "order-not-found":
    "Aktarım komutundaki sipariş taslağı mevcut sistemde bulunamadı.",
  "order-state-conflict":
    "Komut hazırlandıktan sonra sipariş taslağı değişti. Güncel verilerle yeniden doğrulama ve gerekirse yeniden onay gerekir.",
  "approval-invalid":
    "Sipariş için geçerli, izlenebilir ve güncel bir yetkili kullanıcı onayı bulunamadı.",
  "authorization-denied":
    "İşlemi başlatan kullanıcının bu hedef sisteme aktarım yapma yetkisi bulunmuyor.",
  "target-unavailable":
    "Bağlı ERP, MES veya hedef operasyon sistemi şu anda kullanılamıyor.",
  "target-rejected":
    "Hedef sistem sipariş verilerini doğrulama veya iş kuralı nedeniyle kabul etmedi.",
  timeout:
    "Bağlı sistem belirtilen süre içinde kesin bir yanıt vermedi.",
  unknown:
    "Aktarım sırasında sınıflandırılamayan bir hata oluştu.",
} satisfies Record<
  OrderTransferExecutionFailureCode,
  string
>;

export function getOrderTransferExecutionStatusLabel(
  status: OrderTransferExecutionStatus,
): string {
  return ORDER_TRANSFER_EXECUTION_STATUS_LABELS[
    status
  ];
}

export function getOrderTransferExecutionStatusDescription(
  status: OrderTransferExecutionStatus,
): string {
  return ORDER_TRANSFER_EXECUTION_STATUS_DESCRIPTIONS[
    status
  ];
}

export function getOrderTransferExecutionFailureLabel(
  code: OrderTransferExecutionFailureCode,
): string {
  return ORDER_TRANSFER_EXECUTION_FAILURE_LABELS[
    code
  ];
}

export function getOrderTransferExecutionFailureDescription(
  code: OrderTransferExecutionFailureCode,
): string {
  return ORDER_TRANSFER_EXECUTION_FAILURE_DESCRIPTIONS[
    code
  ];
}

export function getOrderTransferExecutionResultLabel(
  result: OrderTransferExecutionResult,
): string {
  if (result.status === "failed") {
    return getOrderTransferExecutionFailureLabel(
      result.code,
    );
  }

  return getOrderTransferExecutionStatusLabel(
    result.status,
  );
}

export function getOrderTransferExecutionResultDescription(
  result: OrderTransferExecutionResult,
): string {
  if (result.status === "failed") {
    return getOrderTransferExecutionFailureDescription(
      result.code,
    );
  }

  return result.message;
}