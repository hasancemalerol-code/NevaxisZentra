export function getMoldLifecycleStateLabel(state: string): string {
  switch (state) {
    case "draft":
      return "Taslak";
    case "active":
      return "Aktif";
    case "maintenance":
      return "Bakımda";
    case "trial":
      return "Denemede";
    case "quarantined":
      return "Karantinada";
    case "retired":
      return "Emekli";
    case "scrapped":
      return "Hurdaya Ayrıldı";
    default:
      return "Bilinmiyor";
  }
}

export function getMoldOwnershipLabel(ownership: string): string {
  switch (ownership) {
    case "company-owned":
      return "İşletmeye Ait";
    case "customer-owned":
      return "Müşteriye Ait";
    case "supplier-owned":
      return "Tedarikçiye Ait";
    case "leased":
      return "Kiralık";
    default:
      return "Bilinmiyor";
  }
}

export function getMoldLocationLabel(location: string): string {
  switch (location) {
    case "warehouse":
      return "Depo";
    case "production-area":
      return "Üretim Alanı";
    case "maintenance-area":
      return "Bakım Alanı";
    case "trial-area":
      return "Deneme Alanı";
    case "external-customer":
      return "Müşteride";
    case "external-supplier":
      return "Tedarikçide";
    case "transit":
      return "Transferde";
    default:
      return "Bilinmiyor";
  }
}

export function getMoldSourceLabel(source: string): string {
  switch (source) {
    case "manual":
      return "Manuel";
    case "imported":
      return "İçe Aktarım";
    case "erp":
      return "ERP";
    default:
      return "Bilinmiyor";
  }
}

export function getMoldCounterSourceLabel(source?: string): string {
  switch (source) {
    case "manual":
      return "Manuel";
    case "production":
      return "Üretim";
    case "edge":
      return "Edge";
    case "erp":
      return "ERP";
    case "imported":
      return "İçe Aktarım";
    default:
      return "Bilinmiyor";
  }
}

export function formatMoldDateTime(value?: string): string {
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

export function formatMoldNumber(value?: number | null): string {
  if (value === undefined || value === null) {
    return "—";
  }

  return value.toLocaleString("tr-TR");
}

export const MOLD_LIFECYCLE_FILTER_OPTIONS = [
  { value: "", label: "Tüm durumlar" },
  { value: "draft", label: "Taslak" },
  { value: "active", label: "Aktif" },
  { value: "maintenance", label: "Bakımda" },
  { value: "trial", label: "Denemede" },
  { value: "quarantined", label: "Karantinada" },
  { value: "retired", label: "Emekli" },
  { value: "scrapped", label: "Hurdaya Ayrıldı" },
] as const;

export const MOLD_OWNERSHIP_FILTER_OPTIONS = [
  { value: "", label: "Tüm sahiplik" },
  { value: "company-owned", label: "İşletmeye Ait" },
  { value: "customer-owned", label: "Müşteriye Ait" },
  { value: "supplier-owned", label: "Tedarikçiye Ait" },
  { value: "leased", label: "Kiralık" },
  { value: "unknown", label: "Bilinmiyor" },
] as const;

export const MOLD_LOCATION_FILTER_OPTIONS = [
  { value: "", label: "Tüm konumlar" },
  { value: "warehouse", label: "Depo" },
  { value: "production-area", label: "Üretim Alanı" },
  { value: "maintenance-area", label: "Bakım Alanı" },
  { value: "trial-area", label: "Deneme Alanı" },
  { value: "external-customer", label: "Müşteride" },
  { value: "external-supplier", label: "Tedarikçide" },
  { value: "transit", label: "Transferde" },
  { value: "unknown", label: "Bilinmiyor" },
] as const;

export const MOLD_OWNERSHIP_FORM_OPTIONS = [
  { value: "company-owned", label: "İşletmeye Ait" },
  { value: "customer-owned", label: "Müşteriye Ait" },
  { value: "supplier-owned", label: "Tedarikçiye Ait" },
  { value: "leased", label: "Kiralık" },
  { value: "unknown", label: "Bilinmiyor" },
] as const;

export const MOLD_LOCATION_FORM_OPTIONS = [
  { value: "warehouse", label: "Depo" },
  { value: "production-area", label: "Üretim Alanı" },
  { value: "maintenance-area", label: "Bakım Alanı" },
  { value: "trial-area", label: "Deneme Alanı" },
  { value: "external-customer", label: "Müşteride" },
  { value: "external-supplier", label: "Tedarikçide" },
  { value: "transit", label: "Transferde" },
  { value: "unknown", label: "Bilinmiyor" },
] as const;

export const MOLD_SOURCE_FORM_OPTIONS = [
  { value: "manual", label: "Manuel" },
  { value: "imported", label: "İçe Aktarım" },
  { value: "erp", label: "ERP" },
  { value: "unknown", label: "Bilinmiyor" },
] as const;

export const MOLD_TRANSITION_TARGETS: Record<string, readonly { value: string; label: string }[]> = {
  draft: [
    { value: "active", label: "Aktif" },
    { value: "quarantined", label: "Karantinada" },
    { value: "retired", label: "Emekli" },
  ],
  active: [
    { value: "maintenance", label: "Bakımda" },
    { value: "trial", label: "Denemede" },
    { value: "quarantined", label: "Karantinada" },
    { value: "retired", label: "Emekli" },
  ],
  maintenance: [
    { value: "active", label: "Aktif" },
    { value: "trial", label: "Denemede" },
    { value: "quarantined", label: "Karantinada" },
    { value: "retired", label: "Emekli" },
  ],
  trial: [
    { value: "active", label: "Aktif" },
    { value: "maintenance", label: "Bakımda" },
    { value: "quarantined", label: "Karantinada" },
    { value: "retired", label: "Emekli" },
  ],
  quarantined: [
    { value: "active", label: "Aktif" },
    { value: "maintenance", label: "Bakımda" },
    { value: "retired", label: "Emekli" },
  ],
  retired: [
    { value: "active", label: "Aktif" },
    { value: "scrapped", label: "Hurdaya Ayrıldı" },
  ],
  scrapped: [],
};
