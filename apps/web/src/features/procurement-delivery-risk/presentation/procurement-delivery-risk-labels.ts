import type { ProcurementDeliveryRiskResult } from "../domain/procurement-delivery-risk";

export const PROCUREMENT_DELIVERY_RISK_DISCLAIMER =
  "Bu ekran yalnızca tedarik teslimat riskini gösterir; otomatik satın alma, tedarikçi iletişimi, stok veya üretim işlemi yapmaz.";

export function getProcurementDeliveryRiskResultLabel(result: ProcurementDeliveryRiskResult): string {
  switch (result) {
    case "overdue":
      return "Gecikmiş";
    case "delivery-risk":
      return "Teslimat riski";
    case "on-track":
      return "Plan dahilinde";
    case "insufficient-data":
      return "Yetersiz veri";
    case "source-conflict":
      return "Kaynak çakışması";
    default:
      return result;
  }
}

export function getProcurementDeliveryRiskResultBadgeClass(result: ProcurementDeliveryRiskResult): string {
  switch (result) {
    case "overdue":
      return "quality-status-badge quality-status-badge--critical";
    case "delivery-risk":
      return "quality-status-badge quality-status-badge--warning";
    case "on-track":
      return "quality-status-badge quality-status-badge--success";
    case "insufficient-data":
      return "quality-status-badge quality-status-badge--muted";
    case "source-conflict":
      return "quality-status-badge quality-status-badge--danger";
    default:
      return "quality-status-badge";
  }
}

export function getProcurementDeliveryRiskFreshnessLabel(freshness?: string): string {
  switch (freshness) {
    case "fresh":
      return "Güncel";
    case "stale":
      return "Eski kaynak";
    case "unknown":
      return "Bilinmiyor";
    default:
      return freshness ?? "—";
  }
}

export function getProcurementDeliveryRiskDriverLabel(code: string): string {
  const labels: Record<string, string> = {
    "purchase-order-missing": "Satın alma siparişi eksik",
    "purchase-order-line-missing": "Sipariş satırı eksik",
    "commitment-date-missing": "Taahhüt tarihi eksik",
    "open-quantity-unknown": "Açık miktar bilinmiyor",
    "order-closed-or-cancelled": "Sipariş kapalı veya iptal",
    "erp-source-stale": "ERP kaynağı güncel değil",
    "erp-source-unknown": "ERP kaynak zamanı bilinmiyor",
    "commitment-date-conflict": "Taahhüt tarihi çakışması",
    "partial-receipt-open-quantity": "Kısmi teslim ve açık miktar",
    "overdue-open-quantity": "Gecikmiş açık miktar",
    "on-track-open-commitment": "Plan dahilinde açık miktar",
  };
  return labels[code] ?? code;
}

export function getProcurementDeliveryRiskEvidenceFieldLabel(field: string): string {
  const labels: Record<string, string> = {
    purchaseOrderNumber: "Satın alma referansı",
    orderStatus: "Sipariş durumu",
    lineNumber: "Satır numarası",
    commitmentDateUtc: "Taahhüt tarihi",
    openQuantity: "Açık miktar",
    receivedQuantity: "Teslim alınan miktar",
    sourceFreshness: "Kaynak tazeliği",
    headerCommitmentDateUtc: "Başlık taahhüt tarihi",
    lineCommitmentDateUtc: "Satır taahhüt tarihi",
  };
  return labels[field] ?? field;
}

const FOLLOW_UP_STATUS_LABELS: Record<string, string> = {
  "requires-review": "İnceleme gerekli",
  acknowledged: "Görüldü",
  "task-created": "Görev oluşturuldu",
  resolved: "Çözüldü",
  dismissed: "Reddedildi",
};

export function getProcurementDeliveryRiskFollowUpStatusLabel(status?: string): string {
  if (!status) return "—";
  return FOLLOW_UP_STATUS_LABELS[status] ?? status;
}

export function getProcurementDeliveryRiskFollowUpAuditEventLabel(eventType: string): string {
  switch (eventType) {
    case "task-linked":
      return "Görev bağlandı";
    case "follow-up-transitioned":
      return "Takip durumu güncellendi";
    default:
      return eventType;
  }
}
