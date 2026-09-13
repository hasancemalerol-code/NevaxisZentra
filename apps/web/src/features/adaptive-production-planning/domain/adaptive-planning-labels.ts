export const ADAPTIVE_PLANNING_TITLE = "Planlama Merkezi";
export const ADAPTIVE_PLANNING_SUBTITLE =
  "Uyarlanabilir üretim planı, senaryo simülasyonu ve insan onaylı replan akışı";

export function getPlanningHorizonLabel(horizon: string): string {
  switch (horizon) {
    case "tactical":
      return "Taktik";
    case "operational":
      return "Operasyonel";
    case "realtime-recovery":
      return "Gerçek Zamanlı Kurtarma";
    default:
      return horizon;
  }
}

export function getAdaptiveLifecycleStatusLabel(status: string): string {
  switch (status) {
    case "draft":
      return "Taslak";
    case "simulating":
      return "Simülasyon";
    case "ready":
      return "Hazır";
    case "approval-pending":
      return "Onay Bekliyor";
    case "published":
      return "Yayınlandı";
    case "active":
      return "Aktif";
    case "superseded":
      return "Geçersiz Kılınmış";
    case "completed":
      return "Tamamlandı";
    case "cancelled":
      return "İptal";
    default:
      return status;
  }
}

export function getAdaptiveLifecycleBadgeTone(status: string): "neutral" | "information" | "success" | "warning" | "danger" | "ai" {
  switch (status) {
    case "active":
    case "published":
      return "success";
    case "approval-pending":
    case "simulating":
      return "warning";
    case "cancelled":
    case "superseded":
      return "danger";
    case "ready":
      return "information";
    case "draft":
      return "neutral";
    default:
      return "ai";
  }
}

export function getReplanActionLabel(action: string): string {
  switch (action) {
    case "keep-current-plan":
      return "Mevcut Planı Koru";
    case "local-adjustment":
      return "Yerel Ayarlama";
    case "partial-replan":
      return "Kısmi Yeniden Planlama";
    case "full-replan":
      return "Tam Yeniden Planlama";
    default:
      return action;
  }
}

export function getAutonomyModeLabel(mode: string): string {
  switch (mode) {
    case "observe":
      return "İzleme";
    case "draft":
      return "Taslak Öneri";
    case "limited":
      return "Sınırlı Otonomi";
    case "full":
      return "Tam Otonomi";
    default:
      return mode;
  }
}

export function getScenarioStatusLabel(status: string): string {
  switch (status) {
    case "draft":
      return "Taslak";
    case "evaluated":
      return "Değerlendirildi";
    case "selected":
      return "Seçildi";
    case "rejected":
      return "Reddedildi";
    case "archived":
      return "Arşiv";
    default:
      return status;
  }
}

export function getTriggerTypeLabel(triggerType: string): string {
  switch (triggerType) {
    case "machine-failure":
      return "Makine Arızası";
    case "material-shortage":
      return "Malzeme Eksikliği";
    case "quality-hold":
      return "Kalite Bekletme";
    case "deadline-risk":
      return "Termin Riski";
    case "capacity-overload":
      return "Kapasite Aşımı";
    case "manual":
      return "Manuel";
    case "scheduled-review":
      return "Planlı İnceleme";
    default:
      return triggerType;
  }
}

export function getDeviationTypeLabel(deviationType: string): string {
  switch (deviationType) {
    case "machine-failure":
      return "Makine Arızası";
    case "material-shortage":
      return "Malzeme Eksikliği";
    case "schedule-slip":
      return "Program Kayması";
    case "quality-hold":
      return "Kalite Bekletme";
    case "capacity-overload":
      return "Kapasite Aşımı";
    default:
      return deviationType;
  }
}

export function getDeviationSeverityLabel(severity: string): string {
  switch (severity) {
    case "low":
      return "Düşük";
    case "medium":
      return "Orta";
    case "high":
      return "Yüksek";
    case "critical":
      return "Kritik";
    default:
      return severity;
  }
}

export function getMaterialReadinessLabel(state: string): string {
  switch (state) {
    case "ready":
      return "Hazır";
    case "partial":
      return "Kısmi";
    case "shortage":
      return "Eksik";
    case "blocked":
      return "Bloklu";
    default:
      return state;
  }
}

export function getOperationStatusLabel(status: string): string {
  switch (status) {
    case "planned":
      return "Planlandı";
    case "in-progress":
      return "Devam Ediyor";
    case "completed":
      return "Tamamlandı";
    case "delayed":
      return "Gecikmiş";
    case "cancelled":
      return "İptal";
    default:
      return status;
  }
}

export function getChangeTypeLabel(changeType: string): string {
  switch (changeType) {
    case "initial-publish":
      return "İlk Yayın";
    case "replan":
      return "Yeniden Planlama";
    case "local-adjustment":
      return "Yerel Ayarlama";
    case "approval":
      return "Onay";
    case "rollback":
      return "Geri Alma";
    default:
      return changeType;
  }
}

export function getActivityEventLabel(eventType: string): string {
  switch (eventType) {
    case "plan-built":
      return "Plan Oluşturuldu";
    case "plan-published":
      return "Plan Yayınlandı";
    case "replan-evaluated":
      return "Replan Değerlendirildi";
    case "approval-requested":
      return "Onay İstendi";
    case "approval-granted":
      return "Onay Verildi";
    case "approval-rejected":
      return "Onay Reddedildi";
    case "simulation-completed":
      return "Simülasyon Tamamlandı";
    case "deviation-detected":
      return "Sapma Tespit Edildi";
    default:
      return eventType;
  }
}

export function getDeadlineRiskLevelLabel(level: string): string {
  switch (level) {
    case "low":
      return "Düşük";
    case "medium":
      return "Orta";
    case "high":
      return "Yüksek";
    case "critical":
      return "Kritik";
    default:
      return level;
  }
}

export function formatAdaptivePercent(value: number): string {
  return `%${Math.round(value)}`;
}

export function formatAdaptiveDateTime(value?: string | null): string {
  if (!value) {
    return "—";
  }

  const date = new Date(value);
  if (Number.isNaN(date.getTime())) {
    return value;
  }

  return new Intl.DateTimeFormat("tr-TR", {
    dateStyle: "medium",
    timeStyle: "short",
  }).format(date);
}

export function formatAdaptiveDurationMinutes(minutes: number): string {
  if (minutes >= 60) {
    const hours = Math.floor(minutes / 60);
    const remainder = Math.round(minutes % 60);
    return remainder > 0 ? `${hours}s ${remainder}dk` : `${hours}s`;
  }
  return `${Math.round(minutes)}dk`;
}
