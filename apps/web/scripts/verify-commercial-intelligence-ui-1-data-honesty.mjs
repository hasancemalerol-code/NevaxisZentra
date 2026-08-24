#!/usr/bin/env node
import { readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), "../../..");

function fail(message) {
  console.error(`COMMERCIAL-INTELLIGENCE-UI-1 data-honesty verifier failed: ${message}`);
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

console.log("Running COMMERCIAL-INTELLIGENCE-UI-1 data-honesty verifier.");

const view = read("apps/web/src/features/commercial-intelligence/components/commercial-intelligence-workspace-view.tsx");
const queries = read("apps/web/src/features/commercial-intelligence/server/commercial-intelligence-queries.ts");
const labels = read("apps/web/src/features/commercial-intelligence/domain/commercial-intelligence-labels.ts");
const formatters = read("apps/web/src/features/commercial-intelligence/domain/commercial-intelligence-formatters.ts");
const presentation = read("apps/web/src/features/commercial-intelligence/components/commercial-intelligence-presentation.ts");
const bundle = `${view}\n${queries}\n${labels}\n${presentation}\n${formatters}`;

assert("ratio formatter multiplies once", formatters.includes("const percent = ratio * 100"));
assert("ratio formatter never clamps", !formatters.includes("Math.min") && !formatters.includes("Math.max"));
assert("validate ratio presentation helper", presentation.includes("validateRatioPresentation"));
assert("signed variance lower bound guard", presentation.includes("ratio < -1"));
assert("KPI reconciliation helper", presentation.includes("reconcileCommercialIntelligenceKpis"));
assert("supplier spread reconciliation", presentation.includes("reconcileCommercialIntelligenceSupplierSpread"));
assert("compatible supplier comparison selector", presentation.includes("selectLatestCompatibleSupplierComparison"));
assert("evidence context resolver", presentation.includes("resolveCommercialIntelligenceEvidenceContext"));
assert("supplier spread threshold breach helper", presentation.includes("isSupplierSpreadThresholdBreach"));
assert("comparison primary label helper", presentation.includes("buildCommercialIntelligenceComparisonPrimaryLabel"));
assert("technical run reference guard", presentation.includes("isTechnicalRunReference"));
assert("kpi cards builder", presentation.includes("buildCommercialIntelligenceKpiCards"));
assert("evidence summary denominator", presentation.includes("reconciliationLabel"));
assert("material localization helper", presentation.includes("localizeCommercialIntelligenceMaterialLabel"));
assert("uuid primary label guard", presentation.includes("isRawUuidPrimaryLabel"));
assert("forbidden UI terms inventory", presentation.includes("COMMERCIAL_INTELLIGENCE_FORBIDDEN_UI_TERMS"));
assert("development banner Turkish", labels.includes("gerçek müşteri tedarik fiyatı"));
assert("simulated evidence label", labels.includes('simulated: "Simülasyon"'));
assert("tie position state label", labels.includes('"tied-comparable-price"'));
forbidText(view, "CI-CMP-PILOT-", "primary chart label");
assert("chart uses business primary label", view.includes("point.primaryLabel"));
assert("supplier overview rows testid", view.includes('data-testid="commercial-intelligence-supplier-overview-rows"'));
assert("kpi scope attributes", view.includes("data-kpi-scope"));
assert("kpi source attributes", view.includes("data-kpi-source"));
assert("no summed heterogeneous denominators in queries", !queries.includes(".reduce((sum, value) => sum + value, 0)") || queries.includes("resolveCommercialIntelligenceEvidenceContext"));
assert("spread finding uses threshold breach", presentation.includes("countSupplierSpreadFindings"));

forbidText(`${view}\n${queries}\n${labels}`, "guaranteed savings", "user-facing bundle");
forbidText(`${view}\n${queries}\n${labels}`, "recommended supplier", "user-facing bundle");
forbidText(`${view}\n${queries}\n${labels}`, "best supplier", "user-facing bundle");
forbidText(`${view}\n${queries}\n${labels}`, "automatic procurement", "user-facing bundle");
forbidText(`${view}\n${queries}\n${labels}`, "AI ", "user-facing bundle");
forbidText(`${view}\n${queries}\n${labels}`, "LLM", "user-facing bundle");
forbidText(view, "bb000001", "view");
assert("no raw uuid primary cells", !/<td>\{[^}]*\.id\}<\/td>/.test(view));

assert("six KPI order", labels.includes('"comparable-observations"') && labels.includes('"insufficient-conflict"'));
assert("comparable KPI label", presentation.includes('"Karşılaştırılabilir gözlemler"'));
assert("excluded KPI label", presentation.includes('"Dışlanan gözlemler"'));
assert("attention KPI label", presentation.includes('"Dikkat gerektiren analizler"'));
assert("favorable KPI label", presentation.includes('"Olumlu sapmalar"'));
assert("spread KPI label", presentation.includes('"Tedarikçi yayılım bulguları"'));
assert("insufficient conflict KPI label", presentation.includes('"Yetersiz / çelişkili kanıt"'));

assert("no dual-scale ratio normalization", !bundle.includes("/ 100") && !bundle.includes("/100"));
assert("no legacy 0-100 storage branch", !/ratio\s*>\s*1\s*\?/.test(bundle));
assert("no signed variance clamp to 100", !/Math\.min\(\s*100\s*,.*variance/i.test(bundle));
assert("no signed variance clamp to -100", !/Math\.max\(\s*-100/i.test(bundle));

function formatRatioAsPercent(ratio) {
  if (ratio === null || ratio === undefined || Number.isNaN(ratio)) return "—";
  const percent = ratio * 100;
  const formatted = new Intl.NumberFormat("tr-TR", {
    style: "decimal",
    maximumFractionDigits: 1,
    signDisplay: "exceptZero",
  }).format(percent);
  return `${formatted}%`;
}

assert(">100% positive variance", formatRatioAsPercent(1.5).includes("150"));
assert("exactly +100%", formatRatioAsPercent(1).includes("100"));
assert("approximately -99.0099%", formatRatioAsPercent(-0.990099).includes("-99"));
assert("supplier spread >100%", formatRatioAsPercent(1.25).includes("125"));

console.log("COMMERCIAL-INTELLIGENCE-UI-1 data-honesty verification passed.");
