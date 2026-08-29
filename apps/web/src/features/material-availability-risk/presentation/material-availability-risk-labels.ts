import type { MaterialAvailabilityRiskStatus } from "../domain/material-availability-risk";

export const MATERIAL_AVAILABILITY_RISK_DISCLAIMER =
  "Bu ekran yalnızca malzeme kullanılabilirliği riskini gösterir; otomatik stok, satın alma veya üretim işlemi yapmaz.";

export const MATERIAL_AVAILABILITY_RISK_TASK_DISCLAIMER =
  "Otomatik stok, satın alma veya üretim işlemi uygulanmaz.";

const STATUS_LABELS: Record<MaterialAvailabilityRiskStatus, string> = {
  sufficient: "Yeterli",
  shortage: "Eksik",
  "insufficient-data": "Yetersiz veri",
  "source-conflict": "Kaynak çelişkisi",
};

export function getMaterialAvailabilityRiskStatusLabel(status: string): string {
  return STATUS_LABELS[status as MaterialAvailabilityRiskStatus] ?? status;
}

export function getMaterialAvailabilityRiskStatusBadgeClass(status: string): string {
  switch (status) {
    case "sufficient":
      return "material-registry-badge material-registry-badge--success";
    case "shortage":
      return "material-registry-badge material-registry-badge--warning";
    case "source-conflict":
      return "material-registry-badge material-registry-badge--quarantined";
    case "insufficient-data":
    default:
      return "material-registry-badge material-registry-badge--neutral";
  }
}

const FOLLOW_UP_STATUS_LABELS: Record<string, string> = {
  "requires-review": "İnceleme gerekli",
  acknowledged: "Görüldü",
  "task-created": "Görev oluşturuldu",
  resolved: "Çözüldü",
  dismissed: "Reddedildi",
};

const TASK_PURPOSE_LABELS: Record<string, string> = {
  "human-review-draft": "Stok ve malzeme inceleme görev taslağı",
  "data-review-draft": "Veri inceleme görev taslağı",
};

export function getMaterialAvailabilityRiskFollowUpStatusLabel(status: string): string {
  return FOLLOW_UP_STATUS_LABELS[status] ?? status;
}

export function getMaterialAvailabilityRiskTaskPurposeLabel(purpose?: string): string {
  if (!purpose) return "—";
  return TASK_PURPOSE_LABELS[purpose] ?? purpose;
}
