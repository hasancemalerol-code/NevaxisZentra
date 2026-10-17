export const CONTINUOUS_IMPROVEMENT_TITLE = "Sürekli İyileştirme";

export const CONTINUOUS_IMPROVEMENT_SUBTITLE =
  "Fabrika kayıpları, tekrarlayan sorunlar ve doğrulanmış iyileştirmeler";

export function getFactoryLossCategoryLabel(category: string): string {
  switch (category) {
    case "downtime":
      return "Duruş";
    case "micro-stop":
      return "Mikro Duruş";
    case "speed-loss":
      return "Hız Kaybı";
    case "scrap":
      return "Fire";
    case "rework":
      return "Yeniden İşleme";
    case "quality-hold":
      return "Kalite Bekletme";
    case "changeover":
      return "Kalıp/Kurulum Değişimi";
    case "material-shortage":
      return "Malzeme Eksikliği";
    case "supplier-delay":
      return "Tedarik Gecikmesi";
    case "mold-unavailability":
      return "Kalıp Uygun Değil";
    case "maintenance-delay":
      return "Bakım Gecikmesi";
    case "capacity-loss":
      return "Kapasite Kaybı";
    case "planning-loss":
      return "Planlama Kaybı";
    case "waiting-approval":
      return "Onay Bekleme";
    case "shift-handover-loss":
      return "Vardiya Devir Kaybı";
    case "repeated-incident":
      return "Tekrarlayan Olay";
    case "other":
      return "Diğer";
    default:
      return category;
  }
}

export function getImprovementBacklogStatusLabel(status: string): string {
  switch (status) {
    case "new":
      return "Yeni";
    case "analyzing":
      return "Analiz Ediliyor";
    case "proposed":
      return "Önerildi";
    case "approval-pending":
      return "Onay Bekliyor";
    case "experimenting":
      return "Deney Sürüyor";
    case "verifying":
      return "Doğrulanıyor";
    case "verified":
      return "Doğrulandı";
    case "standardized":
      return "Standartlaştırıldı";
    case "rejected":
      return "Reddedildi";
    case "inconclusive":
      return "Sonuçsuz";
    case "regressing":
      return "Geriliyor";
    default:
      return status;
  }
}

export function getFactoryExperimentStatusLabel(status: string): string {
  switch (status) {
    case "draft":
      return "Taslak";
    case "approval-pending":
      return "Onay Bekliyor";
    case "approved":
      return "Onaylandı";
    case "running":
      return "Devam Ediyor";
    case "stopped":
      return "Durduruldu";
    case "completed":
      return "Tamamlandı";
    case "verified":
      return "Doğrulandı";
    case "failed":
      return "Başarısız";
    case "inconclusive":
      return "Sonuçsuz";
    default:
      return status;
  }
}

export function getFactoryExperimentTypeLabel(type: string): string {
  switch (type) {
    case "process":
      return "Proses";
    case "planning":
      return "Planlama";
    case "maintenance":
      return "Bakım";
    case "quality":
      return "Kalite";
    case "material":
      return "Malzeme";
    case "mold":
      return "Kalıp";
    case "shift":
      return "Vardiya";
    case "supplier":
      return "Tedarik";
    default:
      return type;
  }
}

export function getFactoryPatternTypeLabel(patternType: string): string {
  switch (patternType) {
    case "recurring-loss":
      return "Tekrarlayan Kayıp";
    case "machine-fault-recurrence":
      return "Makine Arıza Tekrarı";
    default:
      return patternType;
  }
}

export function getSustainabilityStatusLabel(status: string): string {
  switch (status) {
    case "pending":
      return "İzleniyor";
    case "sustained":
      return "Sürdürülüyor";
    case "regressing":
      return "Geriliyor";
    default:
      return status;
  }
}

export function getFactoryLearningImpactBadgeLabel(classification: string): string {
  switch (classification) {
    case "measured":
      return "DOĞRULANDI";
    case "estimated":
    case "projected":
    case "derived":
      return "TAHMİN";
    case "unknown":
      return "YETERSİZ VERİ";
    default:
      return classification.toUpperCase();
  }
}

export function getFactoryLearningImpactBadgeClass(classification: string): string {
  switch (classification) {
    case "measured":
      return "commint-badge commint-badge--favorable";
    case "estimated":
    case "projected":
    case "derived":
      return "commint-badge commint-badge--attention";
    case "unknown":
      return "commint-badge commint-badge--conflict";
    default:
      return "commint-badge";
  }
}

export function formatLossMinutes(minutes: number): string {
  if (minutes >= 60) {
    const hours = Math.floor(minutes / 60);
    const remainder = Math.round(minutes % 60);
    return remainder > 0 ? `${hours} sa ${remainder} dk` : `${hours} sa`;
  }
  return `${Math.round(minutes)} dk`;
}
