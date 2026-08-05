import type {
  OrderApprovalStatus,
  OrderDraftStatus,
  OrderFieldIssueType,
  OrderIntakeSource,
  OrderMatchConfidence,
  OrderTransferStatus,
} from "../domain/order-draft";

export const ORDER_INTAKE_SOURCE_LABELS: Readonly<
  Record<OrderIntakeSource, string>
> = {
  email: "E-posta",
  whatsapp: "WhatsApp",
  erp: "ERP",
  "customer-portal": "Müşteri Portalı",
  "sales-representative": "Satış Temsilcisi",
  "web-form": "Web Formu",
  "file-upload": "Dosya Yükleme",
};

export const ORDER_DRAFT_STATUS_LABELS: Readonly<
  Record<OrderDraftStatus, string>
> = {
  collecting: "Veri toplanıyor",
  "needs-review": "İnceleme gerekiyor",
  "ready-for-approval": "Onaya hazır",
  approved: "Onaylandı",
  rejected: "Reddedildi",
  transferring: "ERP'ye aktarılıyor",
  transferred: "ERP'ye aktarıldı",
  "transfer-failed": "Aktarım başarısız",
};

export const ORDER_CONFIDENCE_LABELS: Readonly<
  Record<OrderMatchConfidence, string>
> = {
  low: "Düşük güven",
  medium: "Orta güven",
  high: "Yüksek güven",
};

export const ORDER_APPROVAL_STATUS_LABELS: Readonly<
  Record<OrderApprovalStatus, string>
> = {
  "not-required": "Henüz onaya gönderilmedi",
  pending: "Onay bekliyor",
  approved: "Onaylandı",
  rejected: "Reddedildi",
  expired: "Onay süresi doldu",
};

export const ORDER_TRANSFER_STATUS_LABELS: Readonly<
  Record<OrderTransferStatus, string>
> = {
  "not-started": "Aktarım başlatılmadı",
  pending: "Aktarım bekliyor",
  "in-progress": "ERP'ye aktarılıyor",
  completed: "ERP'ye aktarıldı",
  failed: "Aktarım başarısız",
};

export const ORDER_FIELD_ISSUE_LABELS: Readonly<
  Record<OrderFieldIssueType, string>
> = {
  missing: "Eksik bilgi",
  conflicting: "Çelişkili bilgi",
  "low-confidence": "Düşük güven",
  invalid: "Geçersiz bilgi",
  outdated: "Güncel olmayan bilgi",
};

export function getOrderIntakeSourceLabel(
  source: OrderIntakeSource,
): string {
  return ORDER_INTAKE_SOURCE_LABELS[source];
}

export function getOrderDraftStatusLabel(
  status: OrderDraftStatus,
): string {
  return ORDER_DRAFT_STATUS_LABELS[status];
}

export function getOrderConfidenceLabel(
  confidence: OrderMatchConfidence,
): string {
  return ORDER_CONFIDENCE_LABELS[confidence];
}

export function getOrderApprovalStatusLabel(
  status: OrderApprovalStatus,
): string {
  return ORDER_APPROVAL_STATUS_LABELS[status];
}

export function getOrderTransferStatusLabel(
  status: OrderTransferStatus,
): string {
  return ORDER_TRANSFER_STATUS_LABELS[status];
}

export function getOrderFieldIssueLabel(
  issueType: OrderFieldIssueType,
): string {
  return ORDER_FIELD_ISSUE_LABELS[issueType];
}