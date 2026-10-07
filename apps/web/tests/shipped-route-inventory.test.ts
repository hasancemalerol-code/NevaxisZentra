import assert from "node:assert/strict";
import { readdirSync, readFileSync } from "node:fs";
import path from "node:path";
import test from "node:test";

import { acceptanceRoleContract } from "../src/lib/authorization/acceptance-role-contract";
import { appsCatalogItems } from "../src/lib/navigation/apps-catalog-navigation";
import { primaryNavigation } from "../src/lib/navigation/primary-navigation";
import {
  CONTEXTUAL_NAVIGATION_SECTIONS,
  HIDDEN_PLACEHOLDER_NAV_HREFS,
  WORKFLOW_PRIMARY_HOMES,
} from "../src/lib/navigation/workflow-primary-navigation";
import {
  isSubscriptionRecoveryRoute,
  resolveEntitlementCodeFromPath,
  SUBSCRIPTION_RECOVERY_ROUTES,
} from "../src/features/saas-subscription/domain/saas-subscription-labels";

// Authoritative shipped-V1 route inventory. Every page under src/app/(app) must be classified here:
//   A = shipped, customer-visible V1 (reachable from navigation, a shipped child page, or a canonical redirect)
//   B = internal / development-only (must be unreachable for customers and fail with an explained response)
//   C = future / deferred (no page may exist for it yet; hidden navigation placeholders live here)
// A new page without an entry, or a stale entry, fails this test. Class A routes must also be entitlement-mapped
// identically in the API (C#) and the web map, otherwise the API fail-closes the route for every tenant and the
// customer is redirected to the subscription page ("module" denial) even on a fully licensed installation.
const repositoryRoot = path.resolve(__dirname, "..", "..", "..");
const appRoot = path.resolve(__dirname, "..", "src", "app", "(app)");

type RouteClass = "A" | "B" | "C";
type InventoryEntry = Readonly<{ cls: RouteClass; why: string }>;

