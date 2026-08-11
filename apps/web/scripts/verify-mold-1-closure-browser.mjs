#!/usr/bin/env node
/**
 * MOLD-1 closure browser validation.
 * Usage:
 *   node verify-mold-1-closure-browser.mjs
 * Env:
 *   MOLD_BROWSER_BASE_URL (default http://localhost:3001)
 *   MOLD_BROWSER_ROLE: manager | readonly | unauthorized
 */
import { chromium } from "playwright";

const baseUrl = process.env.MOLD_BROWSER_BASE_URL ?? "http://localhost:3001";
const role = process.env.MOLD_BROWSER_ROLE ?? "manager";

const scenarios = [
  { name: "1600-expanded", viewport: { width: 1600, height: 900 }, collapsed: false },
  { name: "1600-collapsed", viewport: { width: 1600, height: 900 }, collapsed: true },
  { name: "1024-expanded", viewport: { width: 1024, height: 768 }, collapsed: false },
  { name: "1024-collapsed", viewport: { width: 1024, height: 768 }, collapsed: true },
];

function fail(message) {
  console.error(`MOLD-1 closure browser validation failed (${role}): ${message}`);
  process.exit(1);
}

async function collectFactoryLabels(nav) {
  const factoryLabels = [];
  let inFactory = false;
  for (const group of await nav.locator("section.navigation-group").all()) {
    const groupLabel = await group.locator(".navigation-group-label").textContent();
    if (groupLabel?.includes("Fabrika operasyonları")) inFactory = true;
    else if (inFactory && groupLabel && !groupLabel.includes("Fabrika")) break;
    if (inFactory) factoryLabels.push(...await group.locator(".navigation-label").allTextContents());
  }
  return factoryLabels;
}

async function runViewportScenario(browser, scenario) {
  const context = await browser.newContext({ viewport: scenario.viewport });
  const page = await context.newPage();
  const consoleErrors = [];
  page.on("console", (msg) => {
    if (msg.type() === "error") consoleErrors.push(msg.text());
  });

  await page.goto(`${baseUrl}/dashboard`, { waitUntil: "networkidle" });
  const nav = page.locator("nav[aria-label='Birincil navigasyon']");

  if (scenario.collapsed) {
    const toggle = page.getByRole("button", { name: /Kenar çubuğunu daralt|Kenar çubuğunu genişlet/ });
    const label = await toggle.getAttribute("aria-label");
    if (label?.includes("daralt")) await toggle.click();
  }

  const kaliplarLink = nav.getByRole("link", { name: "Kalıplar" });
  const kaliplarVisible = await kaliplarLink.isVisible().catch(() => false);
  const factoryLabels = await collectFactoryLabels(nav);

  let activeOnKaliplar = false;
  let iconVisible = false;
  let collapsedTitle = null;
  if (kaliplarVisible) {
    iconVisible = await kaliplarLink.locator("svg").first().isVisible();
    collapsedTitle = scenario.collapsed ? await kaliplarLink.getAttribute("title") : null;
    await kaliplarLink.click();
    await page.waitForURL("**/kaliplar");
    activeOnKaliplar = (await nav.getByRole("link", { name: "Kalıplar" }).getAttribute("aria-current")) === "page";
  }

  const overflow = await page.evaluate(() => ({
    doc: document.documentElement.scrollWidth > window.innerWidth,
    body: document.body.scrollWidth > window.innerWidth,
  }));

  await context.close();

  return {
    kaliplarVisible,
    iconVisible,
    factoryLabels,
    placementCorrect:
      factoryLabels.indexOf("Kalıplar") === factoryLabels.indexOf("Makineler") + 1 &&
      factoryLabels.indexOf("Bakım") === factoryLabels.indexOf("Kalıplar") + 1,
    activeOnKaliplar,
    collapsedTitle,
    serializationErrors: consoleErrors.filter((t) => t.includes("Only plain objects") || t.includes("Client Components")),
    hydrationWarnings: consoleErrors.filter((t) => /hydration/i.test(t)),
    horizontalOverflow: overflow.doc || overflow.body,
  };
}

async function validateManager(browser) {
  const results = {};
  for (const scenario of scenarios) {
    results[scenario.name] = await runViewportScenario(browser, scenario);
    if (!results[scenario.name].kaliplarVisible) fail(`${scenario.name}: Kalıplar not visible`);
    if (!results[scenario.name].placementCorrect) fail(`${scenario.name}: incorrect sidebar placement`);
    if (!results[scenario.name].activeOnKaliplar) fail(`${scenario.name}: active route styling missing`);
    if (results[scenario.name].serializationErrors.length) fail(`${scenario.name}: serialization error`);
    if (results[scenario.name].hydrationWarnings.length) fail(`${scenario.name}: hydration warning`);
    if (results[scenario.name].horizontalOverflow) fail(`${scenario.name}: horizontal overflow`);
  }

  const context = await browser.newContext({ viewport: { width: 1600, height: 900 } });
  const page = await context.newPage();
  await page.goto(`${baseUrl}/kaliplar`, { waitUntil: "networkidle" });
  await page.getByRole("button", { name: "Kalıp Oluştur" }).click();
  if (!(await page.getByRole("heading", { name: "Yeni Kalıp" }).isVisible())) fail("create action not visible");

  const firstRow = page.locator("table tbody tr").first();
  if (await firstRow.count()) {
    await firstRow.click();
    await page.getByRole("button", { name: "Kayıt Güncelle" }).waitFor({ state: "visible", timeout: 15000 });
    if (!(await page.getByRole("button", { name: "Kayıt Güncelle" }).isVisible())) fail("edit action not visible");
    if (!(await page.getByRole("button", { name: "Sayaç Kaydet" }).isVisible())) fail("counter action not visible");
    const transitionVisible = await page.getByRole("button", { name: "Durum Geçişi" }).isVisible().catch(() => false);
    if (!transitionVisible) {
      // scrapped/terminal molds have no transition form — acceptable if detail loaded
      if (!(await page.getByText("Yaşam Döngüsü Geçmişi").isVisible())) fail("lifecycle section not visible");
    }
  }
  await context.close();
  return results;
}

