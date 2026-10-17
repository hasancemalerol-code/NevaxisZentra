import { FACILITY_TIME_ZONE } from "@/lib/presentation/format";
import { displayDataName } from "@/lib/presentation/display-name";
export function getMaterialLifecycleStatusLabel(status: string): string {
  switch (status) {
    case "draft":
      return "Taslak";
    case "active":
      return "Aktif";
    case "blocked":
      return "Bloke";
    case "obsolete":
      return "Kullanımdan Kalkmış";
    default:
      return "Bilinmiyor";
  }
}

export function getMaterialCategoryLabel(category: string): string {
  switch (category) {
    case "virgin-polymer":
      return "Virgin Polimer";
    case "compound":
      return "Kompound";
    case "masterbatch":
      return "Masterbatch";
    case "additive":
      return "Katkı Maddesi";
    case "filler":
      return "Dolgu";
    case "regrind":
      return "Regranül";
    case "other":
      return "Diğer";
    default:
      return "Bilinmiyor";
  }
}

export function getPolymerFamilyLabel(polymer: string): string {
  switch (polymer) {
    case "PP":
      return "PP";
    case "PE":
      return "PE";
    case "HDPE":
      return "HDPE";
    case "LDPE":
      return "LDPE";
    case "ABS":
      return "ABS";
    case "PA":
      return "PA";
    case "POM":
      return "POM";
    case "PC":
      return "PC";
    case "PMMA":
      return "PMMA";
    case "PET":
      return "PET";
    case "PBT":
      return "PBT";
    case "PS":
      return "PS";
    case "TPE":
      return "TPE";
    case "TPU":
      return "TPU";
    case "PVC":
      return "PVC";
    case "other":
      return "Diğer";
    case "unknown":
      return "Bilinmiyor";
    default:
      return "Bilinmiyor";
  }
}

export function getPhysicalFormLabel(form: string): string {
  switch (form) {
    case "granule":
      return "Granül";
    case "powder":
      return "Toz";
    case "liquid":
      return "Sıvı";
    case "flake":
      return "Flake";
    case "pellet":
      return "Pelet";
    case "other":
      return "Diğer";
    default:
      return "Bilinmiyor";
  }
}

export function getMaterialSourceLabel(source: string): string {
  switch (source) {
    case "manual":
      return "Manuel";
    case "imported":
      return "İçe Aktarım";
    case "erp":
      return "ERP";
    case "development-pilot":
      return "Örnek veri";
    default:
      return "Bilinmiyor";
  }
}

export function getSpecificationSourceLabel(source: string): string {
  switch (source) {
    case "manual":
      return "Manuel";
    case "imported":
      return "İçe Aktarım";
    case "development-pilot":
      return "Örnek veri";
    default:
      return "Bilinmiyor";
  }
}

export function getTechnicalDataLabel(hasTechnicalData: boolean): string {
  return hasTechnicalData ? "Mevcut" : "Eksik";
}

export function formatMaterialDateTime(value?: string): string {
  if (!value) {
    return "—";
  }

  return new Date(value).toLocaleString("tr-TR", {
    day: "2-digit",
    month: "short",
    year: "numeric",
    hour: "2-digit",
    minute: "2-digit",
    timeZone: FACILITY_TIME_ZONE,
  });
}

export function formatMaterialNumber(value?: number | null): string {
  if (value === undefined || value === null) {
    return "—";
  }

  return value.toLocaleString("tr-TR");
}

export function formatMaterialRange(min?: number, max?: number, unit = ""): string {
  if (min === undefined && max === undefined) {
    return "—";
  }

  if (min !== undefined && max !== undefined) {
    return `${formatMaterialNumber(min)} – ${formatMaterialNumber(max)}${unit ? ` ${unit}` : ""}`;
  }

  const value = min ?? max;
  return `${formatMaterialNumber(value)}${unit ? ` ${unit}` : ""}`;
}