const INVENTORY: Readonly<Record<string, InventoryEntry>> = {
  "/": { cls: "A", why: "nav:/" },
  "/abonelik-erisimi": { cls: "A", why: "licence recovery page (layout redirect target)" },
  "/ai-gorevleri": { cls: "A", why: "canonical-redirect:/" },
  "/ai-gorevleri/[taskId]": { cls: "A", why: "Task Center child (linked from /isler and the Task Center list)" },
  "/ai-gorevleri/otomasyon": { cls: "A", why: "Task Center child (linked from /isler and the Task Center list)" },
  "/ai-gorevleri/playbooklar": { cls: "A", why: "Task Center child (linked from /isler and the Task Center list)" },
  "/ai-gorevleri/playbooklar/[playbookId]": { cls: "A", why: "Task Center child (linked from /isler and the Task Center list)" },
  "/ai-gorevleri/playbooklar/calisma/[runId]": { cls: "A", why: "Task Center child (linked from /isler and the Task Center list)" },
  "/ai-gorevleri/teslimatlar": { cls: "A", why: "Task Center child (linked from /isler and the Task Center list)" },
  "/ai-gorevleri/yeni": { cls: "A", why: "Task Center child (linked from /isler and the Task Center list)" },
  "/ayarlar": { cls: "A", why: "nav:/ayarlar" },
  "/ayarlar/entegrasyonlar": { cls: "A", why: "nav:/ayarlar/entegrasyonlar" },
  "/ayarlar/entegrasyonlar/[connectorId]/dosya-aktarimi": { cls: "A", why: "child-of:/ayarlar/entegrasyonlar" },
  "/ayarlar/entegrasyonlar/gercek-fabrika": { cls: "A", why: "child-of:/ayarlar/entegrasyonlar" },
  "/ayarlar/entegrasyonlar/merkez": { cls: "A", why: "child-of:/ayarlar/entegrasyonlar" },
  "/ayarlar/faturalandirma/odeme-donusu": { cls: "A", why: "child-of:/ayarlar" },
  "/ayarlar/sistem-durumu": { cls: "A", why: "nav:/ayarlar/sistem-durumu" },
  "/ayarlar/veri-aktarim-merkezi": { cls: "A", why: "nav:/ayarlar/veri-aktarim-merkezi" },
  "/bakim": { cls: "A", why: "nav:/bakim" },
  "/bakim/arizalar/[id]": { cls: "A", why: "child-of:/bakim" },
  "/bakim/arizalar/yeni": { cls: "A", why: "child-of:/bakim" },
  "/bakim/durum-zekasi": { cls: "A", why: "child-of:/bakim" },
  "/bakim/emirler/[id]": { cls: "A", why: "child-of:/bakim" },
  "/bakim/emirler/yeni": { cls: "A", why: "child-of:/bakim" },
  "/bakim/kestirimci-riskler": { cls: "A", why: "child-of:/bakim" },
  "/bakim/planlama": { cls: "A", why: "child-of:/bakim" },
  "/bakim/planlar/yeni": { cls: "A", why: "child-of:/bakim" },
  "/bakim/raporlama": { cls: "A", why: "child-of:/bakim" },
  "/bakim/varliklar/[assetReference]": { cls: "A", why: "child-of:/bakim" },
  "/dashboard": { cls: "A", why: "canonical-redirect:/" },
  "/dijital-is-gucu": { cls: "A", why: "nav:/dijital-is-gucu" },
  "/dijital-is-gucu/[workerId]": { cls: "A", why: "child-of:/dijital-is-gucu" },
  "/dijital-is-gucu/aktivite": { cls: "A", why: "child-of:/dijital-is-gucu" },
  "/dijital-is-gucu/calisanlar": { cls: "A", why: "child-of:/dijital-is-gucu" },
  "/dijital-is-gucu/onaylar": { cls: "A", why: "nav:/dijital-is-gucu/onaylar" },
  "/dijital-is-gucu/sonuclar": { cls: "A", why: "nav:/dijital-is-gucu/sonuclar" },
  "/dijital-is-gucu/tamamlanan": { cls: "A", why: "child-of:/dijital-is-gucu" },
  "/dijital-is-gucu/vakalar/[caseId]": { cls: "A", why: "child-of:/dijital-is-gucu" },
  "/enerji": { cls: "A", why: "nav:/enerji" },
  "/erp-entegrasyonlari/baglanti": { cls: "A", why: "ERP workspace child (components/erp/erp-workspace-nav)" },
  "/erp-entegrasyonlari/belgeler": { cls: "A", why: "ERP workspace child (components/erp/erp-workspace-nav)" },
  "/erp-entegrasyonlari/belgeler/[documentId]": { cls: "A", why: "ERP workspace child (components/erp/erp-workspace-nav)" },
  "/erp-entegrasyonlari/eslestirme": { cls: "A", why: "ERP workspace child (components/erp/erp-workspace-nav)" },
  "/erp-entegrasyonlari/hata-kuyrugu": { cls: "A", why: "ERP workspace child (components/erp/erp-workspace-nav)" },
  "/erp-entegrasyonlari/onayli-islemler": { cls: "A", why: "ERP workspace child (components/erp/erp-workspace-nav)" },
  "/erp-entegrasyonlari/onayli-islemler/[writebackId]": { cls: "A", why: "ERP workspace child (components/erp/erp-workspace-nav)" },
  "/erp-entegrasyonlari/saglik": { cls: "A", why: "ERP workspace child (components/erp/erp-workspace-nav)" },
  "/erp-entegrasyonlari/satin-alma": { cls: "A", why: "ERP workspace child (components/erp/erp-workspace-nav)" },
  "/erp-entegrasyonlari/satin-alma/[purchaseOrderId]": { cls: "A", why: "ERP workspace child (components/erp/erp-workspace-nav)" },
  "/erp-entegrasyonlari/senkronizasyonlar": { cls: "A", why: "ERP workspace child (components/erp/erp-workspace-nav)" },
  "/erp-entegrasyonlari/siparisler": { cls: "A", why: "ERP workspace child (components/erp/erp-workspace-nav)" },
  "/erp-entegrasyonlari/siparisler/[orderId]": { cls: "A", why: "ERP workspace child (components/erp/erp-workspace-nav)" },
  "/erp-entegrasyonlari/stok": { cls: "A", why: "ERP workspace child (components/erp/erp-workspace-nav)" },
  "/erp-entegrasyonlari/uretim": { cls: "A", why: "ERP workspace child (components/erp/erp-workspace-nav)" },
  "/erp-entegrasyonlari/uretim/[productionOrderId]": { cls: "A", why: "ERP workspace child (components/erp/erp-workspace-nav)" },
  "/erp-entegrasyonlari/veri-eslestirme": { cls: "A", why: "ERP workspace child (components/erp/erp-workspace-nav)" },
  "/fabrika": { cls: "A", why: "nav:/fabrika" },
  "/fabrika-agi": { cls: "A", why: "nav:/fabrika-agi" },
  "/fabrika-karar-merkezi": { cls: "A", why: "nav:/fabrika-karar-merkezi" },
  "/fabrika-karar-merkezi/vakalar/[caseId]": { cls: "A", why: "child-of:/fabrika-karar-merkezi" },
  "/fabrika-vitrini": { cls: "A", why: "dashboard navigation child (components/dashboard/dashboard-navigation.ts)" },
  "/fabrika-zekasi": { cls: "A", why: "nav:/fabrika-zekasi" },
  "/golden-demo/gumruk-dosyasi": { cls: "A", why: "Sunum Modu child (presentation-mode links)" },
  "/golden-demo/uretim-kurtarma": { cls: "A", why: "Sunum Modu child (presentation-mode links)" },
  "/ihracat": { cls: "A", why: "nav:/ihracat" },
  "/ihracat/[exportCaseId]": { cls: "A", why: "child-of:/ihracat" },
  "/ihracat/ulke-arastirma": { cls: "A", why: "child-of:/ihracat" },
  "/ihracat/ulke-arastirma/[researchCaseId]": { cls: "A", why: "child-of:/ihracat" },
  "/ihracat/ulke-bilgisi": { cls: "A", why: "child-of:/ihracat" },
  "/intelligence-runtime": { cls: "A", why: "nav:/intelligence-runtime" },
  "/internal/erisim-tanilama": { cls: "B", why: "development-only diagnostics; page answers 404 unless runtime mode is development-api" },
  "/iot-plc": { cls: "A", why: "nav:/iot-plc" },
  "/isler": { cls: "A", why: "nav:/isler" },
  "/kaliplar": { cls: "A", why: "nav:/kaliplar" },
  "/kaliplar/takip-ve-gorevler": { cls: "A", why: "child-of:/kaliplar" },
  "/kalite": { cls: "A", why: "nav:/kalite" },
  "/kalite/kontroller/[inspectionId]": { cls: "A", why: "child-of:/kalite" },
  "/kalite/kontroller/[inspectionId]/ozet": { cls: "A", why: "child-of:/kalite" },
  "/kalite/kontroller/yeni": { cls: "A", why: "child-of:/kalite" },
  "/kalite/ncr": { cls: "A", why: "child-of:/kalite" },
  "/kalite/ncr/[ncrId]": { cls: "A", why: "child-of:/kalite" },
  "/kalite/planlar/yeni": { cls: "A", why: "child-of:/kalite" },
  "/kalite/raporlama": { cls: "A", why: "child-of:/kalite" },
  "/kalite/takip-ve-gorevler": { cls: "A", why: "child-of:/kalite" },
  "/makineler": { cls: "A", why: "nav:/makineler" },
  "/malzemeler": { cls: "A", why: "nav:/malzemeler" },
  "/operasyon/yonetim-merkezi": { cls: "A", why: "nav:/operasyon/yonetim-merkezi" },
  "/operasyon/yonetim-merkezi/kararlar/[decisionId]": { cls: "A", why: "child-of:/operasyon/yonetim-merkezi" },
  "/operasyon/yonetim-merkezi/surekli-iyilestirme": { cls: "A", why: "child-of:/operasyon/yonetim-merkezi" },
  "/operasyon/yonetim-merkezi/vakalar/[caseId]": { cls: "A", why: "child-of:/operasyon/yonetim-merkezi" },
  "/operasyonel-riskler": { cls: "A", why: "nav:/operasyonel-riskler" },
  "/operational-value": { cls: "A", why: "nav:/operational-value" },
  "/planlama": { cls: "A", why: "nav:/planlama" },
  "/planlama/[planId]": { cls: "A", why: "child-of:/planlama" },
  "/planlama/[planId]/duzenle": { cls: "A", why: "child-of:/planlama" },
  "/planlama/yeni": { cls: "A", why: "child-of:/planlama" },
  "/proses-oee": { cls: "A", why: "nav:/proses-oee" },
  "/satin-alma/tedarik-riskleri": { cls: "A", why: "nav:/satin-alma/tedarik-riskleri" },
  "/senaryo-kurtarma": { cls: "A", why: "nav:/senaryo-kurtarma" },
  "/siparisler": { cls: "A", why: "nav:/siparisler" },
  "/siparisler/[orderDraftId]": { cls: "A", why: "child-of:/siparisler" },
  "/stok-ve-depo/malzeme-riskleri": { cls: "A", why: "linked from Task Center source references and operations command center" },
  "/sunum-modu": { cls: "A", why: "nav:/sunum-modu" },
  "/surekli-optimizasyon": { cls: "A", why: "nav:/surekli-optimizasyon" },
  "/teams": { cls: "A", why: "nav:/teams" },
  "/teams/bildirimler": { cls: "A", why: "child-of:/teams" },
  "/teams/devretme": { cls: "A", why: "child-of:/teams" },
  "/teams/devretme/[handoverId]": { cls: "A", why: "child-of:/teams" },
  "/teams/devretme/yeni": { cls: "A", why: "child-of:/teams" },
  "/teams/is-kutum": { cls: "A", why: "canonical-redirect:/" },
  "/teams/onaylar": { cls: "A", why: "child-of:/teams" },
  "/teams/onaylar/[requestId]": { cls: "A", why: "child-of:/teams" },
  "/teams/onaylar/yeni": { cls: "A", why: "child-of:/teams" },
  "/teams/ozel-mesajlar": { cls: "A", why: "child-of:/teams" },
  "/ticari-zeka/fiyat-koruma-ve-onay": { cls: "A", why: "nav:/ticari-zeka/fiyat-koruma-ve-onay" },
  "/ticari-zeka/karlilik-uyarilari": { cls: "A", why: "nav:/ticari-zeka/karlilik-uyarilari" },
  "/ticari-zeka/karlilik-yonetim-merkezi": { cls: "A", why: "nav:/ticari-zeka/karlilik-yonetim-merkezi" },
  "/ticari-zeka/malzeme-ve-maliyet-karliligi": { cls: "A", why: "nav:/ticari-zeka/malzeme-ve-maliyet-karliligi" },
  "/ticari-zeka/musteri-karliligi": { cls: "A", why: "nav:/ticari-zeka/musteri-karliligi" },
  "/ticari-zeka/satin-alma-fiyat-analizi": { cls: "A", why: "nav:/ticari-zeka/satin-alma-fiyat-analizi" },
  "/ticari-zeka/satis-fiyat-analizi": { cls: "A", why: "nav:/ticari-zeka/satis-fiyat-analizi" },
  "/ticari-zeka/tedarikci-karsilastirmasi": { cls: "A", why: "nav:/ticari-zeka/tedarikci-karsilastirmasi" },
  "/ticari-zeka/teklif-riskleri": { cls: "A", why: "nav:/ticari-zeka/teklif-riskleri" },
  "/ticari-zeka/urun-karliligi": { cls: "A", why: "nav:/ticari-zeka/urun-karliligi" },
  "/uretim": { cls: "A", why: "nav:/uretim" },
  "/uretim/[orderId]": { cls: "A", why: "child-of:/uretim" },
  "/uretim/hatlar": { cls: "A", why: "nav:/uretim/hatlar" },
  "/uretim/hatlar/[lineId]": { cls: "A", why: "child-of:/uretim/hatlar" },
  "/uretim/istisnalar-ve-gorevler": { cls: "A", why: "child-of:/uretim" },
  "/uretim/kaynaklar": { cls: "A", why: "nav:/uretim/kaynaklar" },
  "/uretim/raporlama": { cls: "A", why: "child-of:/uretim" },
  "/uygulamalar": { cls: "A", why: "nav:/uygulamalar" },
  "/vardiya-devir": { cls: "A", why: "canonical-redirect:/vardiyalar/teslim" },
  "/vardiya-devir/[id]": { cls: "A", why: "canonical-redirect:/vardiyalar/teslim" },
  "/vardiyalar": { cls: "A", why: "nav:/vardiyalar" },
  "/vardiyalar/acik-konular": { cls: "A", why: "child-of:/vardiyalar" },
  "/vardiyalar/aktif": { cls: "A", why: "child-of:/vardiyalar" },
  "/vardiyalar/gecmis": { cls: "A", why: "child-of:/vardiyalar" },
  "/vardiyalar/kaynak": { cls: "A", why: "child-of:/vardiyalar" },
  "/vardiyalar/performans": { cls: "A", why: "child-of:/vardiyalar" },
  "/vardiyalar/plan": { cls: "A", why: "child-of:/vardiyalar" },
  "/vardiyalar/teslim": { cls: "A", why: "child-of:/vardiyalar" },
  "/yonetici-komuta-merkezi": { cls: "A", why: "nav:/yonetici-komuta-merkezi" },
};

