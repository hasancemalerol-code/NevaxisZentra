const INVOICE_STATUS_LABELS: Record<string, string> = {
  draft: "Taslak",
  open: "Açık",
  paid: "Ödendi",
  overdue: "Gecikmiş",
  void: "İptal",
};

const PAYMENT_STATUS_LABELS: Record<string, string> = {
  pending: "Beklemede",
  "awaiting-manual-verification": "Manuel doğrulama bekliyor",
  succeeded: "Başarılı",
  failed: "Başarısız",
  cancelled: "İptal edildi",
  refunded: "İade edildi",
};

const PAYMENT_METHOD_LABELS: Record<string, string> = {
  card: "Kart",
  "bank-transfer": "Banka havalesi",
  "enterprise-invoice": "Kurumsal fatura",
};

const COMPONENT_TYPE_LABELS: Record<string, string> = {
  "tenant-base": "Temel abonelik",
  "included-facility": "Dahil tesis",
  "additional-facility": "Ek tesis",
  "included-connected-machine": "Dahil makine",
  "additional-connected-machine": "Ek makine",
  "included-user": "Dahil kullanıcı",
  "additional-user": "Ek kullanıcı",
};

export function formatInvoiceStatus(status: string): string {
  return INVOICE_STATUS_LABELS[status] ?? "Bilinmeyen durum";
}

export function formatPaymentStatus(status: string): string {
  return PAYMENT_STATUS_LABELS[status] ?? "Bilinmeyen durum";
}

export function formatPaymentMethod(method: string): string {
  return PAYMENT_METHOD_LABELS[method] ?? "Bilinmeyen yöntem";
}

export function formatComponentType(componentType: string): string {
  return COMPONENT_TYPE_LABELS[componentType] ?? componentType;
}

export function formatMoney(amount: number, currencyCode: string): string {
  return new Intl.NumberFormat("tr-TR", {
    style: "currency",
    currency: currencyCode,
    minimumFractionDigits: 2,
  }).format(amount);
}

export function formatBillingDateTime(value: string | null): string {
  if (!value) return "—";
  return new Intl.DateTimeFormat("tr-TR", {
    dateStyle: "medium",
    timeStyle: "short",
  }).format(new Date(value));
}

export function invoiceStatusBadgeClass(status: string): string {
  switch (status) {
    case "paid":
      return "saas-admin-badge saas-admin-badge--success";
    case "open":
      return "saas-admin-badge saas-admin-badge--info";
    case "overdue":
      return "saas-admin-badge saas-admin-badge--warning";
    case "void":
      return "saas-admin-badge saas-admin-badge--muted";
    default:
      return "saas-admin-badge";
  }
}

export function paymentStatusBadgeClass(status: string): string {
  switch (status) {
    case "succeeded":
      return "saas-admin-badge saas-admin-badge--success";
    case "awaiting-manual-verification":
      return "saas-admin-badge saas-admin-badge--warning";
    case "failed":
      return "saas-admin-badge saas-admin-badge--danger";
    default:
      return "saas-admin-badge";
  }
}

export const CARD_PROVIDER_NOT_CONFIGURED_MESSAGE =
  "Kartla ödeme sağlayıcısı henüz yapılandırılmadı.";

export const BILLING_DEVELOPMENT_NOTICE =
  "Geliştirme ortamı — üretim fiyatlandırması veya gerçek ödeme toplama bu pakette sunulmaz.";
