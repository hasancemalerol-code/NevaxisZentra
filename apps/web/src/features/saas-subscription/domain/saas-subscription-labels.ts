import type {
  SaasCommercialEntitlement,
  SaasSubscriptionLimitEvidence,
  SaasSubscriptionSummary,
  TenantEntitlementEvaluation,
  TenantSubscriptionCurrent,
} from "./saas-subscription-types";

const SUBSCRIPTION_STATUS_LABELS: Record<string, string> = {
  trialing: "Deneme",
  active: "Aktif",
  "grace-period": "Ek süre",
  suspended: "Askıda",
  cancelled: "İptal edildi",
  expired: "Süresi doldu",
};

const BILLING_CADENCE_LABELS: Record<string, string> = {
  monthly: "Aylık",
  annual: "Yıllık",
};

const SOURCE_PROVENANCE_LABELS: Record<string, string> = {
  "local-development-pilot": "Geliştirme ortamı",
  "non-production": "Üretim dışı ortam",
  "billing-system": "Faturalama sistemi",
  "manual-override": "Manuel atama",
};

const MODULE_ENTITLEMENT_LABELS: Record<string, string> = {
  "module.dashboard": "Dashboard",
  "module.teams": "Teams",
  "module.orders": "Siparişler",
  "module.planning": "Planlama",
  "module.production": "Üretim",
  "module.export": "İhracat",
  "module.shift-workforce": "Vardiyalar",
  "module.process-oee": "Proses ve OEE",
  "module.connected-machines": "Makineler",
  "module.mold": "Kalıplar",
  "module.material": "Malzemeler",
  "module.maintenance": "Bakım",
  "module.quality": "Kalite Kontrol",
  "module.energy": "Enerji",
  "module.iot-plc": "IoT ve PLC",
  "module.reporting": "Raporlama",
  "module.erp-integration": "ERP entegrasyonları",
  "module.commercial-intelligence": "Ticari zekâ",
  "module.operational-risk": "Operasyonel riskler",
  "module.ai-task-center": "AI Görev Merkezi",
};

const LIMIT_LABELS: Record<string, string> = {
  "user-limit": "Kullanıcı",
  "facility-limit": "Tesis",
  "connected-machine-limit": "Bağlı makine",
};

const ROUTE_ENTITLEMENT_MAPPINGS: readonly Readonly<{ prefix: string; code: string }>[] = [
  { prefix: "/dashboard", code: "module.dashboard" },
  { prefix: "/fabrika-vitrini", code: "module.dashboard" },
  { prefix: "/teams", code: "module.teams" },
  { prefix: "/siparisler", code: "module.orders" },
  { prefix: "/planlama", code: "module.planning" },
  { prefix: "/uretim", code: "module.production" },
  { prefix: "/ihracat", code: "module.export" },
  { prefix: "/vardiyalar", code: "module.shift-workforce" },
  { prefix: "/proses-oee", code: "module.process-oee" },
  { prefix: "/makineler", code: "module.connected-machines" },
  { prefix: "/kaliplar", code: "module.mold" },
  { prefix: "/malzemeler", code: "module.material" },
  { prefix: "/bakim", code: "module.maintenance" },
  { prefix: "/kalite", code: "module.quality" },
  { prefix: "/enerji", code: "module.energy" },
  { prefix: "/iot-plc", code: "module.iot-plc" },
  { prefix: "/raporlar", code: "module.reporting" },
  { prefix: "/erp-entegrasyonlari", code: "module.erp-integration" },
  { prefix: "/ticari-zeka", code: "module.commercial-intelligence" },
  { prefix: "/operasyonel-riskler", code: "module.operational-risk" },
  { prefix: "/riskler", code: "module.operational-risk" },
  { prefix: "/ai-gorevleri", code: "module.ai-task-center" },
];

export const SUBSCRIPTION_RECOVERY_ROUTES = ["/ayarlar", "/abonelik-erisimi"] as const;

function humanizeToken(value: string): string {
  return value
    .split(/[-_.]/)
    .filter(Boolean)
    .map((part) => part.charAt(0).toUpperCase() + part.slice(1))
    .join(" ");
}

