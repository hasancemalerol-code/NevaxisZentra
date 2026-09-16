#!/usr/bin/env node
/**
 * Industrial Enterprise UI Hardening — Visual QA capture
 * Requires: web dev server on NEVAXIS_FACTOS_WEB_BASE_URL (default http://localhost:3001)
 */
import { existsSync, mkdirSync, writeFileSync } from "node:fs";
import { dirname, join, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), "../../..");
const WEB = process.env.NEVAXIS_FACTOS_WEB_BASE_URL ?? "http://localhost:3001";
const OUT_DIR = join(ROOT, "artifacts", "industrial-ui-hardening");
const VIEWPORTS = [
  { width: 1920, height: 1080, label: "1920x1080" },
  { width: 1440, height: 900, label: "1440x900" },
  { width: 1366, height: 768, label: "1366x768" },
];

const ROUTES = [
  { path: "/dashboard", name: "dashboard" },
  { path: "/uretim", name: "production" },
  { path: "/uretim/hatlar", name: "mixed-production-lines" },
  { path: "/dijital-is-gucu", name: "digital-workforce" },
  { path: "/fabrika-karar-merkezi", name: "factory-decision-center" },
  { path: "/fabrika-zekasi", name: "factory-intelligence" },
  { path: "/vardiyalar", name: "shifts" },
  { path: "/bakim", name: "maintenance" },
  { path: "/kalite", name: "quality" },
  { path: "/malzemeler", name: "materials" },
  { path: "/kaliplar", name: "molds" },
];

function fail(message) {
  console.error(`Industrial UI visual QA failed: ${message}`);
  process.exit(1);
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
    if (existsSync(candidate)) return candidate;
  }
  return undefined;
}

async function main() {
  const { chromium } = await import("playwright");
  const executablePath = resolveChromiumExecutable();
  if (!executablePath) {
    fail("Chromium/Chrome executable not found for screenshot capture");
  }

  mkdirSync(OUT_DIR, { recursive: true });
  const browser = await chromium.launch({ executablePath, headless: true });
  const report = [];

  for (const viewport of VIEWPORTS) {
    const context = await browser.newContext({ viewport: { width: viewport.width, height: viewport.height } });
    const page = await context.newPage();

    for (const route of ROUTES) {
      const url = `${WEB}${route.path}`;
      try {
        const response = await page.goto(url, { waitUntil: "networkidle", timeout: 60000 });
        const status = response?.status() ?? 0;
        const fileName = `${route.name}-${viewport.label}.png`;
        const filePath = join(OUT_DIR, fileName);
        await page.screenshot({ path: filePath, fullPage: true });
        const overflowX = await page.evaluate(() => document.documentElement.scrollWidth > window.innerWidth);
        report.push({
          route: route.path,
          viewport: viewport.label,
          httpStatus: status,
          screenshot: filePath,
          horizontalOverflow: overflowX,
          pass: status < 500,
        });
        console.log(`✓ ${route.path} @ ${viewport.label}`);
      } catch (error) {
        report.push({
          route: route.path,
          viewport: viewport.label,
          pass: false,
          error: String(error),
        });
        console.log(`✗ ${route.path} @ ${viewport.label}: ${error}`);
      }
    }

    await context.close();
  }

  await browser.close();
  writeFileSync(join(OUT_DIR, "visual-qa-report.json"), JSON.stringify(report, null, 2));
  const failures = report.filter((entry) => !entry.pass);
  if (failures.length > 0) {
    fail(`${failures.length} capture(s) failed — see ${join(OUT_DIR, "visual-qa-report.json")}`);
  }
  console.log(`Visual QA complete: ${report.length} captures → ${OUT_DIR}`);
}

main().catch((error) => fail(error.message));
