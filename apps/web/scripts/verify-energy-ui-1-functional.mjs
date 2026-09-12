#!/usr/bin/env node
import { createHmac } from "node:crypto";
import { spawnSync } from "node:child_process";
import { existsSync, mkdirSync, readFileSync, statSync } from "node:fs";
import { tmpdir } from "node:os";
import { join } from "node:path";

const WEB = process.env.NEVAXIS_FACTOS_WEB_BASE_URL ?? "http://localhost:3001";
const ORGANIZATION = "dev-pilot-tenant";
const SCREENSHOT_DIR = join(tmpdir(), "NevaxisFactOS-energy-ui-1-screenshots");
const OWNER_SCREENSHOTS = [
  { width: 1600, height: 1000, filename: "energy-ui-1-donut-final-owner-1600x1000.png" },
  { width: 1600, height: 1000, filename: "energy-ui-1-lower-final-owner-1600x1000.png", scrollToLower: true },
  { width: 1024, height: 900, filename: "energy-ui-1-donut-final-owner-1024x900.png" },
];
const VIEWPORTS = [
  { width: 1600, height: 1000, label: "1600x1000" },
  { width: 1024, height: 900, label: "1024x900" },
];
const TOTP_ALPHABET = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";

function fail(message) {
  console.error(`ENERGY-UI-1 functional UI verification failed: ${message}`);
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

function generateTotp(secret) {
  const counter = Math.floor(Date.now() / 1000 / 30);
  const normalized = secret.trim().replace(/=+$/, "").toUpperCase();
  let buffer = 0;
  let bitsLeft = 0;
  const output = [];
  for (const character of normalized) {
    const value = TOTP_ALPHABET.indexOf(character);
    if (value < 0) fail("Invalid MFA secret");
    buffer = (buffer << 5) | value;
    bitsLeft += 5;
    if (bitsLeft >= 8) {
      output.push((buffer >> (bitsLeft - 8)) & 0xff);
      bitsLeft -= 8;
    }
  }
  const secretBytes = Buffer.from(output);
  const counterBytes = Buffer.alloc(8);
  counterBytes.writeBigUInt64BE(BigInt(counter));
  const hash = createHmac("sha1", secretBytes).update(counterBytes).digest();
  const offset = hash[hash.length - 1] & 0x0f;
  const binaryCode =
    ((hash[offset] & 0x7f) << 24)
    | ((hash[offset + 1] & 0xff) << 16)
    | ((hash[offset + 2] & 0xff) << 8)
    | (hash[offset + 3] & 0xff);
  return String(binaryCode % 1_000_000).padStart(6, "0");
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

async function signInThroughForm(page, { organization, email, password }) {
  await page.goto(`${WEB}/giris`, { waitUntil: "networkidle", timeout: 90000 });
  await page.locator("#sign-in-organization").fill(organization);
  await page.locator("#sign-in-email").fill(email);
  await page.locator("#sign-in-password").fill(password);
  await page.getByRole("button", { name: "Oturum aç" }).click();
}

async function assertNoHorizontalOverflow(page, label) {
  const overflow = await page.evaluate(() => ({
    doc: document.documentElement.scrollWidth > document.documentElement.clientWidth + 1,
    body: document.body.scrollWidth > document.body.clientWidth + 1,
  }));
  assert(`${label} no horizontal overflow`, !overflow.doc && !overflow.body);
}

async function completeOwnerMfa(page, mfaSecret) {
  await page.waitForURL("**/mfa-dogrula", { timeout: 30000 });
  await page.locator("#mfa-totp").fill(generateTotp(mfaSecret));
  await page.getByRole("button", { name: "Doğrula" }).click();
  await page.waitForURL("**/dashboard", { timeout: 30000 });
}

async function assertSidebarEnergyVisibility(page, label, expectedCount) {
  await page.goto(`${WEB}/dashboard`, { waitUntil: "networkidle", timeout: 90000 });
  const links = page.locator('.sidebar-navigation a[href="/enerji"]');
  assert(`${label} Enerji sidebar count`, await links.count() === expectedCount);
  if (expectedCount === 1) {
    await links.first().scrollIntoViewIfNeeded();
    assert(`${label} Enerji sidebar reachable`, await links.first().isVisible());
  }
}

async function signInPersona(page, email, password, mfaSecret = null) {
  await signInThroughForm(page, { organization: ORGANIZATION, email, password });
  if (mfaSecret) {
    await completeOwnerMfa(page, mfaSecret);
    return;
  }

  await page.waitForURL("**/dashboard", { timeout: 30000 });
}

async function clickEnergyTab(page, tabId, tabName) {
  await page.locator('[data-testid="energy-tab-rail"]').scrollIntoViewIfNeeded();
  const tab = page.locator(`#energy-tab-${tabId}`);
  await tab.waitFor({ state: "visible", timeout: 30000 });
  await tab.click();
  await page.locator(`#energy-tab-panel-${tabId}`).waitFor({ state: "visible", timeout: 30000 });
  assert(`${tabName} tab selected`, await tab.getAttribute("aria-selected") === "true");
}

function parseTurkishNumber(value) {
  return Number.parseFloat(value.replace(/\./g, "").replace(",", "."));
}

function readPngDimensions(filePath) {
  const buffer = readFileSync(filePath);
  if (buffer.length < 24 || buffer.toString("ascii", 1, 4) !== "PNG") {
    fail(`Invalid PNG file: ${filePath}`);
  }
  return {
    width: buffer.readUInt32BE(16),
    height: buffer.readUInt32BE(20),
  };
}

async function assertBrowserZoom(page, label) {
  const zoom = await page.evaluate(() => Math.round((window.outerWidth / window.innerWidth) * 100));
  assert(`${label} browser zoom 100%`, zoom === 100, String(zoom));
}

async function captureViewportScreenshot(page, spec, label) {
  await page.setViewportSize({ width: spec.width, height: spec.height });
  await assertBrowserZoom(page, label);
  await page.locator('[data-testid="energy-workspace"]').waitFor({ state: "visible", timeout: 30000 });
  await clickEnergyTab(page, "overview", `${label} overview screenshot`);

  assert(
    `${label} Genel Bakış selected for screenshot`,
    await page.locator("#energy-tab-overview").getAttribute("aria-selected") === "true",
  );
  assert(
    `${label} Değerlendirmeler not selected for screenshot`,
    await page.locator("#energy-tab-assessments").getAttribute("aria-selected") === "false",
  );
  assert(`${label} overview chart visible for screenshot`, await page.locator('[data-testid="energy-trend-chart"]').isVisible());
  assert(`${label} overview donut visible for screenshot`, await page.locator('[data-testid="energy-allocation-donut"]').isVisible());
  assert(
    `${label} overview target comparison visible for screenshot`,
    await page.locator('[data-testid="energy-target-comparison"]').isVisible(),
  );
  assert(
    `${label} overview intelligence visible for screenshot`,
    await page.locator('[data-testid="energy-overview-intelligence"]').isVisible(),
  );

  if (spec.scrollToLower) {
    await page.locator('[data-testid="energy-overview-lower-grid"]').scrollIntoViewIfNeeded();
    await page.waitForTimeout(150);
    const lowerLayout = await page.evaluate(() => {
      const overview = document.querySelector('[data-testid="energy-overview-grid"]');
      const lower = document.querySelector('[data-testid="energy-overview-lower-grid"]');
      const intelligence = document.querySelector('[data-testid="energy-overview-intelligence"]');
      const comparison = document.querySelector('[data-testid="energy-target-comparison"]');
      if (!overview || !lower || !intelligence || !comparison) return null;
      const overviewRect = overview.getBoundingClientRect();
      const lowerRect = lower.getBoundingClientRect();
      const intelligenceRect = intelligence.getBoundingClientRect();
      const comparisonRect = comparison.getBoundingClientRect();
      return {
        lowerWidth: lowerRect.width,
        overviewWidth: overviewRect.width,
        performanceWidth: intelligenceRect.width,
        targetWidth: comparisonRect.width,
        widthDelta: Math.abs(intelligenceRect.width - comparisonRect.width),
        heightDelta: Math.abs(intelligenceRect.height - comparisonRect.height),
        performanceLeft: intelligenceRect.left,
        targetLeft: comparisonRect.left,
        lowerLeft: lowerRect.left,
      };
    });
    assert(
      `${label} lower row spans overview width for screenshot`,
      Boolean(lowerLayout && Math.abs(lowerLayout.lowerWidth - lowerLayout.overviewWidth) < 24),
      JSON.stringify(lowerLayout),
    );
    assert(
      `${label} lower cards equal width for screenshot`,
      Boolean(lowerLayout && lowerLayout.widthDelta <= 24),
      JSON.stringify(lowerLayout),
    );
    assert(
      `${label} performance card left half for screenshot`,
      Boolean(lowerLayout && lowerLayout.performanceLeft <= lowerLayout.lowerLeft + lowerLayout.lowerWidth * 0.55),
      JSON.stringify(lowerLayout),
    );
    assert(
      `${label} target card right half for screenshot`,
      Boolean(lowerLayout && lowerLayout.targetLeft >= lowerLayout.lowerLeft + lowerLayout.lowerWidth * 0.45),
      JSON.stringify(lowerLayout),
    );
  }

  mkdirSync(SCREENSHOT_DIR, { recursive: true });
  const screenshotPath = join(SCREENSHOT_DIR, spec.filename);

  await page.screenshot({ path: screenshotPath, fullPage: false });

  const dimensions = readPngDimensions(screenshotPath);
  const fileSize = statSync(screenshotPath).size;
  assert(
    `${label} PNG width ${spec.width}`,
    dimensions.width === spec.width,
    `${dimensions.width}`,
  );
  assert(
    `${label} PNG height ${spec.height}`,
    dimensions.height === spec.height,
    `${dimensions.height}`,
  );
  assert(
    `${label} filename matches PNG dimensions`,
    spec.filename.includes(String(spec.width)) && spec.filename.includes(String(spec.height)),
  );

  console.log(
    `  screenshot: ${screenshotPath} (${dimensions.width}x${dimensions.height}, ${fileSize} bytes, fullPage=false, zoom=100%)`,
  );
  return { screenshotPath, dimensions, fileSize };
}

async function assertKpiSemanticAccuracy(page, label) {
  await clickEnergyTab(page, "overview", `${label} KPI semantic overview`);

  const cards = await page.locator('[data-testid="energy-kpi-card"]').evaluateAll((nodes) =>
    nodes.map((node) => ({
      id: node.getAttribute("data-kpi-id"),
      status: node.getAttribute("data-kpi-status"),
      tone: node.getAttribute("data-kpi-tone"),
      text: node.textContent ?? "",
      ariaLabel: node.getAttribute("aria-label") ?? "",
      railLabel: node.querySelector(".energy-kpi-card__evidence-rail-label")?.textContent?.trim() ?? null,
      railFill: node.querySelector(".energy-kpi-card__evidence-rail-fill")?.getAttribute("style") ?? null,
      railMarker: node.querySelector(".energy-kpi-card__evidence-rail-marker")?.getAttribute("style") ?? null,
    })),
  );

  function card(id) {
    const found = cards.find((entry) => entry.id === id);
    if (!found) fail(`${label} missing KPI card ${id}`);
    return found;
  }

  const peak = card("peak-power");
  assert(`${label} peak actual present`, peak.text.includes("22,5"));
  assert(`${label} peak target present`, peak.text.includes("15"));
  assert(`${label} peak status above target`, peak.status === "Hedefin üzerinde", peak.status ?? "");
  assert(`${label} peak tone attention`, peak.tone === "warning", peak.tone ?? "");
  assert(`${label} peak not success`, peak.tone !== "success");
  assert(`${label} peak badge rail agree`, peak.railLabel === peak.status);
  assert(`${label} peak accessible label`, /22,5.*15.*hedefin üzerinde/i.test(peak.ariaLabel));
  assert(`${label} peak rail beyond target`, Boolean(peak.railFill && peak.railMarker));

  const intensity = card("energy-intensity");
  assert(`${label} intensity actual present`, intensity.text.includes("20"));
  assert(`${label} intensity target present`, intensity.text.includes("8"));
  assert(`${label} intensity status above target`, intensity.status === "Hedefin üzerinde", intensity.status ?? "");
  assert(`${label} intensity tone attention`, intensity.tone === "warning", intensity.tone ?? "");
  assert(`${label} intensity not generic comparison`, !intensity.text.includes("Yoğunluk karşılaştırması"));
  assert(`${label} intensity badge rail agree`, intensity.railLabel === intensity.status);

  const idle = card("idle-consumption");
  assert(`${label} idle actual present`, idle.text.includes("8,2"));
  assert(`${label} idle target present`, idle.text.includes("5"));
  assert(`${label} idle status above target`, idle.status === "Hedefin üzerinde", idle.status ?? "");
  assert(`${label} idle tone attention`, idle.tone === "warning", idle.tone ?? "");
  assert(`${label} idle not generic monitoring`, idle.status !== "Boşta tüketim izleniyor");
  assert(`${label} idle badge rail agree`, idle.railLabel === idle.status);

  const variance = card("target-variance");
  assert(
    `${label} variance status below target or aligned`,
    variance.status === "Hedefin altında" || variance.status === "Hedefle uyumlu",
    variance.status ?? "",
  );
  assert(`${label} variance tone success`, variance.tone === "success", variance.tone ?? "");
  assert(`${label} variance badge rail agree`, variance.railLabel === variance.status);

  const cost = card("estimated-cost");
  assert(`${label} estimated cost neutral status`, cost.status === "Geliştirme tarifesi", cost.status ?? "");
  assert(`${label} estimated cost neutral tone`, cost.tone === "info", cost.tone ?? "");
  assert(`${label} estimated cost value present`, cost.text.includes("₺0,19"));
  assert(`${label} estimated cost no duplicated currency`, !/₺.*TRY|TRY.*₺/.test(cost.text));

  const total = card("total-consumption");
  assert(`${label} total consumption neutral`, total.status === "Raporlama dönemi toplamı", total.status ?? "");
  assert(`${label} total consumption not success`, total.tone !== "success");

  for (const entry of cards) {
    assert(
      `${label} KPI ${entry.id} no contradictory badge rail`,
      !entry.railLabel || entry.railLabel === entry.status || entry.id === "estimated-cost",
      `${entry.status} vs ${entry.railLabel}`,
    );
  }
}

async function assertCanonicalTrendIntegrity(page, label) {
  const metadata = await page.locator('[data-testid="energy-trend-metadata"]').evaluate((node) => ({
    bucketCount: Number(node.getAttribute("data-bucket-count") ?? "0"),
    bucketDurationMs: Number(node.getAttribute("data-bucket-duration-ms") ?? "0"),
    reconciliationTotal: Number(node.getAttribute("data-reconciliation-total") ?? "0"),
  }));
  assert(`${label} at least eight canonical buckets`, metadata.bucketCount >= 8, String(metadata.bucketCount));
  assert(
    `${label} identical bucket duration`,
    metadata.bucketDurationMs === 8 * 60 * 60 * 1000,
    String(metadata.bucketDurationMs),
  );
  assert(
    `${label} visible point count equals bucket count`,
    (await page.locator('[data-testid="energy-trend-marker"]').count()) === metadata.bucketCount,
  );

  const markerGeometry = await page.evaluate(() => {
    const markers = [...document.querySelectorAll('[data-testid="energy-trend-marker"]')];
    const svg = document.querySelector('[data-testid="energy-trend-svg"]');
    if (!svg || markers.length < 2) return null;
    const padLeft = 56;
    const chartWidth = 800 - 56 - 34;
    const times = markers.map((marker) => new Date(marker.getAttribute("data-window-start-utc") ?? 0).getTime());
    const cxs = markers.map((marker) => Number(marker.getAttribute("cx")));
    const minTime = Math.min(...times);
    const maxTime = Math.max(...times);
    const expected = times.map(
      (time) => padLeft + ((time - minTime) / Math.max(maxTime - minTime, 1)) * chartWidth,
    );
    const maxDelta = Math.max(...cxs.map((cx, index) => Math.abs(cx - expected[index])));
    return {
      uniqueStarts: new Set(times).size,
      spread: Math.max(...cxs) - Math.min(...cxs),
      maxDelta,
    };
  });
  assert(
    `${label} bucket timestamps are unique`,
    Boolean(markerGeometry && markerGeometry.uniqueStarts === metadata.bucketCount),
    JSON.stringify(markerGeometry),
  );
  assert(
    `${label} x coordinates derived from timestamps`,
    Boolean(markerGeometry && markerGeometry.maxDelta < 3 && markerGeometry.spread > 40),
    JSON.stringify(markerGeometry),
  );

  const trendSummary = await page.locator('[data-testid="energy-trend-screen-reader"]').innerText();
  assert(`${label} assessment peak spike absent`, !/48,5/.test(trendSummary), trendSummary);
  assert(`${label} assessment idle spike absent`, !/8,2 kWh/.test(trendSummary), trendSummary);

  const scopeCaption = await page.locator(".energy-trend-chart .energy-panel__meta").innerText();
  assert(`${label} canonical scope caption`, /8 saatlik karşılaştırılabilir tüketim pencereleri/i.test(scopeCaption), scopeCaption);
  assert(`${label} incompatible target note shown`, await page.locator('[data-testid="energy-trend-target-note"]').isVisible());
  assert(`${label} incompatible baseline note shown`, await page.locator('[data-testid="energy-trend-baseline-note"]').isVisible());
  assert(`${label} target line absent when incompatible`, await page.locator('[data-testid="energy-trend-target"]').count() === 0);
  assert(`${label} baseline line absent when incompatible`, await page.locator('[data-testid="energy-trend-baseline"]').count() === 0);

  const kpiTotalText = await page.locator('[data-kpi-id="total-consumption"] .energy-kpi-card__value').innerText();
  const kpiTotal = parseTurkishNumber(kpiTotalText.trim());
  assert(
    `${label} KPI total matches canonical trend sum`,
    Math.abs(kpiTotal - metadata.reconciliationTotal) < 0.2,
    `${kpiTotal} vs ${metadata.reconciliationTotal}`,
  );

  assert(`${label} KPI sparkline uses canonical buckets`, await page.locator('[data-testid="energy-kpi-sparkline"] path').count() === 1);
  assert(`${label} chart actual series present`, await page.locator('[data-testid="energy-trend-actual"]').count() > 0);
  assert(`${label} canonical development total 265.5 kWh`, Math.abs(kpiTotal - 265.5) < 0.2, String(kpiTotal));
  assert(`${label} canonical bucket count exactly eight`, metadata.bucketCount === 8, String(metadata.bucketCount));
}

async function assertVisualCoherence(page, label) {
  await clickEnergyTab(page, "overview", `${label} overview coherence`);

  await assertKpiSemanticAccuracy(page, label);

  assert(`${label} chart title accurate`, await page.getByText("Tüketim eğilimi").first().isVisible());
  assert(
    `${label} old chart title absent`,
    (await page.getByText("Tüketim ve güç eğilimi").count()) === 0,
  );

  await assertCanonicalTrendIntegrity(page, label);

  const donutCenter = await page.evaluate(() => ({
    totalLabel: document.querySelector('[data-testid="energy-donut-center-total-label"]')?.textContent?.trim() ?? "",
    totalValue: document.querySelector('[data-testid="energy-donut-center-total-value"]')?.textContent?.trim() ?? "",
    coverageLabel: document.querySelector('[data-testid="energy-donut-center-coverage-label"]')?.textContent?.trim() ?? "",
    coverageValue: document.querySelector('[data-testid="energy-donut-center-coverage-value"]')?.textContent?.trim() ?? "",
    ambiguousLine: [...document.querySelectorAll(".energy-allocation-donut__center span")].some((node) =>
      /kWh\s*·\s*%/.test(node.textContent ?? ""),
    ),
  }));
  assert(`${label} donut total label`, donutCenter.totalLabel === "Toplam tüketim", donutCenter.totalLabel);
  assert(`${label} donut coverage label`, donutCenter.coverageLabel === "Dağıtım kapsamı", donutCenter.coverageLabel);
  assert(`${label} donut total value present`, donutCenter.totalValue.length > 0, donutCenter.totalValue);
  assert(`${label} donut coverage value present`, donutCenter.coverageValue.startsWith("%"), donutCenter.coverageValue);
  assert(`${label} donut no ambiguous coverage line`, !donutCenter.ambiguousLine);

  const allocationRows = await page.locator('[data-testid="energy-allocation-row"]').count();
  assert(`${label} exactly two distribution categories`, allocationRows === 2, String(allocationRows));

  const lowerRowLayout = await page.evaluate(() => {
    const overview = document.querySelector('[data-testid="energy-overview-grid"]');
    const lower = document.querySelector('[data-testid="energy-overview-lower-grid"]');
    const chart = document.querySelector(".energy-panel--chart");
    const intelligence = document.querySelector('[data-testid="energy-overview-intelligence"]');
    const comparison = document.querySelector('[data-testid="energy-target-comparison"]');
    if (!overview || !lower || !chart || !intelligence || !comparison) return null;

    const overviewRect = overview.getBoundingClientRect();
    const lowerRect = lower.getBoundingClientRect();
    const chartRect = chart.getBoundingClientRect();
    const intelligenceRect = intelligence.getBoundingClientRect();
    const comparisonRect = comparison.getBoundingClientRect();
    const stacked = window.innerWidth < 1280;

    return {
      stacked,
      lowerSpansOverview: Math.abs(lowerRect.width - overviewRect.width) < 24,
      lowerLeftAligned: Math.abs(lowerRect.left - overviewRect.left) < 8,
      notNestedInChartColumn:
        stacked
        || (
          Math.abs(lowerRect.width - overviewRect.width) < 24
          && Math.abs(lowerRect.width - chartRect.width) > 48
        ),
      noUnusedRightRegion:
        stacked
        || Math.abs((intelligenceRect.width + comparisonRect.width + (comparisonRect.left - intelligenceRect.right)) - lowerRect.width) < 36,
      widthDelta: Math.abs(intelligenceRect.width - comparisonRect.width),
      heightDelta: Math.abs(intelligenceRect.height - comparisonRect.height),
      performanceWidth: intelligenceRect.width,
      targetWidth: comparisonRect.width,
      lowerRowWidth: lowerRect.width,
      overviewWidth: overviewRect.width,
      cardsInBounds:
        intelligenceRect.left >= overviewRect.left - 1
        && comparisonRect.right <= overviewRect.right + 1,
    };
  });
  assert(
    `${label} lower intelligence row spans overview width`,
    Boolean(lowerRowLayout && (lowerRowLayout.stacked || lowerRowLayout.lowerSpansOverview)),
    JSON.stringify(lowerRowLayout),
  );
  assert(
    `${label} lower cards not nested in left chart column`,
    Boolean(lowerRowLayout?.notNestedInChartColumn),
    JSON.stringify(lowerRowLayout),
  );
  assert(
    `${label} lower row has no unused right-side region`,
    Boolean(lowerRowLayout && (lowerRowLayout.stacked || lowerRowLayout.noUnusedRightRegion)),
    JSON.stringify(lowerRowLayout),
  );
  assert(
    `${label} lower cards equal width within tolerance`,
    Boolean(lowerRowLayout && (lowerRowLayout.stacked || lowerRowLayout.widthDelta <= 24)),
    JSON.stringify(lowerRowLayout),
  );
  assert(
    `${label} lower cards remain inside overview bounds`,
    Boolean(lowerRowLayout?.cardsInBounds),
    JSON.stringify(lowerRowLayout),
  );

  const trueDonutGeometry = await page.evaluate(() => {
    const hole = document.querySelector('[data-testid="energy-allocation-donut-hole"]');
    const center = document.querySelector('[data-testid="energy-donut-center"]');
    const svg = document.querySelector('[data-testid="energy-allocation-donut"] svg');
    const segments = [...document.querySelectorAll('[data-testid="energy-allocation-donut-segment"]')];
    if (!hole || !center || !svg) return null;

    const outerRadius = 48;
    const innerRadius = Number.parseFloat(hole.getAttribute("r") ?? "0");
    const diameterRatio = innerRadius > 0 ? (innerRadius * 2) / (outerRadius * 2) : 0;
    const centerStyle = window.getComputedStyle(center);
    const centerRect = center.getBoundingClientRect();
    const holeRect = hole.getBoundingClientRect();
    const svgRect = svg.getBoundingClientRect();
    const centerBackground = centerStyle.backgroundColor;
    const centerSquare =
      Math.abs(centerRect.width - centerRect.height) <= 1
      && centerStyle.borderRadius.includes("50%");
    const centerInsideHole =
      centerRect.width <= holeRect.width + 1
      && centerRect.height <= holeRect.height + 1
      && Math.abs(centerRect.left + centerRect.width / 2 - (holeRect.left + holeRect.width / 2)) <= 2
      && Math.abs(centerRect.top + centerRect.height / 2 - (holeRect.top + holeRect.height / 2)) <= 2;
    const centerTransparent =
      centerBackground === "transparent"
      || centerBackground === "rgba(0, 0, 0, 0)"
      || centerStyle.backgroundColor === "rgba(0, 0, 0, 0)";
    const labels = [...document.querySelectorAll(".energy-allocation-donut__center span")];
    const overlappingLabels = labels.some((node, index) => {
      const rect = node.getBoundingClientRect();
      for (let otherIndex = index + 1; otherIndex < labels.length; otherIndex += 1) {
        const otherRect = labels[otherIndex].getBoundingClientRect();
        const overlaps =
          rect.left < otherRect.right
          && rect.right > otherRect.left
          && rect.top < otherRect.bottom
          && rect.bottom > otherRect.top;
        if (overlaps) return true;
      }
      return false;
    });

    const midRadius = (outerRadius + innerRadius) / 2;
    const svgPoint = svg.createSVGPoint();
    const donutCenterX = 60;
    const donutCenterY = 60;
    const cardinalAngles = [
      { label: "twelve", angle: -Math.PI / 2 },
      { label: "three", angle: 0 },
      { label: "six", angle: Math.PI / 2 },
      { label: "nine", angle: Math.PI },
    ];
    const ringHits = cardinalAngles.map(({ label, angle }) => {
      svgPoint.x = donutCenterX + midRadius * Math.cos(angle);
      svgPoint.y = donutCenterY + midRadius * Math.sin(angle);
      const hitsSegment = segments.some((segment) => {
        if (!(segment instanceof SVGGeometryElement)) return false;
        return segment.isPointInFill(svgPoint);
      });
      return { label, hitsSegment, hitsCenter: false, tag: hitsSegment ? "path" : null };
    });
    const ringVisibleAtCardinals = ringHits.every((entry) => entry.hitsSegment);
    const notParenthesisArcs = ringHits.find((entry) => entry.label === "twelve")?.hitsSegment
      && ringHits.find((entry) => entry.label === "six")?.hitsSegment;

    let percentSum = 0;
    for (const row of document.querySelectorAll('[data-testid="energy-allocation-row"]')) {
      const match = row.textContent?.match(/([\d.,]+)\s*%/);
      if (match) percentSum += Number.parseFloat(match[1].replace(/\./g, "").replace(",", "."));
    }

    return {
      innerRadius,
      outerRadius,
      diameterRatio,
      centerBackground,
      centerTransparent,
      centerSquare,
      centerInsideHole,
      centerInsideSvg:
        centerRect.left >= svgRect.left - 1
        && centerRect.right <= svgRect.right + 1
        && centerRect.top >= svgRect.top - 1
        && centerRect.bottom <= svgRect.bottom + 1,
      overlappingLabels,
      segmentCount: segments.length,
      segmentPathsPresent: segments.every((segment) => (segment.getAttribute("d") ?? "").length > 0),
      ringVisibleAtCardinals,
      notParenthesisArcs,
      ringHits,
      percentSum,
      holeDiameterPx: holeRect.width,
      centerDiameterPx: Math.max(centerRect.width, centerRect.height),
    };
  });
  assert(
    `${label} donut inner hole present`,
    Boolean(trueDonutGeometry && trueDonutGeometry.innerRadius > 0),
    JSON.stringify(trueDonutGeometry),
  );
  assert(
    `${label} donut inner-hole diameter ratio 58-62%`,
    Boolean(
      trueDonutGeometry
      && trueDonutGeometry.diameterRatio >= 0.58
      && trueDonutGeometry.diameterRatio <= 0.62,
    ),
    JSON.stringify(trueDonutGeometry),
  );
  assert(
    `${label} donut center overlay is transparent`,
    Boolean(trueDonutGeometry?.centerTransparent),
    JSON.stringify(trueDonutGeometry),
  );
  assert(
    `${label} donut center overlay is circular`,
    Boolean(trueDonutGeometry?.centerSquare),
    JSON.stringify(trueDonutGeometry),
  );
  assert(
    `${label} donut center overlay contained in inner hole`,
    Boolean(trueDonutGeometry?.centerInsideHole),
    JSON.stringify(trueDonutGeometry),
  );
  assert(`${label} donut center labels do not overlap`, Boolean(trueDonutGeometry && !trueDonutGeometry.overlappingLabels));
  assert(`${label} donut center contained in svg bounds`, Boolean(trueDonutGeometry?.centerInsideSvg));
  assert(`${label} donut ring segments present`, Boolean(trueDonutGeometry && trueDonutGeometry.segmentCount > 0));
  assert(
    `${label} donut segment paths not clipped empty`,
    Boolean(trueDonutGeometry?.segmentPathsPresent),
    JSON.stringify(trueDonutGeometry),
  );
  assert(
    `${label} donut segment percentages total 360 degrees coverage`,
    Boolean(trueDonutGeometry && Math.abs(trueDonutGeometry.percentSum - 100) < 0.2),
    JSON.stringify(trueDonutGeometry),
  );
  assert(
    `${label} donut ring visible at cardinal points`,
    Boolean(trueDonutGeometry?.ringVisibleAtCardinals),
    JSON.stringify(trueDonutGeometry?.ringHits),
  );
  assert(
    `${label} donut ring not parenthesis arcs`,
    Boolean(trueDonutGeometry?.notParenthesisArcs),
    JSON.stringify(trueDonutGeometry?.ringHits),
  );

  const panelBalance = await page.evaluate(() => {
    if (window.innerWidth < 1280) return { skipped: true };
    const chartPanel = document.querySelector(".energy-panel--chart");
    const distributionPanel = document.querySelector(".energy-panel--distribution");
    const layout = document.querySelector(".energy-allocation-layout");
    if (!chartPanel || !distributionPanel || !layout) return null;
    const chartRect = chartPanel.getBoundingClientRect();
    const distributionRect = distributionPanel.getBoundingClientRect();
    const layoutRect = layout.getBoundingClientRect();
    const donutRect = document.querySelector('[data-testid="energy-allocation-donut"] svg')?.getBoundingClientRect();
    const listRect = document.querySelector('[data-testid="energy-allocation-list"]')?.getBoundingClientRect();
    return {
      skipped: false,
      heightDelta: Math.abs(chartRect.height - distributionRect.height),
      layoutCenterDelta:
        donutRect && listRect ? Math.abs((donutRect.top + donutRect.height / 2) - (listRect.top + listRect.height / 2)) : null,
      layoutInPanel:
        layoutRect.top >= distributionRect.top
        && layoutRect.bottom <= distributionRect.bottom + 1,
    };
  });
  if (!panelBalance?.skipped) {
    assert(
      `${label} chart and distribution panel heights balanced`,
      Boolean(panelBalance && panelBalance.heightDelta < 28),
      JSON.stringify(panelBalance),
    );
    assert(
      `${label} donut and rows vertically centered`,
      Boolean(panelBalance?.layoutCenterDelta !== null && panelBalance.layoutCenterDelta < 36),
      JSON.stringify(panelBalance),
    );
    assert(
      `${label} distribution layout contained in card`,
      Boolean(panelBalance?.layoutInPanel),
      JSON.stringify(panelBalance),
    );
  }

  const workspaceText = await page.locator('[data-testid="energy-workspace"]').innerText();
  assert(
    `${label} owner read-only banner absent`,
    await page.locator('[data-testid="energy-readonly-banner"]').count() === 0,
  );
  assert(`${label} raw 1000-good-parts absent`, !workspaceText.includes("1000-good-parts"));
  assert(`${label} duplicated currency absent`, !/₺.*TRY|TRY.*₺/.test(workspaceText));

  const axisLabels = await page.locator('[data-testid="energy-trend-axis-label"]').allTextContents();
  const trimmedAxisLabels = axisLabels.map((value) => value.trim()).filter(Boolean);
  assert(
    `${label} unique timestamp labels`,
    trimmedAxisLabels.length === new Set(trimmedAxisLabels).size,
    trimmedAxisLabels.join(" | "),
  );
  for (const axisLabel of trimmedAxisLabels) {
    assert(`${label} axis not meter code`, !/^EM-/i.test(axisLabel), axisLabel);
  }
  if (trimmedAxisLabels.length > 0) {
    assert(`${label} axis uses chronological labels`, trimmedAxisLabels.length >= 1);
  }

  const scopeText = await page.locator('[data-testid="energy-trend-chart"] .energy-panel__meta').innerText();
  assert(`${label} coherent chart scope`, scopeText.length > 0, scopeText);

  const screenReader = page.locator('[data-testid="energy-trend-screen-reader"]');
  assert(`${label} screen reader summary present`, await screenReader.count() === 1);
  const screenReaderBox = await screenReader.boundingBox();
  assert(
    `${label} visible accessibility paragraph absent`,
    !screenReaderBox || (screenReaderBox.width <= 1 && screenReaderBox.height <= 1),
    screenReaderBox ? `${screenReaderBox.width}x${screenReaderBox.height}` : "missing",
  );
  assert(
    `${label} short chart caption visible`,
    await page.locator('[data-testid="energy-trend-caption"]').isVisible(),
  );

  const kpiCards = page.locator('[data-testid="energy-kpi-card"]');
  assert(`${label} exactly six KPI cards`, await kpiCards.count() === 6);
  const kpiHeights = await kpiCards.evaluateAll((nodes) =>
    nodes.map((node) => node.getBoundingClientRect().height),
  );
  if (kpiHeights.length > 1) {
    const minHeight = Math.min(...kpiHeights);
    const maxHeight = Math.max(...kpiHeights);
    assert(`${label} KPI card heights consistent`, maxHeight - minHeight < 24, `${minHeight}-${maxHeight}`);
  }
  assert(
    `${label} KPI semantic state text visible`,
    (await page.locator(".energy-kpi-card__tone").count()) >= 6,
  );
  assert(`${label} provenance strip visible`, await page.locator('[data-testid="energy-provenance-strip"]').isVisible());
  const donutSegments = await page.locator('[data-testid="energy-allocation-donut-segment"]').count();
  assert(`${label} distribution donut geometry`, donutSegments > 0, String(donutSegments));

  const chartGeometry = await page.evaluate(() => {
    const svg = document.querySelector('[data-testid="energy-trend-svg"]');
    if (!svg) return null;
    const rect = svg.getBoundingClientRect();
    return { width: rect.width, height: rect.height };
  });
  assert(`${label} main chart meaningful geometry`, Boolean(chartGeometry && chartGeometry.width > 320 && chartGeometry.height > 180), JSON.stringify(chartGeometry));

  const distributionGeometry = await page.evaluate(() => {
    const donut = document.querySelector('[data-testid="energy-allocation-donut"] svg');
    const panel = document.querySelector(".energy-panel--distribution");
    if (!donut || !panel) return null;
    const donutRect = donut.getBoundingClientRect();
    const panelRect = panel.getBoundingClientRect();
    return {
      donutHeightRatio: panelRect.height > 0 ? donutRect.height / panelRect.height : 0,
      donutInBounds: donutRect.right <= panelRect.right + 1 && donutRect.bottom <= panelRect.bottom + 1,
      rowsInBounds: [...document.querySelectorAll('[data-testid="energy-allocation-row"]')].every((node) => {
        const rect = node.getBoundingClientRect();
        return rect.right <= panelRect.right + 1;
      }),
    };
  });
  assert(
    `${label} donut uses distribution card height`,
    Boolean(distributionGeometry && distributionGeometry.donutHeightRatio >= 0.32),
    JSON.stringify(distributionGeometry),
  );
  assert(
    `${label} donut and rows inside distribution card`,
    Boolean(distributionGeometry?.donutInBounds && distributionGeometry?.rowsInBounds),
    JSON.stringify(distributionGeometry),
  );

  const lowerBalance = await page.evaluate(() => {
    const intelligence = document.querySelector('[data-testid="energy-overview-intelligence"]');
    const comparison = document.querySelector('[data-testid="energy-target-comparison"]');
    if (!intelligence || !comparison) return null;
    const intelligenceRect = intelligence.getBoundingClientRect();
    const comparisonRect = comparison.getBoundingClientRect();
    return {
      heightDelta: Math.abs(intelligenceRect.height - comparisonRect.height),
      intelligenceRows: intelligence.querySelectorAll(".energy-intelligence-row").length,
    };
  });
  assert(
    `${label} overview lower columns balanced`,
    Boolean(
      lowerRowLayout
      && (lowerRowLayout.stacked || lowerRowLayout.heightDelta <= 24)
      && (lowerBalance?.intelligenceRows ?? 0) <= 4,
    ),
    JSON.stringify({ lowerRowLayout, lowerBalance }),
  );

  const headerHeight = await page.evaluate(() => {
    const header = document.querySelector(".energy-executive");
    return header ? Math.round(header.getBoundingClientRect().height) : 0;
  });
  assert(`${label} compact header height`, headerHeight >= 110 && headerHeight <= 185, String(headerHeight));

  const lowerViewport = await page.evaluate(() => {
    const lower = document.querySelector('[data-testid="energy-overview-lower-grid"]');
    if (!lower || window.innerWidth < 1280) return { skipped: true };
    const rect = lower.getBoundingClientRect();
    return {
      skipped: false,
      top: rect.top,
      viewportHeight: window.innerHeight,
    };
  });
  if (!lowerViewport.skipped) {
    assert(
      `${label} lower intelligence section begins in viewport`,
      lowerViewport.top < lowerViewport.viewportHeight + 120,
      JSON.stringify(lowerViewport),
    );
  }

  const clippedLabels = await page.evaluate(() => {
    const svg = document.querySelector('[data-testid="energy-trend-svg"]');
    if (!svg) return true;
    const svgRect = svg.getBoundingClientRect();
    return [...document.querySelectorAll('[data-testid="energy-trend-axis-label"]')].every((node) => {
      const rect = node.getBoundingClientRect();
      return rect.left >= svgRect.left - 1 && rect.right <= svgRect.right + 1;
    });
  });
  assert(`${label} no clipped X-axis labels`, clippedLabels);

  const reconciliationText = await page.locator('[data-testid="energy-allocation-reconciliation"]').innerText();
  const totalMatch = reconciliationText.match(/Toplam:\s*([\d.,]+)/);
  const distributedMatch = reconciliationText.match(/Dağıtılan:\s*([\d.,]+)/);
  const residualMatch = reconciliationText.match(/Dağıtılmamış:\s*([\d.,]+)/);
  assert(`${label} reconciliation total present`, Boolean(totalMatch), reconciliationText);
  assert(`${label} reconciliation distributed present`, Boolean(distributedMatch), reconciliationText);
  const displayedTotal = parseTurkishNumber(totalMatch[1]);
  const summaryDistributed = parseTurkishNumber(distributedMatch[1]);
  const summaryResidual = residualMatch ? parseTurkishNumber(residualMatch[1]) : 0;

  const rowTexts = await page.locator('[data-testid="energy-allocation-row"]').allTextContents();
  let allocationSum = 0;
  let distributedRowSum = 0;
  let residualRowValue = 0;
  let percentSum = 0;
  for (const rowText of rowTexts) {
    const kwhMatch = rowText.match(/([\d.,]+)\s*kWh/i);
    const percentMatch = rowText.match(/([\d.,]+)\s*%/);
    if (kwhMatch) {
      const value = parseTurkishNumber(kwhMatch[1]);
      allocationSum += value;
      if (/Dağıtılmamış/i.test(rowText)) {
        residualRowValue = value;
      } else {
        distributedRowSum += value;
      }
    }
    if (percentMatch) percentSum += parseTurkishNumber(percentMatch[1]);
  }

  assert(
    `${label} summary distributed equals distributed rows`,
    Math.abs(summaryDistributed - distributedRowSum) < 0.2,
    `${summaryDistributed} vs ${distributedRowSum}`,
  );
  assert(
    `${label} summary residual equals residual row`,
    Math.abs(summaryResidual - residualRowValue) < 0.2,
    `${summaryResidual} vs ${residualRowValue}`,
  );
  assert(
    `${label} distributed plus residual equals total`,
    Math.abs(summaryDistributed + summaryResidual - displayedTotal) < 0.2,
    `${summaryDistributed} + ${summaryResidual} vs ${displayedTotal}`,
  );
  assert(
    `${label} distribution kWh equals total`,
    Math.abs(allocationSum - displayedTotal) < 0.2,
    `${allocationSum} vs ${displayedTotal}`,
  );
  assert(
    `${label} distribution percent equals 100`,
    Math.abs(percentSum - 100) < 0.2,
    String(percentSum),
  );

  const donutCoverageValue = await page
    .locator('[data-testid="energy-donut-center-coverage-value"]')
    .evaluate((node) => node.textContent?.trim() ?? "");
  const donutCoverage = parseTurkishNumber(donutCoverageValue.replace("%", "").trim());
  const coverageMatch = reconciliationText.match(/Kapsama:\s*([\d.,]+)/);
  const summaryCoverage = coverageMatch
    ? parseTurkishNumber(coverageMatch[1])
    : (summaryDistributed / displayedTotal) * 100;
  assert(
    `${label} donut coverage reconciles`,
    Math.abs(donutCoverage - summaryCoverage) < 0.2,
    `${donutCoverage} vs ${summaryCoverage}`,
  );
  const donutTotalValue = await page
    .locator('[data-testid="energy-donut-center-total-value"]')
    .evaluate((node) => node.textContent?.trim() ?? "");
  assert(
    `${label} donut total reconciles`,
    Math.abs(parseTurkishNumber(donutTotalValue) - displayedTotal) < 0.2,
    `${donutTotalValue} vs ${displayedTotal}`,
  );

  if (summaryResidual > 0.1) {
    assert(`${label} residual visible when incomplete`, residualRowValue > 0.1);
  }

  await clickEnergyTab(page, "assessments", `${label} assessments labels`);
  const assessmentTitles = await page.locator(".energy-assessment-title").allTextContents();
  assert(`${label} localized assessment titles present`, assessmentTitles.length > 0);
  for (const title of assessmentTitles) {
    assert(`${label} primary ASSESS codes absent`, !/^ASSESS-/i.test(title.trim()), title);
  }

  await clickEnergyTab(page, "overview", `${label} overview return`);

  const kpiOverflow = await page.evaluate(() => {
    const grid = document.querySelector('[data-testid="energy-kpi-grid"]');
    if (!grid) return true;
    const gridRect = grid.getBoundingClientRect();
    return [...grid.querySelectorAll("article")].every((card) => {
      const rect = card.getBoundingClientRect();
      return rect.left >= gridRect.left - 1 && rect.right <= gridRect.right + 1;
    });
  });
  assert(`${label} KPI cards within viewport`, kpiOverflow);

  const viewportOverflow = await page.evaluate(() => ({
    doc: document.documentElement.scrollWidth > document.documentElement.clientWidth + 1,
    body: document.body.scrollWidth > document.body.clientWidth + 1,
    rightEdge: [...document.querySelectorAll(".energy-page *")].some((node) => {
      const rect = node.getBoundingClientRect();
      return rect.width > 0 && rect.right > window.innerWidth + 1;
    }),
  }));
  assert(
    `${label} no viewport right-edge overflow`,
    !viewportOverflow.doc && !viewportOverflow.body && !viewportOverflow.rightEdge,
  );
}

async function exerciseEnergyWorkspace(page, label, options = {}) {
  const { expectManageControls = true } = options;
  if (options.checkSidebar !== false) {
    await assertSidebarEnergyVisibility(page, label, 1);
  }
  const response = await page.goto(`${WEB}/enerji`, { waitUntil: "networkidle", timeout: 90000 });
  assert(`${label} /enerji HTTP 200`, response?.status() === 200);

  await page.locator('[data-testid="energy-workspace"]').waitFor({ state: "visible", timeout: 30000 });
  assert(`${label} workspace visible`, await page.getByText("ENERJİ YÖNETİMİ").isVisible());
  assert(`${label} KPI grid visible`, await page.locator('[data-testid="energy-kpi-grid"]').isVisible());
  assert(`${label} Toplam tüketim KPI`, await page.locator('[data-testid="energy-kpi-grid"]').getByText("Toplam tüketim").isVisible());
  assert(`${label} pilot banner`, await page.getByText("Yerel geliştirme / simülasyon verisi").isVisible());

  await clickEnergyTab(page, "measurement-points", `${label} measurement`);
  await page.locator('[data-testid="energy-measurement-registry"]').waitFor({ state: "visible" });

  await clickEnergyTab(page, "targets-baselines", `${label} targets`);
  assert(`${label} targets tab`, await page.getByText("Hesaplanan").first().isVisible());
  assert(`${label} approved target badge`, await page.getByText("Onaylı").first().isVisible());
  if (expectManageControls) {
    assert(
      `${label} manage target action available`,
      await page.getByRole("button", { name: "Hedefi güncelle" }).first().isEnabled(),
    );
  } else {
    assert(`${label} read-only note`, await page.getByText("Salt okunur").first().isVisible());
  }

  await clickEnergyTab(page, "assessments", `${label} assessments`);
  assert(`${label} assessments tab`, await page.locator('[data-testid="energy-assessments-panel"]').isVisible());

  await clickEnergyTab(page, "overview", `${label} overview`);
  assert(`${label} overview trend`, await page.getByText("Tüketim eğilimi").first().isVisible());
  assert(`${label} allocation panel`, await page.getByText("Enerji dağılımı").isVisible());

  if (expectManageControls) {
    await assertVisualCoherence(page, label);
  }

  await assertNoHorizontalOverflow(page, label);
}

console.log("Running ENERGY-UI-1 functional UI verifier.");

const verifierSource = readFileSync(new URL(import.meta.url), "utf8");
const fullPageTrueScreenshotCalls =
  verifierSource.match(/page\.screenshot\(\{[\s\S]*?fullPage:\s*true[\s\S]*?\}\)/g) ?? [];
assert("verifier uses viewport screenshot only", fullPageTrueScreenshotCalls.length === 0);

const ready = await fetch(`${WEB}/giris`, { redirect: "manual" });
assert("web server reachable", ready.status === 200 || ready.status === 307 || ready.status === 308, `status=${ready.status}`);

const ownerPassword = requireEnv("DevelopmentSaasIdentity__TenantOwnerPassword");
const activePassword = requireEnv("DevelopmentSaasIdentity__ActiveUserPassword");
const mfaSecret = requireEnv("DevelopmentSaasIdentity__MfaSharedSecret");

function db(sql) {
  const result = spawnSync(
    "docker",
    ["exec", "NevaxisFactOS-local-postgres-1", "psql", "-U", "crevafactos", "-d", "crevafactos_local", "-t", "-A", "-c", sql],
    { encoding: "utf8" },
  );
  if (result.status !== 0) fail(result.stderr || result.stdout);
}

function seedEnergyReadOverrideForMaintenance() {
  db(`
    DELETE FROM tenant_membership_permission_overrides WHERE id = 'dd000003-00e4-4000-8000-0000000000e4';
    INSERT INTO tenant_membership_permission_overrides (
      id, tenant_id, tenant_membership_id, permission_definition_id, effect, record_scope, status, created_at_utc, updated_at_utc, version)
    VALUES (
      'dd000003-00e4-4000-8000-0000000000e4',
      'bb000001-0001-4000-8000-000000000001',
      'bb000003-0223-4000-8000-000000000223',
      '10000000-0000-4000-8000-000000000076',
      'allow', 'tenant', 'Active', NOW(), NOW(), 1);
  `);
}

function removeEnergyReadOverrideForMaintenance() {
  db(`DELETE FROM tenant_membership_permission_overrides WHERE id = 'dd000003-00e4-4000-8000-0000000000e4';`);
}

function cleanClosureOverrides() {
  db(`
    DELETE FROM tenant_membership_permission_overrides
    WHERE id = 'dd000003-00e4-4000-8000-0000000000e4';
  `);
}

cleanClosureOverrides();
seedEnergyReadOverrideForMaintenance();

const { chromium } = await import("playwright");
const browser = await chromium.launch({
  headless: true,
  ...(resolveChromiumExecutable() ? { executablePath: resolveChromiumExecutable() } : {}),
});

const consoleErrors = [];

try {
  for (const viewport of VIEWPORTS) {
    const ownerContext = await browser.newContext({
      viewport: { width: viewport.width, height: viewport.height },
      deviceScaleFactor: 1,
    });
    const ownerPage = await ownerContext.newPage();
    ownerPage.on("console", (msg) => {
      if (msg.type() === "error") consoleErrors.push(msg.text());
    });
    ownerPage.on("pageerror", (err) => consoleErrors.push(String(err)));

    await signInPersona(ownerPage, "tenant-owner@dev.local", ownerPassword, mfaSecret);
    await exerciseEnergyWorkspace(ownerPage, `owner-${viewport.label}`);

    if (viewport.label === "1600x1000") {
      await captureViewportScreenshot(ownerPage, OWNER_SCREENSHOTS[0], "owner-donut-final-1600x1000");
      await captureViewportScreenshot(ownerPage, OWNER_SCREENSHOTS[1], "owner-lower-final-1600x1000");
    } else if (viewport.label === "1024x900") {
      await captureViewportScreenshot(ownerPage, OWNER_SCREENSHOTS[2], "owner-donut-final-1024x900");
    }

    const materialsContext = await browser.newContext({
      viewport: { width: viewport.width, height: viewport.height },
      deviceScaleFactor: 1,
    });
    const materialsPage = await materialsContext.newPage();
    await signInPersona(materialsPage, "materials-sales@dev.local", activePassword);
    await materialsPage.goto(`${WEB}/dashboard`, { waitUntil: "networkidle", timeout: 90000 });
    await assertSidebarEnergyVisibility(materialsPage, `unauthorized-${viewport.label}`, 0);
    const deniedResponse = await materialsPage.goto(`${WEB}/enerji`, { waitUntil: "domcontentloaded", timeout: 90000 });
    const deniedText = await materialsPage.locator("body").innerText();
    assert(
      `unauthorized-${viewport.label} /enerji denied`,
      deniedResponse?.status() === 403
        || deniedText.includes("Bu modüle erişim yetkiniz yok")
        || deniedText.includes("Erişim reddedildi"),
      `status=${deniedResponse?.status()} text=${deniedText.slice(0, 200)}`,
    );

    const readContext = await browser.newContext({
      viewport: { width: viewport.width, height: viewport.height },
      deviceScaleFactor: 1,
    });
    const readPage = await readContext.newPage();
    await signInPersona(readPage, "maintenance-team@dev.local", activePassword);
    await exerciseEnergyWorkspace(readPage, `energy-read-${viewport.label}`, { expectManageControls: false });

    await ownerContext.close();
    await materialsContext.close();
    await readContext.close();
  }
} finally {
  await browser.close();
  removeEnergyReadOverrideForMaintenance();
}

assert("no hydration errors", !consoleErrors.some((entry) => /hydration/i.test(entry)), consoleErrors.join("; "));
assert("no console errors", consoleErrors.length === 0, consoleErrors.join("; "));

console.log("ENERGY-UI-1 functional UI verification passed.");
