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
