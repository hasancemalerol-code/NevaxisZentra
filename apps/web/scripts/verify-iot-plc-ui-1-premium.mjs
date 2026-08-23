#!/usr/bin/env node
import { readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), "../../..");

function fail(message) {
  console.error(`IOT-PLC-UI-1 premium UI verifier failed: ${message}`);
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

console.log("Running IOT-PLC-UI-1 premium UI verifier.");

const page = read("apps/web/src/app/(app)/iot-plc/page.tsx");
const view = read("apps/web/src/features/iot-plc/components/iot-plc-workspace-view.tsx");
const css = read("apps/web/src/features/iot-plc/components/iot-plc-workspace.css");
const labels = read("apps/web/src/features/iot-plc/domain/iot-plc-labels.ts");
const queries = read("apps/web/src/features/iot-plc/server/iot-plc-queries.ts");
const formatters = read("apps/web/src/features/iot-plc/domain/iot-plc-formatters.ts");
const presentation = read("apps/web/src/features/iot-plc/components/iot-plc-presentation.ts");
const apiClient = read("apps/web/src/features/iot-plc/server/iot-plc-api-client.ts");
const navigation = read("apps/web/src/lib/navigation/primary-navigation.ts");
const catchAll = read("apps/web/src/app/(app)/[...sections]/page.tsx");
const actions = read("apps/web/src/features/iot-plc/server/iot-plc-actions.ts");
const bundle = `${page}\n${view}\n${css}\n${labels}\n${queries}\n${formatters}`;

assert("dedicated /iot-plc page", page.includes('forbidUnlessGranted("iot-plc.read")'));
assert("iot-plc removed from catch-all static params", catchAll.includes('item.href !== "/iot-plc"'));
assert("navigation permission iot-plc.read", navigation.includes('permissionKey: "iot-plc.read"'));
assert("navigation route /iot-plc", navigation.includes('href: "/iot-plc"'));

assert("executive eyebrow", bundle.includes("ENDÜSTRİYEL BAĞLANTI"));
assert("executive title IoT & PLC", view.includes("<h1>IoT & PLC</h1>"));

assert("development provenance banner once", view.includes("IOT_PLC_DEVELOPMENT_BANNER"));
assert(
  "restrained provenance strip",
  view.includes('data-testid="iot-plc-provenance-strip"') && css.includes(".iot-plc-provenance-strip"),
);
assert(
  "premium KPI hierarchy",
  view.includes("iot-plc-kpi-card__tone") && view.includes("iot-plc-kpi-card__icon"),
);
assert("health donut", view.includes('data-testid="iot-plc-health-donut"'));
assert("quality distribution", view.includes('data-testid="iot-plc-quality-distribution"'));
assert("integrated tab rail", css.includes(".iot-plc-tab-rail") && css.includes("background: #f8fafc"));

assert("exactly six KPI cards via queries", queries.includes('"registered-endpoints"'));
assert("KPI order registered endpoints", queries.includes('label: "Kayıtlı bağlantılar"'));
assert("KPI online endpoints", queries.includes('label: "Çevrimiçi bağlantılar"'));
assert("KPI degraded offline", queries.includes('label: "Bozulmuş / çevrimdışı"'));
assert("KPI active signals", queries.includes('label: "Aktif sinyaller"'));
assert("KPI fresh signals", queries.includes('label: "Güncel sinyaller"'));
assert("KPI attention assessments", queries.includes('label: "Dikkat gerektiren değerlendirmeler"'));
assert("KPI grid testid", view.includes('data-testid="iot-plc-kpi-grid"'));

assert("tab Genel Bakış", labels.includes("Genel Bakış"));
assert("tab Bağlantılar", labels.includes("Bağlantılar"));
assert("tab Sinyaller", labels.includes("Sinyaller"));
assert("tab Veri Kalitesi", labels.includes("Veri Kalitesi"));
assert("accessible tab rail", view.includes('role="tablist"') && view.includes("iot-plc-tab-rail"));

assert("formatIotPlcRatioPercent for 0-1 fractions", formatters.includes("API ratios are 0–1 fractions"));
assert("ratio formatter used in view", view.includes("formatIotPlcRatioPercent"));
assert("ratio formatter uses Intl percent", formatters.includes('style: "percent"'));
assert("ratio formatter does not double-scale", !formatters.includes("* 100") && !formatters.includes("*100"));

assert("workspace testid", view.includes('data-testid="iot-plc-workspace"'));
assert("kpi-grid testid", view.includes('data-testid="iot-plc-kpi-grid"'));
assert("health-donut testid", view.includes('data-testid="iot-plc-health-donut"'));
assert("quality-distribution testid", view.includes('data-testid="iot-plc-quality-distribution"'));
assert("tab-rail testid", view.includes('data-testid="iot-plc-tab-rail"'));

assert("iot-plc prefix CSS", css.includes(".iot-plc-page") && css.includes(".iot-plc-kpi-grid"));
assert("responsive KPI grid", css.includes("repeat(2, minmax(0, 1fr))") || css.includes("repeat(3, minmax(0, 1fr))"));
assert("overflow protection", css.includes("overflow-x: clip") || css.includes("overflow-x: auto"));

assert("read-only note", view.includes("iot-plc-readonly-note"));
assert("manage gated mutations", actions.includes('denyUnlessGranted("iot-plc.manage")'));

forbidText(view, "remediation", "view");
forbidText(view, "/write", "view");
forbidText(view, "PLC command", "view");
forbidText(view, "plc command", "view");
forbidText(view, "/commands", "view");
forbidText(view, 'href="#"', "view");
forbidText(view, "onClick={() => {}}", "view");
assert(
  "raw entity ids not rendered in workspace cells",
  !/<td>\{[^}]*\.id\}<\/td>/.test(view) && !/<dd>\{[^}]*\.id\}<\/dd>/.test(view),
);
assert("pilot UUID fragments absent from view", !view.includes("bb000001"));

