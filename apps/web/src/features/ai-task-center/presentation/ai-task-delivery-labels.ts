export const AI_TASK_DELIVERY_STATUS_LABELS: Readonly<Record<string, string>> = {
  queued: "Kuyrukta",
  processing: "Gönderiliyor",
  sent: "Gönderildi",
  "failed-retryable": "Yeniden Denenecek",
  "failed-permanent": "Kalıcı Hata",
  "dead-letter": "Başarısız",
  cancelled: "İptal Edildi",
};

export const AI_TASK_DELIVERY_CHANNEL_LABELS: Readonly<Record<string, string>> = {
  "NevaxisFactOS-teams": "FactOS Teams",
  email: "E-posta",
  whatsapp: "WhatsApp",
  "microsoft-teams": "Microsoft Teams",
  sms: "SMS",
};

export function getAiTaskDeliveryStatusLabel(status: string): string {
  return AI_TASK_DELIVERY_STATUS_LABELS[status] ?? status;
}

export function getAiTaskDeliveryChannelLabel(channel: string): string {
  return AI_TASK_DELIVERY_CHANNEL_LABELS[channel] ?? channel;
}

export const AI_TASK_EMAIL_PROVIDER_STATUS_LABELS: Readonly<Record<string, string>> = {
  accepted: "Sunucu tarafından kabul edildi",
  rejected: "Reddedildi",
  "transport-failed": "Gönderim başarısız",
};

export const AI_TASK_EMAIL_READINESS_LABELS: Readonly<Record<string, string>> = {
  "not-configured": "Yapılandırılmadı",
  configured: "Yapılandırıldı",
  ready: "Hazır",
  degraded: "Kısıtlı",
  "invalid-credentials": "Kimlik bilgileri geçersiz",
  disabled: "Devre dışı",
};

export function getAiTaskEmailProviderStatusLabel(status: string | null | undefined): string {
  if (!status) return "—";
  return AI_TASK_EMAIL_PROVIDER_STATUS_LABELS[status] ?? "—";
}

export function getAiTaskEmailReadinessLabel(channel: {
  available: boolean;
  reasonCode: string | null;
}): string {
  if (channel.available) return "Hazır";
  const code = channel.reasonCode ?? "not-configured";
  return AI_TASK_EMAIL_READINESS_LABELS[code] ?? "Henüz yapılandırılmadı";
}

export function maskEmailForDisplay(email: string): string {
  const atIndex = email.indexOf("@");
  if (atIndex <= 0) return "***";
  const local = email.slice(0, atIndex);
  const domain = email.slice(atIndex + 1);
  const maskedLocal = local.length <= 1 ? "*" : `${local[0]}***${local[local.length - 1]}`;
  return `${maskedLocal}@${domain}`;
}

export const AI_TASK_WHATSAPP_PROVIDER_STATUS_LABELS: Readonly<Record<string, string>> = {
  accepted: "Sağlayıcı Kabul Etti",
  sent: "Gönderildi",
  delivered: "Teslim Edildi",
  read: "Okundu",
  failed: "Başarısız",
};

export const AI_TASK_WHATSAPP_READINESS_LABELS: Readonly<Record<string, string>> = {
  "not-configured": "Yapılandırılmadı",
  configured: "Yapılandırıldı",
  ready: "Hazır",
  degraded: "Kısıtlı",
  "invalid-credentials": "Kimlik bilgileri geçersiz",
  disabled: "Devre dışı",
};

export const AI_TASK_WHATSAPP_CONSENT_LABELS: Readonly<Record<string, string>> = {
  known: "Onaylı",
  unknown: "Bilinmiyor",
  "not-verified": "Doğrulanmadı",
};

export function getAiTaskWhatsAppProviderStatusLabel(status: string | null | undefined): string {
  if (!status) return "—";
  return AI_TASK_WHATSAPP_PROVIDER_STATUS_LABELS[status] ?? "—";
}

export function getAiTaskWhatsAppReadinessLabel(channel: {
  available: boolean;
  reasonCode: string | null;
}): string {
  if (channel.available) return "Hazır";
  const code = channel.reasonCode ?? "not-configured";
  return AI_TASK_WHATSAPP_READINESS_LABELS[code] ?? "Henüz yapılandırılmadı";
}

export function getAiTaskWhatsAppConsentLabel(status: string | null | undefined): string {
  if (!status) return AI_TASK_WHATSAPP_CONSENT_LABELS.unknown;
  return AI_TASK_WHATSAPP_CONSENT_LABELS[status] ?? AI_TASK_WHATSAPP_CONSENT_LABELS.unknown;
}

export const AI_TASK_SECURE_ACCESS_TTL_OPTIONS = [
  { value: "1h", label: "1 saat" },
  { value: "8h", label: "8 saat" },
  { value: "24h", label: "24 saat" },
  { value: "48h", label: "48 saat" },
  { value: "7d", label: "7 gün" },
] as const;
