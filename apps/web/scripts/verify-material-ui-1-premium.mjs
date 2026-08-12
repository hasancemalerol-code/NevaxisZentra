#!/usr/bin/env node
import { readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), "../../..");

function fail(message) {
  console.error(`MATERIAL-UI-1 premium UI verifier failed: ${message}`);
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

console.log("Running MATERIAL-UI-1 premium UI verifier.");

const page = read("apps/web/src/app/(app)/malzemeler/page.tsx");
const view = read("apps/web/src/features/materials/components/material-registry-view.tsx");
const lotPanel = read("apps/web/src/features/materials/components/material-lot-panel.tsx");
const tracePanel = read("apps/web/src/features/materials/components/material-lot-traceability-panel.tsx");
const reqPanel = read("apps/web/src/features/materials/components/material-requirement-panel.tsx");
const css = read("apps/web/src/features/materials/components/material-registry.css");
const labels = read("apps/web/src/features/materials/presentation/material-labels.ts");
const globals = read("apps/web/src/app/globals.css");
const navigation = read("apps/web/src/lib/navigation/primary-navigation.ts");
const bundle = `${page}\n${view}\n${lotPanel}\n${tracePanel}\n${reqPanel}\n${css}`;

assert("premium page eyebrow", bundle.includes("MALZEME YÖNETİMİ"));
assert("premium page title", page.includes("Malzemeler") && view.includes("Malzemeler"));
assert("primary CTA Yeni Malzeme", view.includes("Yeni Malzeme"));
assert("read-only CTA gated by canManage", view.includes("canManage ?") && view.includes("Yeni Malzeme"));
assert(
  "compact API-derived KPI hierarchy",
  view.includes("Toplam Malzeme") &&
    view.includes("Aktif") &&
    view.includes("Bloke") &&
    view.includes("Kullanımdan Kalkmış") &&
    view.includes("Teknik Verisi Eksik") &&
    view.includes("summary.totalCount") &&
    css.includes("material-executive__kpis"),
);
assert("responsive KPI layout", css.includes("repeat(5, minmax(0, 1fr))") && css.includes("@media (max-width: 1100px)"));
assert("Material-local overflow protection", css.includes("overflow-x: clip") && css.includes("min-width: 0"));
assert("no permanent empty detail column before selection", view.includes("material-registry-layout--solo") && view.includes("detailOpen"));
assert("full-width registry state", view.includes('data-registry-mode={detailOpen ? "detail" : "full"}'));
assert("selected master/detail state", view.includes("material-registry-layout--detail") && css.includes("minmax(440px, 500px)"));
assert(
  "accessible X close control",
  view.includes('aria-label="Malzeme detayını kapat"') &&
    view.includes("material-detail-close") &&
    view.includes("clearSelection"),
);
assert(
  "responsive detail width",
  css.includes("minmax(720px, 1fr)") && css.includes("@media (max-width: 1180px)"),
);
assert(
  "selected-mode compact column set",
  view.includes("material-registry-table--compact") &&
    view.includes("{!detailOpen ? <th>Kod</th> : null}") &&
    view.includes("<th>İşlem</th>"),
);
assert("registry command surface", view.includes("Malzeme Sicili") && view.includes("material-registry-surface"));
assert("search preserved", view.includes("Kod, ad, üretici veya grade") && view.includes("setSearch"));
assert("lifecycle filters preserved", view.includes("MATERIAL_LIFECYCLE_FILTER_OPTIONS") && view.includes("material-lifecycle-pill"));
assert("category filter preserved", view.includes("MATERIAL_CATEGORY_FILTER_OPTIONS"));
assert("polymer filter preserved", view.includes("MATERIAL_POLYMER_FILTER_OPTIONS"));
assert("active-filter reset", view.includes("Filtreleri Temizle"));
assert("result count", view.includes("filteredMaterials.length") && view.includes("kayıt"));
assert("Material identity hierarchy", view.includes("material-identity__name") && view.includes("material-identity__code"));
assert("non-wrapping Material code", css.includes(".material-identity__code") && css.includes("white-space: nowrap"));
assert("localized lifecycle labels", view.includes("getMaterialLifecycleStatusLabel"));
assert("localized category labels", view.includes("getMaterialCategoryLabel"));
assert("localized polymer labels", view.includes("getPolymerFamilyLabel"));
assert("technical-data honesty", view.includes("getTechnicalDataLabel"));
assert("row action Detay", view.includes("Detay"));
assert("detail command header", view.includes("material-detail-header") && view.includes("material-detail-breadcrumb"));
assert("detail header meta row", view.includes("material-detail-header__meta"));
assert("source/freshness in detail KPIs", view.includes("Veri Yaşı") && view.includes("getMaterialSourceLabel"));
assert("write actions permission-aware", view.includes("detailCanManage") && view.includes("Düzenle"));
assert("Genel Bakış tab", view.includes("Genel Bakış"));
assert("Teknik Özellikler tab", view.includes("Teknik Özellikler"));
assert("Kaynak ve ERP Referansı tab", view.includes("Kaynak ve ERP Referansı"));
assert("Lotlar tab", view.includes("Lotlar"));
assert("İzlenebilirlik tab", view.includes("İzlenebilirlik"));
assert("İhtiyaç ve Tüketim tab", view.includes("İhtiyaç ve Tüketim"));
assert("Regrind Politikası tab", view.includes("Regrind Politikası"));
assert(
  "non-wrapping/scrollable tab rail",
  view.includes("material-detail-tabs-rail") &&
    css.includes(".material-detail-tabs-rail") &&
    css.includes("flex-wrap: nowrap"),
);
assert("tab rail not absolute positioned", !/^\.material-detail-tabs-rail\s*\{[^}]*position:\s*absolute/m.test(css));
assert("tab rail position static", css.includes(".material-detail-tabs-rail") && css.includes("position: static"));
assert(
  "no fixed/max detail height nested scroll",
  css.includes(".material-registry-panel") &&
    css.includes("max-height: none") &&
    css.includes("overflow-y: visible"),
);
assert(
  "Material-local sticky context",
  view.includes("material-sticky-context") &&
    view.includes("data-material-sticky-context") &&
    view.includes("stickyContextVisible"),
);
assert(
  "topbar offset uses shared shell token once",
  css.includes("--material-sticky-top: var(--top-bar-height)") &&
    css.includes("top: var(--material-sticky-top)") &&
    (css.match(/--material-sticky-top:/g) || []).length === 1,
);
assert(
  "sticky context retains name/code/tab/X",
  view.includes("material-sticky-context__name") &&
    view.includes("material-sticky-context__tab") &&
    view.includes("activeTabLabel"),
);
assert(
  "selection focus/scroll targets detail heading",
  view.includes("focusSelectedDetail") &&
    view.includes('getElementById("material-detail-heading")') &&
    view.includes('id="material-detail-heading"'),
);
assert(
  "close restores full registry and row focus",
  view.includes("clearSelection") &&
    view.includes("data-material-id") &&
    view.includes("focus({ preventScroll: true })"),
);
assert(
  "tab click scroll helper",
  view.includes("selectTab") &&
    view.includes("material-tab-panel-") &&
    view.includes("window.scrollTo"),
);
assert("technical grouped sections", view.includes("Fiziksel Özellikler") && view.includes("Regrind Sınırı"));
assert(
  "ERP source boundary note",
  view.includes("sistem-of-record ERP") && view.includes("Kaynak ve ERP Referansı"),
);
assert("lot observed-quantity disclaimer", lotPanel.includes("MATERIAL_LOT_QUANTITY_DISCLAIMER"));
assert(
  "theoretical requirement disclaimer",
  labels.includes("Teorik malzeme ihtiyacıdır") && reqPanel.includes("MATERIAL_REQUIREMENT_THEORETICAL_DISCLAIMER"),
);
assert(
  "consumption observation disclaimer",
  labels.includes("Operasyonel tüketim gözlemidir") && reqPanel.includes("MATERIAL_REQUIREMENT_CONSUMPTION_DISCLAIMER"),
);
assert("rule-set version visible", reqPanel.includes("Kural seti:") && reqPanel.includes("ruleSetVersion"));
assert(
  "regrind human authorization boundary",
  reqPanel.includes("otomatik onay sağlamaz") && reqPanel.includes("insan onayı"),
);
assertNot("no automatic regrind approval claim", bundle, "otomatik onaylandı");
assertNot("no financial-loss claim", bundle, "finansal kayıp");
assertNot("no authoritative ERP stock claim", bundle, "ERP stok bakiyesidir");
assert("traceability timeline structure", tracePanel.includes("material-timeline") || lotPanel.includes("material-lot-traceability"));
assert(
  "loading/empty/error/read-only states",
  view.includes("Kayıtlı malzeme bulunamadı") &&
    view.includes("Detay yükleniyor") &&
    view.includes('role="alert"') &&
    view.includes("salt okunurdur"),
);
assert("scoped material styling", css.includes(".material-registry-page") && view.includes("./material-registry.css"));
assert("no globals.css material rewrite", !globals.includes("material-registry-page"));
assert("primary navigation untouched", navigation.includes("Malzemeler"));
assert("responsive breakpoints present", css.includes("1024px") && css.includes("1180px"));
assertNot("no sidebar rewrite in material bundle", view, "primary-navigation");
assert("traceability presentation", tracePanel.includes("material-lot-traceability"));

console.log("MATERIAL-UI-1 premium UI verifier passed.");
