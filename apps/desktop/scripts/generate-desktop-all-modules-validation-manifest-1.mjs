#!/usr/bin/env node
import { readFileSync, writeFileSync } from "node:fs";
import { dirname, join, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), "../../..");
const OUT = join(ROOT, "apps/desktop/scripts/desktop-all-modules-validation-manifest-1.json");

const EXTRA_SALES_ROUTES = [
  {
    group: "Çalışma alanı",
    label: "Playbooklar",
    href: "/ai-gorevleri/playbooklar",
    permissionKey: null,
    subscriptionModulePath: "/ai-gorevleri",
    expectedVisible: false,
    salesCritical: true,
    notes: "Sales surface; not in primary sidebar",
  },
  {
    group: "Çalışma alanı",
    label: "Otomasyon",
    href: "/ai-gorevleri/otomasyon",
    permissionKey: null,
    subscriptionModulePath: "/ai-gorevleri",
    expectedVisible: false,
    salesCritical: true,
    notes: "Sales surface; not in primary sidebar",
  },
];

const SUBSCRIPTION_MODULE_BY_PATH = new Map([
  ["/", "module.ai-task-center"],
  ["/dashboard", "module.dashboard"],
  ["/operasyon/yonetim-merkezi", "module.production"],
  ["/teams", "module.teams"],
  ["/siparisler", "module.orders"],
  ["/planlama", "module.planning"],
  ["/uretim", "module.production"],
  ["/ihracat", "module.export"],
  ["/vardiyalar", "module.shift-workforce"],
  ["/vardiya-devir", "module.shift-workforce"],
  ["/proses-oee", "module.process-oee"],
  ["/makineler", "module.connected-machines"],
  ["/kaliplar", "module.mold"],
  ["/malzemeler", "module.material"],
  ["/stok-ve-depo/malzeme-riskleri", "module.material"],
  ["/bakim", "module.maintenance"],
  ["/kalite", "module.quality"],
  ["/enerji", "module.energy"],
  ["/iot-plc", "module.iot-plc"],
  ["/operasyonel-riskler", "module.operational-risk"],
  ["/riskler", "module.operational-risk"],
  ["/ticari-zeka", "module.commercial-intelligence"],
  ["/satin-alma", "module.erp-integration"],
  ["/erp-entegrasyonlari", "module.erp-integration"],
  ["/raporlar", "module.reporting"],
  ["/ayarlar", "(recovery-exempt)"],
  ["/ai-gorevleri", "module.ai-task-center"],
]);

function resolveSubscriptionModule(href, subscriptionModulePath) {
  if (href === "/ayarlar") return "(recovery-exempt)";
  const candidates = [subscriptionModulePath, href, `/${href.split("/").filter(Boolean)[0] ?? ""}`];
  for (const candidate of candidates) {
    if (!candidate) continue;
    const hit = SUBSCRIPTION_MODULE_BY_PATH.get(candidate);
    if (hit) return hit;
  }
  return null;
}

