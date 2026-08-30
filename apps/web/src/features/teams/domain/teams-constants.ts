export const TEAMS_MESSAGE_TASK_DRAFT_DISCLAIMER =
  "Görev taslağı yalnızca insan incelemesi için oluşturulur; mesaj otomatik üretim, satın alma, bakım, kalite veya başka bir operasyon işlemi başlatmaz.";

export const TEAMS_APPROVAL_DISCLAIMER =
  "Onay isteği yalnızca insan kararı kaydeder; onay, ret veya değişiklik talebi otomatik üretim, satın alma, stok, bakım, kalite, finans veya görev işlemi başlatmaz.";

export const TEAMS_APPROVAL_STATUSES = {
  pending: "Beklemede",
  approved: "Onaylandı",
  rejected: "Reddedildi",
  "changes-requested": "Değişiklik istendi",
  cancelled: "İptal edildi",
} as const;

export const TEAMS_APPROVAL_DECISIONS = {
  approved: "Onayla",
  rejected: "Reddet",
  "changes-requested": "Değişiklik iste",
} as const;

export const TEAMS_HANDOVER_DISCLAIMER =
  "Vardiya devri yalnızca operasyon bağlamı kaydeder; görev, onay, üretim, satın alma, stok, bakım, kalite veya finans işlemi başlatmaz.";

export const TEAMS_HANDOVER_STATUSES = {
  draft: "Taslak",
  published: "Yayımlanmış",
} as const;

export const TEAMS_HANDOVER_SHIFT_TYPES = {
  morning: "Sabah",
  evening: "Akşam",
  night: "Gece",
  custom: "Özel",
} as const;

export const TEAMS_HANDOVER_ITEM_KINDS = {
  attention: "Dikkat",
  incident: "Olay",
  "pending-work": "Bekleyen iş",
  "quality-note": "Kalite notu",
  "maintenance-note": "Bakım notu",
  "material-note": "Malzeme notu",
} as const;

export const TEAMS_HANDOVER_AUDIT_EVENTS = {
  "handover-created": "Oluşturuldu",
  "handover-updated": "Güncellendi",
  "handover-published": "Yayımlandı",
  "handover-acknowledged": "Teslim alındı",
} as const;

export function isHandoverChannelType(channelType: string): boolean {
  return channelType === "department" || channelType === "operational";
}

export const TEAMS_WORK_INBOX_DISCLAIMER =
  "İş kutusu yalnızca mevcut kaynaklardaki size ait işleri bir arada gösterir; görev, onay, vardiya devri veya mesaj işlemi başlatmaz.";

export const TEAMS_WORK_INBOX_SOURCE_KINDS = {
  "ai-task": "AI görevi",
  approval: "Onay isteği",
  handover: "Vardiya devri",
  mention: "Etiket",
} as const;
