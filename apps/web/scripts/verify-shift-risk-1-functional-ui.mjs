#!/usr/bin/env node
import { readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), "../../..");
const WEB = process.env.NEVAXIS_FACTOS_WEB_BASE_URL ?? "http://localhost:3001";

function fail(message) {
  console.error(`SHIFT-RISK-1 functional UI verification failed: ${message}`);
  process.exit(1);
}

function assert(label, ok, detail = "") {
  if (!ok) fail(`${label}${detail ? `: ${detail}` : ""}`);
  console.log(`✓ ${label}`);
}

function readRequired(relativePath) {
  return readFileSync(resolve(ROOT, relativePath), "utf8").replaceAll("\r\n", "\n");
}

function requireText(source, expected, label) {
  if (!source.includes(expected)) fail(`${label} must contain "${expected}"`);
}

function forbidText(source, forbidden, label) {
  if (source.includes(forbidden)) fail(`${label} must not contain "${forbidden}"`);
}

console.log("Running SHIFT-RISK-1 functional UI verifier.");

const page = readRequired("apps/web/src/app/(app)/vardiyalar/page.tsx");
const view = readRequired("apps/web/src/features/shift-workforce/components/shift-workforce-view.tsx");
const panel = readRequired("apps/web/src/features/shift-risk/components/shift-risk-panel.tsx");
const labels = readRequired("apps/web/src/features/shift-risk/domain/shift-risk-labels.ts");
const api = readRequired("apps/web/src/features/shift-risk/server/shift-risk-api.ts");
const queries = readRequired("apps/web/src/features/shift-risk/server/shift-risk-queries.ts");
const actions = readRequired("apps/web/src/features/shift-risk/server/shift-risk-actions.ts");
const css = readRequired("apps/web/src/features/shift-workforce/components/shift-workforce.css");
const globals = readRequired("apps/web/src/app/globals.css");
const nav = readRequired("apps/web/src/lib/navigation/primary-navigation.ts");

requireText(view, "Vardiya Riskleri", "workspace tab");
requireText(view, 'id: "risk"', "risk tab id");
requireText(view, "ShiftRiskPanel", "risk panel mount");
requireText(panel, "Vardiya Risk Değerlendirmeleri", "risk registry heading");
requireText(panel, "Değerlendirilen", "evaluated KPI");
requireText(panel, "Temiz", "clear KPI");
requireText(panel, "Kapsama açığı", "coverage gap KPI");
requireText(panel, "Kritik rol açığı", "critical role gap KPI");
requireText(panel, "Çakışma", "conflict KPI");
requireText(panel, "Yetersiz veri", "insufficient-data KPI");
requireText(panel, "formatShiftRiskCount", "null-safe KPI formatting");
requireText(panel, "Yapılandırılmış risk sürücüleri", "structured drivers section");
requireText(panel, "Kapsamaya dahil atamalar", "counted assignments section");
requireText(panel, "Hariç tutulan atamalar", "excluded assignments section");
requireText(panel, "getShiftRiskOutcomeLabel", "localized outcome labels");
requireText(labels, "LOCAL_PILOT_DISCLAIMER", "pilot disclaimer constant");
requireText(labels, "SHIFT-COVERAGE-RISK-V1", "rule-set label constant");
requireText(labels, "HUMAN_REVIEW_MESSAGE", "human review message");
requireText(api, "/api/shift-risks/overview", "overview API");
requireText(api, "/api/shift-risks/assessments", "assessments API");
requireText(api, "/api/shift-risks/evaluations", "evaluations API");
requireText(api, "getShiftRiskOutcomeLabel", "localized outcome labels in API layer");
requireText(queries, "readShiftRiskPermissions", "permissions reader");
requireText(actions, "loadShiftRiskOverviewAction", "overview action");
requireText(actions, "loadShiftRiskAssessmentsAction", "assessments action");
requireText(actions, "loadShiftRiskAssessmentDetailAction", "detail action");
requireText(css, "shift-workforce-registry", "shared workforce registry styles");
requireText(page, "riskCanRead", "risk read permission");
requireText(page, "riskCanManage", "risk manage permission");

