import type {
  CommercialIntelligenceTabId,
  CommercialSalesPriceTabId,
  CommercialSupplierComparisonTabId,
} from "../types/commercial-intelligence-types";

export const COMMERCIAL_INTELLIGENCE_DEVELOPMENT_BANNER =
  "Örnek veri — gerçek tedarik fiyatı veya piyasa kanıtı değildir.";

export const COMMERCIAL_SALES_PRICE_DEVELOPMENT_BANNER =
  "Örnek veri — gerçek satış işlemi, fatura, teklif veya piyasa kanıtı değildir.";

const SALES_PRICE_PROVENANCE_LABELS: Record<string, string> = {
  "Deterministic local Development sales-price evidence — simulated, non-production, not real customer, invoice, quotation, or market evidence.":
    "Örnek satış fiyatı verisi — gerçek müşteri işlemi, fatura, teklif veya piyasa kanıtı değildir.",
  "Deterministic local Development commercial-intelligence data — simulated, non-production, not real customer evidence, market prices, or guaranteed savings.":
    "Örnek ticari veri — gerçek müşteri kanıtı, piyasa fiyatı veya garantili tasarruf değildir.",
};

export const COMMERCIAL_INTELLIGENCE_READONLY_NOTE =
  "Salt okunur erişim: profil düzenleme ve fiyat değerlendirme işlemleri devre dışıdır.";

export const COMMERCIAL_INTELLIGENCE_CURRENT_EVIDENCE_SCOPE =
  "Mevcut satın alma fiyat kanıtı penceresi";

export const COMMERCIAL_SUPPLIER_COMPARISON_SUBTITLE =
  "Karşılaştırılabilir satın alma fiyat kanıtına göre tedarikçi medyan konumlarını, yayılımı ve kanıt bağlamını izleyin.";

export const COMMERCIAL_SUPPLIER_COMPARISON_EVIDENCE_SCOPE =
  "Seçili tedarikçi karşılaştırma penceresi";

export const COMMERCIAL_SALES_PRICE_SUBTITLE =
  "Satış fiyatı kanıtlarını, dönemsel sapmaları ve müşteri bazlı karşılaştırmaları tek çalışma alanında izleyin.";

export const COMMERCIAL_SALES_PRICE_EVIDENCE_SCOPE =
  "Seçili satış fiyat kanıt penceresi";

export const REAL_TRANSACTION_MAPPING_NOTE =
  "Gerçek satış işlemi eşlemesi henüz tamamlanmadı. Eşleme kurulana kadar bu çalışma alanında değer gösterilmez.";

export const COMMERCIAL_SALES_PRICE_TAB_OPTIONS: ReadonlyArray<
  Readonly<{ id: CommercialSalesPriceTabId; label: string }>
> = [
  { id: "overview", label: "Genel Bakış" },
  { id: "temporal", label: "Dönemsel Sapma" },
  { id: "customers", label: "Müşteri Karşılaştırması" },
  { id: "history", label: "Değerlendirme Geçmişi" },
];

export const COMMERCIAL_SALES_PRICE_KPI_ORDER = [
  "comparable-sales-evidence",
  "excluded-evidence",
  "current-median-sales-price",
  "reference-median-sales-price",
  "attention-deviations",
  "compared-customers",
] as const;

export const COMMERCIAL_SUPPLIER_COMPARISON_TAB_OPTIONS: ReadonlyArray<
  Readonly<{ id: CommercialSupplierComparisonTabId; label: string }>
> = [
  { id: "overview", label: "Genel Bakış" },
  { id: "ranking", label: "Tedarikçi Sıralaması" },
  { id: "history", label: "Karşılaştırma Geçmişi" },
];