export const MATERIAL_LIFECYCLE_FILTER_OPTIONS = [
  { value: "", label: "Tüm durumlar" },
  { value: "draft", label: "Taslak" },
  { value: "active", label: "Aktif" },
  { value: "blocked", label: "Bloke" },
  { value: "obsolete", label: "Kullanımdan Kalkmış" },
] as const;

export const MATERIAL_CATEGORY_FILTER_OPTIONS = [
  { value: "", label: "Tüm kategoriler" },
  { value: "virgin-polymer", label: "Virgin Polimer" },
  { value: "compound", label: "Kompound" },
  { value: "masterbatch", label: "Masterbatch" },
  { value: "additive", label: "Katkı Maddesi" },
  { value: "filler", label: "Dolgu" },
  { value: "regrind", label: "Regranül" },
  { value: "other", label: "Diğer" },
] as const;

export const MATERIAL_POLYMER_FILTER_OPTIONS = [
  { value: "", label: "Tüm polimerler" },
  { value: "PP", label: "PP" },
  { value: "PE", label: "PE" },
  { value: "HDPE", label: "HDPE" },
  { value: "LDPE", label: "LDPE" },
  { value: "ABS", label: "ABS" },
  { value: "PA", label: "PA" },
  { value: "POM", label: "POM" },
  { value: "PC", label: "PC" },
  { value: "PMMA", label: "PMMA" },
  { value: "PET", label: "PET" },
  { value: "PBT", label: "PBT" },
  { value: "PS", label: "PS" },
  { value: "TPE", label: "TPE" },
  { value: "TPU", label: "TPU" },
  { value: "PVC", label: "PVC" },
  { value: "other", label: "Diğer" },
] as const;

export const MATERIAL_CATEGORY_FORM_OPTIONS = [
  { value: "virgin-polymer", label: "Virgin Polimer" },
  { value: "compound", label: "Kompound" },
  { value: "masterbatch", label: "Masterbatch" },
  { value: "additive", label: "Katkı Maddesi" },
  { value: "filler", label: "Dolgu" },
  { value: "regrind", label: "Regranül" },
  { value: "other", label: "Diğer" },
] as const;

export const MATERIAL_POLYMER_FORM_OPTIONS = [
  { value: "PP", label: "PP" },
  { value: "PE", label: "PE" },
  { value: "HDPE", label: "HDPE" },
  { value: "LDPE", label: "LDPE" },
  { value: "ABS", label: "ABS" },
  { value: "PA", label: "PA" },
  { value: "POM", label: "POM" },
  { value: "PC", label: "PC" },
  { value: "PMMA", label: "PMMA" },
  { value: "PET", label: "PET" },
  { value: "PBT", label: "PBT" },
  { value: "PS", label: "PS" },
  { value: "TPE", label: "TPE" },
  { value: "TPU", label: "TPU" },
  { value: "PVC", label: "PVC" },
  { value: "other", label: "Diğer" },
  { value: "unknown", label: "Bilinmiyor" },
] as const;

export const MATERIAL_PHYSICAL_FORM_OPTIONS = [
  { value: "granule", label: "Granül" },
  { value: "powder", label: "Toz" },
  { value: "liquid", label: "Sıvı" },
  { value: "flake", label: "Flake" },
  { value: "pellet", label: "Pelet" },
  { value: "other", label: "Diğer" },
] as const;

export const MATERIAL_SOURCE_FORM_OPTIONS = [
  { value: "manual", label: "Manuel" },
  { value: "imported", label: "İçe Aktarım" },
  { value: "erp", label: "ERP" },
  { value: "development-pilot", label: "Örnek veri" },
] as const;

export const MATERIAL_SPECIFICATION_SOURCE_FORM_OPTIONS = [
  { value: "manual", label: "Manuel" },
  { value: "imported", label: "İçe Aktarım" },
  { value: "development-pilot", label: "Örnek veri" },
] as const;

