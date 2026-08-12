#!/usr/bin/env node
import { readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), "../../..");

function fail(message) {
  console.error(`PROCESS-OEE-3 functional UI verification failed: ${message}`);
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

console.log("Running PROCESS-OEE-3 functional UI verifier.");

const view = readRequired("apps/web/src/features/process-oee/components/process-oee-view.tsx");
const labels = readRequired("apps/web/src/features/process-oee/presentation/process-oee-labels.ts");
const apiClient = readRequired("apps/web/src/features/process-oee/server/process-oee-api-client.ts");
const actions = readRequired("apps/web/src/features/process-oee/server/process-oee-actions.ts");
const reader = readRequired("apps/web/src/features/process-oee/server/process-oee-reader.ts");
const domain = readRequired("apps/web/src/features/process-oee/domain/process-oee.ts");

assert(
  "stability section title",
  view.includes("Proses Kararlılığı") && view.includes("Duruş Analizi"),
);
assert("shift comparison section title", view.includes("Vardiya Karşılaştırması"));
assert("stability detail state", view.includes("ProcessOeeStabilityDetail") || view.includes("stabilityDetail"));
assert("shift comparison state", view.includes("ProcessOeeShiftComparison") || view.includes("shiftComparison"));
assert("load stability detail action", view.includes("loadProcessOeeStabilityDetailAction"));
assert("load shift comparison action", view.includes("loadProcessOeeShiftComparisonAction"));
assert("stability status label", view.includes("Kararlılık durumu"));
assert("cycle sample label", view.includes("Çevrim örneği"));
assert("median cycle label", view.includes("Medyan çevrim"));
assert("variability MAD label", view.includes("Değişkenlik (MAD)"));
assert("drift direction label", view.includes("Kayma yönü"));
assert("micro-stop label", view.includes("Mikro duruş"));
assert("classified/unclassified downtime label", view.includes("Sınıflı / sınıfsız duruş"));
assert("downtime pareto heading", view.includes("Duruş Pareto"));
assert("insufficient stability warning", view.includes("Yetersiz veya bayat kanıt; normal sonuç üretilmedi"));
assert("shift table headers", view.includes("<th>Vardiya</th>") && view.includes("<th>Kararlılık</th>"));
assert("comparability warning rendering", view.includes("comparabilityWarningLabel"));

assert("getStabilityResultStateLabel export", labels.includes("export function getStabilityResultStateLabel"));
assert("getStabilityCategoryLabel export", labels.includes("export function getStabilityCategoryLabel"));
assert("getDriftDirectionLabel export", labels.includes("export function getDriftDirectionLabel"));
assert("getDowntimeReasonLabel export", labels.includes("export function getDowntimeReasonLabel"));
assert("getStabilityExplanationLabel export", labels.includes("export function getStabilityExplanationLabel"));
assert("getComparabilityWarningLabel export", labels.includes("export function getComparabilityWarningLabel"));
assert("getDataSufficiencyLabel export", labels.includes("export function getDataSufficiencyLabel"));
assert("stable label", labels.includes('"stable"') && labels.includes("Kararlı"));
assert("variable label", labels.includes('"variable"') && labels.includes("Değişken"));
assert("drifting label", labels.includes('"drifting"') && labels.includes("Kayıyor"));
assert("cycle variability label", labels.includes('"cycle-variability"') && labels.includes("Çevrim değişkenliği"));
assert("micro-stop category label", labels.includes('"micro-stop"') && labels.includes("Mikro duruş"));
assert("unknown downtime label", labels.includes('"unknown"') && labels.includes("Bilinmeyen"));

assert("getProcessOeeAssessmentStability export", apiClient.includes("export function getProcessOeeAssessmentStability"));
assert("stability API path", apiClient.includes("/stability"));
assert("getProcessOeeShiftComparison export", apiClient.includes("export function getProcessOeeShiftComparison"));
assert("shift comparison API path", apiClient.includes("/shift-comparison"));
assert("stability result label mapping in client", apiClient.includes("getStabilityResultStateLabel"));
assert("stability category label mapping in client", apiClient.includes("getStabilityCategoryLabel"));
assert("drift direction label mapping in client", apiClient.includes("getDriftDirectionLabel"));
assert("downtime reason label mapping in client", apiClient.includes("getDowntimeReasonLabel"));
assert("comparability warning label mapping in client", apiClient.includes("getComparabilityWarningLabel"));
assert("data sufficiency label mapping in client", apiClient.includes("getDataSufficiencyLabel"));

assert("stability detail action wiring", actions.includes("loadProcessOeeStabilityDetailAction"));
assert("shift comparison action wiring", actions.includes("loadProcessOeeShiftComparisonAction"));
assert("stability detail reader wiring", reader.includes("readProcessOeeStabilityDetail"));
assert("shift comparison reader wiring", reader.includes("readProcessOeeShiftComparison"));
assert("stability detail reader uses stability client", reader.includes("getProcessOeeAssessmentStability"));
assert("shift comparison reader uses shift client", reader.includes("getProcessOeeShiftComparison"));
assert("ProcessOeeStabilityDetail domain type", domain.includes("ProcessOeeStabilityDetail"));
assert("ProcessOeeShiftComparison domain type", domain.includes("ProcessOeeShiftComparison"));

assert("resultStateLabel rendered", view.includes("{stabilityDetail.resultStateLabel}"));
assert("driftDirectionLabel rendered", view.includes("{stabilityDetail.driftDirectionLabel}"));
assert("categoryLabel rendered in stability driver list", view.includes("{driver.categoryLabel}"));
assert("explanationLabel rendered in stability driver list", view.includes("{driver.explanationLabel}"));
assert("resultStateLabel rendered in stability driver list", view.includes("{driver.resultStateLabel}"));
assert("reasonLabel rendered in pareto list", view.includes("{row.reasonLabel}"));
assert("shiftLabel rendered in comparison table", view.includes("{row.shiftLabel}"));
assert("stabilityResultStateLabel rendered in shift table", view.includes("{row.stabilityResultStateLabel"));
assert(
  "reasonCode not rendered as visible stability text",
  !view.replaceAll("key={driver.reasonCode}", "").includes("{driver.reasonCode}"),
);
assert(
  "pareto reasonCode key only",
  !view.replaceAll("key={row.reasonCode}", "").includes("{row.reasonCode}"),
);
assert(
  "shiftCode not rendered as visible text",
  !view.replaceAll("key={`${row.shiftCode}-${row.windowStartUtc}`}", "").includes("{row.shiftCode}"),
);
forbidText(view, "{driver.category}", "must not render raw stability category enum");
forbidText(view, "{stabilityDetail.resultState}", "must not render raw stability result enum");
forbidText(view, "{stabilityDetail.driftDirection}", "must not render raw drift direction enum");
forbidText(view, "{row.stabilityResultState}", "must not render raw shift stability enum");

const shiftSectionStart = view.indexOf('aria-label="Vardiya Karşılaştırması"');
const shiftSectionEnd = view.indexOf("function renderEvidenceTab", shiftSectionStart + 1);
const shiftSection = shiftSectionStart >= 0 && shiftSectionEnd > shiftSectionStart
  ? view.slice(shiftSectionStart, shiftSectionEnd)
  : view;

forbidText(view, "finansal", "must not claim financial impact");
forbidText(view, "maliyet", "must not claim cost impact");
forbidText(view, "kapasite", "must not claim capacity impact");
forbidText(view, "revenue", "must not claim revenue impact");
forbidText(view, "ROI", "must not claim ROI impact");
forbidText(view, "kâr", "must not claim profit impact");
forbidText(view, "kar ", "must not claim profit impact");
forbidText(shiftSection, "operatör", "shift comparison must not attribute results to operators");
forbidText(shiftSection, "personel", "shift comparison must not recommend personnel changes");
forbidText(view, "predictive", "must not claim predictive failure");
forbidText(view, "tahmin", "must not claim predictive failure");

console.log("PROCESS-OEE-3 functional UI verification passed.");
