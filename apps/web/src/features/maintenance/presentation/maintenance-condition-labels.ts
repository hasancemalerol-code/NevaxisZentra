import type {
  MaintenanceConditionDataFreshness,
  MaintenanceConditionDataSufficiency,
  MaintenanceConditionStatus,
  MaintenanceConditionTrendState,
} from "../domain/maintenance-condition";

export function getMaintenancePredictiveConditionStatusLabel(status: MaintenanceConditionStatus): string {
  switch (status) {
    case "normal":
      return "Normal";
    case "watch":
      return "İzlenmeli";
    case "degraded":
      return "Bozulma Eğilimi";
    case "critical":
      return "Kritik";
    default:
      return "Bilinmiyor";
  }
}

export function getMaintenancePredictiveConditionStatusTone(
  status: MaintenanceConditionStatus,
): "success" | "warning" | "danger" | "neutral" {
  switch (status) {
    case "normal":
      return "success";
    case "watch":
      return "warning";
    case "degraded":
    case "critical":
      return "danger";
    default:
      return "neutral";
  }
}

export function getMaintenanceDataSufficiencyLabel(level: MaintenanceConditionDataSufficiency): string {
  switch (level) {
    case "high":
      return "Yüksek";
    case "medium":
      return "Orta";
    case "low":
      return "Düşük";
    default:
      return "Yetersiz";
  }
}

export function getMaintenanceDataFreshnessLabel(level: MaintenanceConditionDataFreshness): string {
  switch (level) {
    case "fresh":
      return "Güncel";
    case "stale":
      return "Eski";
    default:
      return "Bilinmiyor";
  }
}

export function getMaintenanceTrendStateLabel(trend: MaintenanceConditionTrendState): string {
  switch (trend) {
    case "stable":
      return "Stabil";
    case "increasing":
      return "Yükseliyor";
    case "decreasing":
      return "Düşüyor";
    case "volatile":
      return "Dalgalı";
    default:
      return "Yetersiz Veri";
  }
}

export function getMaintenanceAnomalySeverityLabel(severity: string): string {
  switch (severity) {
    case "info":
      return "Bilgi";
    case "warning":
      return "Uyarı";
    case "high":
      return "Yüksek";
    case "critical":
      return "Kritik";
    default:
      return severity;
  }
}

export const MAINTENANCE_CONDITION_SIMULATOR_DISCLAIMER =
  "Bu değerlendirme simülatör verisiyle doğrulanmıştır; gerçek makine kestirici bakım doğrulaması henüz yapılmamıştır.";

export const MAINTENANCE_CONDITION_ANOMALY_EXPLANATION =
  "Normal davranıştan sapma algılandı.";

export const MAINTENANCE_CONDITION_STALE_WARNING =
  "Son makine verisi güncel değil; durum değerlendirmesi sınırlıdır.";

export function formatMaintenanceConditionAssetLabel(
  assetReference: string,
  assetName?: string,
  machineCode?: string,
): string {
  if (assetName && machineCode) {
    return `${assetName} (${machineCode})`;
  }
  return assetName ?? assetReference;
}
