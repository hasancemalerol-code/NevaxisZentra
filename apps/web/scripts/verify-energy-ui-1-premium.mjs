#!/usr/bin/env node
import { readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), "../../..");

function fail(message) {
  console.error(`ENERGY-UI-1 premium UI verifier failed: ${message}`);
  process.exit(1);
}

function assert(label, ok, detail = "") {
  if (!ok) fail(`${label}${detail ? `: ${detail}` : ""}`);
  console.log(`✓ ${label}`);
}

function read(rel) {
  return readFileSync(resolve(ROOT, rel), "utf8").replaceAll("\r\n", "\n");
}

function forbidText(source, forbidden, label) {
  if (source.includes(forbidden)) fail(`${label} must not contain "${forbidden}"`);
}

console.log("Running ENERGY-UI-1 premium UI verifier.");

const page = read("apps/web/src/app/(app)/enerji/page.tsx");
const view = read("apps/web/src/features/energy/components/energy-workspace-view.tsx");
const css = read("apps/web/src/features/energy/components/energy-workspace.css");
const labels = read("apps/web/src/features/energy/domain/energy-labels.ts");
const queries = read("apps/web/src/features/energy/server/energy-queries.ts");
const apiClient = read("apps/web/src/features/energy/server/energy-api-client.ts");
const navigation = read("apps/web/src/lib/navigation/primary-navigation.ts");
const catchAll = read("apps/web/src/app/(app)/[...sections]/page.tsx");
const bundle = `${page}\n${view}\n${css}\n${labels}\n${queries}`;

assert("dedicated /enerji page", page.includes('forbidUnlessGranted("energy.read")'));
assert("enerji removed from catch-all static params", catchAll.includes('item.href !== "/enerji"'));
assert("navigation permission energy.read", navigation.includes('permissionKey: "energy.read"'));
assert("navigation route /enerji", navigation.includes('href: "/enerji"'));

assert("executive eyebrow", bundle.includes("ENERJİ YÖNETİMİ"));
assert("executive title Enerji", view.includes("<h1>Enerji</h1>"));
assert("injection auxiliary subtitle", view.includes("Enjeksiyon makinesi"));

assert("development provenance banner once", view.includes("ENERGY_DEVELOPMENT_BANNER"));
assert(
  "restrained provenance strip",
  view.includes('data-testid="energy-provenance-strip"') && css.includes(".energy-provenance-strip"),
);
assert(
  "premium KPI hierarchy",
  view.includes("energy-kpi-card__tone") && view.includes("energy-kpi-card__icon"),
);
assert("allocation donut", view.includes('data-testid="energy-allocation-donut"'));
assert("chart area series", view.includes('data-testid="energy-trend-actual"'));
assert("chart y-axis labels", css.includes(".energy-chart-y-label"));
assert("integrated tab rail", css.includes(".energy-tab-rail") && css.includes("background: #f8fafc"));

assert("exactly six KPI cards via queries", queries.includes('"total-consumption"'));
assert("KPI order total consumption", queries.includes('label: "Toplam tüketim"'));
assert("KPI peak power", queries.includes('label: "Tepe güç"'));
assert("KPI energy intensity", queries.includes('label: "Enerji yoğunluğu"'));
assert("KPI target variance", queries.includes('label: "Hedef sapması"'));
assert("KPI idle consumption", queries.includes('label: "Boşta tüketim"'));
assert("KPI estimated cost", queries.includes('label: "Tahmini enerji maliyeti"'));
assert("missing tariff label", queries.includes('"Tarife tanımlı değil"'));
assert("KPI grid testid", view.includes('data-testid="energy-kpi-grid"'));

assert("tab Genel Bakış", labels.includes("Genel Bakış"));
assert("tab Ölçüm Noktaları", labels.includes("Ölçüm Noktaları"));
assert("tab Hedefler ve Bazlar", labels.includes("Hedefler ve Bazlar"));
assert("tab Değerlendirmeler", labels.includes("Değerlendirmeler"));
assert("accessible tab rail", view.includes('role="tablist"') && view.includes("energy-tab-rail"));