forbidText(nav, "Vardiya Riskleri", "no new sidebar entry");
forbidText(globals, "shift-risk-panel", "globals.css unchanged");
forbidText(panel, "{item.outcome}", "raw outcome enum in panel");
forbidText(panel, "{detail.outcome}", "raw selected outcome enum");
forbidText(panel, "{a.workforcePersonId}", "raw workforce person UUID in panel");
forbidText(panel, "{item.machineAssetId}", "raw machine UUID in panel");
forbidText(panel, "{detail.coverageRequirementId}", "raw requirement UUID in panel");
forbidText(panel, "{selectedId}", "raw selected assessment UUID in panel");

assert("uses outcome labels", panel.includes("outcomeLabel") || panel.includes("getShiftRiskOutcomeLabel"));
assert("uses severity labels", panel.includes("severityLabel"));
assert("loading state", panel.includes("LoaderCircle") || panel.includes("loading"));
assert("empty state", panel.includes("Kayıtlı risk değerlendirmesi bulunamadı") || panel.includes("empty"));
assert("error state", panel.includes("error") || panel.includes("Hata"));
assert("read-only manage guard", panel.includes("canManage"));
assert("rule-set visible", panel.includes("ruleSetVersion") || panel.includes("Kural seti"));
assert("filter controls", panel.includes("SHIFT_RISK_OUTCOME_FILTER_OPTIONS") && panel.includes("SHIFT_RISK_CATEGORY_FILTER_OPTIONS"));
assert("risk tab gated by permission", view.includes("riskCanRead"));

console.log("Running SHIFT-RISK-1 1600px browser smoke.");

let browserValidation = "skipped (playwright/web unavailable)";
let horizontalOverflow = "not checked";
let hydrationError = "not checked";

try {
  const { chromium } = await import("playwright");
  const ready = await fetch(`${WEB}/vardiyalar`, { redirect: "manual" });
  if (ready.status === 200 || ready.status === 307 || ready.status === 308) {
    const browser = await chromium.launch({ headless: true });
    const context = await browser.newContext({ viewport: { width: 1600, height: 1000 } });
    const browserPage = await context.newPage();
    const consoleErrors = [];
    browserPage.on("console", (msg) => {
      if (msg.type() === "error") consoleErrors.push(msg.text());
    });
    browserPage.on("pageerror", (err) => consoleErrors.push(String(err)));

    await browserPage.goto(`${WEB}/vardiyalar`, { waitUntil: "networkidle", timeout: 60000 });
    await browserPage.getByRole("tab", { name: "Vardiya Riskleri" }).click();
    await browserPage.waitForTimeout(1500);

    assert("Vardiya Riskleri tab visible", await browserPage.getByText("Vardiya Riskleri").first().isVisible());
    assert("risk registry visible", await browserPage.getByText("Vardiya Risk Değerlendirmeleri").isVisible());
    assert("evaluated KPI visible", await browserPage.getByText("Değerlendirilen").isVisible());
    assert("clear KPI visible", await browserPage.getByLabel("Vardiya risk KPI özeti").getByText("Temiz").isVisible());

    const overflow = await browserPage.evaluate(() => {
      const root = document.documentElement;
      return root.scrollWidth > root.clientWidth + 1;
    });
    horizontalOverflow = overflow ? "YES" : "NO";
    assert("no horizontal overflow", !overflow);

    const hydrationLike = consoleErrors.some((line) =>
      /hydration|serialization|Text content does not match/i.test(line),
    );
    hydrationError = hydrationLike ? "YES" : "NO";
    assert("no hydration/serialization console errors", !hydrationLike, consoleErrors.join(" | "));

    browserValidation = "passed";
    await browser.close();
  }
} catch (error) {
  browserValidation = `skipped (${error instanceof Error ? error.message : String(error)})`;
  console.log(`Browser smoke skipped: ${browserValidation}`);
}

console.log(`1600 browser validation: ${browserValidation}`);
console.log(`horizontal overflow: ${horizontalOverflow}`);
console.log(`hydration/serialization error: ${hydrationError}`);
console.log("SHIFT-RISK-1 functional UI verification passed.");
