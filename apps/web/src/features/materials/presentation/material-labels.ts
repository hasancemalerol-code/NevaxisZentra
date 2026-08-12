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
      return "Geliştirme Pilot";
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
      return "Geliştirme Pilot";
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
  { value: "development-pilot", label: "Geliştirme Pilot" },
] as const;

export const MATERIAL_SPECIFICATION_SOURCE_FORM_OPTIONS = [
  { value: "manual", label: "Manuel" },
  { value: "imported", label: "İçe Aktarım" },
  { value: "development-pilot", label: "Geliştirme Pilot" },
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
