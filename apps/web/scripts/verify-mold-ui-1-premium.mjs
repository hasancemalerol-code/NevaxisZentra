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
assert(
  "compact API-derived KPI hierarchy",
  view.includes("Toplam Kalıp") &&
    view.includes("Aktif") &&
    view.includes("Bakımda") &&
    view.includes("Denemede") &&
    view.includes("Kritik Dikkat") &&
    view.includes("summary.totalCount") &&
    view.includes("summary.quarantinedCount"),
);
assert("Operasyonel Dikkat section", view.includes("Operasyonel Dikkat"));
assert("attention empty state", view.includes("takip gerektiren operasyonel kalıp kaydı yok"));
assert("registry command surface", view.includes("Kalıp Sicili") && view.includes("mold-registry-surface"));
assert("search preserved", view.includes("Kalıp kodu veya adı") && view.includes("setSearch"));
assert("lifecycle filters preserved", view.includes("MOLD_LIFECYCLE_FILTER_OPTIONS"));
assert("ownership filter preserved", view.includes("MOLD_OWNERSHIP_FILTER_OPTIONS"));
assert("active-filter reset", view.includes("Filtreleri Temizle"));
assert("result count", view.includes("kayıt") && view.includes("filteredMolds.length"));
assert("Mold identity hierarchy", view.includes("mold-identity__name") && view.includes("mold-identity__code"));
assert("localized lifecycle labels", view.includes("getMoldLifecycleStateLabel"));
assert("localized ownership labels", view.includes("getMoldOwnershipLabel"));
assert("localized location labels", view.includes("getMoldLocationLabel"));
assert("row action Detay", view.includes(">Detay<") || view.includes("Detay"));
assert("detail command header", history.includes("mold-detail-header") && history.includes("mold-detail-breadcrumb"));
assert("breadcrumb context", history.includes("Kalıplar /"));
assert("lifecycle badge in detail", history.includes("getMoldLifecycleStateLabel(selectedSummary.lifecycleState)"));
assert("shot-count context", history.includes("Güncel Sayaç"));
assert("write actions permission-aware", history.includes("canManage ?") && history.includes("Düzenle"));
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
assert("responsive breakpoints present", css.includes("1024px") && css.includes("1200px"));
assert("Sayaç Kaydet action preserved", history.includes("Sayaç Kaydet") && history.includes("recordMoldCounterAction"));
assert("Durum Değiştir action preserved", history.includes("Durum Değiştir") && history.includes("transitionMoldLifecycleAction"));

console.log("MOLD-UI-1 premium UI verifier passed.");
