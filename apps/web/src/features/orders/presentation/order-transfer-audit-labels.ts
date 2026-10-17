import type {
  OrderTransferAuditOutcome,
  OrderTransferAuditRecord,
} from "../application/order-transfer-audit-record";

export const ORDER_TRANSFER_AUDIT_OUTCOME_LABELS = {
  completed: "Tamamlandı",
  "partially-completed": "Kısmen tamamlandı",
  failed: "Başarısız",
} satisfies Record<
  OrderTransferAuditOutcome,
  string
>;

export const ORDER_TRANSFER_AUDIT_OUTCOME_DESCRIPTIONS = {
  completed:
    "Aktarım bağlı sistem tarafından başarıyla tamamlandı.",
  "partially-completed":
    "Aktarımın bazı adımları tamamlandı ancak takip gerektiren işlemler bulunuyor.",
  failed:
    "Aktarım tamamlanamadı ve hata kaydı audit geçmişine işlendi.",
} satisfies Record<
  OrderTransferAuditOutcome,
  string
>;

export function getOrderTransferAuditOutcomeLabel(
  outcome: OrderTransferAuditOutcome,
): string {
  return ORDER_TRANSFER_AUDIT_OUTCOME_LABELS[
    outcome
  ];
}

export function getOrderTransferAuditOutcomeDescription(
  outcome: OrderTransferAuditOutcome,
): string {
  return ORDER_TRANSFER_AUDIT_OUTCOME_DESCRIPTIONS[
    outcome
  ];
}

export function getOrderTransferAuditFollowUpLabel(
  record: OrderTransferAuditRecord,
): string {
  switch (record.outcome) {
    case "completed":
      return "Takip gerekmiyor";

    case "partially-completed":
      return "Operasyon takibi gerekiyor";

    case "failed":
      return record.retryable
        ? "Yeniden Deneme uygun"
        : "Manuel inceleme gerekiyor";
  }
}

export function getOrderTransferAuditOutcomeTone(
  outcome: OrderTransferAuditOutcome,
):
  | "success"
  | "warning"
  | "danger" {
  switch (outcome) {
    case "completed":
      return "success";

    case "partially-completed":
      return "warning";

    case "failed":
      return "danger";
  }
}