export function normalizeSubscriptionRoute(routePath: string): string {
  const trimmed = routePath.trim();
  if (trimmed.length === 0) {
    return "/";
  }

  const withLeadingSlash = trimmed.startsWith("/") ? trimmed : `/${trimmed}`;
  const normalized = withLeadingSlash.replace(/\/+$/, "").toLowerCase();
  return normalized.length === 0 ? "/" : normalized;
}

export function isSubscriptionRecoveryRoute(routePath: string): boolean {
  const normalized = normalizeSubscriptionRoute(routePath);
  return SUBSCRIPTION_RECOVERY_ROUTES.some(
    (route) => normalized === route || normalized.startsWith(`${route}/`),
  );
}

export function resolveEntitlementCodeFromPath(routePath: string): string | null {
  if (isSubscriptionRecoveryRoute(routePath)) {
    return null;
  }

  const normalized = normalizeSubscriptionRoute(routePath);
  for (const mapping of ROUTE_ENTITLEMENT_MAPPINGS) {
    if (normalized === mapping.prefix || normalized.startsWith(`${mapping.prefix}/`)) {
      return mapping.code;
    }
  }

  return null;
}

export function resolveSubscriptionAccessPathname(routePath: string): string {
  return normalizeSubscriptionRoute(routePath);
}

export function formatSubscriptionStatus(status: string | null | undefined): string {
  if (!status) {
    return "Yetersiz veri";
  }

  return SUBSCRIPTION_STATUS_LABELS[status] ?? "Yetersiz veri";
}

export function formatBillingCadence(cadence: string | null | undefined): string {
  if (!cadence) {
    return "Yetersiz veri";
  }

  return BILLING_CADENCE_LABELS[cadence] ?? "Yetersiz veri";
}

export function formatSourceProvenance(provenance: string | null | undefined): string {
  if (!provenance) {
    return "Yetersiz veri";
  }

  return SOURCE_PROVENANCE_LABELS[provenance] ?? humanizeToken(provenance);
}

export function formatPlanLabel(subscription: SaasSubscriptionSummary): string {
  if (subscription.planCode) {
    return humanizeToken(subscription.planCode);
  }

  return "Plan bilgisi yok";
}

export function formatEntitlementLabel(code: string): string {
  return MODULE_ENTITLEMENT_LABELS[code] ?? humanizeToken(code.replace(/^module\./, ""));
}

export function formatLimitLabel(entitlementCode: string | null | undefined): string {
  if (!entitlementCode) {
    return "Kullanım limiti";
  }

  return LIMIT_LABELS[entitlementCode] ?? "Kullanım limiti";
}

export function formatDateTime(value: string | null | undefined): string {
  if (!value) {
    return "—";
  }

  return new Intl.DateTimeFormat("tr-TR", {
    dateStyle: "medium",
    timeStyle: "short",
  }).format(new Date(value));
}

export function formatUsageLimit(limit: SaasSubscriptionLimitEvidence | null | undefined): string {
  if (!limit || !limit.limitConfigured) {
    return "Yetersiz veri";
  }

  const usage = limit.usageValue ?? 0;
  const maximum = limit.limitValue ?? "—";
  return `${usage} / ${maximum}`;
}

export function formatOverLimitWarning(evaluation: TenantEntitlementEvaluation): string | null {
  if (evaluation.allowed || !evaluation.limit?.limitConfigured) {
    return null;
  }

  if (evaluation.reasonCode.includes("limit-exceeded")) {
    const label = formatLimitLabel(evaluation.entitlementCode);
    return `${label} limiti aşıldı (${formatUsageLimit(evaluation.limit)}).`;
  }

  return null;
}

export function formatGraceWarning(
  subscription: TenantSubscriptionCurrent | null,
): string | null {
  if (!subscription) {
    return null;
  }

  const { subscription: summary, paidApplicationAccess } = subscription;
  const inGrace =
    summary.subscriptionStatus === "grace-period"
    || paidApplicationAccess.graceState?.inGracePeriod === true;

  if (!inGrace) {
    return null;
  }

  const until = summary.graceUntilUtc ?? paidApplicationAccess.graceState?.graceUntilUtc;
  if (until) {
    return `Aboneliğiniz ek süre döneminde. Erişim ${formatDateTime(until)} tarihine kadar devam eder.`;
  }

  return "Aboneliğiniz ek süre döneminde. Ödeme veya plan güncellemesi gerekebilir.";
}