function discoverPageRoutes(directory = appRoot, found: string[] = []): string[] {
  for (const entry of readdirSync(directory, { withFileTypes: true })) {
    const full = path.join(directory, entry.name);
    if (entry.isDirectory()) discoverPageRoutes(full, found);
    else if (entry.name === "page.tsx") {
      const relative = path.relative(appRoot, path.dirname(full)).split(path.sep).join("/");
      found.push(relative === "" ? "/" : "/" + relative);
    }
  }
  return found;
}

const exampleOf = (route: string) => route.replace(/\[[^\]]+\]/g, "00000000-0000-4000-8000-000000000001");
const inventoryByExample = new Map(Object.entries(INVENTORY).map(([route, entry]) => [exampleOf(route), entry] as const));

test("every page route is classified in the shipped route inventory (no unclassified, no stale entries)", () => {
  const pages = discoverPageRoutes().filter((route) => !route.includes("[...")).sort();
  assert.deepEqual(
    pages.filter((route) => !(route in INVENTORY)),
    [],
    "pages missing from the inventory",
  );
  assert.deepEqual(
    Object.keys(INVENTORY).filter((route) => !pages.includes(route)),
    [],
    "inventory entries without a page",
  );
});

test("class B routes are explicitly dev-only and class C routes have no page", () => {
  const pages = new Set(discoverPageRoutes());
  for (const [route, entry] of Object.entries(INVENTORY)) {
    if (entry.cls === "B") {
      assert.match(entry.why, /development-only|internal/i, `${route} must say why it is not shipped`);
    }
    if (entry.cls === "C") {
      assert.equal(pages.has(route), false, `${route} is deferred and must not have a page`);
    }
  }
  const diagnostics = readFileSync(path.join(appRoot, "internal", "erisim-tanilama", "page.tsx"), "utf8");
  assert.match(
    diagnostics,
    /development-api[\s\S]*notFound\(\)/,
    "internal diagnostics must answer 404 outside development-api",
  );
});