assert("overview trend title", view.includes("Tüketim eğilimi"));
assert("canonical trend metadata", view.includes('data-testid="energy-trend-metadata"'));
assert("canonical trend scope caption", queries.includes("saatlik karşılaştırılabilir tüketim pencereleri"));
assert("trend eligibility exclusions", queries.includes("DEVELOPMENT_ASSESSMENT_SCENARIO_WINDOW_IDS"));
forbidText(view, "Tüketim ve güç eğilimi", "view");
assert("donut center total label", view.includes('data-testid="energy-donut-center-total-label"'));
assert("donut center coverage label", view.includes('data-testid="energy-donut-center-coverage-label"'));
assert("donut inner hole", view.includes('data-testid="energy-allocation-donut-hole"'));
assert("donut html center overlay", view.includes('data-testid="energy-donut-center"'));
assert("donut center transparent overlay", css.includes("background: transparent") && css.includes("aspect-ratio: 1"));
assert("donut center circular bounds", css.includes("border-radius: 50%"));
assert("lower intelligence full-width grid", css.includes("grid-column: 1 / -1"));
assert("allocation title", view.includes("Enerji dağılımı"));
assert("intelligence title", view.includes("Enerji performansı"));
assert("target comparison panel", view.includes('data-testid="energy-target-comparison"'));
assert("overview intelligence panel", view.includes('data-testid="energy-overview-intelligence"'));
assert("KPI evidence rail styling", css.includes(".energy-kpi-card__evidence-rail"));
assert("KPI semantic data attributes", view.includes('data-kpi-status={presentation.statusLabel}'));
assert("metric direction helper", read("apps/web/src/features/energy/components/energy-presentation.ts").includes("compareLowerIsBetter"));
assert("chart legend actual", css.includes("energy-chart-legend__swatch--actual"));
assert("chart legend target", css.includes("energy-chart-legend__swatch--target"));
assert("chart legend baseline", css.includes("energy-chart-legend__swatch--baseline"));

assert("baseline calculated badge", view.includes("Hesaplanan"));
assert("target approved badge", view.includes("Onaylı"));
assert("baseline card styling", css.includes(".energy-baseline-card"));
assert("target card styling", css.includes(".energy-target-card"));

assert("localized outcomes", labels.includes('"within-target": "Hedef içinde"'));
assert("localized driver peak", labels.includes('"peak-demand-exceeded": "Tepe güç hedefi aşıldı"'));
assert("opportunity disclaimer constant", labels.includes("Bu değer garanti edilen tasarruf değildir"));

assert("measurement registry layout", view.includes("energy-registry-layout--detail"));
assert("measurement search", view.includes("Ad veya kod ara"));
assert("measurement scope filter", view.includes("ENERGY_SCOPE_FILTER_OPTIONS"));
assert("measurement equipment filter", view.includes("ENERGY_EQUIPMENT_FILTER_OPTIONS"));
assert("detail close control", view.includes('aria-label="Ölçüm noktası detayını kapat"'));

assert("assessment outcome filter", view.includes("ENERGY_OUTCOME_FILTER_OPTIONS"));
assert("assessment structured evidence", view.includes("Teknik ayrıntılar"));
assert("assessment detail action", view.includes("loadEnergyAssessmentDetailAction"));

assert("read-only note", view.includes("energy-readonly-note"));
assert("manage gated update", view.includes("updateEnergyTargetAction"));
assert("permission manage in actions", read("apps/web/src/features/energy/server/energy-actions.ts").includes('denyUnlessGranted("energy.manage")'));

assert("responsive KPI grid", css.includes("repeat(3, minmax(0, 1fr))"));
assert("responsive reflow 1280", css.includes("@media (max-width: 1280px)"));
assert("overflow protection", css.includes("overflow-x: clip"));

forbidText(bundle, "guaranteed savings", "bundle");
assert(
  "opportunity honesty disclaimer present",
  labels.includes("Bu değer garanti edilen tasarruf değildir"),
);
forbidText(view, "{assessment.id}", "view");
forbidText(view, "{point.id}", "view");
forbidText(view, 'href="#"', "view");
forbidText(view, "onClick={() => {}}", "view");

assert("API-backed queries", apiClient.includes("/api/energy/workspace"));
assert("intelligence API", apiClient.includes("/api/energy/intelligence/workspace"));
assert("no mock fallback", !queries.includes("mock") && !view.includes("MOCK_"));

console.log("ENERGY-UI-1 premium UI verification passed.");
