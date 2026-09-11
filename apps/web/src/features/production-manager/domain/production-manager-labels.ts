export const PRODUCTION_MANAGER_TITLE = "AI Üretim Yöneticisi";

export const PRODUCTION_MANAGER_SUBTITLE =
  "Tahmin, senaryo simülasyonu ve kontrollü karar akışı";

export function getProductionManagerConfidenceLabel(band: string): string {
  switch (band) {
    case "high":
      return "Yüksek Güven";
    case "medium":
      return "Orta Güven";
    case "low":
      return "Düşük Güven";
    case "insufficient-data":
      return "Yetersiz Veri";
    default:
      return band;
  }
}

export function getProductionManagerAutonomyLabel(mode: string): string {
  switch (mode) {
    case "observe":
      return "İZLE";
    case "draft":
      return "ÖNER";
    case "limited":
      return "SINIRLI OTONOM";
    case "full":
      return "TAM OTONOM";
    default:
      return mode;
  }
}

export function getProductionManagerStatusLabel(status: string): string {
  switch (status) {
    case "pending-approval":
      return "Onay Bekliyor";
    case "approved":
      return "Onaylandı";
    case "executing":
      return "Uygulanıyor";
    case "verified":
      return "Doğrulandı";
    case "failed":
      return "Başarısız";
    case "proposed":
      return "Önerildi";
    default:
      return status;
  }
}

export function getProductionManagerDecisionTypeLabel(type: string): string {
  switch (type) {
    case "order-delay":
      return "Termin Riski";
    case "machine-risk":
      return "Makine Riski";
    case "material-depletion":
      return "Malzeme Tükenmesi";
    case "quality-escalation":
      return "Kalite Eskalasyonu";
    case "mold-risk":
      return "Kalıp Riski";
    case "capacity-overload":
      return "Kapasite Aşımı";
    default:
      return type;
  }
}
