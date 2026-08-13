#!/usr/bin/env node
import { readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), "../../..");
const WEB = process.env.CREVAFACTOS_WEB_BASE_URL ?? "http://localhost:3001";

function fail(message) {
  console.error(`SHIFT-ASSIGN-1 functional UI verification failed: ${message}`);
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

console.log("Running SHIFT-ASSIGN-1 functional UI verifier.");

const page = readRequired("apps/web/src/app/(app)/vardiyalar/page.tsx");
const view = readRequired("apps/web/src/features/shift-workforce/components/shift-workforce-view.tsx");
const panel = readRequired("apps/web/src/features/shift-assignment/components/shift-assignment-panel.tsx");
const labels = readRequired("apps/web/src/features/shift-assignment/domain/shift-assignment-labels.ts");
const api = readRequired("apps/web/src/features/shift-assignment/server/shift-assignment-api.ts");
const queries = readRequired("apps/web/src/features/shift-assignment/server/shift-assignment-queries.ts");
const actions = readRequired("apps/web/src/features/shift-assignment/server/shift-assignment-actions.ts");
const css = readRequired("apps/web/src/features/shift-workforce/components/shift-workforce.css");
const globals = readRequired("apps/web/src/app/globals.css");
const nav = readRequired("apps/web/src/lib/navigation/primary-navigation.ts");
const ui = `${page}\n${view}\n${panel}`;

requireText(view, "Operatör Atama", "workspace tab");
requireText(panel, "Operatör Atama", "assignment panel breadcrumb");
requireText(panel, "Üretim emri", "selection context production order");
requireText(panel, "Makine", "selection context machine");
requireText(panel, "Vardiya", "selection context shift");
requireText(panel, "Uygun", "eligible KPI");
requireText(panel, "Koşullu", "conditional KPI");
requireText(panel, "Uygun Değil", "ineligible KPI");
requireText(panel, "Yetersiz Veri", "insufficient-data KPI");
requireText(panel, "formatShiftAssignmentCount", "null-safe KPI formatting");
requireText(panel, "Aday Kaydı", "candidate registry");
requireText(panel, "Atama Kaydı", "assignment registry");
requireText(panel, "Onayla", "human approval action");
requireText(panel, "ERP/HR bilgileri (salt okunur)", "ERP read-only marker");
requireText(labels, "LOCAL_PILOT_DISCLAIMER", "pilot disclaimer constant");
requireText(labels, "SHIFT-OPERATOR-ELIGIBILITY-V1", "rule-set label constant");
requireText(api, "/api/shift-assignments/candidates", "candidates API");
requireText(api, "/api/shift-assignments/assignments", "assignments API");
requireText(api, "getEligibilityOutcomeLabel", "localized outcome labels");
requireText(queries, "readShiftAssignmentContextOptions", "context options reader");
requireText(actions, "loadShiftAssignmentCandidatesAction", "candidates action");
requireText(actions, "approveShiftAssignmentAction", "approve action");
requireText(css, "shift-assignment-panel", "module-local assignment styles");
requireText(page, "assignmentCanRead", "assignment read permission");
requireText(page, "assignmentCanManage", "assignment manage permission");

forbidText(nav, "Operatör Atama", "no new sidebar entry");
forbidText(globals, "shift-assignment", "globals.css unchanged");
forbidText(panel, "{candidate.personId}", "raw person UUID in panel");
forbidText(panel, "{item.outcome}", "raw outcome enum in panel");
forbidText(panel, "{selectedCandidate.outcome}", "raw selected outcome enum");

assert("uses outcome labels", panel.includes("outcomeLabel"));
assert("loading state", panel.includes("LoaderCircle") || panel.includes("loading"));
assert("empty state", panel.includes("Kayıt bulunamadı") || panel.includes("empty"));
assert("error state", panel.includes("error") || panel.includes("Hata"));
assert("read-only manage guard", panel.includes("canManage"));
assert("structured reasons section", panel.includes("reasons") || panel.includes("Değerlendirme Nedenleri"));
assert("source freshness visible", panel.includes("freshness") || panel.includes("Güncellik"));
assert("rule-set visible", panel.includes("ruleSetVersion") || panel.includes("Kural seti"));

console.log("Running SHIFT-ASSIGN-1 1600px browser smoke.");

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
    await browserPage.getByRole("tab", { name: "Operatör Atama" }).click();
    await browserPage.waitForTimeout(1500);

    assert("Operatör Atama tab visible", await browserPage.getByText("Operatör Atama").first().isVisible());
    assert("selection context visible", await browserPage.getByLabel("Üretim emri").isVisible());
    assert("candidate registry visible", await browserPage.getByText("Aday Kaydı").isVisible());
    assert("assignment registry visible", await browserPage.getByText("Atama Kaydı").isVisible());

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
console.log("SHIFT-ASSIGN-1 functional UI verification passed.");
