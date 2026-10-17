import type { OperationalRiskTabId } from "../types/operational-risk-types";

export const OPERATIONAL_RISK_DEVELOPMENT_BANNER =
  "Örnek veri — gerçek müşteri operasyonel kanıtı değildir.";

export const OPERATIONAL_RISK_READONLY_NOTE =
  "Salt okunur erişim: risk yaşam döngüsü, profil yönetimi ve kontrol çalışması işlemleri devre dışıdır.";

export const OPERATIONAL_RISK_CURRENT_EVIDENCE_SCOPE =
  "Mevcut vaka ve tespit kanıtı penceresi";

export const OPERATIONAL_RISK_TAB_OPTIONS: ReadonlyArray<
  Readonly<{ id: OperationalRiskTabId; label: string }>
> = [
  { id: "overview", label: "Genel Bakış" },
  { id: "cases", label: "Risk Vakaları" },
  { id: "profiles", label: "Tespit Profilleri" },
  { id: "runs", label: "Kontrol Çalışmaları" },
];

export const OPERATIONAL_RISK_KPI_ORDER = [
  "open-cases",
  "critical-high",
  "new-detections",
  "under-review",
  "insufficient-data",
  "source-conflict",
] as const;

const LIFECYCLE_LABELS: Record<string, string> = {
  detected: "Tespit edildi",
  "under-review": "İncelemede",
  accepted: "Kabul edildi",
  "mitigation-planned": "Azaltma planlandı",
  monitoring: "İzleniyor",
  resolved: "Çözüldü",
  dismissed: "Kapatıldı",
};

const SEVERITY_LABELS: Record<string, string> = {
  low: "Düşük",
  moderate: "Orta",
  high: "Yüksek",
  critical: "Kritik",
};

const CATEGORY_LABELS: Record<string, string> = {
  "material-availability": "Malzeme uygunluğu",
  supplier: "Tedarik",
  "order-delivery": "Sipariş ve termin",
  "cost-deviation": "Maliyet sapması",
  "production-continuity": "Üretim sürekliliği",
  quality: "Kalite",
  maintenance: "Bakım",
  energy: "Enerji",
  "iot-connectivity": "IoT bağlantısı",
  other: "Diğer",
};

const SCOPE_LABELS: Record<string, string> = {
  facility: "Tesis",
  material: "Malzeme",
  supplier: "Tedarikçi",
  "purchase-order": "Satın alma siparişi",
  "customer-order": "Müşteri siparişi",
  "production-order": "Üretim emri",
  machine: "Makine",
  mold: "Kalıp",
  shipment: "Sevkiyat",
  "energy-measurement-point": "Enerji ölçüm noktası",
  "iot-endpoint": "IoT uç noktası",
  other: "Diğer",
};

const EVIDENCE_LABELS: Record<string, string> = {
  measured: "Ölçülmüş",
  calculated: "Hesaplanmış",
  estimated: "Tahmini",
  simulated: "Örnek veri",
  "insufficient-data": "Veri yetersiz",
  "source-conflict": "Kaynak uyuşmazlığı",
};

const ASSESSMENT_LABELS: Record<string, string> = {
  scored: "Puanlandı",
  "insufficient-data": "Veri yetersiz",
  "source-conflict": "Kaynak uyuşmazlığı",
};

const DETECTION_OUTCOME_LABELS: Record<string, string> = {
  "risk-detected": "Risk tespit edildi",
  "within-threshold": "Eşik içinde",
  "insufficient-data": "Veri yetersiz",
  "source-conflict": "Kaynak uyuşmazlığı",
};

const PROFILE_STATUS_LABELS: Record<string, string> = {
  draft: "Taslak",
  approved: "Onaylı",
  inactive: "Pasif",
};

const RUN_STATUS_LABELS: Record<string, string> = {
  completed: "Tamamlandı",
  failed: "Başarısız",
};

const SOURCE_MODULE_LABELS: Record<string, string> = {
  energy: "Enerji",
  "iot-plc": "IoT ve PLC",
  maintenance: "Bakım",
  material: "Malzeme",
  supplier: "Tedarik",
  order: "Sipariş ve termin",
  quality: "Kalite",
  production: "Üretim",
  cost: "Maliyet",
  "development-simulator": "Örnek veri kaynağı",
};

