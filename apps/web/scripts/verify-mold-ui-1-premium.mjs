#!/usr/bin/env node
import { readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), "../../..");

function fail(message) {
  console.error(`MOLD-UI-1 premium UI verifier failed: ${message}`);
  process.exit(1);
}

function assert(label, ok, detail = "") {
  if (!ok) fail(`${label}${detail ? `: ${detail}` : ""}`);
  console.log(`✓ ${label}`);
}

function read(rel) {
  return readFileSync(resolve(ROOT, rel), "utf8").replaceAll("\r\n", "\n");
}

function assertNot(label, source, forbidden) {
  if (source.includes(forbidden)) fail(`${label}: found forbidden "${forbidden}"`);
  console.log(`✓ ${label}`);
}

console.log("Running MOLD-UI-1 premium UI verifier.");

const page = read("apps/web/src/app/(app)/kaliplar/page.tsx");
const view = read("apps/web/src/features/molds/components/mold-registry-view.tsx");
const history = read("apps/web/src/features/molds/components/mold-history-detail-panel.tsx");
const compatibility = read("apps/web/src/features/molds/components/mold-compatibility-detail-panel.tsx");
const css = read("apps/web/src/features/molds/components/mold-registry.css");
const labels = read("apps/web/src/features/molds/presentation/mold-labels.ts");
const historyLabels = read("apps/web/src/features/molds/presentation/mold-history-labels.ts");
const compatibilityLabels = read("apps/web/src/features/molds/presentation/mold-compatibility-labels.ts");
const globals = read("apps/web/src/app/globals.css");
const navigation = read("apps/web/src/lib/navigation/primary-navigation.ts");
const moldUiBundle = `${page}\n${view}\n${history}\n${compatibility}\n${css}`;

