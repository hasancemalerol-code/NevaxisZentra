#!/usr/bin/env node
import { readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), "../../..");

function fail(message) {
  console.error(`PROCESS-OEE-UI-1 premium verification failed: ${message}`);
  process.exit(1);
}

function assert(label, ok, detail = "") {
  if (!ok) fail(`${label}${detail ? `: ${detail}` : ""}`);
  console.log(`✓ ${label}`);
}

function readRequired(relativePath) {
  return readFileSync(resolve(ROOT, relativePath), "utf8").replaceAll("\r\n", "\n");
}

function forbidText(source, forbidden, label) {
  if (source.includes(forbidden)) fail(`${label} must not contain "${forbidden}"`);
}

console.log("Running PROCESS-OEE-UI-1 premium verifier.");

const view = readRequired("apps/web/src/features/process-oee/components/process-oee-view.tsx");
const css = readRequired("apps/web/src/features/process-oee/components/process-oee.css");
const page = readRequired("apps/web/src/app/(app)/proses-oee/page.tsx");
const labels = readRequired("apps/web/src/features/process-oee/presentation/process-oee-labels.ts");

assert("executive eyebrow", view.includes("PROSES VE VERİMLİLİK"));
assert("executive title", view.includes("Proses &amp; OEE"));
assert("injection-focused subtitle", view.includes("Enjeksiyon hatları"));
assert("executive KPI strip", view.includes("process-oee-executive__kpis"));
assert("KPI OEE binding", view.includes("formatPercent(kpiOee)"));
assert("KPI availability binding", view.includes("formatPercent(kpiAvailability)"));
assert("KPI performance binding", view.includes("formatPercent(kpiPerformance)"));
assert("KPI quality binding", view.includes("formatPercent(kpiQuality)"));
assert("KPI window label", view.includes("İncelenen Pencere"));
assert("KPI data sufficiency", view.includes("Veri Yeterliliği"));
assert("unavailable value dash via formatPercent", labels.includes('return "—"'));

assert("operational attention section", view.includes("Operasyonel Dikkat"));
assert("attention builder", view.includes("buildAttentionItems"));
assert("attention deterministic ordering", view.includes(".sort((a, b) => a.priority - b.priority"));
assert("attention selection wiring", view.includes("selectAssessment(assessment, item.tab)"));
assert("attention empty state", view.includes("Takip gerektiren operasyonel değerlendirme bulunmuyor"));
forbidText(view, "yapay zeka", "view");
forbidText(view, "AI ", "view");

assert("registry title", view.includes("OEE Değerlendirme Pencereleri"));
assert("registry full width without selection", css.includes(".process-oee-workspace {"));
assert("no blank detail panel when unselected", view.includes("detailOpen && selectedSummary"));
assert("search filter preserved", view.includes("process-oee-search"));
assert("machine filter preserved", view.includes("process-oee-machine"));
assert("result count", view.includes("filteredAssessments.length"));
assert("identity hierarchy machine first", view.includes("process-oee-cell-primary"));
assert("selected row state", view.includes('data-selected={item.id === selectedId'));
assert("keyboard row selection", view.includes('event.key === "Enter"'));
assert("inline detail action", view.includes("Detay"));
assert("close button", view.includes("process-oee-detail-close"));
assert("close accessible name", view.includes('aria-label="Proses detayını kapat"'));
assert("close preserves filters", view.includes("function clearSelection"));
assert("close restores focus", view.includes("data-assessment-id"));
assert("close restores full registry", view.includes('setSelectedId("")'));

forbidText(view, ">insufficient-data<", "raw enum in JSX text");
forbidText(view, ">stable<", "raw enum in JSX text");

assert("master-detail workspace", css.includes("process-oee-workspace--detail-open"));
assert("registry width token", css.includes("--poee-registry-width"));
assert("1024 stacked behavior", css.includes("@media (max-width: 1024px)"));
assert("detail header breadcrumb", view.includes("process-oee-detail-breadcrumb"));
assert("detail header period", view.includes("process-oee-detail-header__period"));
assert("detail header summary metrics", view.includes("process-oee-detail-header__summary"));

assert("tab rail", view.includes("process-oee-tab-rail"));
assert("tab Genel Bakış", view.includes('"Genel Bakış"'));
assert("tab OEE Kayıp Analizi", view.includes('"OEE Kayıp Analizi"'));
assert("tab Proses Kararlılığı", view.includes('"Proses Kararlılığı"'));
assert("tab Duruş Analizi", view.includes('"Duruş Analizi"'));
assert("tab Vardiya Karşılaştırması", view.includes('"Vardiya Karşılaştırması"'));
assert("tab Hesaplama Kanıtları", view.includes('"Hesaplama Kanıtları"'));
assert("tab nowrap", css.includes("white-space: nowrap"));
assert("sticky context", view.includes("process-oee-sticky-context"));
assert("sticky top offset from token", css.includes("var(--top-bar-height"));
assert("sticky scroll margin", css.includes("scroll-margin-top: calc(var(--poee-sticky-top)"));

assert("leading loss block", view.includes("Öncelikli kayıp"));
assert("contributing losses list", view.includes("Katkı veren kayıplar"));
assert("loss rule set", view.includes("lossDetail.ruleSetVersion"));
assert("stability state label", view.includes("stabilityDetail.resultStateLabel"));
assert("sample count", view.includes("Çevrim örneği"));
assert("median cycle", view.includes("Medyan çevrim"));
assert("MAD variability", view.includes("Değişkenlik (MAD)"));
assert("drift direction", view.includes("Kayma yönü"));
assert("stability rule set", view.includes("stabilityDetail.ruleSetVersion"));
assert("micro-stop summary", view.includes("Mikro duruş"));
assert("median micro-stop", view.includes("Medyan mikro duruş"));
assert("downtime pareto", view.includes("Duruş Pareto"));
assert("classified unclassified", view.includes("Sınıflı / sınıfsız duruş"));
assert("pareto bar without chart lib", css.includes("process-oee-pareto-row__bar"));
assert("shift labels", view.includes("row.shiftLabel"));
assert("comparability warning", view.includes("comparabilityWarningLabel"));
forbidText(view, "operatör", "operator blame");
forbidText(view, "personel", "personnel recommendation");

assert("loading state", view.includes("Detay yükleniyor"));
assert("empty state", view.includes("Değerlendirme bulunamadı"));
assert("error state", view.includes("process-oee-alert--error"));
assert("read-only state", view.includes("Salt okunur erişim"));
assert("insufficient warning", view.includes("Veri yetersiz"));
assert("stale warning", view.includes("Bayat veri"));
assert("source conflict warning", view.includes("Kaynak çakışması"));

forbidText(view, "MapPost", "backend mutation");
forbidText(view, "globals.css", "globals change");
assert("scoped css import", view.includes('./process-oee.css"'));
assert("route preserved", page.includes('readProcessOeeWorkspace'));

console.log("PROCESS-OEE-UI-1 premium verification passed.");
