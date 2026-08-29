import type { MaterialAvailabilityRiskStatus } from "../domain/material-availability-risk";

export const MATERIAL_AVAILABILITY_RISK_DISCLAIMER =
  "Bu ekran yalnızca malzeme kullanılabilirliği riskini gösterir; otomatik stok, satın alma veya üretim işlemi yapmaz.";

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
