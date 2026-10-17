import type { CommercialQuotationRiskTabId } from "../types/commercial-quotation-risk-types";

export const COMMERCIAL_QUOTATION_RISK_SUBTITLE =
  "Teklif fiyatı, geçerlilik, ödeme vadesi, risk eğilimi ve müşteri referansı konumlarını kayıtlı kanıt ve karşılaştırılan toplamla izleyin.";

export const COMMERCIAL_QUOTATION_RISK_DEVELOPMENT_BANNER =
  "Gerçek teklif veri bağlantısı henüz tamamlanmadı. Bağlantı kurulana kadar müşteri, teklif, sipariş, fatura veya piyasa verisi gösterilmez.";

export const COMMERCIAL_QUOTATION_RISK_READONLY_NOTE =
  "Salt okunur erişim: risk profili düzenleme devre dışıdır.";

export const COMMERCIAL_QUOTATION_RISK_EVIDENCE_SCOPE =
  "Seçili teklif risk kanıt penceresi";

export const REAL_QUOTATION_MAPPING_NOTE =
  "Gerçek teklif veri eşlemesi henüz tamamlanmadı. Eşleme kurulana kadar bu çalışma alanında değer gösterilmez.";

export const COMMERCIAL_QUOTATION_RISK_TAB_OPTIONS: ReadonlyArray<
  Readonly<{ id: CommercialQuotationRiskTabId; label: string }>
> = [
  { id: "overview", label: "Genel Bakış" },
  { id: "assessments", label: "Değerlendirmeler" },
  { id: "trends", label: "Risk Eğilimleri" },
  { id: "customers", label: "Müşteri Karşılaştırması" },
  { id: "profile", label: "Risk Profili" },
];

export const COMMERCIAL_QUOTATION_RISK_KPI_ORDER = [
  "candidate-assessments",
  "attention-assessments",
  "within-threshold-assessments",
  "insufficient-or-conflict",
  "current-risk-incidence",
] as const;

const QUOTATION_RISK_PROVENANCE_LABELS: Record<string, string> = {
  "Deterministic local Development quotation-risk evidence — simulated, non-production, not real customer, quotation, invoice, order, or market evidence.":
    "Örnek teklif riski verisi — gerçek müşteri, teklif, sipariş, fatura veya piyasa verisi değildir.",
};

const QUOTATION_RISK_OUTCOME_LABELS: Record<string, string> = {
  "within-threshold": "Eşik içinde",
  attention: "Dikkat gerektirir",
  "insufficient-data": "Yetersiz veri",
  "source-conflict": "Kaynak çakışması",
  "within-risk-band": "Risk bandı içinde",
  "risk-increase": "Risk artışı",
  "risk-decrease": "Risk azalışı",
};

const QUOTATION_RISK_COMPARISON_TYPE_LABELS: Record<string, string> = {
  "temporal-risk-trend": "Zamansal risk eğilimi",
  "customer-risk-position": "Müşteri risk konumu",
};

const QUOTATION_RISK_POSITION_STATE_LABELS: Record<string, string> = {
  "lowest-observed-risk-position": "En düşük gözlenen risk konumu",
  "within-risk-band": "Risk bandı içinde",
  "higher-observed-risk-position": "Daha yüksek gözlenen risk konumu",
  "tied-risk-position": "Eşit risk konumu",
  "insufficient-data": "Yetersiz veri",
  "source-conflict": "Kaynak çakışması",
};

const QUOTATION_RISK_DRIVER_LABELS: Record<string, string> = {
  "quoted-price-below-reference": "Teklif fiyatı referansın altında",
  "quoted-price-above-reference": "Teklif fiyatı referansın üstünde",
  "quotation-expired": "Teklif süresi doldu",
  "quotation-expiring-soon": "Teklif süresi yakında doluyor",
  "payment-term-exceeded": "Ödeme vadesi aşıldı",
  "insufficient-comparable-sales-evidence": "Yetersiz karşılaştırılabilir satış kanıtı",
  "stale-reference-evidence": "Eski referans kanıtı",
  "source-conflict": "Kaynak çakışması",
  "incompatible-currency": "Uyumsuz para birimi",
  "incompatible-unit": "Uyumsuz birim",
  "incompatible-price-basis": "Uyumsuz fiyat bazı",
  "within-profile": "Profil içinde",
  "risk-incidence-increase": "Risk sıklığı artışı",
  "risk-incidence-decrease": "Risk sıklığı azalışı",
  "price-risk-concentration": "Fiyat riski yoğunlaşması",
  "expiry-risk-concentration": "Geçerlilik riski yoğunlaşması",
  "payment-term-risk-concentration": "Ödeme vadesi riski yoğunlaşması",
  "customer-risk-concentration": "Müşteri risk yoğunlaşması",
  "customer-risk-tie": "Müşteri risk eşitliği",
  "insufficient-reference-window": "Referans pencere yetersiz",
  "insufficient-current-window": "Güncel pencere yetersiz",
  "insufficient-customer-evidence": "Müşteri kanıtı yetersiz",
  "within-risk-band": "Risk bandı içinde",
};

function humanizeToken(value: string): string {
  return value
    .split(/[-_.]/)
    .filter(Boolean)
    .map((part) => part.charAt(0).toUpperCase() + part.slice(1))
    .join(" ");
}

function lookupLabel(map: Record<string, string>, value: string | null | undefined): string {
  if (!value?.trim()) return "Belirtilmemiş";
  return map[value.trim()] ?? humanizeToken(value);
}

export function localizeCommercialQuotationRiskProvenance(value: string | null | undefined): string {
  if (!value?.trim()) return "Belirtilmemiş";
  return QUOTATION_RISK_PROVENANCE_LABELS[value.trim()] ?? value.trim();
}

export function getCommercialQuotationRiskOutcomeLabel(value: string | null | undefined): string {
  return lookupLabel(QUOTATION_RISK_OUTCOME_LABELS, value);
}

export function getCommercialQuotationRiskComparisonTypeLabel(value: string | null | undefined): string {
  return lookupLabel(QUOTATION_RISK_COMPARISON_TYPE_LABELS, value);
}

export function getCommercialQuotationRiskPositionStateLabel(value: string | null | undefined): string {
  return lookupLabel(QUOTATION_RISK_POSITION_STATE_LABELS, value);
}

export function getCommercialQuotationRiskDriverReasonLabel(value: string | null | undefined): string {
  return lookupLabel(QUOTATION_RISK_DRIVER_LABELS, value);
}

export function inferQuotationRiskEvidenceStatus(outcome: string): string {
  if (outcome === "source-conflict") return "source-conflict";
  if (outcome === "insufficient-data") return "insufficient-data";
  return "simulated";
}

export function getQuotationRiskDriverFamilyLabel(driverCode: string | null | undefined): string {
  if (!driverCode) return "Belirtilmemiş";
  if (driverCode.includes("price") || driverCode.includes("quoted")) return "Fiyat riski";
  if (driverCode.includes("expir") || driverCode.includes("valid")) return "Geçerlilik riski";
  if (driverCode.includes("payment")) return "Ödeme vadesi riski";
  if (driverCode === "within-profile" || driverCode === "within-risk-band") return "Eşik içinde";
  if (driverCode === "source-conflict") return "Kaynak çakışması";
  if (driverCode.startsWith("insufficient")) return "Yetersiz veri";
  return "Diğer";
}
