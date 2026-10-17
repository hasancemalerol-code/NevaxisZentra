export const SHIFT_HANDOVER_STATUSES = {
  draft: "Taslak",
  "ready-for-handover": "Devre Hazır",
  "handed-over": "Devredildi",
  acknowledged: "Teslim Alındı",
  completed: "Tamamlandı",
  cancelled: "İptal",
} as const;

export const SHIFT_HANDOVER_SHIFT_PERIODS = {
  morning: "Sabah",
  evening: "Akşam",
  night: "Gece",
  custom: "Özel",
} as const;

export const SHIFT_HANDOVER_ITEM_CATEGORIES = {
  production: "Üretim",
  machine: "Makine",
  maintenance: "Bakım",
  quality: "Kalite",
  material: "Malzeme",
  order: "Sipariş",
  safety: "Güvenlik",
  general: "Genel",
} as const;

export const SHIFT_HANDOVER_ITEM_SEVERITIES = {
  low: "Düşük",
  medium: "Orta",
  high: "Yüksek",
  critical: "Kritik",
} as const;

export const SHIFT_HANDOVER_ITEM_STATUSES = {
  open: "Açık",
  resolved: "Çözüldü",
  "carried-over": "Devredildi",
} as const;

export const SHIFT_HANDOVER_AUDIT_EVENTS = {
  created: "Oluşturuldu",
  updated: "Güncellendi",
  "item-added": "Madde eklendi",
  "item-updated": "Madde güncellendi",
  "ready-for-handover": "Devre hazırlandı",
  "reverted-to-draft": "Taslağa alındı",
  "handed-over": "Devredildi",
  acknowledged: "Teslim alındı",
  completed: "Tamamlandı",
  cancelled: "İptal edildi",
  "follow-up-task-linked": "Takip görevi bağlandı",
} as const;

export type ShiftHandoverStatus = keyof typeof SHIFT_HANDOVER_STATUSES;

function lookup(map: Readonly<Record<string, string>>, key: string, fallback: string): string {
  return map[key] ?? fallback;
}

export function shiftHandoverStatusLabel(status: string): string {
  return lookup(SHIFT_HANDOVER_STATUSES, status, "Durum belirsiz");
}

export function shiftHandoverPeriodLabel(period: string): string {
  return lookup(SHIFT_HANDOVER_SHIFT_PERIODS, period, "Özel");
}

export function shiftHandoverCategoryLabel(category: string): string {
  return lookup(SHIFT_HANDOVER_ITEM_CATEGORIES, category, "Genel");
}

export function shiftHandoverSeverityLabel(severity: string): string {
  return lookup(SHIFT_HANDOVER_ITEM_SEVERITIES, severity, "Orta");
}

export function shiftHandoverItemStatusLabel(status: string): string {
  return lookup(SHIFT_HANDOVER_ITEM_STATUSES, status, "Açık");
}

export function shiftHandoverEventLabel(eventType: string): string {
  return lookup(SHIFT_HANDOVER_AUDIT_EVENTS, eventType, "Kayıt güncellendi");
}

/** True only for a canonical GUID; anything else can never be a handover record. */
export function isShiftHandoverId(value: string): boolean {
  return /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i.test(value);
}

const SHIFT_HANDOVER_DEPARTMENTS: Readonly<Record<string, string>> = {
  production: "Üretim",
  maintenance: "Bakım",
  quality: "Kalite",
  warehouse: "Depo",
  logistics: "Lojistik",
  planning: "Planlama",
  packaging: "Paketleme",
  assembly: "Montaj",
  safety: "Güvenlik",
  general: "Genel",
};

/** Department is free text on the record; known canonical values are shown in Turkish, typed names stay as typed. */
export function shiftHandoverDepartmentLabel(department: string): string {
  const trimmed = department.trim();
  if (!trimmed) {
    return "Belirtilmedi";
  }
  return SHIFT_HANDOVER_DEPARTMENTS[trimmed.toLowerCase()] ?? trimmed;
}
