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
  "crevafactos-teams": "CrevaFactOS Teams",
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

export const AI_TASK_SECURE_ACCESS_TTL_OPTIONS = [
  { value: "1h", label: "1 saat" },
  { value: "8h", label: "8 saat" },
  { value: "24h", label: "24 saat" },
  { value: "48h", label: "48 saat" },
  { value: "7d", label: "7 gün" },
] as const;
