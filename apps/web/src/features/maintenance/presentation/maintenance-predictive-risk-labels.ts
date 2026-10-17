import type {
  MaintenancePredictiveRecommendationCategory,
  MaintenancePredictiveReviewPriority,
  MaintenancePredictiveRiskBand,
  MaintenancePredictiveRiskFollowUpStatus,
} from "../domain/maintenance-predictive-risk";

export const MAINTENANCE_PREDICTIVE_RISK_RULE_SET_VERSION = "MAINT-PRED-RISK-V1";

export const MAINTENANCE_PREDICTIVE_RISK_SAFETY_COPY =
  "Bakım önerileri kural tabanlı karar desteğidir; otomatik bakım talebi veya bakım emri oluşturmaz. Operasyonel aksiyon yetkili kullanıcı incelemesi ile başlatılır.";

export const MAINTENANCE_PREDICTIVE_RISK_SIMULATOR_COPY =
  "Kanıtlar kayıtlı bakım ve durum verisine dayanır; sahada makine doğrulamasının yerine geçmez.";

export const MAINTENANCE_PREDICTIVE_RISK_TASK_DISCLAIMER =
  "Bu görev taslağı otomatik bakım emri, duruş veya makine müdahalesi başlatmaz.";

const FOLLOW_UP_STATUS_LABELS: Record<MaintenancePredictiveRiskFollowUpStatus, string> = {
  active: "Aktif risk",
  "data-review-needed": "Veri incelemesi gerekli",
  monitoring: "İzlemede",
  "task-draft-created": "Görev taslağı oluşturuldu",
  "human-reviewed": "İnsan incelemesi yapıldı",
  resolved: "Kapatıldı",
};

export function getMaintenancePredictiveRiskFollowUpStatusLabel(
  status: MaintenancePredictiveRiskFollowUpStatus,
): string {
  return FOLLOW_UP_STATUS_LABELS[status] ?? status;
}

const RISK_BAND_LABELS: Record<MaintenancePredictiveRiskBand, string> = {
  "insufficient-data": "Yetersiz Veri",
  low: "Düşük Risk",
  watch: "İzlenmeli",
  high: "Yüksek Risk",
  critical: "Kritik Risk",
};

const REVIEW_PRIORITY_LABELS: Record<MaintenancePredictiveReviewPriority, string> = {
  routine: "Rutin",
  elevated: "Yükseltilmiş",
  planned: "Planlı",
  urgent: "Acil",
};

const RECOMMENDATION_CATEGORY_LABELS: Record<MaintenancePredictiveRecommendationCategory, string> = {
  "review-data-quality": "Veri Kalitesini İncele",
  "continue-routine-monitoring": "Rutin İzlemeye Devam Et",
  "close-monitoring": "Yakın İzleme",
  "plan-technical-review": "Teknik İnceleme Planla",
  "urgent-authorized-technical-review": "Acil Yetkili Teknik İnceleme",
};

export function getMaintenancePredictiveRiskBandLabel(band: MaintenancePredictiveRiskBand): string {
  return RISK_BAND_LABELS[band] ?? band;
}

export function getMaintenancePredictiveReviewPriorityLabel(
  priority: MaintenancePredictiveReviewPriority,
): string {
  return REVIEW_PRIORITY_LABELS[priority] ?? priority;
}

export function getMaintenancePredictiveRecommendationCategoryLabel(
  category: MaintenancePredictiveRecommendationCategory,
): string {
  return RECOMMENDATION_CATEGORY_LABELS[category] ?? category;
}

export function getMaintenancePredictiveRiskBandTone(
  band: MaintenancePredictiveRiskBand,
): "neutral" | "ready" | "paused" | "warning" | "danger" {
  switch (band) {
    case "critical":
      return "danger";
    case "high":
      return "warning";
    case "watch":
      return "paused";
    case "low":
      return "ready";
    default:
      return "neutral";
  }
}

export function getMaintenancePredictiveReviewPriorityTone(
  priority: MaintenancePredictiveReviewPriority,
): "neutral" | "ready" | "paused" | "warning" | "danger" {
  switch (priority) {
    case "urgent":
      return "danger";
    case "planned":
      return "warning";
    case "elevated":
      return "paused";
    default:
      return "ready";
  }
}

export function formatMaintenancePredictiveDriverLabel(driverCode: string): string {
  const labels: Record<string, string> = {
    "insufficient-data": "Yetersiz veri",
    "stale-evidence": "Bayat kanıt",
    "missing-evaluation": "Eksik değerlendirme",
    "condition-status": "Durum profili",
    "active-anomaly": "Aktif anomali",
    "persistent-anomaly": "Kalıcı anomali",
    "multiple-critical-anomalies": "Çoklu kritik anomali",
    "alarm-context": "Alarm bağlamı",
    "downtime-context": "Duruş bağlamı",
    "maintenance-history": "Bakım geçmişi",
    "corroborated-critical-evidence": "Doğrulanmış kritik kanıt",
  };

  return labels[driverCode] ?? "Risk sürücüsü";
}

export function formatMaintenancePredictiveEvidenceTypeLabel(evidenceType: string): string {
  const labels: Record<string, string> = {
    "condition-profile": "Durum profili",
    "condition-anomaly": "Anomali kanıtı",
    "maintenance-history": "Bakım geçmişi",
    "edge-alarm": "Saha alarmı",
    "edge-downtime": "Saha duruşu",
  };

  return labels[evidenceType] ?? "Kanıt";
}
