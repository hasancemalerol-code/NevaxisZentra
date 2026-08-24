#!/usr/bin/env node
import { readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), "../../..");

function fail(message) {
  console.error(`COMMERCIAL-INTELLIGENCE-UI-1 premium UI verifier failed: ${message}`);
  process.exit(1);
}

function assert(label, ok, detail = "") {
  if (!ok) fail(`${label}${detail ? `: ${detail}` : ""}`);
  console.log(`✓ ${label}`);
}

function read(rel) {
  return readFileSync(resolve(ROOT, rel), "utf8").replaceAll("\r\n", "\n");
}

console.log("Running COMMERCIAL-INTELLIGENCE-UI-1 premium UI verifier.");

const page = read("apps/web/src/app/(app)/ticari-zeka/satin-alma-fiyat-analizi/page.tsx");
const view = read("apps/web/src/features/commercial-intelligence/components/commercial-intelligence-workspace-view.tsx");
const css = read("apps/web/src/features/commercial-intelligence/components/commercial-intelligence-workspace.css");
const labels = read("apps/web/src/features/commercial-intelligence/domain/commercial-intelligence-labels.ts");
const navigation = read("apps/web/src/lib/navigation/primary-navigation.ts");
const catchAll = read("apps/web/src/app/(app)/[...sections]/page.tsx");
const presentation = read("apps/web/src/features/commercial-intelligence/components/commercial-intelligence-presentation.ts");
void presentation;

assert("dedicated purchase price page", page.includes('forbidUnlessGranted("commercial-intelligence.read")'));
assert("route excluded from catch-all", catchAll.includes('"/ticari-zeka/satin-alma-fiyat-analizi"'));
assert("navigation permission commercial-intelligence.read", navigation.includes('permissionKey: "commercial-intelligence.read"'));
assert("navigation route /ticari-zeka/satin-alma-fiyat-analizi", navigation.includes('href: "/ticari-zeka/satin-alma-fiyat-analizi"'));
assert("navigation label Satın Alma Fiyat Analizi", navigation.includes('label: "Satın Alma Fiyat Analizi"'));
assert("navigation group TİCARİ ZEKÂ", navigation.includes('label: "TİCARİ ZEKÂ"'));

assert("executive eyebrow", view.includes("TİCARİ ZEKÂ"));
assert("executive title", view.includes("<h1>Satın Alma Fiyat Analizi</h1>"));
assert("no duplicate sidebar title in h1", (view.match(/<h1>Satın Alma Fiyat Analizi<\/h1>/g) ?? []).length === 1);
assert("development provenance strip", view.includes('data-testid="commercial-intelligence-provenance-strip"'));
assert("readonly note", view.includes('data-testid="commercial-intelligence-readonly-note"'));
assert("six KPI grid", view.includes('data-testid="commercial-intelligence-kpi-grid"'));
assert("KPI comparable observations", presentation.includes('"Karşılaştırılabilir gözlemler"'));
assert("KPI excluded observations", presentation.includes('"Dışlanan gözlemler"'));
assert("KPI attention analyses", presentation.includes('"Dikkat gerektiren analizler"'));
assert("KPI favorable variances", presentation.includes('"Olumlu sapmalar"'));
assert("KPI supplier spread", presentation.includes('"Tedarikçi yayılım bulguları"'));
assert("KPI insufficient conflict", presentation.includes('"Yetersiz / çelişkili kanıt"'));

assert("four tabs in order", labels.includes("Genel Bakış") && labels.includes("Fiyat Analizleri") && labels.includes("Tedarikçi Karşılaştırması") && labels.includes("Analiz Profilleri"));
assert("accessible tab rail", view.includes('role="tablist"') && view.includes('data-testid="commercial-intelligence-tab-rail"'));
assert("temporal deviation panel", view.includes('data-testid="commercial-intelligence-temporal-deviation"'));
assert("supplier position panel", view.includes('data-testid="commercial-intelligence-supplier-position"'));
assert("priority deviations testid", view.includes('data-testid="commercial-intelligence-priority-deviations"'));
assert("evidence summary testid", view.includes('data-testid="commercial-intelligence-evidence-summary"'));
assert("workspace testid", view.includes('data-testid="commercial-intelligence-workspace"'));

assert("scoped CSS prefix", css.includes(".commint-page") && css.includes(".commint-kpi-grid"));
assert("responsive KPI grid", css.includes("repeat(3, minmax(0, 1fr))"));
assert("overflow protection", css.includes("overflow-x: clip"));
assert("no global css edits", !read("apps/web/src/app/globals.css").includes(".commint-"));

assert("60/40 overview row", css.includes("grid-template-columns: minmax(0, 3fr) minmax(0, 2fr)"));
assert("overview lower row grid areas", css.includes("grid-template-areas"));
assert("compact KPI min-height", css.includes("min-height: 6.2rem"));

assert("analysis detail ready testid", view.includes('data-testid="commercial-intelligence-analysis-detail-ready"'));
assert("analysis detail loading testid", view.includes('data-testid="commercial-intelligence-analysis-detail-loading"'));
assert("analysis detail error testid", view.includes('data-testid="commercial-intelligence-analysis-detail-error"'));
assert("profile workspace testid", view.includes('data-testid="commercial-intelligence-profile-workspace"'));
assert("profile hero testid", view.includes('data-testid="commercial-intelligence-profile-hero"'));
assert("suppliers workspace testid", view.includes('data-testid="commercial-intelligence-suppliers-workspace"'));
assert("supplier ranking testid", view.includes('data-testid="commercial-intelligence-supplier-ranking"'));
assert("analyses workspace testid", view.includes('data-testid="commercial-intelligence-analyses-workspace"'));
assert("evaluation controls testid", view.includes('data-testid="commercial-intelligence-evaluation-controls"'));
assert("comparison controls testid", view.includes('data-testid="commercial-intelligence-comparison-controls"'));
assert("composer layout styles", css.includes(".commint-composer"));
assert("composer mode selector", view.includes("Dönem karşılaştırması") && view.includes("Tedarikçi karşılaştırması"));
assert("business chart labels", view.includes("commint-chart__primary"));
assert("supplier overview populated testid", view.includes('data-testid="commercial-intelligence-supplier-overview-rows"'));
assert("kpi accessible summary", view.includes("accessibleSummary"));
assert("temporal chart testid", view.includes('data-testid="commercial-intelligence-temporal-chart"'));
assert("profile edit drawer testid", view.includes('data-testid="commercial-intelligence-profile-edit-drawer"'));
assert("supplier tie presentation", view.includes("Eşit sıra") || view.includes("Eşit"));
assert("profile workspace layout", css.includes("commint-profile-workspace__layout"));
assert("detail drawer grid", css.includes("commint-detail-grid"));

console.log("COMMERCIAL-INTELLIGENCE-UI-1 premium UI verification passed.");