export const MATERIAL_TRANSITION_TARGETS: Record<string, readonly { value: string; label: string }[]> = {
  draft: [
    { value: "active", label: "Aktif" },
    { value: "blocked", label: "Bloke" },
  ],
  active: [
    { value: "blocked", label: "Bloke" },
    { value: "obsolete", label: "Kullanımdan Kalkmış" },
  ],
  blocked: [
    { value: "active", label: "Aktif" },
    { value: "obsolete", label: "Kullanımdan Kalkmış" },
  ],
  obsolete: [],
};

export const MATERIAL_LOT_QUANTITY_DISCLAIMER =
  "Operasyonel gözlem miktarıdır; ERP stok bakiyesi değildir.";

export function getMaterialLotStatusLabel(status: string): string {
  switch (status) {
    case "draft":
      return "Taslak";
    case "received":
      return "Alındı";
    case "available":
      return "Kullanılabilir";
    case "quality-hold":
      return "Kalite Bekliyor";
    case "blocked":
      return "Bloke";
    case "closed":
      return "Kapatıldı";
    default:
      return "Bilinmiyor";
  }
}

export function getMaterialLotQualityStatusLabel(status: string): string {
  switch (status) {
    case "pending":
      return "Beklemede";
    case "accepted":
      return "Kabul";
    case "conditionally-accepted":
      return "Koşullu Kabul";
    case "rejected":
      return "Red";
    case "not-required":
      return "Gerekli Değil";
    case "unknown":
      return "Bilinmiyor";
    default:
      return "Bilinmiyor";
  }
}

export function getMaterialLotCertificateTypeLabel(type: string): string {
  switch (type) {
    case "certificate-of-analysis":
      return "Analiz Sertifikası (CoA)";
    case "material-certificate":
      return "Malzeme Sertifikası";
    case "safety-data-sheet":
      return "Güvenlik Bilgi Formu (SDS)";
    case "technical-data-sheet":
      return "Teknik Veri Formu (TDS)";
    case "conformity-certificate":
      return "Uygunluk Sertifikası";
    case "other":
      return "Diğer";
    default:
      return "Bilinmiyor";
  }
}

export function getMaterialLotEventTypeLabel(eventType: string): string {
  switch (eventType) {
    case "lot-created":
      return "Lot Oluşturuldu";
    case "receipt-observed":
      return "Kabul Gözlemi";
    case "location-transfer":
      return "Konum Transferi";
    case "status-changed":
      return "Durum Değişikliği";
    case "quality-status-recorded":
      return "Kalite Durumu";
    case "certificate-linked":
      return "Belge Bağlandı";
    case "note-recorded":
      return "Not";
    default:
      return "Olay";
  }
}

export function formatMaterialLotLocation(
  warehouse?: string | null,
  location?: string | null,
): string {
  if (!warehouse && !location) {
    return "—";
  }

  if (warehouse && location) {
    return `${warehouse} / ${location}`;
  }

  return warehouse ?? location ?? "—";
}

export function formatMaterialLotExpiry(
  expiryDate?: string,
  retestDate?: string,
  isApproaching?: boolean,
): string {
  const parts: string[] = [];
  if (expiryDate) {
    parts.push(`SKT: ${formatMaterialDateOnly(expiryDate)}`);
  }
  if (retestDate) {
    parts.push(`Yeniden test: ${formatMaterialDateOnly(retestDate)}`);
  }
  if (parts.length === 0) {
    return "—";
  }
  return isApproaching ? `${parts.join(" · ")} (yaklaşıyor)` : parts.join(" · ");
}

export function formatMaterialDateOnly(value?: string): string {
  if (!value) {
    return "—";
  }

  return new Date(value).toLocaleDateString("tr-TR", {
    day: "2-digit",
    month: "short",
    year: "numeric",
    timeZone: FACILITY_TIME_ZONE,
  });
}

export function formatMaterialQuantity(value?: number | null, unit?: string): string {
  if (value === undefined || value === null) {
    return "—";
  }

  const formatted = value.toLocaleString("tr-TR");
  return unit ? `${formatted} ${unit}` : formatted;
}

