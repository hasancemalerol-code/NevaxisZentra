import type { SerializableNavigationItem } from "./navigation-types";

/** Primary workflow-first homes (ULTRA A). */
export const WORKFLOW_PRIMARY_HOMES: readonly SerializableNavigationItem[] = [
  { label: "Bugün", href: "/", iconKey: "list-todo", exact: true },
  { label: "Fabrika", href: "/fabrika", iconKey: "factory" },
  { label: "Plan", href: "/planlama", iconKey: "calendar-range" },
  { label: "İşler", href: "/isler", iconKey: "activity" },
  { label: "Uygulamalar", href: "/uygulamalar", iconKey: "boxes" },
] as const;

/** Nav routes that are empty shells — hidden from primary/apps catalog. */
export const HIDDEN_PLACEHOLDER_NAV_HREFS = new Set([
  "/riskler/malzeme-uygunlugu",
  "/riskler/tedarik",
  "/riskler/siparis-termin",
  "/riskler/maliyet-sapmalari",
  "/riskler/kapasite",
  "/riskler/kalite",
  "/raporlar",
]);

export type WorkflowHomeId = "bugun" | "fabrika" | "plan" | "isler" | "apps";

export type ContextualNavigationSection = Readonly<{
  homeId: WorkflowHomeId;
  label: string;
  items: readonly SerializableNavigationItem[];
}>;

const BUGUN_CONTEXT: ContextualNavigationSection = {
  homeId: "bugun",
  label: "Bugün",
  items: [
    { label: "Öncelikli İşler", href: "/", iconKey: "list-todo", exact: true },
    { label: "Vardiyalar", href: "/vardiyalar", iconKey: "calendar-clock", permissionKey: "shift-workforce.read", subscriptionModulePath: "/vardiyalar" },
    { label: "Karar Merkezi", href: "/fabrika-karar-merkezi", iconKey: "gauge", permissionKey: "factory-case.read", subscriptionModulePath: "/operasyon/yonetim-merkezi" },
    { label: "Yönetici Komuta", href: "/yonetici-komuta-merkezi", iconKey: "gauge", permissionKey: "factory-case.read", subscriptionModulePath: "/operasyon/yonetim-merkezi" },
    { label: "Operasyon Yönetimi", href: "/operasyon/yonetim-merkezi", iconKey: "layout-dashboard", subscriptionModulePath: "/operasyon/yonetim-merkezi" },
    { label: "Fabrika Zekâsı", href: "/fabrika-zekasi", iconKey: "circle-gauge", permissionKey: "factory-intelligence.read" },
    { label: "Sunum Modu", href: "/sunum-modu", iconKey: "activity", permissionKey: "presentation-mode.access" },
  ],
};

const FABRIKA_CONTEXT: ContextualNavigationSection = {
  homeId: "fabrika",
  label: "Fabrika",
  items: [
    { label: "Canlı Durum", href: "/fabrika", iconKey: "factory", exact: true },
    { label: "Üretim Emirleri", href: "/uretim", iconKey: "factory", subscriptionModulePath: "/uretim" },
    { label: "Hatlar", href: "/uretim/hatlar", iconKey: "factory", permissionKey: "mixed-production.read", subscriptionModulePath: "/uretim" },
    { label: "Kaynaklar", href: "/uretim/kaynaklar", iconKey: "cpu", permissionKey: "mixed-production.read", subscriptionModulePath: "/uretim" },
    { label: "Makineler", href: "/makineler", iconKey: "cpu", permissionKey: "edge.read", subscriptionModulePath: "/makineler" },
    { label: "Proses & OEE", href: "/proses-oee", iconKey: "circle-gauge", permissionKey: "process-oee.read", subscriptionModulePath: "/proses-oee" },
  ],
};

const PLAN_CONTEXT: ContextualNavigationSection = {
  homeId: "plan",
  label: "Plan",
  items: [
    { label: "Canlı Plan", href: "/planlama", iconKey: "calendar-range", subscriptionModulePath: "/planlama", exact: true },
    { label: "Fabrika Ağı", href: "/fabrika-agi", iconKey: "factory", permissionKey: "factory-network.read", subscriptionModulePath: "/planlama" },
    { label: "Siparişler (Önizleme)", href: "/siparisler", iconKey: "shopping-basket", subscriptionModulePath: "/siparisler" },
  ],
};

const ISLER_CONTEXT: ContextualNavigationSection = {
  homeId: "isler",
  label: "İşler",
  items: [
    { label: "Açık İşler", href: "/isler", iconKey: "activity", exact: true },
    { label: "Dijital İş Gücü", href: "/dijital-is-gucu", iconKey: "users-round", permissionKey: "digital-workforce.read" },
    { label: "Onaylar", href: "/dijital-is-gucu/onaylar", iconKey: "shield-check", permissionKey: "digital-workforce.read" },
    { label: "Sonuçlar", href: "/dijital-is-gucu/sonuclar", iconKey: "circle-gauge", permissionKey: "digital-workforce.read" },
    { label: "FactOS Teams", href: "/teams", iconKey: "messages-square", subscriptionModulePath: "/teams" },
  ],
};

export const CONTEXTUAL_NAVIGATION_SECTIONS: readonly ContextualNavigationSection[] = [
  BUGUN_CONTEXT,
  FABRIKA_CONTEXT,
  PLAN_CONTEXT,
  ISLER_CONTEXT,
];

const BUGUN_PREFIXES = [
  "/",
  "/vardiyalar",
  "/yonetici-komuta-merkezi",
  "/fabrika-karar-merkezi",
  "/operasyon/yonetim-merkezi",
  "/fabrika-zekasi",
  "/sunum-modu",
  "/golden-demo",
];

const FABRIKA_PREFIXES = ["/fabrika", "/uretim", "/makineler", "/proses-oee"];

const PLAN_PREFIXES = ["/planlama", "/fabrika-agi", "/siparisler"];

const ISLER_PREFIXES = ["/isler", "/dijital-is-gucu", "/teams", "/ai-gorevleri"];

const APPS_PREFIXES = ["/uygulamalar", "/bakim", "/kalite", "/malzemeler", "/kaliplar", "/enerji", "/iot-plc", "/ihracat", "/ticari-zeka", "/erp-entegrasyonlari", "/operasyonel-riskler", "/satin-alma", "/ayarlar", "/stok-ve-depo"];

function matchesPrefix(pathname: string, prefix: string): boolean {
  if (prefix === "/") {
    return pathname === "/";
  }

  return pathname === prefix || pathname.startsWith(`${prefix}/`);
}

export function resolveActiveWorkflowHome(pathname: string): WorkflowHomeId {
  if (APPS_PREFIXES.some((prefix) => matchesPrefix(pathname, prefix))) {
    return "apps";
  }

  if (ISLER_PREFIXES.some((prefix) => matchesPrefix(pathname, prefix))) {
    return "isler";
  }

  if (PLAN_PREFIXES.some((prefix) => matchesPrefix(pathname, prefix))) {
    return "plan";
  }

  if (FABRIKA_PREFIXES.some((prefix) => matchesPrefix(pathname, prefix))) {
    return "fabrika";
  }

  if (BUGUN_PREFIXES.some((prefix) => matchesPrefix(pathname, prefix))) {
    return "bugun";
  }

  return "apps";
}

export function resolveContextualNavigationSection(
  pathname: string,
): ContextualNavigationSection | null {
  const homeId = resolveActiveWorkflowHome(pathname);
  if (homeId === "apps") {
    return null;
  }

  return CONTEXTUAL_NAVIGATION_SECTIONS.find((section) => section.homeId === homeId) ?? null;
}