function parsePrimaryNavigation() {
  const source = readFileSync(resolve(ROOT, "apps/web/src/lib/navigation/primary-navigation.ts"), "utf8");
  const items = [];
  let currentGroup = "Unknown";
  for (const line of source.split("\n")) {
    const groupMatch = line.match(/^\s*label:\s*"([^"]+)",\s*$/);
    if (groupMatch && line.includes("label:") && !line.includes("href:")) {
      const next = source.indexOf("items:", source.indexOf(line));
      if (next !== -1 && next - source.indexOf(line) < 80) currentGroup = groupMatch[1];
    }
    const itemMatch = line.match(/^\s*label:\s*"([^"]+)",\s*$/);
    const hrefMatch = line.match(/^\s*href:\s*"([^"]+)",\s*$/);
    const permMatch = line.match(/^\s*permissionKey:\s*"([^"]+)",\s*$/);
    const subMatch = line.match(/^\s*subscriptionModulePath:\s*"([^"]+)",\s*$/);
    if (itemMatch && !line.includes("items:")) {
      items.push({
        navigationGroup: currentGroup,
        module: itemMatch[1],
        route: null,
        requiredPermission: null,
        subscriptionModulePath: null,
        expectedVisible: true,
        salesCritical: false,
        notes: "",
      });
    }
    if (hrefMatch && items.length > 0 && items[items.length - 1].route === null) {
      items[items.length - 1].route = hrefMatch[1];
    }
    if (permMatch && items.length > 0) items[items.length - 1].requiredPermission = permMatch[1];
    if (subMatch && items.length > 0) items[items.length - 1].subscriptionModulePath = subMatch[1];
  }

  const primary = items
    .filter((item) => item.route)
    .map((item) => ({
      route: item.route,
      module: item.module,
      navigationGroup: item.navigationGroup,
      expectedVisible: item.expectedVisible,
      requiredPermission: item.requiredPermission,
      subscriptionModule: resolveSubscriptionModule(item.route, item.subscriptionModulePath),
      validationLevel: "full",
      salesCritical: item.route === "/" || item.route === "/dashboard" || item.route.startsWith("/ai-gorevleri"),
      notes: item.notes,
    }));

  const extra = EXTRA_SALES_ROUTES.map((item) => ({
    route: item.href,
    module: item.label,
    navigationGroup: item.group,
    expectedVisible: item.expectedVisible,
    requiredPermission: item.permissionKey,
    subscriptionModule: resolveSubscriptionModule(item.href, item.subscriptionModulePath),
    validationLevel: "full",
    salesCritical: item.salesCritical,
    notes: item.notes,
  }));

  return [...primary, ...extra];
}

const routes = parsePrimaryNavigation();
const manifest = {
  package: "NevaxisFactOS-DESKTOP-SHELL-ALL-MODULES-VALIDATION-1",
  baseline: "NevaxisFactOS-SALES-DEMO-ALL-MODULES-READINESS-1",
  generatedAt: new Date().toISOString(),
  canonicalRouteCount: routes.length,
  representativeScreenshots: {
    "01-dashboard.png": "/dashboard",
    "02-ai-gorev-merkezi.png": "/",
    "03-playbook-run-detail.png": "/ai-gorevleri/playbooklar?gorunum=calisan",
    "04-uretim.png": "/uretim",
    "05-makineler.png": "/makineler",
    "06-bakim.png": "/bakim",
    "07-kalite.png": "/kalite",
    "08-enerji.png": "/enerji",
    "09-teams.png": "/teams",
    "10-operasyonel-riskler.png": "/operasyonel-riskler",
    "11-ticari-zeka.png": "/ticari-zeka/karlilik-yonetim-merkezi",
    "12-erp-entegrasyonlari.png": "/erp-entegrasyonlari/baglanti",
    "13-raporlar.png": "/raporlar",
    "14-ayarlar.png": "/ayarlar",
  },
  salesCriticalFlows: [
    { name: "AI Görev Merkezi task surface", route: "/" },
    { name: "Playbooklar library", route: "/ai-gorevleri/playbooklar" },
    { name: "Playbook Çalışan Akışlar", route: "/ai-gorevleri/playbooklar?gorunum=calisan" },
    { name: "Production story", route: "/uretim" },
    { name: "Machines story", route: "/makineler" },
    { name: "Maintenance story", route: "/bakim" },
    { name: "Quality story", route: "/kalite" },
    { name: "Energy story", route: "/enerji" },
    { name: "Commercial intelligence hub", route: "/ticari-zeka/karlilik-yonetim-merkezi" },
    { name: "ERP connection", route: "/erp-entegrasyonlari/baglanti" },
  ],
  routes,
};

writeFileSync(OUT, `${JSON.stringify(manifest, null, 2)}\n`, "utf8");
console.log(`Wrote ${routes.length} routes to ${OUT}`);
