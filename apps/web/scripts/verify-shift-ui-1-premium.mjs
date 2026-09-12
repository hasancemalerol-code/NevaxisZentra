#!/usr/bin/env node
import { existsSync } from "node:fs";
import { mkdirSync } from "node:fs";
import { readFileSync } from "node:fs";
import { dirname, join, resolve } from "node:path";
import { fileURLToPath } from "node:url";
import { tmpdir } from "node:os";

const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), "../../..");
const WEB = process.env.NEVAXIS_FACTOS_WEB_BASE_URL ?? "http://localhost:3001";
const SCREENSHOT_DIR = join(tmpdir(), "NevaxisFactOS-shift-ui-1-screenshots");

function fail(message) {
  console.error(`SHIFT-UI-1 premium UI verification failed: ${message}`);
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

console.log("Running SHIFT-UI-1 premium UI verifier.");

const page = readRequired("apps/web/src/app/(app)/vardiyalar/page.tsx");
const view = readRequired("apps/web/src/features/shift-workforce/components/shift-workforce-view.tsx");
const plan = readRequired("apps/web/src/features/shift-workforce/components/shift-plan-panel.tsx");
const css = readRequired("apps/web/src/features/shift-workforce/components/shift-workforce.css");
const assignment = readRequired("apps/web/src/features/shift-assignment/components/shift-assignment-panel.tsx");
const riskPanel = readRequired("apps/web/src/features/shift-risk/components/shift-risk-panel.tsx");
const continuity = readRequired("apps/web/src/features/shift-risk/components/shift-continuity-panel.tsx");
const globals = readRequired("apps/web/src/app/globals.css");
const nav = readRequired("apps/web/src/lib/navigation/primary-navigation.ts");
const ui = `${page}\n${view}\n${plan}\n${css}\n${assignment}\n${riskPanel}\n${continuity}`;

requireText(view, "VARDİYA OPERASYONLARI", "page eyebrow");
requireText(view, "Vardiyalar", "page title");
requireText(view, "Vardiya Planı", "plan tab");
requireText(view, "Personel ve Yetkinlikler", "personnel tab");
requireText(view, "Operatör Atama", "assignment tab");
requireText(view, "Vardiya Riskleri", "risk tab");
requireText(riskPanel, "Kapsama ve Çakışmalar", "coverage sub-view");
requireText(riskPanel, "Personel Sürekliliği", "continuity sub-view");
requireText(view, "shift-workforce-workspace-tabs--primary", "controlled tab rail");
requireText(view, "Aktif vardiya", "executive KPI");
requireText(view, "Kullanılabilir personel", "executive KPI");
requireText(view, "Onaylı atama", "executive KPI");
requireText(view, "Kapsama açığı", "executive KPI");
requireText(view, "Kritik rol riski", "executive KPI");
requireText(view, "Veri/yetkinlik uyarısı", "executive KPI");
requireText(view, "formatShiftWorkforceCount", "null-safe KPI formatting");
requireText(view, "Personel detayını kapat", "person close accessible name");
requireText(view, "data-focus-restore", "focus restoration hook");
requireText(view, "shift-workforce-sticky-context", "sticky selected context");
requireText(css, "--swf-sticky-top: var(--top-bar-height", "topbar offset token");
requireText(css, "--swf-registry-width: 30rem", "registry width");
requireText(css, "scrollbar-width: none", "hidden tab scrollbar");
requireText(plan, "Vardiya Planı", "plan panel heading");
requireText(assignment, "Aday detayını kapat", "assignment close accessible name");
requireText(assignment, "Onayla", "explicit human approval");
requireText(riskPanel, "Kapsama risk detayını kapat", "coverage close accessible name");
requireText(continuity, "Süreklilik risk detayını kapat", "continuity close accessible name");
requireText(continuity, "HUMAN_REVIEW_MESSAGE", "human review message");
requireText(continuity, "LOCAL_PILOT_DISCLAIMER", "local pilot disclaimer");

forbidText(nav, "Vardiya Planı", "no new sidebar entry");
forbidText(globals, "shift-workforce", "globals.css unchanged");
forbidText(globals, "shift-plan-panel", "globals.css unchanged");
forbidText(ui, "automatic assignment", "automatic assignment language");
forbidText(ui, "fatigue", "fatigue inference");
forbidText(ui, "overtime violation", "overtime claim");
forbidText(ui, "financial-loss", "financial-loss claim");
forbidText(view, "{person.id}", "raw person UUID");
forbidText(continuity, "{item.workforcePersonId}", "raw workforce UUID");

assert("module-local CSS only", css.includes(".shift-workforce-page"));
assert("detail close controls present", view.includes("shift-workforce-detail-close"));
assert("registry restoration class", view.includes("shift-workforce-workspace--detail-open"));

mkdirSync(SCREENSHOT_DIR, { recursive: true });

const screenshotPaths = {};
const browserResults = {
  main1600: "NOT_RUN",
  person1600: "NOT_RUN",
  selectedPerson1600: "NOT_RUN",
  assignment1600: "NOT_RUN",
  approved1600: "NOT_RUN",
  coverage1600: "NOT_RUN",
  coverageDetail1600: "NOT_RUN",
  continuity1600: "NOT_RUN",
  continuityDetail1600: "NOT_RUN",
  close1600: "NOT_RUN",
  main1024: "NOT_RUN",
  detail1024: "NOT_RUN",
  risk1024: "NOT_RUN",
  close1024: "NOT_RUN",
  horizontalOverflow1600: "NOT_RUN",
  horizontalOverflow1024: "NOT_RUN",
  hydrationError: "NOT_RUN",
};

async function capture(pageRef, name) {
  const path = join(SCREENSHOT_DIR, `${name}.png`);
  await pageRef.screenshot({ path, fullPage: true });
  screenshotPaths[name] = path;
}

async function assertNoHorizontalOverflow(pageRef, label) {
  const overflow = await pageRef.evaluate(() => document.documentElement.scrollWidth > document.documentElement.clientWidth + 1);
  assert(`${label} no horizontal overflow`, !overflow);
  return overflow ? "YES" : "NO";
}

function resolveChromiumExecutable() {
  const candidates = [
    process.env.PLAYWRIGHT_CHROMIUM_EXECUTABLE_PATH,
    process.env.CHROME_PATH,
    "C:\\Program Files\\Google\\Chrome\\Application\\chrome.exe",
    "C:\\Program Files (x86)\\Microsoft\\Edge\\Application\\msedge.exe",
    "C:\\Program Files\\Microsoft\\Edge\\Application\\msedge.exe",
  ].filter(Boolean);

  for (const candidate of candidates) {
    if (existsSync(candidate)) {
      return candidate;
    }
  }

  return undefined;
}

try {
  const { chromium } = await import("playwright");
  const ready = await fetch(`${WEB}/vardiyalar`, { redirect: "manual" });
  if (ready.status !== 200 && ready.status !== 307 && ready.status !== 308) {
    fail(`web server not reachable at ${WEB}/vardiyalar (status=${ready.status})`);
  }

  const executablePath = resolveChromiumExecutable();
  const browser = await chromium.launch({
    headless: true,
    ...(executablePath ? { executablePath } : {}),
  });
  const consoleErrors = [];

  async function openPage(width, height) {
    const context = await browser.newContext({ viewport: { width, height } });
    const browserPage = await context.newPage();
    browserPage.on("console", (msg) => {
      if (msg.type() === "error") consoleErrors.push(msg.text());
    });
    browserPage.on("pageerror", (err) => consoleErrors.push(String(err)));
    await browserPage.goto(`${WEB}/vardiyalar`, { waitUntil: "networkidle", timeout: 90000 });
    return { context, browserPage };
  }

  const { context: ctx1600, browserPage: page1600 } = await openPage(1600, 1000);
  await capture(page1600, "1600-main-workspace");
  browserResults.main1600 = "PASS";
  assert("1600 main workspace visible", await page1600.getByRole("heading", { name: "Vardiyalar" }).isVisible());

  await page1600.getByRole("tab", { name: "Personel ve Yetkinlikler" }).click();
  await page1600.waitForTimeout(800);
  await capture(page1600, "1600-person-registry");
  browserResults.person1600 = "PASS";
  assert("1600 person registry visible", await page1600.getByText("Personel Kaydı").isVisible());

  const detailButton = page1600.getByRole("button", { name: "Detay" }).first();
  if (await detailButton.isVisible()) {
    await detailButton.click();
    await page1600.waitForTimeout(1000);
    await capture(page1600, "1600-selected-person");
    browserResults.selectedPerson1600 = "PASS";
    assert("1600 selected person detail", await page1600.getByLabel("Personel detayını kapat").isVisible());
  }

  await page1600.getByRole("tab", { name: "Operatör Atama" }).click();
  await page1600.waitForTimeout(1000);
  await capture(page1600, "1600-operator-assignment");
  browserResults.assignment1600 = "PASS";
  assert("1600 operator assignment visible", await page1600.getByRole("heading", { name: "Seçim Bağlamı" }).isVisible());

  if (await page1600.getByText("Atama Kaydı").isVisible()) {
    await capture(page1600, "1600-approved-assignment");
    browserResults.approved1600 = "PASS";
  }

  await page1600.getByRole("tab", { name: "Vardiya Riskleri" }).click();
  await page1600.waitForTimeout(1000);
  await capture(page1600, "1600-coverage-risk");
  browserResults.coverage1600 = "PASS";
  assert("1600 coverage sub-view", await page1600.getByRole("tab", { name: "Kapsama ve Çakışmalar" }).isVisible());

  const riskDetail = page1600.getByRole("button", { name: "Detay" }).first();
  if (await riskDetail.isVisible()) {
    await riskDetail.click();
    await page1600.waitForTimeout(800);
    await capture(page1600, "1600-selected-coverage-detail");
    browserResults.coverageDetail1600 = "PASS";
  }

  await page1600.getByRole("tab", { name: "Personel Sürekliliği" }).click();
  await page1600.waitForTimeout(1000);
  await capture(page1600, "1600-continuity");
  browserResults.continuity1600 = "PASS";
  assert("1600 continuity visible", await page1600.getByText("Personel Sürekliliği Değerlendirmeleri").isVisible());

  const continuityDetail = page1600.getByRole("button", { name: "Detay" }).first();
  if (await continuityDetail.isVisible()) {
    await continuityDetail.click();
    await page1600.waitForTimeout(800);
    await capture(page1600, "1600-selected-continuity-detail");
    browserResults.continuityDetail1600 = "PASS";
    const close = page1600.getByLabel("Süreklilik risk detayını kapat");
    if (await close.isVisible()) {
      await close.click();
      await page1600.waitForTimeout(500);
      await capture(page1600, "1600-close-result");
      browserResults.close1600 = "PASS";
    }
  }

  browserResults.horizontalOverflow1600 = await assertNoHorizontalOverflow(page1600, "1600");
  await ctx1600.close();

  const { context: ctx1024, browserPage: page1024 } = await openPage(1024, 900);
  await capture(page1024, "1024-main-workspace");
  browserResults.main1024 = "PASS";

  await page1024.getByRole("tab", { name: "Personel ve Yetkinlikler" }).click();
  await page1024.waitForTimeout(800);
  const detail1024 = page1024.getByRole("button", { name: "Detay" }).first();
  if (await detail1024.isVisible()) {
    await detail1024.click();
    await page1024.waitForTimeout(800);
    await capture(page1024, "1024-selected-detail");
    browserResults.detail1024 = "PASS";
    const closePerson = page1024.getByLabel("Personel detayını kapat");
    if (await closePerson.isVisible()) {
      await closePerson.click();
      await page1024.waitForTimeout(500);
      await capture(page1024, "1024-close-result");
      browserResults.close1024 = "PASS";
    }
  }

  await page1024.getByRole("tab", { name: "Vardiya Riskleri" }).click();
  await page1024.waitForTimeout(800);
  await page1024.getByRole("tab", { name: "Personel Sürekliliği" }).click();
  await page1024.waitForTimeout(800);
  const riskDetail1024 = page1024.getByRole("button", { name: "Detay" }).first();
  if (await riskDetail1024.isVisible()) {
    await riskDetail1024.click();
    await page1024.waitForTimeout(800);
    await capture(page1024, "1024-selected-risk-detail");
    browserResults.risk1024 = "PASS";
  }

  browserResults.horizontalOverflow1024 = await assertNoHorizontalOverflow(page1024, "1024");
  await ctx1024.close();

  const hydrationLike = consoleErrors.some((line) =>
    /hydration|serialization|Text content does not match/i.test(line),
  );
  browserResults.hydrationError = hydrationLike ? "YES" : "NO";
  assert("no hydration/serialization console errors", !hydrationLike, consoleErrors.join(" | "));

  await browser.close();
} catch (error) {
  fail(error instanceof Error ? error.message : String(error));
}

console.log("Screenshot directory:", SCREENSHOT_DIR);
for (const [key, path] of Object.entries(screenshotPaths)) {
  console.log(`${key}: ${path}`);
}
console.log("Browser results:", JSON.stringify(browserResults));
console.log("SHIFT-UI-1 premium UI verification passed.");