assert("premium page eyebrow", moldUiBundle.includes("KALIP YÖNETİMİ"));
assert("premium page title", page.includes("Kalıplar") && view.includes("Kalıplar"));
assert("primary CTA Yeni Kalıp", view.includes("Yeni Kalıp"));
assert("read-only CTA gated by canManage", view.includes("canManage ?") && view.includes("Yeni Kalıp"));
assert("executive command surface", view.includes("mold-executive") && css.includes(".mold-executive"));
assert(
  "compact API-derived KPI hierarchy",
  view.includes("Toplam Kalıp") &&
    view.includes("Aktif") &&
    view.includes("Bakımda") &&
    view.includes("Denemede") &&
    view.includes("Kritik Dikkat") &&
    view.includes("summary.totalCount") &&
    view.includes("summary.quarantinedCount") &&
    css.includes("mold-executive__kpis"),
);
assert("responsive KPI layout", css.includes("repeat(5, minmax(0, 1fr))") && css.includes("@media (max-width: 1100px)"));
assert("Mold-local overflow protection", css.includes("overflow-x: clip") && css.includes("min-width: 0"));
assert(
  "compact attention empty state",
  view.includes("Takip gerektiren operasyonel kalıp kaydı bulunmuyor.") &&
    css.includes(".mold-executive__quiet"),
);
assert("no permanent empty detail column before selection", view.includes("mold-registry-layout--solo") && view.includes("detailOpen"));
assert("full-width registry state", view.includes('data-registry-mode={detailOpen ? "detail" : "full"}'));
assert("full-width registry CSS", css.includes(".mold-registry-layout--solo") && css.includes("minmax(0, 1fr)"));
assert("selected master/detail state", view.includes("mold-registry-layout--detail") && css.includes(".mold-registry-layout--detail"));
assert(
  "accessible X close control",
  history.includes('aria-label="Kalıp detayını kapat"') &&
    history.includes("mold-detail-close") &&
    history.includes("onClose") &&
    view.includes("onClose={clearSelection}") &&
    !history.includes("Sicile Dön"),
);
assert("registry full-width after close", view.includes("clearSelection") && view.includes("mold-registry-layout--solo"));
assert(
  "responsive detail width",
  css.includes("minmax(440px, 500px)") &&
    css.includes("minmax(720px, 1fr)") &&
    css.includes("@media (max-width: 1180px)") &&
    css.includes(".mold-registry-layout--detail"),
);
assert(
  "selected-mode compact column set",
  view.includes("mold-registry-table--compact") &&
    view.includes("{!detailOpen ? <th>Sahiplik</th> : null}") &&
    view.includes("{!detailOpen ? <th>Göz</th> : null}") &&
    view.includes("{!detailOpen ? <th>Sayaç</th> : null}") &&
    view.includes("{!detailOpen ? <th>Güncelleme</th> : null}") &&
    view.includes("<th>Lokasyon</th>") &&
    view.includes("<th>İşlem</th>"),
);
assert(
  "full-mode complete column set",
  view.includes("<th>Sahiplik</th>") &&
    view.includes("<th>Göz</th>") &&
    view.includes("<th>Sayaç</th>") &&
    view.includes("<th>Güncelleme</th>") &&
    view.includes("!detailOpen"),
);
assert("action column visibility", view.includes("mold-registry-row-action") && view.includes("Detay") && css.includes(".mold-registry-table--compact .mold-registry-row-action"));
assert(
  "responsive detail KPI grid",
  css.includes("mold-registry-kpis--compact") &&
    css.includes("@container mold-detail") &&
    css.includes("repeat(4, minmax(0, 1fr))") &&
    css.includes("repeat(2, minmax(0, 1fr))"),
);
assert(
  "normal-flow ordering: KPIs before tabs before content",
  history.indexOf('aria-label="Kalıp geçmişi özeti"') < history.indexOf("mold-detail-tabs-rail") &&
    history.indexOf("mold-detail-tabs-rail") < history.indexOf('id="mold-tab-panel-overview"'),
);
assert(
  "non-wrapping/scrollable tab rail",
  history.includes("mold-detail-tabs-rail") &&
    css.includes(".mold-detail-tabs-rail") &&
    css.includes("flex-wrap: nowrap") &&
    css.includes("overflow-x: auto"),
);
assert("tab rail not absolute positioned", !/^\.mold-detail-tabs-rail\s*\{[^}]*position:\s*absolute/m.test(css));
assert("tab rail not sticky", !/^\.mold-detail-tabs-rail\s*\{[^}]*position:\s*sticky/m.test(css));
assert("tab rail position static", css.includes(".mold-detail-tabs-rail") && css.includes("position: static"));
assert(
  "no fixed/max detail height nested scroll",
  css.includes(".mold-registry-panel") &&
    css.includes("max-height: none") &&
    css.includes("overflow-y: visible") &&
    !css.includes("max-height: calc(100vh"),
);
assert("stacked 1024 detail layout", css.includes("@media (max-width: 1180px)") && css.includes("grid-template-columns: minmax(0, 1fr)"));
assert(
  "definition-list information layout",
  history.includes("mold-registry-meta-grid") &&
    css.includes(".mold-registry-meta-grid > div") &&
    css.includes(".mold-overview-card__title"),
);
assert("no page-level overflow styling", css.includes("overflow-x: clip") && css.includes("min-width: 0"));
assert("detail close removes Sicile Dön", !history.includes("Sicile Dön"));
assert("registry command surface", view.includes("Kalıp Sicili") && view.includes("mold-registry-surface"));
assert("search preserved", view.includes("Kalıp kodu veya adı") && view.includes("setSearch"));
assert("lifecycle filters preserved", view.includes("MOLD_LIFECYCLE_FILTER_OPTIONS") && view.includes("mold-lifecycle-pill"));
assert("ownership filter preserved", view.includes("MOLD_OWNERSHIP_FILTER_OPTIONS"));
assert("active-filter reset", view.includes("Filtreleri Temizle"));
assert("result count", view.includes("kayıt") && view.includes("filteredMolds.length"));
assert("Mold identity hierarchy", view.includes("mold-identity__name") && view.includes("mold-identity__code"));
assert("non-wrapping Mold code", css.includes(".mold-identity__code") && css.includes("white-space: nowrap") && css.includes("text-overflow: ellipsis"));
assert("detail code nowrap", css.includes(".mold-detail-header__code") && css.includes("white-space: nowrap"));
assert("localized lifecycle labels", view.includes("getMoldLifecycleStateLabel"));
assert("localized ownership labels", view.includes("getMoldOwnershipLabel"));
assert("localized location labels", view.includes("getMoldLocationLabel"));
assert("row action Detay", view.includes("Detay"));
assert("detail command header", history.includes("mold-detail-header") && history.includes("mold-detail-breadcrumb"));
assert("breadcrumb context", history.includes("Kalıplar /"));
assert("lifecycle badge in detail", history.includes("getMoldLifecycleStateLabel(selectedSummary.lifecycleState)"));
assert("shot-count context", history.includes("Güncel Sayaç"));
assert("write actions permission-aware", history.includes("canManage") && history.includes("Düzenle"));
assert("Genel Bakış tab", history.includes("Genel Bakış"));
assert("Bakım Geçmişi tab", history.includes("Bakım Geçmişi"));
assert("Revizyonlar tab", history.includes("Revizyonlar"));
assert("Denemeler tab", history.includes("Denemeler"));
assert("Zaman Çizelgesi tab", history.includes("Zaman Çizelgesi"));
assert("Makine Uyumluluğu tab", history.includes("Makine Uyumluluğu"));
assert("history presentation sections", history.includes("Bakım Kaydı") && history.includes("Revizyon Kaydı") && history.includes("Deneme Kaydı"));
assert("timeline structure", history.includes("mold-timeline") && history.includes("getMoldTimelineKindLabel"));
assert(
  "compatibility KPI summary",
  compatibility.includes("Uyumlu") &&
    compatibility.includes("Koşullu") &&
    compatibility.includes("Uyumsuz") &&
    compatibility.includes("Yetersiz Veri"),
);
assert("machine comparison table", compatibility.includes("Teknik Durum") && compatibility.includes("Kritik Kısıt"));
assert("compatibility evidence panel", compatibility.includes("Kanıt ve Kural Sonuçları"));
assert("data source/freshness visible", compatibility.includes("Veri Güncelliği") || compatibility.includes("Gereksinim eski"));
assert(
  "theoretical capacity disclaimer",
  compatibilityLabels.includes("Gerçek üretim kapasitesi veya OEE değildir.") &&
    compatibility.includes("MOLD_COMPATIBILITY_THEORETICAL_CAPACITY_DISCLAIMER"),
);
assertNot("no compatibility percentage", moldUiBundle, "% uyum");
assertNot("no OEE claim language", moldUiBundle, "OEE'dir");
assert(
  "test fixtures not hidden through UI logic",
  !view.includes("Loading State Test") &&
    !view.includes("Idempotency Test") &&
    !view.includes("filter((mold) => !mold.name.includes") &&
    !view.includes("hideTest"),
);
assert(
  "loading/empty/error/read-only states",
  view.includes("Kayıtlı kalıp bulunamadı") &&
    history.includes("Detay yükleniyor") &&
    (view.includes('role="alert"') || page.includes('role="alert"')) &&
    history.includes("yalnızca görüntüleme yetkiniz"),
);
assert("localized enum helpers present", labels.includes("getMoldLifecycleStateLabel") && historyLabels.includes("getMoldMaintenanceTypeLabel"));
assert("scoped mold styling", css.includes(".mold-registry-page") && view.includes("./mold-registry.css"));
assert("no globals.css mold rewrite", !globals.includes("mold-registry-page") && !globals.includes("mold-detail-header"));
assert("primary navigation untouched by this package", navigation.includes('iconKey: "molds"'));
assert("responsive breakpoints present", css.includes("1024px") && css.includes("1100px") && css.includes("1180px"));
assert("Sayaç Kaydet action preserved", history.includes("Sayaç Kaydet") && history.includes("recordMoldCounterAction"));
assert("Durum Değiştir action preserved", history.includes("Durum Değiştir") && history.includes("transitionMoldLifecycleAction"));