export function formatInsufficientData(): string {
  return "Abonelik bilgisi şu anda yüklenemedi. Lütfen daha sonra tekrar deneyin.";
}

export function formatAccessDeniedHeadline(
  denialReason: "paid-application" | "module" | "service-unavailable" | null,
): string {
  if (denialReason === "module") {
    return "Bu modüle erişim aboneliğinizde tanımlı değil";
  }

  if (denialReason === "service-unavailable") {
    return "Abonelik bilgisi şu anda yüklenemedi";
  }

  return "Uygulama erişimi şu anda kullanılamıyor";
}

export function formatAccessDeniedSummary(
  subscription: TenantSubscriptionCurrent | null,
  denialReason: "paid-application" | "module" | "service-unavailable" | null,
): string {
  if (denialReason === "service-unavailable") {
    return formatInsufficientData();
  }

  if (!subscription) {
    return formatInsufficientData();
  }

  const statusLabel = formatSubscriptionStatus(subscription.subscription.subscriptionStatus);

  if (denialReason === "module") {
    return `Mevcut planınız (${statusLabel}) bu bölümü kapsamıyor. Erişim için planınızı yükseltmeniz veya yöneticinizle iletişime geçmeniz gerekebilir.`;
  }

  switch (subscription.subscription.subscriptionStatus) {
    case "suspended":
      return "Aboneliğiniz askıya alındı. Faturalandırma veya destek ekibiyle iletişime geçerek erişimi yeniden açabilirsiniz.";
    case "cancelled":
      return subscription.subscription.cancelAtPeriodEnd
        ? `Aboneliğiniz iptal edildi; dönem sonuna kadar (${formatDateTime(subscription.subscription.currentPeriodEndUtc)}) sınırlı erişim sürebilir.`
        : "Aboneliğiniz iptal edildi. Uygulamaya devam etmek için yeni bir plan seçmeniz gerekir.";
    case "expired":
      return "Abonelik süreniz doldu. Uygulamaya devam etmek için planınızı yenilemeniz gerekir.";
    case "trialing":
      return "Deneme süreniz sona erdi veya deneme koşulları karşılanmıyor.";
    default:
      return `Abonelik durumunuz: ${statusLabel}. Erişim için yöneticinizle veya destek ekibiyle iletişime geçin.`;
  }
}

export function listModuleEntitlementSummary(
  entitlements: readonly SaasCommercialEntitlement[],
): readonly string[] {
  return entitlements
    .filter((entry) => entry.code.startsWith("module.") && entry.booleanValue === true)
    .map((entry) => entry.displayNameTr || formatEntitlementLabel(entry.code))
    .sort((left, right) => left.localeCompare(right, "tr"));
}

export function isModuleEntitled(
  subscription: TenantSubscriptionCurrent,
  entitlementCode: string,
): boolean {
  if (!subscription.paidApplicationAccess.allowed) {
    return false;
  }

  if (entitlementCode === "module.shift-workforce") {
    return subscription.moduleAccess.shiftWorkforce.allowed;
  }

  const entitlement = subscription.entitlements.find((entry) => entry.code === entitlementCode);
  if (!entitlement) {
    return true;
  }

  if (entitlement.semantics === "unavailable" || entitlement.semantics === "unknown") {
    return false;
  }

  if (
    entitlement.valueKind === "module-access"
    || entitlement.valueKind === "feature-access"
    || entitlement.valueKind === "boolean-capability"
  ) {
    if (entitlement.semantics === "unlimited") {
      return true;
    }

    if (entitlement.semantics === "limited") {
      return entitlement.booleanValue === true;
    }

    return false;
  }

  return true;
}

export function isGraceWarningActive(subscription: TenantSubscriptionCurrent | null): boolean {
  return formatGraceWarning(subscription) !== null;
}
