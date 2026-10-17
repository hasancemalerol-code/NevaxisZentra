export const COMMERCIAL_PROFITABILITY_SUBTITLE =
  "Ürün kârlılığı karşılaştırma zekâsı; referans ve güncel dönem katkı marjı, maliyet bileşeni ve konum farklarını kayıtlı verilere dayanarak gösterir.";

export const COMMERCIAL_PROFITABILITY_DEVELOPMENT_BANNER =
  "Gerçek gelir ve maliyet veri bağlantısı henüz tamamlanmadı. Bağlantı kurulana kadar otomatik fiyat veya kârlılık kararı üretilmez.";

export const COMMERCIAL_PROFITABILITY_READONLY_NOTE =
  "Bu çalışma alanı salt okunurdur. Karşılaştırma sonuçları arka uçta hesaplanır; arayüzde yeniden hesaplama yapılmaz.";

export const COMMERCIAL_PROFITABILITY_EVIDENCE_SCOPE =
  "Katkı marjı, birim katkı ve maliyet payı yalnızca karşılaştırılabilir değerlendirme kapsamında raporlanır.";

export const REAL_PROFITABILITY_MAPPING_NOTE =
  "Gerçek kârlılık eşlemesi tamamlanmadı; eksik veya çakışan kanıtlar açıkça işaretlenir.";

export const COMMERCIAL_PROFITABILITY_TAB_OPTIONS = [
  { id: "overview", label: "Genel Bakış" },
  { id: "temporal", label: "Kârlılık Eğilimleri" },
  { id: "cost-mix", label: "Maliyet Bileşeni" },
  { id: "positions", label: "Konum Karşılaştırması" },
] as const;

const COMPARISON_TYPE_LABELS: Record<string, string> = {
  "temporal-contribution-trend": "Zamansal katkı eğilimi",
  "cost-component-mix-change": "Maliyet bileşeni karışımı",
  "customer-contribution-position": "Müşteri katkı konumu",
  "material-contribution-position": "Ürün katkı konumu",
};

const OUTCOME_LABELS: Record<string, string> = {
  "within-comparison-band": "Karşılaştırma bandı içinde",
  "contribution-deterioration": "Katkı bozulması",
  "contribution-improvement": "Katkı iyileşmesi",
  "insufficient-data": "Yetersiz veri",
  "source-conflict": "Kaynak çakışması",
};

const POSITION_STATE_LABELS: Record<string, string> = {
  "higher-observed-contribution-position": "Daha yüksek gözlenen katkı konumu",
  "lower-observed-contribution-position": "Daha düşük gözlenen katkı konumu",
  "within-observed-contribution-band": "Gözlenen katkı bandı içinde",
  "tied-contribution-position": "Eşit katkı konumu",
  "insufficient-data": "Yetersiz veri",
  "source-conflict": "Kaynak çakışması",
};

const DRIVER_REASON_LABELS: Record<string, string> = {
  "contribution-margin-deterioration": "Katkı marjı bozulması",
  "contribution-margin-improvement": "Katkı marjı iyileşmesi",
  "negative-contribution-concentration": "Negatif katkı yoğunluğu",
  "below-threshold-concentration": "Eşik altı yoğunluk",
  "direct-material-cost-increase": "Direkt malzeme maliyeti artışı",
  "direct-labor-cost-increase": "Direkt işçilik maliyeti artışı",
  "machine-cost-increase": "Makine maliyeti artışı",
  "energy-cost-increase": "Enerji maliyeti artışı",
  "tooling-cost-increase": "Kalıp/alet maliyeti artışı",
  "quality-cost-increase": "Kalite maliyeti artışı",
  "scrap-rework-cost-increase": "Hurda/yeniden işleme maliyeti artışı",
  "packaging-cost-increase": "Ambalaj maliyeti artışı",
  "logistics-cost-increase": "Lojistik maliyeti artışı",
  "allocated-overhead-cost-increase": "Dağıtılmış genel gider artışı",
  "other-cost-increase": "Diğer maliyet artışı",
  "cost-share-increase": "Maliyet payı artışı",
  "customer-contribution-concentration": "Müşteri katkı yoğunlaşması",
  "material-contribution-concentration": "Ürün katkı yoğunlaşması",
  "contribution-position-tie": "Eşit katkı konumu",
  "insufficient-reference-window": "Yetersiz referans penceresi",
  "insufficient-current-window": "Yetersiz güncel penceresi",
  "insufficient-scope-evidence": "Yetersiz kapsam kanıtı",
  "incomplete-cost-coverage": "Eksik maliyet kapsamı",
  "source-conflict": "Kaynak çakışması",
  "incompatible-currency": "Uyumsuz para birimi",
  "incompatible-unit": "Uyumsuz birim",
  "incompatible-price-basis": "Uyumsuz fiyat esası",
  "within-comparison-band": "Karşılaştırma bandı içinde",
};

const COMPONENT_KIND_LABELS: Record<string, string> = {
  "direct-material": "Direkt malzeme",
  "direct-labor": "Direkt işçilik",
  machine: "Makine",
  energy: "Enerji",
};

export function getCommercialProfitabilityComparisonTypeLabel(value: string | null | undefined): string {
  if (!value) return "—";
  return COMPARISON_TYPE_LABELS[value] ?? value;
}

export function getCommercialProfitabilityOutcomeLabel(value: string | null | undefined): string {
  if (!value) return "—";
  return OUTCOME_LABELS[value] ?? value;
}

export function getCommercialProfitabilityPositionStateLabel(value: string | null | undefined): string {
  if (!value) return "—";
  return POSITION_STATE_LABELS[value] ?? value;
}

export function getCommercialProfitabilityDriverReasonLabel(value: string | null | undefined): string {
  if (!value) return "—";
  return DRIVER_REASON_LABELS[value] ?? value;
}

export function getCommercialProfitabilityComponentKindLabel(value: string | null | undefined): string {
  if (!value) return "—";
  return COMPONENT_KIND_LABELS[value] ?? value;
}

export function localizeCommercialProfitabilityProvenance(value: string | null | undefined): string {
  if (!value?.trim()) return "—";
  if (value.includes("simulated") || value.includes("simülasyon") || value.includes("Development")) {
    return "Örnek veri";
  }
  return value;
}

export function inferProfitabilityConfidenceTone(
  outcome: string,
  evidenceStatus: string,
): "neutral" | "positive" | "negative" | "warning" {
  if (outcome === "source-conflict" || evidenceStatus === "source-conflict") return "warning";
  if (outcome === "insufficient-data" || evidenceStatus === "insufficient-data") return "warning";
  if (outcome === "contribution-improvement") return "positive";
  if (outcome === "contribution-deterioration") return "negative";
  return "neutral";
}