assert(
  "Mold-local sticky context only in selected detail panel",
  history.includes("data-mold-sticky-context") &&
    history.includes("mold-sticky-context") &&
    view.includes("data-detail-open") &&
    !view.includes("mold-sticky-context"),
);
assert(
  "topbar offset uses shared shell token once",
  css.includes("--mold-sticky-top: var(--top-bar-height)") &&
    css.includes("top: var(--mold-sticky-top)") &&
    !css.includes("scroll-margin-top: 5.5rem") &&
    (css.match(/--mold-sticky-top:/g) || []).length === 1,
);
assert(
  "sticky context retains name/code/lifecycle/active tab/X",
  history.includes("mold-sticky-context__name") &&
    history.includes("mold-sticky-context__code") &&
    history.includes("mold-sticky-context__tab") &&
    history.includes("activeTabLabel") &&
    history.includes('aria-label="Kalıp detayını kapat"'),
);
assert(
  "selection focus/scroll targets detail heading",
  view.includes("focusSelectedDetail") &&
    view.includes('getElementById("mold-detail-heading")') &&
    history.includes('id="mold-detail-heading"') &&
    history.includes('id="mold-detail-focus-target"') &&
    css.includes(".mold-detail-header") &&
    css.includes("scroll-margin-top: var(--mold-sticky-top)"),
);
assert(
  "close restores full registry and row focus",
  view.includes("clearSelection") &&
    view.includes("data-mold-id") &&
    view.includes("mold-registry-layout--solo") &&
    view.includes("focus({ preventScroll: true })"),
);
assert(
  "no fixed/max-height nested detail scroller",
  css.includes("max-height: none") &&
    css.includes("overflow-y: visible") &&
    !css.includes("max-height: calc(100vh"),
);
assert(
  "normal-flow KPI → tabs → content ordering",
  history.indexOf('aria-label="Kalıp geçmişi özeti"') < history.indexOf("mold-detail-tabs-rail") &&
    history.indexOf("mold-detail-tabs-rail") < history.indexOf('id="mold-tab-panel-overview"'),
);
assert(
  "tab click scrolls content below sticky/topbar",
  history.includes("selectTab") &&
    history.includes("mold-tab-panel-") &&
    history.includes("window.scrollTo") &&
    css.includes("[id^=\"mold-tab-panel-\"]") &&
    css.includes("calc(var(--mold-sticky-top) + var(--mold-sticky-context-height)"),
);
assert(
  "compact detail-card spacing",
  css.includes(".mold-overview-card") &&
    css.includes("padding: 1rem 1.1rem") &&
    css.includes(".mold-registry-meta-grid") &&
    css.includes("gap: 0.75rem 1rem"),
);
assert(
  "no verifier-record hiding logic",
  !view.includes("Loading State Test") &&
    !view.includes("Idempotency Test") &&
    !view.includes("hideTest") &&
    !view.includes("filter((mold) => !mold.name.includes"),
);

console.log("MOLD-UI-1 premium UI verifier passed.");
console.log("Integration Gate follow-up: Mold verifier fixture cleanup required before pilot/demo acceptance.");