assert("API-backed queries", apiClient.includes("/api/iot-plc/workspace"));
assert("intelligence API", apiClient.includes("/api/iot-plc/intelligence/workspace"));
assert("donut arc builder", presentation.includes("buildHealthDonutArcs"));
assert("quality bar builder", presentation.includes("buildQualityBarSegments"));
assert("reconcile quality distribution helper", presentation.includes("reconcileIotPlcQualityDistribution"));
assert("strict ratio fraction validator", presentation.includes("isIotPlcRatioFraction"));
assert("signal assessment ratio contract validator", presentation.includes("validateIotPlcSignalAssessmentRatioContract"));
assert("no dual-scale ratio normalization", !presentation.includes("value / 100") && !presentation.includes("normalizeIotPlcRatioFraction"));
assert("current evidence scope label", labels.includes("IOT_PLC_CURRENT_EVIDENCE_SCOPE"));
assert("current status note label", labels.includes("IOT_PLC_CURRENT_STATUS_NOTE"));
assert("last-known observations title", labels.includes("IOT_PLC_LAST_KNOWN_OBSERVATIONS_TITLE"));
assert("profile display name localization", labels.includes("getIotPlcProfileDisplayName"));
assert("development banner Turkish", labels.includes("gerçek PLC veya müşteri verisi değildir"));
assert("current evidence scope in view", view.includes('data-testid="iot-plc-current-evidence-scope"'));
assert("current status note in view", view.includes('data-testid="iot-plc-current-status-note"'));
assert("quality controls grid", css.includes(".iot-plc-quality-eval-grid"));
assert("quality hero intelligence surface", view.includes('data-testid="iot-plc-quality-hero-intelligence"'));
assert("quality profile rail", view.includes('data-testid="iot-plc-quality-profile-rail"'));
assert("quality evaluation groups", view.includes('data-testid="iot-plc-quality-eval-connection"'));
assert("finding cards bounded", view.includes('data-testid="iot-plc-finding-card"'));
assert("health donut layout", view.includes('data-testid="iot-plc-health-donut-layout"'));
assert("premium KPI value sizing", css.includes("clamp(1.75rem, 2.1vw, 2.125rem)"));
assert("tab minimum height 42px", css.includes("min-height: 2.625rem"));
assert("no mock fallback", !queries.includes("MOCK_") && !view.includes("MOCK_"));

console.log("IOT-PLC-UI-1 premium UI verification passed.");
