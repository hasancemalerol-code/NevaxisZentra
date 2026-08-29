export const COMMERCIAL_PROFITABILITY_COMMAND_CENTER_SUBTITLE =
  "Yönetim karar merkezi; kârlılık uyarıları, fiyat koruma senaryoları, kanıt incelemeleri ve bağlı AI görevlerini tek okuma yüzeyinde birleştirir.";

export const COMMERCIAL_PROFITABILITY_COMMAND_CENTER_SCOPE_NOTE =
  "Bu sayfa yalnızca okuma ve derin bağlantı sağlar; onay, geçiş veya görev aksiyonu buradan yapılmaz.";

export const COMMERCIAL_PROFITABILITY_COMMAND_CENTER_EMPTY_QUEUE_NOTE =
  "Seçili kapsamda öncelikli insan kararı gerektiren kayıt bulunamadı.";

export const COMMERCIAL_PROFITABILITY_COMMAND_CENTER_PARTIAL_NOTE =
  "Bazı özetler sayfalanmış kaynak listelerinden türetildi; tam küresel toplam iddia edilmez.";

export const COMMERCIAL_PROFITABILITY_COMMAND_CENTER_AI_TASK_PARTIAL_NOTE =
  "Bağlı AI görev sayısı için ai-task.read yetkisi gerekir; yetki yoksa kısmi kapsam gösterilir.";

export const COMMERCIAL_PROFITABILITY_COMMAND_CENTER_ROUTE_PATH =
  "/ticari-zeka/karlilik-yonetim-merkezi";

const SOURCE_TYPE_LABELS: Record<string, string> = {
  alert: "Kârlılık uyarısı",
  "pricing-scenario": "Fiyat senaryosu",
  "ai-task": "AI görevi",
  "evidence-review": "Kanıt incelemesi",
};

export function getCommercialProfitabilityCommandCenterSourceTypeLabel(
  sourceType: string,
): string {
  return SOURCE_TYPE_LABELS[sourceType] ?? sourceType;
}
