#!/usr/bin/env node
import { createHmac } from "node:crypto";
import { existsSync, mkdirSync } from "node:fs";
import { dirname, join, resolve } from "node:path";
import { tmpdir } from "node:os";
import { fileURLToPath } from "node:url";

const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), "../../..");
const WEB = process.env.NEVAXIS_FACTOS_WEB_BASE_URL ?? "http://localhost:3001";
const ORGANIZATION = "dev-pilot-tenant";
const VIEWPORTS = [
  { width: 1920, height: 1080, label: "1920x1080" },
  { width: 3840, height: 2160, label: "3840x2160" },
];
const SCREENSHOT_DIR = join(tmpdir(), "NevaxisZentra-dashboard-showcase-1");

const UUID_PATTERN = /\b[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}\b/i;
const KPI_LABELS = [
  "Gerçekleşen Üretim",
  "OEE",
  "Fire Oranı",
  "Çevrim Süresi",
  "Kalite Uygunluğu",
  "Hedef Gerçekleşmesi",
];

function fail(message) {
  console.error(`DASHBOARD-SHOWCASE-1 functional-ui verification failed: ${message}`);
  process.exit(1);
}

function assert(label, ok, detail = "") {
  if (!ok) fail(`${label}${detail ? `: ${detail}` : ""}`);
  console.log(`✓ ${label}`);
}