const DRIVER_LABELS: Record<string, string> = {
  "material-shortfall": "Malzeme yetersizliği",
  "supplier-delay": "Tedarik gecikmesi",
  "order-delivery-delay": "Termin gecikmesi",
  "cost-deviation-exceeded": "Maliyet sapması aşıldı",
  "production-continuity-risk": "Üretim sürekliliği riski",
  "quality-risk": "Kalite riski",
  "maintenance-risk": "Bakım riski",
  "energy-risk": "Enerji riski",
  "connectivity-risk": "Bağlantı riski",
  "source-stale": "Kaynak bayat",
  "missing-threshold": "Eşik eksik",
  "missing-denominator": "Karşılaştırılan toplam eksik",
  "insufficient-evidence": "Yetersiz kanıt",
  "source-conflict": "Kaynak uyuşmazlığı",
  "within-threshold": "Eşik içinde",
};

export const OPERATIONAL_RISK_SOURCE_MODULES = [
  "energy",
  "iot-plc",
  "maintenance",
  "material",
  "supplier",
  "order",
  "quality",
  "production",
  "cost",
] as const;

export const OPERATIONAL_RISK_ACTIVE_SOURCE_MODULES = new Set([
  "energy",
  "iot-plc",
  "maintenance",
]);

export const OPERATIONAL_RISK_TERMINAL_STATUSES = new Set(["resolved", "dismissed"]);

export const OPERATIONAL_RISK_ALLOWED_TRANSITIONS: Readonly<Record<string, readonly string[]>> = {
  detected: ["under-review", "accepted", "dismissed"],
  "under-review": ["accepted", "mitigation-planned", "monitoring", "dismissed"],
  accepted: ["mitigation-planned", "monitoring", "dismissed"],
  "mitigation-planned": ["monitoring", "resolved", "dismissed"],
  monitoring: ["resolved", "dismissed"],
};

function humanizeToken(value: string): string {
  return value
    .split(/[-_.]/)
    .filter(Boolean)
    .map((part) => part.charAt(0).toUpperCase() + part.slice(1))
    .join(" ");
}

export function getOperationalRiskLifecycleLabel(status: string): string {
  return LIFECYCLE_LABELS[status] ?? humanizeToken(status);
}

export function getOperationalRiskSeverityLabel(band: string | null | undefined): string {
  if (!band) return "Belirlenmedi";
  return SEVERITY_LABELS[band] ?? humanizeToken(band);
}

export function getOperationalRiskCategoryLabel(category: string): string {
  return CATEGORY_LABELS[category] ?? humanizeToken(category);
}

export function getOperationalRiskScopeLabel(scopeType: string): string {
  return SCOPE_LABELS[scopeType] ?? humanizeToken(scopeType);
}

export function getOperationalRiskEvidenceLabel(state: string): string {
  return EVIDENCE_LABELS[state] ?? humanizeToken(state);
}

export function getOperationalRiskAssessmentLabel(outcome: string): string {
  return ASSESSMENT_LABELS[outcome] ?? humanizeToken(outcome);
}

export function getOperationalRiskDetectionOutcomeLabel(outcome: string): string {
  return DETECTION_OUTCOME_LABELS[outcome] ?? humanizeToken(outcome);
}

export function getOperationalRiskProfileStatusLabel(status: string): string {
  return PROFILE_STATUS_LABELS[status] ?? humanizeToken(status);
}

export function getOperationalRiskRunStatusLabel(status: string): string {
  return RUN_STATUS_LABELS[status] ?? humanizeToken(status);
}

export function getOperationalRiskSourceModuleLabel(module: string | null | undefined): string {
  if (!module) return "Belirtilmedi";
  return SOURCE_MODULE_LABELS[module] ?? humanizeToken(module);
}

export function getOperationalRiskDriverLabel(code: string | null | undefined): string {
  if (!code) return "Belirtilmedi";
  return DRIVER_LABELS[code] ?? humanizeToken(code);
}

export function getOperationalRiskTransitionLabel(status: string): string {
  return getOperationalRiskLifecycleLabel(status);
}