test("every navigation entry resolves to a classified class A page (or is a documented hidden placeholder)", () => {
  const hrefs = new Set<string>([
    ...primaryNavigation.flatMap((group) => group.items.map((item) => item.href)),
    ...WORKFLOW_PRIMARY_HOMES.map((item) => item.href),
    ...CONTEXTUAL_NAVIGATION_SECTIONS.flatMap((section) => section.items.map((item) => item.href)),
    ...appsCatalogItems.map((item) => item.href),
    ...acceptanceRoleContract.routes.map((route) => route.path).filter((route): route is string => Boolean(route)),
  ]);
  const visible = [...hrefs].filter((href) => !HIDDEN_PLACEHOLDER_NAV_HREFS.has(href));
  assert.deepEqual(
    visible.filter((href) => !inventoryByExample.has(href)),
    [],
    "navigation points at routes that have no page (route-map drift)",
  );
  for (const href of visible) {
    assert.equal(inventoryByExample.get(href)?.cls, "A", `${href} is in navigation so it must be class A`);
  }
  for (const hidden of HIDDEN_PLACEHOLDER_NAV_HREFS) {
    assert.equal(inventoryByExample.has(hidden), false, `${hidden} is a hidden placeholder (class C) and must have no page`);
  }
  // The routes named in the V1 acceptance charter.
  for (const required of [
    "/", "/dashboard", "/fabrika", "/planlama", "/isler", "/ai-gorevleri", "/vardiyalar",
    "/yonetici-komuta-merkezi", "/fabrika-karar-merkezi", "/uygulamalar", "/ayarlar/veri-aktarim-merkezi",
    "/operasyon/yonetim-merkezi", "/fabrika-zekasi", "/sunum-modu",
  ]) {
    assert.equal(INVENTORY[required]?.cls, "A", `${required} must be a shipped route`);
  }
});

