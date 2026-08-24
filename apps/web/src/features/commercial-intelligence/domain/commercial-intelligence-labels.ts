import type { CommercialIntelligenceTabId } from "../types/commercial-intelligence-types";

export const COMMERCIAL_INTELLIGENCE_DEVELOPMENT_BANNER =
  "Yerel geliştirme / simülasyon kanıtı — gerçek müşteri tedarik fiyatı veya piyasa kanıtı değildir.";

export const COMMERCIAL_INTELLIGENCE_READONLY_NOTE =
  "Salt okunur erişim: profil düzenleme ve fiyat değerlendirme işlemleri devre dışıdır.";

export const COMMERCIAL_INTELLIGENCE_CURRENT_EVIDENCE_SCOPE =
  "Mevcut satın alma fiyat kanıtı penceresi";

export const COMMERCIAL_INTELLIGENCE_TAB_OPTIONS: ReadonlyArray<
  Readonly<{ id: CommercialIntelligenceTabId; label: string }>
> = [
  { id: "overview", label: "Genel Bakış" },
  { id: "analyses", label: "Fiyat Analizleri" },
  { id: "suppliers", label: "Tedarikçi Karşılaştırması" },
  { id: "profiles", label: "Analiz Profilleri" },
];

export const COMMERCIAL_INTELLIGENCE_KPI_ORDER = [
  "comparable-observations",
  "excluded-observations",
  "attention-analyses",
  "favorable-variances",
  "supplier-spread",
  "insufficient-conflict",
] as const;

const OUTCOME_LABELS: Record<string, string> = {
  "within-reference-range": "Referans aralığında",
  attention: "Dikkat gerektirir",
  "favorable-variance": "Olumlu sapma",
  "insufficient-data": "Veri yetersiz",
  "source-conflict": "Kaynak uyuşmazlığı",
};

const COMPARISON_TYPE_LABELS: Record<string, string> = {
  "temporal-price-deviation": "Dönemsel fiyat sapması",
  "supplier-price-position": "Tedarikçi fiyat konumu",
};

const COMPARISON_DIMENSION_LABELS: Record<string, string> = {
  material: "Malzeme",
  "material-supplier": "Malzeme ve tedarikçi",
  "purchase-order-line": "Satın alma siparişi satırı",
  facility: "Tesis",
};

const EVIDENCE_STATUS_LABELS: Record<string, string> = {
  "measured-transaction": "Ölçülmüş işlem",
  calculated: "Hesaplanmış",
  simulated: "Simülasyon",
  "insufficient-data": "Veri yetersiz",
  "source-conflict": "Kaynak uyuşmazlığı",
};

const PROFILE_STATUS_LABELS: Record<string, string> = {
  draft: "Taslak",
  approved: "Onaylı",
  inactive: "Pasif",
};

const POSITION_STATE_LABELS: Record<string, string> = {
  "lowest-comparable-price": "En düşük karşılaştırılabilir fiyat",
  "within-spread-threshold": "Yayılım eşiği içinde",
  "higher-comparable-price": "Daha yüksek karşılaştırılabilir fiyat",
  "tied-comparable-price": "Eşit karşılaştırılabilir fiyat",
  "insufficient-data": "Veri yetersiz",
  "source-conflict": "Kaynak uyuşmazlığı",
};

const PRICE_BASIS_LABELS: Record<string, string> = {
  excluded: "Hariç",
  included: "Dahil",
  unknown: "Belirsiz",
};

const DRIVER_REASON_LABELS: Record<string, string> = {
  "within-reference-range": "Referans aralığında",
  "purchase-price-increase": "Satın alma fiyatı artışı",
  "favorable-price-variance": "Olumlu fiyat sapması",
  "supplier-price-spread": "Tedarikçi fiyat yayılımı",
  "insufficient-comparable-observations": "Yetersiz karşılaştırılabilir gözlem",
  "incompatible-currency": "Uyumsuz para birimi",
  "incompatible-unit": "Uyumsuz birim",
  "incompatible-price-basis": "Uyumsuz fiyat bazı",
  "missing-reference-price": "Referans fiyat eksik",
  "duplicate-source-evidence": "Yinelenen kaynak kanıtı",
  "source-conflict": "Kaynak uyuşmazlığı",
  "current-window-price-increase": "Güncel pencerede fiyat artışı",
  "current-window-favorable-variance": "Güncel pencerede olumlu sapma",
  "supplier-lowest-comparable-median": "En düşük karşılaştırılabilir medyan",
  "supplier-higher-comparable-median": "Daha yüksek karşılaştırılabilir medyan",
  "supplier-median-tie": "Tedarikçi medyan eşitliği",
  "insufficient-current-window": "Güncel pencere yetersiz",
  "insufficient-reference-window": "Referans pencere yetersiz",
  "insufficient-supplier-observations": "Tedarikçi gözlemleri yetersiz",
  "single-comparable-supplier": "Tek karşılaştırılabilir tedarikçi",
  "missing-reference-median": "Referans medyan eksik",
};