export const MATERIAL_LOT_STATUS_FILTER_OPTIONS = [
  { value: "", label: "Tüm durumlar" },
  { value: "draft", label: "Taslak" },
  { value: "received", label: "Alındı" },
  { value: "available", label: "Kullanılabilir" },
  { value: "quality-hold", label: "Kalite Bekliyor" },
  { value: "blocked", label: "Bloke" },
  { value: "closed", label: "Kapatıldı" },
] as const;

export const MATERIAL_LOT_QUALITY_FILTER_OPTIONS = [
  { value: "", label: "Tüm kalite durumları" },
  { value: "pending", label: "Beklemede" },
  { value: "accepted", label: "Kabul" },
  { value: "conditionally-accepted", label: "Koşullu Kabul" },
  { value: "rejected", label: "Red" },
  { value: "not-required", label: "Gerekli Değil" },
] as const;

export const MATERIAL_LOT_EXPIRY_FILTER_OPTIONS = [
  { value: "", label: "Tüm SKT durumları" },
  { value: "approaching", label: "Süresi Yaklaşan" },
  { value: "expired", label: "Süresi Geçmiş" },
] as const;

export const MATERIAL_LOT_QUALITY_FORM_OPTIONS = [
  { value: "pending", label: "Beklemede" },
  { value: "accepted", label: "Kabul" },
  { value: "conditionally-accepted", label: "Koşullu Kabul" },
  { value: "rejected", label: "Red" },
  { value: "not-required", label: "Gerekli Değil" },
] as const;

export const MATERIAL_LOT_CERTIFICATE_FORM_OPTIONS = [
  { value: "certificate-of-analysis", label: "Analiz Sertifikası (CoA)" },
  { value: "material-certificate", label: "Malzeme Sertifikası" },
  { value: "safety-data-sheet", label: "Güvenlik Bilgi Formu (SDS)" },
  { value: "technical-data-sheet", label: "Teknik Veri Formu (TDS)" },
  { value: "conformity-certificate", label: "Uygunluk Sertifikası" },
  { value: "other", label: "Diğer" },
] as const;

export const MATERIAL_LOT_TRANSITION_TARGETS: Record<string, readonly { value: string; label: string }[]> = {
  draft: [
    { value: "received", label: "Alındı" },
    { value: "blocked", label: "Bloke" },
  ],
  received: [
    { value: "quality-hold", label: "Kalite Bekliyor" },
    { value: "available", label: "Kullanılabilir" },
    { value: "blocked", label: "Bloke" },
  ],
  "quality-hold": [
    { value: "available", label: "Kullanılabilir" },
    { value: "blocked", label: "Bloke" },
  ],
  available: [
    { value: "quality-hold", label: "Kalite Bekliyor" },
    { value: "blocked", label: "Bloke" },
    { value: "closed", label: "Kapatıldı" },
  ],
  blocked: [
    { value: "quality-hold", label: "Kalite Bekliyor" },
    { value: "closed", label: "Kapatıldı" },
  ],
  closed: [],
};

export function getMaterialRequirementTypeLabel(type: string): string {
  switch (type) {
    case "production-order":
      return "Üretim Emri";
    case "trial":
      return "Deneme";
    case "setup":
      return "Kurulum";
    case "manual-planning":
      return "Manuel Planlama";
    case "other":
      return "Diğer";
    default:
      return "Bilinmiyor";
  }
}

export function getMaterialConsumptionObservationTypeLabel(type: string): string {
  switch (type) {
    case "consumption-observed":
      return "Tüketim Gözlemi";
    case "setup-consumption-observed":
      return "Kurulum Tüketimi";
    case "trial-consumption-observed":
      return "Deneme Tüketimi";
    case "scrap-observed":
      return "Fire Gözlemi";
    case "regrind-use-observed":
      return "Regrind Kullanımı";
    case "correction-observed":
      return "Düzeltme Gözlemi";
    default:
      return "Bilinmiyor";
  }
}

