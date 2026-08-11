#!/usr/bin/env node
/** Quick sidebar proof for one role. Env: MOLD_BROWSER_ROLE, MOLD_BROWSER_BASE_URL */
import { chromium } from "playwright";

const baseUrl = process.env.MOLD_BROWSER_BASE_URL ?? "http://localhost:3001";
const role = process.env.MOLD_BROWSER_ROLE ?? "manager";

async function factoryLabels(page) {
  const nav = page.locator("nav[aria-label='Birincil navigasyon']");
  const labels = [];
  let inFactory = false;
  for (const group of await nav.locator("section.navigation-group").all()) {
    const gl = await group.locator(".navigation-group-label").textContent();
    if (gl?.includes("Fabrika operasyonları")) inFactory = true;
    else if (inFactory && gl && !gl.includes("Fabrika")) break;
    if (inFactory) labels.push(...await group.locator(".navigation-label").allTextContents());
  }
  return labels;
}

const browser = await chromium.launch({ headless: true });
const page = await (await browser.newContext({ viewport: { width: 1600, height: 900 } })).newPage();
await page.goto(`${baseUrl}/dashboard`, { waitUntil: "networkidle" });
const nav = page.locator("nav[aria-label='Birincil navigasyon']");
const labels = await factoryLabels(page);
const kaliplarVisible = await nav.getByRole("link", { name: "Kalıplar" }).isVisible().catch(() => false);
const placementCorrect =
  labels.indexOf("Kalıplar") === labels.indexOf("Makineler") + 1 &&
  labels.indexOf("Bakım") === labels.indexOf("Kalıplar") + 1;

let unauthorizedRoute = null;
if (role === "unauthorized") {
  await page.goto(`${baseUrl}/kaliplar`, { waitUntil: "networkidle" });
  unauthorizedRoute = await page.getByText("Bu modüle erişim yetkiniz yok").isVisible();
}

const result = { role, kaliplarVisible, placementCorrect, labels, unauthorizedRoute };
console.log(JSON.stringify(result));
await browser.close();

if (role === "unauthorized") {
  if (kaliplarVisible || !unauthorizedRoute) process.exit(1);
} else {
  if (!kaliplarVisible || !placementCorrect) process.exit(1);
}