// --- API (C#) entitlement map, parsed from source so the two sides cannot drift -------------------------------
const csharp = readFileSync(
  path.join(repositoryRoot, "packages", "NevaxisZentra.Persistence", "SaasSubscription", "SaasSubscriptionModuleEntitlements.cs"),
  "utf8",
);

const csharpCodes = new Map<string, string>(
  [...csharp.matchAll(/public const string (\w+) = "(module\.[a-z-]+)";/g)].map((m) => [m[1], m[2]] as const),
);
const csharpMappings = [...csharp.matchAll(/\("(\/[a-z0-9/-]+)", EntitlementCodes\.(\w+)\)/g)].map(
  (m) => [m[1], csharpCodes.get(m[2])!] as const,
);
const csharpExemptBlock = csharp.match(/RecoveryExemptRoutes = new\([^)]*\)\s*\{([\s\S]*?)\};/);
const csharpExactExempt = new Set([...(csharpExemptBlock?.[1] ?? "").matchAll(/"(\/[a-z-]*)"/g)].map((m) => m[1]));
const csharpPrefixExempt = [...csharp.matchAll(/normalized\.StartsWith\("(\/[a-z-]+\/)"/g)].map((m) => m[1]);

function csharpExempt(route: string): boolean {
  const normalized = route.replace(/\/+$/, "").toLowerCase() || "/";
  return csharpExactExempt.has(normalized) || csharpPrefixExempt.some((prefix) => normalized.startsWith(prefix));
}

function csharpEntitlement(route: string): string | null {
  const normalized = route.replace(/\/+$/, "").toLowerCase() || "/";
  for (const [prefix, code] of csharpMappings) {
    if (normalized === prefix || normalized.startsWith(prefix + "/")) return code;
  }
  return null;
}

test("the C# map was parsed", () => {
  assert.ok(csharpMappings.length > 30);
  assert.ok(csharpExactExempt.has("/"));
  assert.ok(csharpPrefixExempt.includes("/ayarlar/"));
});

test("every shipped (class A) page is licence-gated identically by the API and the web map, or is recovery-exempt", () => {
  const failures: string[] = [];
  for (const [route, entry] of Object.entries(INVENTORY)) {
    if (entry.cls !== "A" || entry.why.startsWith("canonical-redirect")) continue;
    const example = exampleOf(route);
    if (example === "/abonelik-erisimi") continue; // web-shell recovery destination; never evaluated by the API
    const webCode = resolveEntitlementCodeFromPath(example);
    const apiCode = csharpEntitlement(example);
    const apiExempt = csharpExempt(example);

    if (!apiExempt && apiCode === null) {
      failures.push(`${route}: the API fail-closes this route (no entitlement mapping and not recovery-exempt)`);
      continue;
    }
    if (apiExempt) {
      if (webCode !== null && !isSubscriptionRecoveryRoute(example)) {
        failures.push(`${route}: exempt in the API but gated by the web map (${webCode})`);
      }
      continue;
    }
    if (apiCode !== webCode) failures.push(`${route}: API=${apiCode} web=${webCode}`);
  }
  assert.deepEqual(failures, []);
});

test("class B diagnostics are exempt from the licence redirect so they answer with their own 404", () => {
  assert.equal(csharpExempt("/internal/erisim-tanilama"), true);
  assert.equal(isSubscriptionRecoveryRoute("/internal/erisim-tanilama"), true);
});

test("web recovery routes are a subset of the API exemptions (plus the web-only recovery destination)", () => {
  for (const route of SUBSCRIPTION_RECOVERY_ROUTES) {
    if (route === "/abonelik-erisimi") continue;
    assert.equal(csharpExempt(route) || csharpExempt(`${route}/x`), true, `${route} is exempt on the web but gated by the API`);
  }
});

test("canonical redirects point at classified shipped routes", () => {
  for (const [route, entry] of Object.entries(INVENTORY)) {
    if (!entry.why.startsWith("canonical-redirect:")) continue;
    const target = entry.why.slice("canonical-redirect:".length).split("?")[0];
    assert.equal(inventoryByExample.has(exampleOf(target)), true, `${route} redirects to ${target}, which is not a page`);
  }
});
