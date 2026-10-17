import { requiredRoutePermission } from "@/lib/authorization/acceptance-role-contract";
import type { SerializableNavigationItem } from "./navigation-types";

/** Primary workflow-first homes (ULTRA A). */
export const WORKFLOW_PRIMARY_HOMES: readonly SerializableNavigationItem[] = [
  { label: "Bugün", href: "/", iconKey: "list-todo", exact: true },
  { label: "Fabrika", href: "/fabrika", iconKey: "factory", permissionKey: requiredRoutePermission("fabrika") },
  { label: "Plan", href: "/planlama", iconKey: "calendar-range", permissionKey: requiredRoutePermission("plan") },
  { label: "İşler", href: "/isler", iconKey: "activity", permissionKey: requiredRoutePermission("isler") },
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

export type WorkflowHomeId = "bugun" | "fabrika" | "plan" | "isler" | "ticari-zeka" | "apps";

export type ContextualNavigationSection = Readonly<{
  homeId: WorkflowHomeId;
  label: string;
  items: readonly SerializableNavigationItem[];
}>;

const BUGUN_CONTEXT: ContextualNavigationSection = {
  homeId: "bugun",
  label: "Bugün",
  items: [
    { label: "Vardiyalar", href: "/vardiyalar", iconKey: "calendar-clock", permissionKey: "shift-workforce.read", subscriptionModulePath: "/vardiyalar" },
    { label: "Vardiya Devri", href: "/vardiya-devir", iconKey: "users-round", permissionKey: "shift-workforce.read", subscriptionModulePath: "/vardiyalar" },
    { label: "Karar Merkezi", href: "/fabrika-karar-merkezi", iconKey: "gauge", permissionKey: "factory-case.read", subscriptionModulePath: "/operasyon/yonetim-merkezi" },
    { label: "Yönetici Komuta", href: "/yonetici-komuta-merkezi", iconKey: "chart-no-axes-combined", permissionKey: "factory-case.read", subscriptionModulePath: "/operasyon/yonetim-merkezi" },
    { label: "Operasyon Yönetimi", href: "/operasyon/yonetim-merkezi", iconKey: "layout-dashboard", subscriptionModulePath: "/operasyon/yonetim-merkezi" },
    { label: "Fabrika Zekâsı", href: "/fabrika-zekasi", iconKey: "circle-gauge", permissionKey: "factory-intelligence.read" },
    { label: "Sunum Modu", href: "/sunum-modu", iconKey: "activity", permissionKey: "presentation-mode.access" },
  ],
};

const FABRIKA_CONTEXT: ContextualNavigationSection = {
  homeId: "fabrika",
  label: "Fabrika",
  items: [
    { label: "Genel Bakış", href: "/fabrika", iconKey: "factory", permissionKey: requiredRoutePermission("fabrika") },
    { label: "Hatlar", href: "/uretim/hatlar", iconKey: "factory", permissionKey: requiredRoutePermission("hatlar") },
    {
      label: "Makineler ve Kaynaklar",
      href: "/uretim/kaynaklar",
      iconKey: "cpu",
      permissionKey: requiredRoutePermission("kaynaklar"),
    },
    { label: "Üretim Emirleri", href: "/uretim", iconKey: "factory", subscriptionModulePath: "/uretim" },
    { label: "Makineler", href: "/makineler", iconKey: "cpu", permissionKey: "edge.read", subscriptionModulePath: "/makineler" },
    { label: "Proses ve OEE", href: "/proses-oee", iconKey: "circle-gauge", permissionKey: "process-oee.read", subscriptionModulePath: "/proses-oee" },
    { label: "Enerji", href: "/enerji", iconKey: "zap", permissionKey: "energy.read", subscriptionModulePath: "/enerji" },
    { label: "Kalite", href: "/kalite", iconKey: "shield-check", subscriptionModulePath: "/kalite" },
    { label: "Kalıp ve Takımlar", href: "/kaliplar", iconKey: "molds", permissionKey: "mold.read", subscriptionModulePath: "/kaliplar" },
  ],
};

const PLAN_CONTEXT: ContextualNavigationSection = {
  homeId: "plan",
  label: "Plan",
  items: [
    { label: "Canlı Plan", href: "/planlama", iconKey: "calendar-range", subscriptionModulePath: "/planlama", permissionKey: requiredRoutePermission("plan") },
    { label: "Fabrika Ağı", href: "/fabrika-agi", iconKey: "factory", permissionKey: "factory-network.read", subscriptionModulePath: "/planlama" },
    { label: "Siparişler", href: "/siparisler", iconKey: "shopping-basket", subscriptionModulePath: "/siparisler" },
    { label: "Senaryo Kurtarma", href: "/senaryo-kurtarma", iconKey: "triangle-alert", permissionKey: requiredRoutePermission("senaryo"), subscriptionModulePath: "/planlama" },
  ],
};

const ISLER_CONTEXT: ContextualNavigationSection = {
  homeId: "isler",
  label: "İşler",
  items: [
    { label: "Açık İşler", href: "/isler", iconKey: "activity", permissionKey: requiredRoutePermission("isler") },
    { label: "Görev Merkezi", href: "/ai-gorevleri", iconKey: "list-todo" },
    { label: "Dijital İş Gücü", href: "/dijital-is-gucu", iconKey: "users-round", permissionKey: "digital-workforce.read" },
    { label: "Onaylar", href: "/dijital-is-gucu/onaylar", iconKey: "shield-check", permissionKey: "digital-workforce.read" },
    { label: "Sonuçlar", href: "/dijital-is-gucu/sonuclar", iconKey: "circle-gauge", permissionKey: "digital-workforce.read" },
    { label: "Zentra Teams", href: "/teams", iconKey: "messages-square", subscriptionModulePath: "/teams" },
  ],
};

const TICARI_ZEKA_CONTEXT: ContextualNavigationSection = {
  homeId: "ticari-zeka",
  label: "Ticari Zekâ",
  items: [
    { label: "Satın Alma Fiyat Analizi", href: "/ticari-zeka/satin-alma-fiyat-analizi", iconKey: "shopping-basket", permissionKey: "commercial-intelligence.read", subscriptionModulePath: "/ticari-zeka" },
    { label: "Tedarikçi Karşılaştırması", href: "/ticari-zeka/tedarikci-karsilastirmasi", iconKey: "users-round", permissionKey: "commercial-intelligence.read", subscriptionModulePath: "/ticari-zeka" },
    { label: "Satış Fiyat Analizi", href: "/ticari-zeka/satis-fiyat-analizi", iconKey: "badge-dollar-sign", permissionKey: "commercial-intelligence.read", subscriptionModulePath: "/ticari-zeka" },
    { label: "Ürün Kârlılığı", href: "/ticari-zeka/urun-karliligi", iconKey: "circle-gauge" },
    { label: "Müşteri Kârlılığı", href: "/ticari-zeka/musteri-karliligi", iconKey: "users-round" },
    { label: "Malzeme ve Maliyet", href: "/ticari-zeka/malzeme-ve-maliyet-karliligi", iconKey: "boxes" },
    { label: "Kârlılık Uyarıları", href: "/ticari-zeka/karlilik-uyarilari", iconKey: "triangle-alert", permissionKey: "commercial-intelligence.read", subscriptionModulePath: "/ticari-zeka" },
    { label: "Fiyat Koruma ve Onay", href: "/ticari-zeka/fiyat-koruma-ve-onay", iconKey: "shield-check", permissionKey: "commercial-intelligence.read", subscriptionModulePath: "/ticari-zeka" },
    { label: "Kârlılık Yönetim Merkezi", href: "/ticari-zeka/karlilik-yonetim-merkezi", iconKey: "layout-dashboard", permissionKey: "commercial-intelligence.read", subscriptionModulePath: "/ticari-zeka" },
    { label: "Teklif Riskleri", href: "/ticari-zeka/teklif-riskleri", iconKey: "file-warning" },
  ],
};

const APPS_CONTEXT: ContextualNavigationSection = {
  homeId: "apps",
  label: "Uygulamalar",
  items: [
    { label: "Uygulama Kataloğu", href: "/uygulamalar", iconKey: "boxes", exact: true },
    { label: "Bakım", href: "/bakim", iconKey: "wrench", permissionKey: "maintenance.read", subscriptionModulePath: "/bakim" },
    { label: "Malzemeler", href: "/malzemeler", iconKey: "package", permissionKey: "material.read", subscriptionModulePath: "/malzemeler" },
    { label: "Stok ve Depo Riskleri", href: "/stok-ve-depo/malzeme-riskleri", iconKey: "package-search" },
    { label: "Operasyonel Riskler", href: "/operasyonel-riskler", iconKey: "triangle-alert", permissionKey: "operational-risk.read", subscriptionModulePath: "/riskler" },
    { label: "IoT ve PLC", href: "/iot-plc", iconKey: "cable", permissionKey: "iot-plc.read", subscriptionModulePath: "/iot-plc" },
    { label: "İhracat", href: "/ihracat", iconKey: "ship", subscriptionModulePath: "/ihracat" },
    { label: "ERP Entegrasyonları", href: "/erp-entegrasyonlari/baglanti", iconKey: "cable", matchPrefix: "/erp-entegrasyonlari" },
    { label: "Ayarlar", href: "/ayarlar", iconKey: "settings", permissionKey: "admin-access.read" },
  ],
};

export const CONTEXTUAL_NAVIGATION_SECTIONS: readonly ContextualNavigationSection[] = [
  BUGUN_CONTEXT,
  FABRIKA_CONTEXT,
  PLAN_CONTEXT,
  ISLER_CONTEXT,
  TICARI_ZEKA_CONTEXT,
  APPS_CONTEXT,
];

const BUGUN_PREFIXES = [
  "/",
  "/dashboard",
  "/vardiyalar",
  "/vardiya-devir",
  "/yonetici-komuta-merkezi",
  "/fabrika-karar-merkezi",
  "/operasyon/yonetim-merkezi",
  "/fabrika-zekasi",
  "/sunum-modu",
  "/golden-demo",
];

const FABRIKA_PREFIXES = ["/fabrika", "/uretim", "/makineler", "/proses-oee", "/enerji", "/kalite", "/kaliplar"];

const PLAN_PREFIXES = ["/planlama", "/senaryo-kurtarma", "/fabrika-agi", "/siparisler"];

const ISLER_PREFIXES = ["/isler", "/dijital-is-gucu", "/teams", "/ai-gorevleri"];

const TICARI_ZEKA_PREFIXES = ["/ticari-zeka"];

const APPS_PREFIXES = ["/uygulamalar", "/bakim", "/malzemeler", "/iot-plc", "/ihracat", "/erp-entegrasyonlari", "/operasyonel-riskler", "/satin-alma", "/ayarlar", "/stok-ve-depo"];

function matchesPrefix(pathname: string, prefix: string): boolean {
  if (prefix === "/") {
    return pathname === "/";
  }

  return pathname === prefix || pathname.startsWith(`${prefix}/`);
}

export function resolveActiveWorkflowHome(pathname: string): WorkflowHomeId {
  if (TICARI_ZEKA_PREFIXES.some((prefix) => matchesPrefix(pathname, prefix))) {
    return "ticari-zeka";
  }

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

  return CONTEXTUAL_NAVIGATION_SECTIONS.find((section) => section.homeId === homeId) ?? null;
}
