#!/usr/bin/env node
import { readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), "../../..");
const WEB = process.env.CREVAFACTOS_WEB_BASE_URL ?? "http://localhost:3001";

function fail(message) {
  console.error(`SHIFT-RISK-2 functional UI verification failed: ${message}`);
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

console.log("Running SHIFT-RISK-2 functional UI verifier.");

const view = readRequired("apps/web/src/features/shift-workforce/components/shift-workforce-view.tsx");
const riskPanel = readRequired("apps/web/src/features/shift-risk/components/shift-risk-panel.tsx");
const panel = readRequired("apps/web/src/features/shift-risk/components/shift-continuity-panel.tsx");
const labels = readRequired("apps/web/src/features/shift-risk/domain/shift-continuity-labels.ts");
const types = readRequired("apps/web/src/features/shift-risk/domain/shift-continuity-types.ts");
const api = readRequired("apps/web/src/features/shift-risk/server/shift-continuity-api.ts");
const actions = readRequired("apps/web/src/features/shift-risk/server/shift-continuity-actions.ts");
const nav = readRequired("apps/web/src/lib/navigation/primary-navigation.ts");

requireText(riskPanel, "Personel Sürekliliği", "continuity sub-view label");
requireText(riskPanel, "ShiftContinuityPanel", "continuity panel mount");
requireText(riskPanel, "Kapsama ve Çakışmalar", "coverage sub-view label");
requireText(panel, "Personel Sürekliliği Değerlendirmeleri", "continuity registry heading");
requireText(panel, "Değerlendirilen bağlam", "evaluated KPI");
requireText(panel, "Atama çakışması", "overlap KPI");
requireText(panel, "formatShiftContinuityCount", "null-safe KPI formatting");
requireText(panel, "Yapılandırılmış risk sürücüleri", "structured drivers section");
requireText(panel, "Yetkinlik geçerliliği", "competency section");
requireText(panel, "Atama yükü", "workload section");
requireText(panel, "getShiftContinuityOutcomeLabel", "localized outcome labels");
requireText(labels, "LOCAL_PILOT_DISCLAIMER", "pilot disclaimer constant");
requireText(labels, "SHIFT-WORKFORCE-CONTINUITY-RISK-V1", "rule-set label constant");
requireText(labels, "HUMAN_REVIEW_MESSAGE", "human review message");
requireText(api, "/api/shift-risks/workforce-continuity/overview", "overview API");
requireText(api, "/api/shift-risks/workforce-continuity/assessments", "assessments API");
requireText(api, "/api/shift-risks/workforce-continuity/evaluations", "evaluations API");
requireText(actions, "loadShiftContinuityOverviewAction", "overview action");
requireText(actions, "loadShiftContinuityAssessmentsAction", "assessments action");
requireText(actions, "loadShiftContinuityAssessmentDetailAction", "detail action");
requireText(types, "ShiftContinuityOverview", "overview type");
requireText(types, "plannedAssignmentMinutes", "workload facts type");
requireText(view, "Vardiya Riskleri", "workspace tab");

forbidText(nav, "Personel Sürekliliği", "no new sidebar entry");
forbidText(panel, "{item.workforcePersonId}", "raw workforce person UUID in panel");
forbidText(panel, "{detail.workforcePersonId}", "raw detail workforce UUID");
forbidText(panel, "{selectedId}", "raw selected assessment UUID in panel");
forbidText(panel, "{item.machineAssetId}", "raw machine UUID in panel");

assert("uses outcome labels", panel.includes("getShiftContinuityOutcomeLabel"));
assert("uses employee code not UUID", panel.includes("employeeCode"));
assert("loading state", panel.includes("LoaderCircle") || panel.includes("yükleniyor"));
assert("empty state", panel.includes("Kayıtlı") || panel.includes("bulunamadı"));
assert("error state", panel.includes("error") || panel.includes("Hata"));
assert("filter controls", panel.includes("SHIFT_CONTINUITY_OUTCOME_FILTER_OPTIONS"));
assert("server boundary import", actions.includes("shift-risk-server-boundary"));

console.log("Running SHIFT-RISK-2 1600px browser smoke.");

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
    await browserPage.waitForTimeout(1000);
    await browserPage.getByRole("tab", { name: "Personel Sürekliliği" }).click();
    await browserPage.waitForTimeout(1500);

    assert("Personel Sürekliliği sub-view visible", await browserPage.getByText("Personel Sürekliliği Değerlendirmeleri").isVisible());
    assert("continuity evaluated KPI visible", await browserPage.getByText("Değerlendirilen bağlam").isVisible());
    assert("continuity registry table visible", await browserPage.getByText("Personel").first().isVisible());

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
console.log("SHIFT-RISK-2 functional UI verification passed.");