function requireEnv(name) {
  const value = process.env[name]?.trim();
  if (!value) fail(`Missing environment variable ${name}`);
  return value;
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

function base32Decode(encoded) {
  const alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
  const normalized = encoded.trim().replace(/=+$/, "").toUpperCase();
  const output = [];
  let buffer = 0;
  let bitsLeft = 0;
  for (const character of normalized) {
    const value = alphabet.indexOf(character);
    if (value < 0) fail("Invalid Base32 character in MFA secret");
    buffer = (buffer << 5) | value;
    bitsLeft += 5;
    if (bitsLeft >= 8) {
      output.push((buffer >> (bitsLeft - 8)) & 0xff);
      bitsLeft -= 8;
    }
  }
  return Buffer.from(output);
}

function generateTotp(secret) {
  const counter = Math.floor(Date.now() / 1000 / 30);
  const counterBytes = Buffer.alloc(8);
  counterBytes.writeBigUInt64BE(BigInt(counter));
  const hash = createHmac("sha1", base32Decode(secret)).update(counterBytes).digest();
  const offset = hash[hash.length - 1] & 0x0f;
  const binaryCode =
    ((hash[offset] & 0x7f) << 24)
    | ((hash[offset + 1] & 0xff) << 16)
    | ((hash[offset + 2] & 0xff) << 8)
    | (hash[offset + 3] & 0xff);
  return String(binaryCode % 1_000_000).padStart(6, "0");
}

async function signInThroughForm(page, { organization, email, password, mfaSecret }) {
  await page.goto(`${WEB}/giris`, { waitUntil: "networkidle", timeout: 90000 });
  await page.locator("#sign-in-organization").fill(organization);
  await page.locator("#sign-in-email").fill(email);
  await page.locator("#sign-in-password").fill(password);
  await page.getByRole("button", { name: "Oturum aç" }).click();

  if (mfaSecret) {
    await page.waitForURL("**/mfa-dogrula", { timeout: 30000 });
    await page.locator("#mfa-totp").fill(generateTotp(mfaSecret));
    await page.getByRole("button", { name: "Doğrula" }).click();
  }
}

async function assertNoOverflow(page, label) {
  const overflow = await page.evaluate(() => ({
    horizontal:
      document.documentElement.scrollWidth > document.documentElement.clientWidth + 1
      || document.body.scrollWidth > document.body.clientWidth + 1,
    vertical:
      document.documentElement.scrollHeight > document.documentElement.clientHeight + 1
      || document.body.scrollHeight > document.body.clientHeight + 1,
  }));
  assert(`${label} no horizontal overflow`, !overflow.horizontal);
  assert(`${label} no vertical overflow`, !overflow.vertical);
}

async function assertChartGeometry(page, label) {
  const geometry = await page.evaluate(() => {
    const viewportWidth = document.documentElement.clientWidth;
    const viewportHeight = document.documentElement.clientHeight;
    const bodyText = document.body.innerText ?? "";
    const presentationMode = document.querySelector(".factory-showcase")?.getAttribute("data-presentation-mode") ?? "";
    const isDevelopmentShowcase = presentationMode === "development-showcase";

    function hasVisibleBox(selector) {
      return [...document.querySelectorAll(selector)].some((element) => {
        const box = element.getBoundingClientRect();
        return box.width > 8 && box.height > 2;
      });
    }

    function countVisible(selector, minWidth = 8, minHeight = 2) {
      return [...document.querySelectorAll(selector)].filter((element) => {
        const box = element.getBoundingClientRect();
        return box.width > minWidth && box.height > minHeight;
      }).length;
    }

    const kpiSparklines = countVisible(".factory-showcase-kpi__sparkline-line");
    const hourlyActual = countVisible('[data-chart-kind="hourly-production"] [data-series="actual"]', 40, 2);
    const hourlyTarget = countVisible('[data-chart-kind="hourly-production"] [data-series="target"]', 40, 2);
    const hourlyPoints = countVisible('[data-chart-kind="hourly-production"] [data-series="actual-point"]', 4, 4);
    const oeeBars = countVisible('[data-chart-kind="oee-components"] [data-series="oee-bar"]', 20, 8);
    const donutSegments = countVisible('[data-chart-kind="distribution"] [data-series="distribution-segment"]', 20, 20);
    const distributionRows = document.querySelectorAll(".factory-showcase-distribution__row").length;
    const pendingVisible = bodyText.includes("Veri bekleniyor");
    const bottomPanels = [...document.querySelectorAll(".factory-showcase__bottom-grid .factory-showcase__panel")];
    const bottomInsideViewport = bottomPanels.every((panel) => {
      const box = panel.getBoundingClientRect();
      return box.bottom <= viewportHeight + 1 && box.right <= viewportWidth + 1;
    });

    const oeeHeadline = bodyText.match(/OEE[\s\S]*?(%\d+[.,]\d+)/);
    const oeeBarsText = [...document.querySelectorAll('[data-chart-kind="oee-components"] .factory-showcase-bar__value')]
      .map((element) => element.textContent?.trim() ?? "");

    return {
      scrollWidth: document.documentElement.scrollWidth,
      clientWidth: document.documentElement.clientWidth,
      scrollHeight: document.documentElement.scrollHeight,
      clientHeight: document.documentElement.clientHeight,
      kpiSparklines,
      hourlyActual,
      hourlyTarget,
      hourlyPoints,
      oeeBars,
      donutSegments,
      distributionRows,
      pendingVisible,
      bottomInsideViewport,
      isDevelopmentShowcase,
      oeeBarsText,
      oeeHeadline,
    };
  });

  assert(`${label} scrollWidth equals clientWidth`, geometry.scrollWidth <= geometry.clientWidth + 1, `${geometry.scrollWidth}/${geometry.clientWidth}`);
  assert(`${label} scrollHeight within clientHeight`, geometry.scrollHeight <= geometry.clientHeight + 1, `${geometry.scrollHeight}/${geometry.clientHeight}`);
  assert(`${label} six KPI sparkline geometries`, geometry.kpiSparklines >= 6, `count=${geometry.kpiSparklines}`);
  assert(`${label} hourly actual series geometry`, geometry.hourlyActual >= 1, `count=${geometry.hourlyActual}`);
  assert(`${label} hourly target series geometry`, geometry.hourlyTarget >= 1, `count=${geometry.hourlyTarget}`);
  assert(`${label} hourly point markers`, geometry.hourlyPoints >= 12, `count=${geometry.hourlyPoints}`);
  assert(`${label} three OEE bars`, geometry.oeeBars === 3, `count=${geometry.oeeBars}`);
  assert(`${label} donut segments visible`, geometry.donutSegments >= 4, `count=${geometry.donutSegments}`);
  assert(`${label} four distribution groups`, geometry.distributionRows === 4, `count=${geometry.distributionRows}`);
  assert(`${label} bottom panels inside viewport`, geometry.bottomInsideViewport);
  if (geometry.isDevelopmentShowcase) {
    assert(`${label} no pending placeholder in development showcase`, !geometry.pendingVisible);
  }
}

async function exerciseShowcase(page, label) {
  await page.goto(`${WEB}/fabrika-vitrini`, { waitUntil: "networkidle", timeout: 90000 });
  assert(`${label} route loaded`, page.url().includes("/fabrika-vitrini"));

  const bodyText = await page.locator("body").innerText();
  assert(`${label} header title`, bodyText.includes("Fabrika Operasyon Görünümü"));
  assert(`${label} brand text`, bodyText.includes("NevaxisZentra"));
  assert(`${label} live status`, bodyText.includes("Operasyon akışı izleniyor"));
  assert(`${label} refresh label`, bodyText.includes("Son güncelleme:"));
  assert(`${label} hourly chart`, bodyText.includes("Saatlik Üretim Performansı"));
  assert(`${label} oee components`, bodyText.includes("OEE Bileşenleri"));
  assert(`${label} distribution`, bodyText.includes("Üretim Dağılımı"));
  assert(`${label} no raw UUID`, !UUID_PATTERN.test(bodyText));
  assert(`${label} no sidebar`, (await page.locator(".app-sidebar").count()) === 0);
  assert(`${label} no top bar`, (await page.locator(".top-bar").count()) === 0);

  for (const kpiLabel of KPI_LABELS) {
    assert(`${label} KPI ${kpiLabel}`, bodyText.includes(kpiLabel));
  }

  const kpiCount = await page.locator(".factory-showcase-kpi").count();
  assert(`${label} exactly six KPI cards`, kpiCount === 6);

  await assertChartGeometry(page, label);
  await assertNoOverflow(page, label);

  const screenshotPath = join(SCREENSHOT_DIR, `dashboard-showcase-${label}.png`);
  await page.screenshot({ path: screenshotPath, fullPage: false });
  console.log(`Screenshot saved: ${screenshotPath}`);
}

console.log("Running DASHBOARD-SHOWCASE-1 functional-ui verifier.");
mkdirSync(SCREENSHOT_DIR, { recursive: true });

const ownerPassword = requireEnv("DevelopmentSaasIdentity__TenantOwnerPassword");
const mfaSharedSecret = requireEnv("DevelopmentSaasIdentity__MfaSharedSecret");

const ready = await fetch(`${WEB}/giris`, { redirect: "manual" });
assert("web server reachable", ready.status === 200 || ready.status === 307 || ready.status === 308, `status=${ready.status}`);

const { chromium } = await import("playwright");
const executablePath = resolveChromiumExecutable();
const browser = await chromium.launch({
  headless: true,
  ...(executablePath ? { executablePath } : {}),
});

const consoleErrors = [];

try {
  for (const viewport of VIEWPORTS) {
    const context = await browser.newContext({ viewport });
    const page = await context.newPage();
    page.on("console", (msg) => {
      if (msg.type() === "error") consoleErrors.push(msg.text());
    });
    page.on("pageerror", (err) => consoleErrors.push(String(err)));

    await signInThroughForm(page, {
      organization: ORGANIZATION,
      email: "admin@demo.nevaxiszentra.local",
      password: ownerPassword,
      mfaSecret: mfaSharedSecret,
    });
    await page.waitForURL("**/dashboard", { timeout: 30000 });

    await exerciseShowcase(page, viewport.label);
    await context.close();
  }

  const hydrationLike = consoleErrors.some((line) =>
    /hydration|serialization|Text content does not match|Only plain objects/i.test(line),
  );
  assert("no hydration/serialization/console errors", !hydrationLike, consoleErrors.join(" | "));
} finally {
  await browser.close();
}

console.log("DASHBOARD-SHOWCASE-1 functional-ui verification passed.");