export const COMMERCIAL_SUPPLIER_COMPARISON_KPI_ORDER = [
  "comparable-suppliers",
  "comparable-observations",
  "excluded-observations",
  "price-spread",
  "tied-positions",
  "attention-evidence-issues",
] as const;

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
  "positive-price-variance": "Olumlu fiyat sapması",
  "insufficient-data": "Veri yetersiz",
  "source-conflict": "Kaynak uyuşmazlığı",
};

const COMPARISON_TYPE_LABELS: Record<string, string> = {
  "temporal-price-deviation": "Dönemsel fiyat sapması",
  "supplier-price-position": "Tedarikçi fiyat konumu",
  "temporal-sales-price-deviation": "Dönemsel satış fiyat sapması",
  "customer-price-position": "Müşteri fiyat konumu",
};

const COMPARISON_DIMENSION_LABELS: Record<string, string> = {
  material: "Malzeme",
  "material-supplier": "Malzeme ve tedarikçi",
  "material-customer": "Malzeme ve müşteri",
  "purchase-order-line": "Satın alma siparişi satırı",
  "sales-order-line": "Satış siparişi satırı",
  facility: "Tesis",
};

const EVIDENCE_STATUS_LABELS: Record<string, string> = {
  "measured-transaction": "Ölçülmüş işlem",
  calculated: "Hesaplanmış",
  simulated: "Örnek veri",
  "insufficient-data": "Veri yetersiz",
  "source-conflict": "Kaynak uyuşmazlığı",
};

/** Neutral wording for a tenant without any recorded commercial data: never "sample data". */
export const COMMERCIAL_NO_SOURCE_LABEL = "Kayıtlı veri yok";
export const COMMERCIAL_NO_EVIDENCE_LABEL = "Henüz kanıt yok";

/** True for provenance text that describes development or sample data. */
export function isCommercialSampleProvenance(value: string | null | undefined): boolean {
  if (!value) return false;
  return /Deterministic local Development|simulated|Örnek veri|Örnek /i.test(value);
}

/** Provenance line for a workspace: sample-data wording is shown only when the tenant really holds pilot data. */
export function resolveCommercialProvenance(
  value: string | null | undefined,
  hasPilotData: boolean,
  localize: (raw: string | null | undefined) => string = (raw) => raw ?? "",
): string {
  if (!hasPilotData && isCommercialSampleProvenance(value)) return COMMERCIAL_NO_SOURCE_LABEL;
  return localize(value);
}

/** Workspace-level evidence label: "sample data" only for real pilot data, otherwise a neutral truthful label. */
export function getCommercialWorkspaceEvidenceLabel(hasPilotData: boolean, hasRecords = false): string {
  if (hasPilotData) return EVIDENCE_STATUS_LABELS.simulated;
  return hasRecords ? EVIDENCE_STATUS_LABELS.calculated : COMMERCIAL_NO_EVIDENCE_LABEL;
}

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
  "sales-price-increase": "Satış fiyatı artışı",
  "sales-price-decrease": "Satış fiyatı düşüşü",
  "favorable-price-variance": "Olumlu fiyat sapması",
  "positive-price-variance": "Olumlu fiyat sapması",
  "supplier-price-spread": "Tedarikçi fiyat yayılımı",
  "customer-price-spread": "Müşteri fiyat yayılımı",
  "insufficient-comparable-observations": "Yetersiz karşılaştırılabilir gözlem",
  "incompatible-currency": "Uyumsuz para birimi",
  "incompatible-unit": "Uyumsuz birim",
  "incompatible-price-basis": "Uyumsuz fiyat bazı",
  "incompatible-evidence-kind": "Uyumsuz kanıt türü",
  "incompatible-gross-net-basis": "Uyumsuz brüt/net bazı",
  "missing-reference-price": "Referans fiyat eksik",
  "duplicate-source-evidence": "Yinelenen kaynak kanıtı",
  "source-conflict": "Kaynak uyuşmazlığı",
  "current-window-price-increase": "Güncel pencerede fiyat artışı",
  "current-window-favorable-variance": "Güncel pencerede olumlu sapma",
  "supplier-lowest-comparable-median": "En düşük karşılaştırılabilir medyan",
  "supplier-higher-comparable-median": "Daha yüksek karşılaştırılabilir medyan",
  "supplier-median-tie": "Tedarikçi medyan eşitliği",
  "customer-lowest-comparable-median": "En düşük karşılaştırılabilir müşteri medyanı",
  "customer-higher-comparable-median": "Daha yüksek karşılaştırılabilir müşteri medyanı",
  "customer-median-tie": "Müşteri medyan eşitliği",
  "insufficient-current-window": "Güncel pencere yetersiz",
  "insufficient-reference-window": "Referans pencere yetersiz",
  "insufficient-supplier-observations": "Tedarikçi gözlemleri yetersiz",
  "insufficient-customer-observations": "Müşteri gözlemleri yetersiz",
  "single-comparable-supplier": "Tek karşılaştırılabilir tedarikçi",
  "single-comparable-customer": "Tek karşılaştırılabilir müşteri",
  "missing-reference-median": "Referans medyan eksik",
  "customer-mismatch": "Müşteri uyuşmazlığı",
  "cancelled-or-returned": "İptal veya iade",
};