const EXCLUSION_REASON_LABELS: Record<string, string> = {
  "incompatible-currency": "Uyumsuz para birimi",
  "incompatible-unit": "Uyumsuz birim",
  "incompatible-price-basis": "Uyumsuz fiyat bazı",
  "duplicate-source-evidence": "Yinelenen kaynak kanıtı",
  "outside-window": "Pencere dışı",
  "disallowed-evidence-status": "İzin verilmeyen kanıt durumu",
  "invalid-quantity-or-price": "Geçersiz miktar veya fiyat",
  "supplier-mismatch": "Tedarikçi uyuşmazlığı",
};

const UNAVAILABLE_DIMENSION_LABELS: Record<string, string> = {
  delivery: "Teslimat",
  quality: "Kalite",
  service: "Hizmet",
  "credit-terms": "Kredi koşulları",
  "lead-time": "Tedarik süresi",
};

const SEVERITY_LABELS: Record<string, string> = {
  low: "Düşük",
  moderate: "Orta",
  high: "Yüksek",
  critical: "Kritik",
};

function humanizeToken(value: string): string {
  return value
    .split(/[-_.]/)
    .filter(Boolean)
    .map((part) => part.charAt(0).toUpperCase() + part.slice(1))
    .join(" ");
}

function lookupLabel(map: Record<string, string>, value: string | null | undefined): string {
  if (!value?.trim()) return "Belirtilmedi";
  return map[value.trim()] ?? humanizeToken(value);
}

export function getCommercialIntelligenceOutcomeLabel(value: string | null | undefined): string {
  return lookupLabel(OUTCOME_LABELS, value);
}

export function getCommercialIntelligenceComparisonTypeLabel(value: string | null | undefined): string {
  return lookupLabel(COMPARISON_TYPE_LABELS, value);
}

export function getCommercialIntelligenceComparisonDimensionLabel(value: string | null | undefined): string {
  return lookupLabel(COMPARISON_DIMENSION_LABELS, value);
}

export function getCommercialIntelligenceEvidenceStatusLabel(value: string | null | undefined): string {
  return lookupLabel(EVIDENCE_STATUS_LABELS, value);
}

export function getCommercialIntelligenceProfileStatusLabel(value: string | null | undefined): string {
  return lookupLabel(PROFILE_STATUS_LABELS, value);
}

export function getCommercialIntelligencePositionStateLabel(value: string | null | undefined): string {
  return lookupLabel(POSITION_STATE_LABELS, value);
}

export function getCommercialIntelligencePriceBasisLabel(value: string | null | undefined): string {
  return lookupLabel(PRICE_BASIS_LABELS, value);
}

export function getCommercialIntelligenceDriverReasonLabel(value: string | null | undefined): string {
  return lookupLabel(DRIVER_REASON_LABELS, value);
}

export function getCommercialIntelligenceExclusionReasonLabel(value: string | null | undefined): string {
  return lookupLabel(EXCLUSION_REASON_LABELS, value);
}

export function getCommercialIntelligenceUnavailableDimensionLabel(value: string | null | undefined): string {
  return lookupLabel(UNAVAILABLE_DIMENSION_LABELS, value);
}

export function getCommercialIntelligenceSeverityLabel(value: string | null | undefined): string {
  return lookupLabel(SEVERITY_LABELS, value);
}

export const COMMERCIAL_INTELLIGENCE_OUTCOME_COLORS: Record<string, string> = {
  "within-reference-range": "#16a34a",
  attention: "#d97706",
  "favorable-variance": "#2563eb",
  "insufficient-data": "#64748b",
  "source-conflict": "#7c3aed",
};
