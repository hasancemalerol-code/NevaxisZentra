#!/usr/bin/env node
import { readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), "../../..");

function fail(message) {
  console.error(`PROCESS-OEE-2 functional UI verification failed: ${message}`);
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

console.log("Running PROCESS-OEE-2 functional UI verifier.");

const view = readRequired("apps/web/src/features/process-oee/components/process-oee-view.tsx");
const labels = readRequired("apps/web/src/features/process-oee/presentation/process-oee-labels.ts");
const apiClient = readRequired("apps/web/src/features/process-oee/server/process-oee-api-client.ts");
const actions = readRequired("apps/web/src/features/process-oee/server/process-oee-actions.ts");
const reader = readRequired("apps/web/src/features/process-oee/server/process-oee-reader.ts");
const domain = readRequired("apps/web/src/features/process-oee/domain/process-oee.ts");

assert("loss analysis section title", view.includes("OEE Kayıp Analizi"));
assert("loss detail state", view.includes("ProcessOeeLossDetail") || view.includes("lossDetail"));
assert("load loss detail action", view.includes("loadProcessOeeLossDetailAction"));
assert("leading loss panel", view.includes("Öncelikli kayıp"));
assert("normalized impact label", view.includes("Normalleştirilmiş OEE etkisi"));
assert("insufficient loss warning", view.includes("sonuç uydurulmadı"));
assert("source conflict loss warning", view.includes("Kaynak çakışması tespit edildi"));
assert("stale evidence loss warning", view.includes("Bayat kanıt kullanıldı"));

assert("getLossCategoryLabel export", labels.includes("export function getLossCategoryLabel"));
assert("getLossExplanationLabel export", labels.includes("export function getLossExplanationLabel"));
assert("getLossFreshnessLabel export", labels.includes("export function getLossFreshnessLabel"));
assert("availability loss label", labels.includes('"availability-loss"') && labels.includes("Kullanılabilirlik kaybı"));
assert("performance loss label", labels.includes('"performance-loss"') && labels.includes("Performans kaybı"));
assert("quality loss label", labels.includes('"quality-loss"') && labels.includes("Kalite kaybı"));
assert("insufficient data loss label", labels.includes('"insufficient-data"') && labels.includes("Veri yetersiz"));
assert("source conflict loss label", labels.includes('"source-conflict"') && labels.includes("Kaynak çakışması"));
assert("downtime explanation label", labels.includes("downtime-reduces-operating-time"));
assert("missing downtime explanation", labels.includes("downtime-evidence-missing"));

assert("getProcessOeeAssessmentLosses export", apiClient.includes("export function getProcessOeeAssessmentLosses"));
assert("losses API path", apiClient.includes("/losses"));
assert("category label mapping in client", apiClient.includes("getLossCategoryLabel(category)"));
assert("explanation label mapping in client", apiClient.includes("getLossExplanationLabel(explanationCode)"));
assert("freshness label mapping in client", apiClient.includes("getLossFreshnessLabel(freshnessState)"));

assert("loss detail action wiring", actions.includes("loadProcessOeeLossDetailAction"));
assert("loss detail reader wiring", reader.includes("readProcessOeeLossDetail"));
assert("loss detail reader uses losses client", reader.includes("getProcessOeeAssessmentLosses"));
assert("ProcessOeeLossDetail domain type", domain.includes("ProcessOeeLossDetail"));

assert("categoryLabel rendered in driver list", view.includes("{driver.categoryLabel}"));
assert("categoryLabel rendered for leading loss", view.includes("{lossDetail.leadingLoss.categoryLabel}"));
assert("explanationLabel rendered in driver list", view.includes("{driver.explanationLabel}"));
assert(
  "reasonCode not rendered as visible text",
  !view.replaceAll("key={driver.reasonCode}", "").includes("{driver.reasonCode}"),
);
forbidText(view, "{driver.category}", "must not render raw loss category enum");
forbidText(view, "{lossDetail.leadingLoss.category}", "must not render raw leading loss category enum");
forbidText(view, "{driver.explanationCode}", "must not render raw explanation code enum");

forbidText(view, "finansal", "must not claim financial impact");
forbidText(view, "maliyet", "must not claim cost impact");
forbidText(view, "kapasite", "must not claim capacity impact");
forbidText(view, "revenue", "must not claim revenue impact");
forbidText(view, "ROI", "must not claim ROI impact");
forbidText(view, "kâr", "must not claim profit impact");
forbidText(view, "kar ", "must not claim profit impact");

console.log("PROCESS-OEE-2 functional UI verification passed.");