export function getMaterialRequirementAssessmentStatusLabel(status: string): string {
  switch (status) {
    case "calculated":
      return "Hesaplandı";
    case "insufficient-data":
      return "Yetersiz Veri";
    case "within-tolerance":
      return "Tolerans Dahilinde";
    case "above-plan":
      return "Plan Üstü";
    case "below-plan":
      return "Plan Altı";
    case "compliant":
      return "Uyumlu";
    case "prohibited":
      return "Yasak";
    case "exceeds-limit":
      return "Limit Aşımı";
    default:
      return "Bilinmiyor";
  }
}

export function getMaterialAssessmentDriverStateLabel(state: string): string {
  switch (state) {
    case "pass":
      return "Uygun";
    case "warn":
      return "Uyarı";
    case "fail":
      return "Uygunsuz";
    case "unknown":
      return "Bilinmiyor";
    default:
      return "Bilinmiyor";
  }
}

export function getMaterialAssessmentEvidenceFieldLabel(field: string): string {
  switch (field) {
    case "goodPartTargetQuantity":
      return "Hedef Sağlam Parça";
    case "partWeightGrams":
      return "Parça Ağırlığı (g)";
    case "runnerWeightGramsPerShot":
      return "Yolluk Ağırlığı/Vuruş (g)";
    case "effectiveCavityCount":
      return "Etkin Göz Sayısı";
    case "setupAllowanceGrams":
      return "Kurulum Payı (g)";
    case "processLossAllowancePercent":
      return "Proses Kaybı (%)";
    case "productionOrderReference":
      return "Üretim Emri Referansı";
    case "moldReference":
      return "Kalıp Referansı";
    case "plannedRegrindPercent":
      return "Planlanan Regrind (%)";
    case "maximumRegrindPercent":
      return "Maks. Regrind (%)";
    case "theoreticalRequirementGrams":
      return "Teorik İhtiyaç (g)";
    case "observedConsumptionGrams":
      return "Gözlenen Tüketim (g)";
    case "erpSourceFreshness":
      return "ERP Kaynak Tazeliği";
    case "erpAvailableQuantity":
      return "ERP Kullanılabilir Miktar";
    case "erpReservedQuantity":
      return "ERP Rezerve Miktar";
    case "requiredQuantityGrams":
      return "Gereken Miktar (g)";
    default:
      return displayDataName(field.replace(/([a-z])([A-Z])/g, "$1 $2").replace(/^./, (c) => c.toUpperCase()));
  }
}

export function formatMaterialGrams(value?: number | null): string {
  if (value === undefined || value === null) {
    return "—";
  }

  return `${value.toLocaleString("tr-TR", { maximumFractionDigits: 4 })} g`;
}

export function formatMaterialPercent(value?: number | null): string {
  if (value === undefined || value === null) {
    return "—";
  }

  return `${value.toLocaleString("tr-TR", { maximumFractionDigits: 2 })} %`;
}

export const MATERIAL_REQUIREMENT_TYPE_FORM_OPTIONS = [
  { value: "production-order", label: "Üretim Emri" },
  { value: "trial", label: "Deneme" },
  { value: "setup", label: "Kurulum" },
  { value: "manual-planning", label: "Manuel Planlama" },
  { value: "other", label: "Diğer" },
] as const;

export const MATERIAL_CONSUMPTION_OBSERVATION_FORM_OPTIONS = [
  { value: "consumption-observed", label: "Tüketim Gözlemi" },
  { value: "setup-consumption-observed", label: "Kurulum Tüketimi" },
  { value: "trial-consumption-observed", label: "Deneme Tüketimi" },
  { value: "scrap-observed", label: "Fire Gözlemi" },
  { value: "regrind-use-observed", label: "Regrind Kullanımı" },
  { value: "correction-observed", label: "Düzeltme Gözlemi" },
] as const;

export const MATERIAL_REQUIREMENT_THEORETICAL_DISCLAIMER =
  "Teorik malzeme ihtiyacıdır; ERP stok rezervasyonu veya gerçek tüketim değildir.";

export const MATERIAL_REQUIREMENT_CONSUMPTION_DISCLAIMER =
  "Operasyonel tüketim gözlemidir; ERP stok hareketi değildir.";
