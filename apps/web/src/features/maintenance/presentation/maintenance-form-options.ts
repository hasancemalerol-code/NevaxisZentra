export const MAINTENANCE_ASSET_TYPE_OPTIONS = [
  { value: "machine", label: "Makine" },
  { value: "mold", label: "Kalıp" },
  { value: "auxiliary", label: "Yardımcı Ekipman" },
  { value: "tooling", label: "Takım" },
  { value: "facility", label: "Tesis" },
] as const;

export const MAINTENANCE_REQUEST_TYPE_OPTIONS = [
  { value: "fault", label: "Arıza" },
  { value: "corrective", label: "Düzeltici Bakım" },
  { value: "preventive", label: "Periyodik Bakım" },
  { value: "inspection", label: "Kontrol" },
  { value: "other", label: "Diğer" },
] as const;

export const MAINTENANCE_PRIORITY_OPTIONS = [
  { value: "low", label: "Düşük" },
  { value: "normal", label: "Normal" },
  { value: "high", label: "Yüksek" },
  { value: "critical", label: "Kritik" },
] as const;

export const MAINTENANCE_INTERVAL_TYPE_OPTIONS = [
  { value: "days", label: "Gün" },
  { value: "weeks", label: "Hafta" },
  { value: "months", label: "Ay" },
] as const;
