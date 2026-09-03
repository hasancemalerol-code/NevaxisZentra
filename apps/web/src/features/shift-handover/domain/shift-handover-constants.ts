export const SHIFT_HANDOVER_STATUSES = {
  draft: "Taslak",
  "ready-for-handover": "Devre Haz─▒r",
  "handed-over": "Devredildi",
  acknowledged: "Teslim Al─▒nd─▒",
  completed: "Tamamland─▒",
  cancelled: "─░ptal",
} as const;

export const SHIFT_HANDOVER_SHIFT_PERIODS = {
  morning: "Sabah",
  evening: "Ak┼şam",
  night: "Gece",
  custom: "├ûzel",
} as const;

export const SHIFT_HANDOVER_ITEM_CATEGORIES = {
  production: "├£retim",
  machine: "Makine",
  maintenance: "Bak─▒m",
  quality: "Kalite",
  material: "Malzeme",
  order: "Sipari┼ş",
  safety: "G├╝venlik",
  general: "Genel",
} as const;

export const SHIFT_HANDOVER_ITEM_SEVERITIES = {
  low: "D├╝┼ş├╝k",
  medium: "Orta",
  high: "Y├╝ksek",
  critical: "Kritik",
} as const;

export const SHIFT_HANDOVER_AUDIT_EVENTS = {
  created: "Olu┼şturuldu",
  updated: "G├╝ncellendi",
  "item-added": "Madde eklendi",
  "item-updated": "Madde g├╝ncellendi",
  "ready-for-handover": "Devre haz─▒rland─▒",
  "reverted-to-draft": "Tasla─şa al─▒nd─▒",
  "handed-over": "Devredildi",
  acknowledged: "Teslim al─▒nd─▒",
  completed: "Tamamland─▒",
  cancelled: "─░ptal edildi",
  "follow-up-task-linked": "Takip g├Ârevi ba─şland─▒",
} as const;

export type ShiftHandoverStatus = keyof typeof SHIFT_HANDOVER_STATUSES;
