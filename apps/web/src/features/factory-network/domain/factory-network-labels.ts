export const FACTORY_NETWORK_TITLE = "Fabrika Ağı";
export const FACTORY_NETWORK_SUBTITLE =
  "Çok tesisli kapasite dengesi, tahsis kararları ve ağ dayanıklılığı yönetimi";

export function formatNetworkPercent(value: number): string {
  return `${value.toFixed(1)}%`;
}

export function formatNetworkDateTime(value?: string): string {
  if (!value) {
    return "—";
  }

  try {
    return new Intl.DateTimeFormat("tr-TR", {
      dateStyle: "short",
      timeStyle: "short",
    }).format(new Date(value));
  } catch {
    return value;
  }
}

export function getNetworkLifecycleStatusLabel(status: string): string {
  switch (status) {
    case "active":
      return "Aktif";
    case "inactive":
      return "Pasif";
    default:
      return status;
  }
}

export function getNetworkLifecycleBadgeTone(
  status: string,
): "neutral" | "information" | "success" | "warning" | "danger" | "ai" {
  switch (status) {
    case "active":
      return "success";
    case "inactive":
      return "neutral";
    default:
      return "information";
  }
}

export function getAllocationStatusLabel(status: string): string {
  switch (status) {
    case "draft":
      return "Taslak";
    case "simulating":
      return "Simülasyon";
    case "ready":
      return "Hazır";
    case "approval-pending":
      return "Onay Bekliyor";
    case "approved":
      return "Onaylandı";
    case "executing":
      return "Uygulanıyor";
    case "published":
      return "Yayınlandı";
    case "verified":
      return "Doğrulandı";
    case "cancelled":
      return "İptal";
    default:
      return status;
  }
}

export function getAllocationStatusBadgeTone(
  status: string,
): "neutral" | "information" | "success" | "warning" | "danger" | "ai" {
  switch (status) {
    case "published":
    case "verified":
    case "approved":
      return "success";
    case "approval-pending":
    case "simulating":
    case "executing":
      return "warning";
    case "cancelled":
      return "danger";
    case "ready":
      return "information";
    default:
      return "neutral";
  }
}

export function getNetworkActionTypeLabel(actionType: string): string {
  switch (actionType) {
    case "keep-current-facility":
      return "Mevcut Fabrikayı Koru";
    case "move-full-order":
      return "Siparişi Tam Taşı";
    case "move-remaining-operations":
      return "Kalan Operasyonları Taşı";
    case "split-order":
      return "Siparişi Böl";
    case "move-future-batch":
      return "Gelecek Partiyi Taşı";
    case "temporary-capacity-shift":
      return "Geçici Kapasite Kaydırma";
    default:
      return actionType;
  }
}

export function getNetworkDecisionTypeLabel(decisionType: string): string {
  switch (decisionType) {
    case "cross-plant-reallocation":
      return "Tesisler Arası Yeniden Tahsis";
    case "order-split":
      return "Sipariş Bölme";
    case "network-capacity-recovery":
      return "Ağ Kapasite Kurtarma";
    case "facility-failover":
      return "Fabrika Yedekleme";
    default:
      return decisionType;
  }
}

export function getNetworkTriggerTypeLabel(triggerType: string): string {
  switch (triggerType) {
    case "capacity-overload":
      return "Kapasite Aşımı";
    case "deadline-risk":
      return "Termin Riski";
    case "material-shortage":
      return "Malzeme Eksikliği";
    case "machine-failure":
      return "Makine Arızası";
    case "quality-hold":
      return "Kalite Bekletme";
    case "manual":
      return "Manuel";
    case "scheduled-review":
      return "Planlı İnceleme";
    default:
      return triggerType;
  }
}

export function getNetworkAutonomyModeLabel(mode: string): string {
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
    case "proposed":
      return "Önerildi";
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

export function getDemandRiskLevelLabel(riskLevel: string): string {
  switch (riskLevel) {
    case "low":
      return "Düşük";
    case "medium":
      return "Orta";
    case "high":
      return "Yüksek";
    case "critical":
      return "Kritik";
    default:
      return riskLevel;
  }
}

export function getDemandRiskBadgeTone(
  riskLevel: string,
): "neutral" | "information" | "success" | "warning" | "danger" | "ai" {
  switch (riskLevel) {
    case "critical":
      return "danger";
    case "high":
      return "warning";
    case "medium":
      return "information";
    case "low":
      return "success";
    default:
      return "neutral";
  }
}

export function getPlanningStateLabel(state: string): string {
  switch (state) {
    case "stable":
      return "Dengeli";
    case "constrained":
      return "Kısıtlı";
    case "overloaded":
      return "Aşırı Yüklü";
    case "recovery":
      return "Kurtarma";
    default:
      return state;
  }
}

export function getActivityEventLabel(eventType: string): string {
  switch (eventType) {
    case "allocation-created":
      return "Tahsis Oluşturuldu";
    case "allocation-approved":
      return "Tahsis Onaylandı";
    case "allocation-executed":
      return "Tahsis Uygulandı";
    case "allocation-verified":
      return "Tahsis Doğrulandı";
    case "simulation-completed":
      return "Simülasyon Tamamlandı";
    case "policy-updated":
      return "Politika Güncellendi";
    case "opportunity-detected":
      return "Fırsat Tespit Edildi";
    default:
      return eventType;
  }
}

export function getFacilityRoleLabel(role: string): string {
  switch (role) {
    case "hub":
      return "Merkez";
    case "member":
      return "Üye";
    case "overflow":
      return "Taşma";
    default:
      return role;
  }
}

export function getRecommendedActionLabel(action: string): string {
  switch (action) {
    case "keep-current-allocation":
      return "Mevcut Tahsisi Koru";
    case "reallocate":
      return "Yeniden Tahsis Et";
    case "split-allocation":
      return "Tahsisi Böl";
    case "defer":
      return "Ertele";
    default:
      return action;
  }
}

export function getReadinessLabel(ready: boolean): string {
  return ready ? "Hazır" : "Eksik";
}