async function validateReadonly(browser) {
  const context = await browser.newContext({ viewport: { width: 1600, height: 900 } });
  const page = await context.newPage();
  await page.goto(`${baseUrl}/dashboard`, { waitUntil: "networkidle" });
  const nav = page.locator("nav[aria-label='Birincil navigasyon']");
  if (!(await nav.getByRole("link", { name: "Kalıplar" }).isVisible())) fail("Kalıplar not visible for read-only user");
  await nav.getByRole("link", { name: "Kalıplar" }).click();
  await page.waitForURL("**/kaliplar");
  if (!(await page.getByRole("heading", { name: "Kalıplar" }).isVisible())) fail("/kaliplar did not open");
  if (await page.getByRole("button", { name: "Kalıp Oluştur" }).count()) fail("create action should be unavailable");
  const firstRow = page.locator("table tbody tr").first();
  if (await firstRow.count()) {
    await firstRow.click();
    if (await page.getByRole("button", { name: "Kayıt Güncelle" }).count()) fail("edit should be unavailable");
    if (await page.getByRole("button", { name: "Sayaç Kaydet" }).count()) fail("counter should be unavailable");
    if (await page.getByRole("button", { name: "Durum Geçişi" }).count()) fail("transition should be unavailable");
  }
  await context.close();
  return { pass: true };
}

async function validateUnauthorized(browser) {
  const context = await browser.newContext({ viewport: { width: 1600, height: 900 } });
  const page = await context.newPage();
  await page.goto(`${baseUrl}/dashboard`, { waitUntil: "networkidle" });
  const nav = page.locator("nav[aria-label='Birincil navigasyon']");
  if (await nav.getByRole("link", { name: "Kalıplar" }).isVisible()) fail("Kalıplar should be hidden");
  await page.goto(`${baseUrl}/kaliplar`, { waitUntil: "networkidle" });
  const unauthorized = await page.getByText("Bu modüle erişim yetkiniz yok").isVisible();
  if (!unauthorized) fail("unauthorized direct route behavior missing");
  await context.close();
  return { pass: true };
}

async function validateLoadingState(browser) {
  const context = await browser.newContext({ viewport: { width: 1600, height: 900 } });
  const page = await context.newPage();
  await page.goto(`${baseUrl}/kaliplar`, { waitUntil: "networkidle" });

  const createButton = page.getByRole("button", { name: "Kalıp Oluştur" });
  if (!(await createButton.isVisible())) {
    await context.close();
    fail("loading-state test requires manager session with create action");
  }

  await createButton.click();
  await page.locator('input[name="code"]').fill(`LOAD-${Date.now()}`);
  await page.locator('input[name="name"]').fill("Loading State Test");

  await page.route("**/*", async (route) => {
    if (route.request().url().includes("/kaliplar") && route.request().method() === "POST") {
      await new Promise((resolve) => setTimeout(resolve, 1500));
    }
    await route.continue();
  });

  const submit = page.getByRole("button", { name: "Kaydet" });
  await submit.click();
  const loadingVisible = await page.getByText("İşleniyor…").isVisible({ timeout: 3000 }).catch(() => false);
  if (!loadingVisible) {
    await context.close();
    fail("loading state not visible during mutation");
  }

  await page.waitForFunction(() => !document.body.textContent?.includes("İşleniyor…"), null, { timeout: 15000 }).catch(() => {});
  const resolved = await page.getByRole("heading", { name: "Kalıplar" }).isVisible();
  const blank = (await page.locator("body").innerText()).trim().length < 20;
  const debugText = /debug|stack trace|Unhandled/i.test(await page.content());
  await context.close();
  if (blank) fail("blank page during loading");
  if (debugText) fail("raw debug text visible");
  if (!resolved) fail("loading state did not resolve to registry workspace");
  return { pass: true };
}

console.log(`Running MOLD-1 closure browser validation for role=${role} at ${baseUrl}`);
const browser = await chromium.launch({ headless: true });
try {
  if (role === "manager") {
    const results = await validateManager(browser);
    console.log(JSON.stringify({ role, pass: true, viewports: results }, null, 2));
  } else if (role === "readonly") {
    console.log(JSON.stringify({ role, ...(await validateReadonly(browser)) }, null, 2));
  } else if (role === "unauthorized") {
    console.log(JSON.stringify({ role, ...(await validateUnauthorized(browser)) }, null, 2));
  } else if (role === "loading") {
    console.log(JSON.stringify({ role, ...(await validateLoadingState(browser)) }, null, 2));
  } else {
    fail(`unknown role ${role}`);
  }
  console.log(`MOLD-1 closure browser validation passed (${role}).`);
} finally {
  await browser.close();
}