const EXCLUSION_REASON_LABELS: Record<string, string> = {
  "incompatible-currency": "Uyumsuz para birimi",
  "incompatible-unit": "Uyumsuz birim",
  "incompatible-price-basis": "Uyumsuz fiyat bazı",
  "incompatible-evidence-kind": "Uyumsuz kanıt türü",
  "incompatible-gross-net-basis": "Uyumsuz brüt/net bazı",
  "duplicate-source-evidence": "Yinelenen kaynak kanıtı",
  "outside-window": "Pencere dışı",
  "disallowed-evidence-status": "İzin verilmeyen kanıt durumu",
  "invalid-quantity-or-price": "Geçersiz miktar veya fiyat",
  "supplier-mismatch": "Tedarikçi uyuşmazlığı",
  "customer-mismatch": "Müşteri uyuşmazlığı",
  "cancelled-or-returned": "İptal veya iade",
};

const SOURCE_EVIDENCE_KIND_LABELS: Record<string, string> = {
  "sales-order-line": "Satış siparişi satırı",
  "invoice-line": "Fatura satırı",
  "quotation-line": "Teklif satırı",
  "manual-development-simulator": "Manuel örnek veri",
};

const GROSS_NET_BASIS_LABELS: Record<string, string> = {
  gross: "Brüt",
  net: "Net",
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

export function getCommercialIntelligenceSourceEvidenceKindLabel(value: string | null | undefined): string {
  return lookupLabel(SOURCE_EVIDENCE_KIND_LABELS, value);
}

export function getCommercialIntelligenceGrossNetBasisLabel(value: string | null | undefined): string {
  return lookupLabel(GROSS_NET_BASIS_LABELS, value);
}

export function localizeCommercialSalesPriceProvenance(value: string | null | undefined): string {
  if (value === null || value === undefined) return "";
  const trimmed = value.trim();
  if (!trimmed) return "";
  const mapped = SALES_PRICE_PROVENANCE_LABELS[trimmed];
  if (mapped) return mapped;
  if (trimmed.includes("Deterministic local Development")) {
    if (trimmed.toLowerCase().includes("sales-price")) {
      return SALES_PRICE_PROVENANCE_LABELS[
        "Deterministic local Development sales-price evidence — simulated, non-production, not real customer, invoice, quotation, or market evidence."
      ];
    }
    return "Örnek veri — gerçek müşteri verisi değildir.";
  }
  return trimmed;
}

export const COMMERCIAL_INTELLIGENCE_OUTCOME_COLORS: Record<string, string> = {
  "within-reference-range": "#16a34a",
  attention: "#d97706",
  "favorable-variance": "#2563eb",
  "insufficient-data": "#64748b",
  "source-conflict": "#7c3aed",
};
