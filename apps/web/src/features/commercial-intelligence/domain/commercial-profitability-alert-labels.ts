export const COMMERCIAL_PROFITABILITY_ALERT_SUBTITLE =
  "Kârlılık uyarıları; kayıtlı karşılaştırma kanıtına dayalı bozulma ve inceleme sinyallerini insan onaylı iş akışında gösterir.";

export const COMMERCIAL_PROFITABILITY_ALERT_SCOPE_NOTE =
  "Uyarılar otomatik ticari karar üretmez; yalnızca inceleme, görev taslağı ve yaşam döngüsü geçişleri desteklenir.";

export const COMMERCIAL_PROFITABILITY_ALERT_EMPTY_NOTE =
  "Seçili filtreler için kârlılık uyarısı bulunamadı.";

export const COMMERCIAL_PROFITABILITY_ALERT_EMPTY_MANAGE_HINT =
  " Uyarıları oluşturmak için yönetim yetkisi gerekir.";

export const COMMERCIAL_PROFITABILITY_ALERT_NO_SELECTION_NOTE =
  "Detay görmek için tablodan bir kârlılık uyarısı seçin.";

export const COMMERCIAL_PROFITABILITY_ALERT_AUTOMATION_DISCLAIMER =
  "Otomatik ticari işlem yapılmaz.";

const CATEGORY_LABELS: Record<string, string> = {
  "product-contribution-deterioration": "Ürün katkı bozulması",
  "customer-contribution-deterioration": "Müşteri katkı bozulması",
  "material-contribution-deterioration": "Malzeme katkı bozulması",
  "cost-mix-deterioration": "Maliyet karışımı bozulması",
  "data-review": "Veri incelemesi",
};

const SEVERITY_LABELS: Record<string, string> = {
  low: "Düşük",
  medium: "Orta",
  high: "Yüksek",
  critical: "Kritik",
};

const LIFECYCLE_LABELS: Record<string, string> = {
  active: "Aktif",
  acknowledged: "Onaylandı",
  "task-created": "Görev oluşturuldu",
  dismissed: "Reddedildi",
  resolved: "Çözüldü",
};

const AUDIT_EVENT_LABELS: Record<string, string> = {
  materialized: "Materyalize edildi",
  refreshed: "Yenilendi",
  acknowledged: "Onaylandı",
  dismissed: "Reddedildi",
  "task-linked": "Görev bağlandı",
  resolved: "Çözüldü",
};

export function getCommercialProfitabilityAlertCategoryLabel(category: string | null | undefined): string {
  if (!category) return "—";
  return CATEGORY_LABELS[category] ?? category;
}

export function getCommercialProfitabilityAlertSeverityLabel(severity: string | null | undefined): string {
  if (!severity) return "—";
  return SEVERITY_LABELS[severity] ?? severity;
}

export function getCommercialProfitabilityAlertLifecycleLabel(status: string | null | undefined): string {
  if (!status) return "—";
  return LIFECYCLE_LABELS[status] ?? status;
}

export function getCommercialProfitabilityAlertAuditEventLabel(eventType: string | null | undefined): string {
  if (!eventType) return "—";
  return AUDIT_EVENT_LABELS[eventType] ?? eventType;
}
