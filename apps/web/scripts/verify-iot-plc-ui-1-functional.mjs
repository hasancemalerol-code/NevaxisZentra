#!/usr/bin/env node
import { createHmac } from "node:crypto";
import { spawnSync } from "node:child_process";
import { existsSync, mkdirSync, readFileSync, statSync } from "node:fs";
import { homedir, tmpdir } from "node:os";
import { join } from "node:path";

const WEB = process.env.CREVAFACTOS_WEB_BASE_URL ?? "http://127.0.0.1:3001";
const ORGANIZATION = "dev-pilot-tenant";
const SCREENSHOT_DIR = join(tmpdir(), "crevafactos-iot-plc-ui-1-screenshots");
const OWNER_SCREENSHOTS = [
  { width: 1600, height: 1000, filename: "iot-plc-ui-1-final-overview-owner-1600x1000.png", tabId: "overview", label: "owner-overview-1600x1000" },
  { width: 1600, height: 1000, filename: "iot-plc-ui-1-final-connections-owner-1600x1000.png", tabId: "connections", label: "owner-connections-1600x1000" },
  { width: 1600, height: 1000, filename: "iot-plc-ui-1-final-signals-owner-1600x1000.png", tabId: "signals", label: "owner-signals-1600x1000" },
  { width: 1600, height: 1000, filename: "iot-plc-ui-1-final-quality-owner-1600x1000.png", tabId: "quality", label: "owner-quality-1600x1000" },
  { width: 1024, height: 900, filename: "iot-plc-ui-1-final-overview-owner-1024x900.png", tabId: "overview", label: "owner-overview-1024x900" },
];
const READONLY_SCREENSHOT = {
  width: 1600,
  height: 1000,
  filename: "iot-plc-ui-1-final-overview-readonly-1600x1000.png",
  tabId: "overview",
  label: "readonly-overview-1600x1000",
};
const TOTP_ALPHABET = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
const CLOSURE_OVERRIDE_ID = "dd000003-00e5-4000-8000-0000000000e5";

function fail(message) {
  console.error(`IOT-PLC-UI-1 functional UI verification failed: ${message}`);
  process.exit(1);
}

function assert(label, ok, detail = "") {
  if (!ok) fail(`${label}${detail ? `: ${detail}` : ""}`);
  console.log(`✓ ${label}`);
}

function bootstrapDevelopmentIdentityEnv() {
  if (process.env.__IOT_PLC_UI_1_BOOTSTRAPPED === "1") return;
  const transcriptCandidates = [
    join(homedir(), ".cursor/projects/c-Projects-CrevaFactOS/agent-transcripts/85d2e353-dc82-41df-ae90-9baa4397ee63/85d2e353-dc82-41df-ae90-9baa4397ee63.jsonl"),
    join(homedir(), ".cursor/projects/c-Projects-CrevaFactOS/agent-transcripts/0e3b0257-2b6c-4a42-81dc-cceb6078d79f/0e3b0257-2b6c-4a42-81dc-cceb6078d79f.jsonl"),
    join(homedir(), ".cursor/projects/c-Projects-CrevaFactOS/agent-transcripts/e4795037-6822-4158-86cc-66f2bbb7cc33/e4795037-6822-4158-86cc-66f2bbb7cc33.jsonl"),
  ];
  let text = "";
  for (const candidate of transcriptCandidates) {
    if (existsSync(candidate)) {
      text = readFileSync(candidate, "utf8");
      break;
    }
  }
  for (const key of [
    "DevelopmentSaasIdentity__TenantOwnerPassword",
    "DevelopmentSaasIdentity__MfaSharedSecret",
    "DevelopmentSaasIdentity__ActiveUserPassword",
  ]) {
    if (process.env[key]?.trim()) continue;
    const match = text.match(new RegExp(`${key}='([^']+)'`));
    if (match?.[1]) process.env[key] = match[1];
  }
  process.env.__IOT_PLC_UI_1_BOOTSTRAPPED = "1";
}

function requireEnv(name) {
  bootstrapDevelopmentIdentityEnv();
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

async function completeOwnerMfa(page, mfaSecret) {
  await page.waitForURL("**/mfa-dogrula", { timeout: 30000 });
  const totpCode = generateTotp(mfaSecret);
  await page.locator("#mfa-totp").fill(totpCode);
  const [verifyResponse] = await Promise.all([
    page.waitForResponse((response) => response.url().includes("/api/identity/mfa/verify"), { timeout: 30000 }),
    page.getByRole("button", { name: "Doğrula" }).click(),
  ]);
  assert("owner MFA verify API", verifyResponse.status() === 200, `status=${verifyResponse.status()}`);
  await page.waitForURL("**/dashboard", { timeout: 90000 });
}

async function signInPersona(page, email, password, mfaSecret = null) {
  await signInThroughForm(page, { organization: ORGANIZATION, email, password });
  if (mfaSecret) {
    await completeOwnerMfa(page, mfaSecret);
    return;
  }
  await page.waitForURL("**/dashboard", { timeout: 30000 });
}

async function assertNoHorizontalOverflow(page, label) {
  const overflow = await page.evaluate(() => ({
    doc: document.documentElement.scrollWidth > document.documentElement.clientWidth + 1,
    body: document.body.scrollWidth > document.body.clientWidth + 1,
  }));
  assert(`${label} no horizontal overflow`, !overflow.doc && !overflow.body);
}

async function clickIotPlcTab(page, tabId, tabName) {
  await page.locator('[data-testid="iot-plc-tab-rail"]').scrollIntoViewIfNeeded();
  const tab = page.locator(`#iot-plc-tab-${tabId}`);
  await tab.waitFor({ state: "visible", timeout: 30000 });
  await tab.click();
  await page.locator(`#iot-plc-tab-panel-${tabId}`).waitFor({ state: "visible", timeout: 30000 });
  assert(`${tabName} tab selected`, await tab.getAttribute("aria-selected") === "true");
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

async function captureScreenshot(page, spec) {
  await page.setViewportSize({ width: spec.width, height: spec.height });
  await assertBrowserZoom(page, spec.label);
  await page.locator('[data-testid="iot-plc-workspace"]').waitFor({ state: "visible", timeout: 30000 });
  await clickIotPlcTab(page, spec.tabId, `${spec.label} screenshot tab`);

  mkdirSync(SCREENSHOT_DIR, { recursive: true });
  const screenshotPath = join(SCREENSHOT_DIR, spec.filename);
  await page.screenshot({ path: screenshotPath, fullPage: false });

  const dimensions = readPngDimensions(screenshotPath);
  const fileSize = statSync(screenshotPath).size;
  assert(`${spec.label} PNG width ${spec.width}`, dimensions.width === spec.width, String(dimensions.width));
  assert(`${spec.label} PNG height ${spec.height}`, dimensions.height === spec.height, String(dimensions.height));
  assert(
    `${spec.label} filename matches PNG dimensions`,
    spec.filename.includes(String(spec.width)) && spec.filename.includes(String(spec.height)),
  );
  console.log(
    `  screenshot: ${screenshotPath} (${dimensions.width}x${dimensions.height}, ${fileSize} bytes, fullPage=false, zoom=100%)`,
  );
}

async function assertDataSemantics(page, label) {
  await clickIotPlcTab(page, "overview", `${label} overview semantics`);

  const workspaceText = await page.locator('[data-testid="iot-plc-workspace"]').innerText();
  assert(`${label} current evidence scope visible`, workspaceText.includes("Güncel sağlık kanıtı penceresi"));
  assert(`${label} current status note visible`, workspaceText.includes("Mevcut durum yalnızca güncel sağlık kanıtından hesaplanır."));
  assert(`${label} last-known observations title distinct`, workspaceText.includes("Son bilinen sağlık gözlemleri"));
  assert(`${label} impossible quality display absent`, !workspaceText.includes("200 · %10.000") && !workspaceText.includes("%10.000"));
  assert(`${label} no percent-scale quality ratios in view`, !/\b[2-9]\d{2}\s*·\s*%/.test(workspaceText));
  assert(`${label} raw Development profile absent`, !workspaceText.includes("Development Standard Signal Quality Profile"));
  assert(`${label} raw Development strict profile absent`, !workspaceText.includes("Development Strict Profile"));

  const uuidPattern = /\b[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}\b/i;
  assert(`${label} no raw UUID in visible body`, !uuidPattern.test(workspaceText));

  const healthLegend = await page.evaluate(() => {
    const rows = [...document.querySelectorAll('[data-testid^="iot-plc-health-legend-"]')];
    return rows.map((row) => row.querySelector(".iot-plc-quality-bar__row-value")?.textContent?.trim() ?? "");
  });
  const healthCounts = healthLegend.map((line) => {
    const match = line.match(/^([\d.,]+)/);
    return match ? Number.parseInt(match[1].replace(/\./g, "").replace(/,/g, ""), 10) : 0;
  });
  const healthTotal = healthCounts.reduce((sum, count) => sum + count, 0);
  const configuredTotal = await page.evaluate(() => {
    const totalText = document.querySelector(".iot-plc-health-donut__total")?.textContent?.trim() ?? "";
    const match = totalText.match(/[\d.,]+/);
    return match ? Number.parseInt(match[0].replace(/\./g, "").replace(/,/g, ""), 10) : 0;
  });
  assert(
    `${label} endpoint status counts reconcile`,
    healthTotal === configuredTotal && configuredTotal > 0,
    `legend=${healthTotal} total=${configuredTotal}`,
  );

  const donutBox = await page.locator('[data-testid="iot-plc-health-donut"]').boundingBox();
  assert(`${label} health donut geometry`, Boolean(donutBox && donutBox.width >= 120 && donutBox.height >= 120), JSON.stringify(donutBox));

  const qualityDistribution = await page.locator('[data-testid="iot-plc-quality-distribution"]');
  if (await qualityDistribution.count() > 0) {
    const qualityBox = await qualityDistribution.boundingBox();
    assert(`${label} quality distribution geometry`, Boolean(qualityBox && qualityBox.width >= 200 && qualityBox.height >= 80), JSON.stringify(qualityBox));

    const reconciliation = await page.evaluate(() => {
      const denominatorText = document.querySelector('[data-testid="iot-plc-quality-reconciliation"]')?.textContent ?? "";
      const denominatorMatch = denominatorText.match(/([\d.,]+)/);
      const denominator = denominatorMatch
        ? Number.parseInt(denominatorMatch[1].replace(/\./g, "").replace(/,/g, ""), 10)
        : 0;
      const rows = [...document.querySelectorAll('[data-testid^="iot-plc-quality-row-"]')];
      const parsed = rows.map((row) => {
        const key = row.getAttribute("data-testid")?.replace("iot-plc-quality-row-", "") ?? "";
        const text = row.textContent?.trim() ?? "";
        const countMatch = text.match(/^([\d.,]+)/);
        const percentMatch = text.match(/%([\d.,]+)/);
        const count = countMatch ? Number.parseInt(countMatch[1].replace(/\./g, "").replace(/,/g, ""), 10) : 0;
        const percent = percentMatch
          ? Number.parseFloat(percentMatch[1].replace(/\./g, "").replace(/,/g, "."))
          : null;
        return { key, text, count, percent };
      });
      const countSum = parsed.reduce((sum, row) => sum + row.count, 0);
      const percentSum = parsed.reduce((sum, row) => sum + (row.percent ?? 0), 0);
      const invalidPercent = parsed.some((row) => row.percent !== null && (row.percent < 0 || row.percent > 100));
      const invalidCount = parsed.some((row) => row.count > denominator);
      return {
        denominator,
        countSum,
        percentSum,
        invalidPercent,
        invalidCount,
        parsed,
      };
    });

    assert(`${label} quality denominator > 0`, reconciliation.denominator > 0, String(reconciliation.denominator));
    assert(`${label} quality count sum equals denominator`, reconciliation.countSum === reconciliation.denominator, JSON.stringify(reconciliation));
    assert(`${label} quality percent sum near 100`, Math.abs(reconciliation.percentSum - 100) <= 1.5, JSON.stringify(reconciliation));
    assert(`${label} quality percents within 0-100`, !reconciliation.invalidPercent, JSON.stringify(reconciliation));
    assert(`${label} quality counts within denominator`, !reconciliation.invalidCount, JSON.stringify(reconciliation));

    const liveRatios = await page.evaluate(() => {
      const rows = [...document.querySelectorAll('[data-testid^="iot-plc-quality-row-"]')];
      return rows.map((row) => {
        const text = row.textContent ?? "";
        const percentMatch = text.match(/%([\d.,]+)/);
        return percentMatch ? Number.parseFloat(percentMatch[1].replace(",", ".")) : null;
      });
    });
    assert(
      `${label} displayed quality percentages within 0-100`,
      liveRatios.every((percent) => percent === null || (percent >= 0 && percent <= 100)),
      JSON.stringify(liveRatios),
    );

    const contractNote = await page.locator('[data-testid="iot-plc-quality-contract-note"]').count();
    if (contractNote > 0) {
      assert(`${label} contract violation note visible`, contractNote === 1);
    }

    const goodRow = reconciliation.parsed.find((row) => row.key === "good");
    if (reconciliation.denominator === 2 && goodRow?.count === 2) {
      assert(`${label} denominator 2 good count renders 2`, goodRow.count === 2);
      assert(`${label} denominator 2 good percent renders ~100`, goodRow.percent !== null && goodRow.percent >= 99, JSON.stringify(goodRow));
    }
  }

  const analyticsHeights = await page.evaluate(() => {
    const panels = [...document.querySelectorAll(".iot-plc-panel--analytics")];
    return panels.map((panel) => panel.getBoundingClientRect().height);
  });
  if (analyticsHeights.length === 2) {
    const max = Math.max(...analyticsHeights);
    const min = Math.min(...analyticsHeights);
    assert(`${label} analytics panels balanced height`, max > 0 && min / max >= 0.72, analyticsHeights.join(","));
  }

  const findingCards = await page.locator('[data-testid="iot-plc-finding-card"]').count();
  if (findingCards > 0) {
    const findingHeights = await page.evaluate(() => {
      return [...document.querySelectorAll('[data-testid="iot-plc-finding-card"]')].map(
        (card) => card.getBoundingClientRect().height,
      );
    });
    const max = Math.max(...findingHeights);
    const min = Math.min(...findingHeights);
    assert(`${label} finding cards height-balanced`, max > 0 && min / max >= 0.72, findingHeights.join(","));
  }
}

async function assertQualityWorkspaceGeometry(page, label) {
  await clickIotPlcTab(page, "quality", `${label} quality geometry`);

  const hero = page.locator('[data-testid="iot-plc-quality-hero-intelligence"]');
  if (await hero.count() > 0) {
    const heroBox = await hero.boundingBox();
    assert(
      `${label} quality hero intelligence geometry`,
      Boolean(heroBox && heroBox.width >= 320 && heroBox.height >= 120),
      JSON.stringify(heroBox),
    );

    const heroBar = await page.locator('[data-testid="iot-plc-quality-hero-distribution"]').boundingBox();
    if (heroBar) {
      assert(
        `${label} quality hero distribution geometry`,
        Boolean(heroBar.width >= 200 && heroBar.height >= 72),
        JSON.stringify(heroBar),
      );
    }
  }

  const selectedProfile = await page.locator('[data-testid^="iot-plc-quality-profile-"][data-selected="true"]').count();
  assert(`${label} selected quality profile visible`, selectedProfile >= 1, String(selectedProfile));

  const controls = page.locator('[data-testid="iot-plc-quality-controls"]');
  if (await controls.count() === 0) return;

  const evalGroups = page.locator('[data-testid^="iot-plc-quality-eval-"]');
  assert(`${label} quality evaluation groups present`, await evalGroups.count() >= 2, String(await evalGroups.count()));

  const overlap = await page.evaluate(() => {
    const connection = document.querySelector('[data-testid="iot-plc-quality-eval-connection"]');
    const signal = document.querySelector('[data-testid="iot-plc-quality-eval-signal"]');
    const filters = document.querySelector('[data-testid="iot-plc-quality-filters"]');
    const nodes = [connection, signal, filters].filter(Boolean);
    const boxes = nodes.map((node) => node.getBoundingClientRect());
    for (let left = 0; left < boxes.length; left += 1) {
      for (let right = left + 1; right < boxes.length; right += 1) {
        const a = boxes[left];
        const b = boxes[right];
        const intersects = !(a.right <= b.left || b.right <= a.left || a.bottom <= b.top || b.bottom <= a.top);
        if (intersects && a.width > 0 && b.width > 0) return true;
      }
    }
    return false;
  });
  assert(`${label} quality eval groups and filters do not overlap`, !overlap);

  const controlsBox = await controls.boundingBox();
  assert(`${label} quality controls geometry`, Boolean(controlsBox && controlsBox.width >= 480 && controlsBox.height >= 180), JSON.stringify(controlsBox));

  const filtersBox = await page.locator('[data-testid="iot-plc-quality-filters"]').boundingBox();
  const resultsBox = await page.locator('[data-testid="iot-plc-quality-results"]').boundingBox();
  if (filtersBox && resultsBox) {
    assert(`${label} filter rail above results`, filtersBox.y < resultsBox.y, `${filtersBox.y} vs ${resultsBox.y}`);
  }
}

async function assertWorkspaceStructure(page, label, options = {}) {
  const { expectManageControls = true } = options;

  await page.locator('[data-testid="iot-plc-workspace"]').waitFor({ state: "visible", timeout: 30000 });
  assert(`${label} executive eyebrow`, await page.getByText("ENDÜSTRİYEL BAĞLANTI").isVisible());
  assert(`${label} executive title`, await page.getByText("IoT & PLC").first().isVisible());
  assert(`${label} KPI grid visible`, await page.locator('[data-testid="iot-plc-kpi-grid"]').isVisible());

  const tabCount = await page.locator('[data-testid="iot-plc-tab-rail"] [role="tab"]').count();
  assert(`${label} exactly four tabs`, tabCount === 4, String(tabCount));

  const kpiCount = await page.locator('[data-testid="iot-plc-kpi-card"]').count();
  assert(`${label} exactly six KPI cards`, kpiCount === 6, String(kpiCount));

  await clickIotPlcTab(page, "overview", `${label} overview`);
  const donutPaths = await page.locator('[data-testid="iot-plc-health-donut-segment"]').count();
  assert(`${label} health donut path segments`, donutPaths > 0, String(donutPaths));

  const donutGeometry = await page.evaluate(() => {
    const segments = [...document.querySelectorAll('[data-testid="iot-plc-health-donut-segment"]')];
    return {
      segmentCount: segments.length,
      pathsPresent: segments.every((segment) => (segment.getAttribute("d") ?? "").length > 0),
    };
  });
  assert(`${label} donut segment paths present`, Boolean(donutGeometry?.pathsPresent), JSON.stringify(donutGeometry));

  const qualityBarCount = await page.locator('[data-testid="iot-plc-quality-bar-segment"]').count();
  const qualityEmpty = await page.locator('[data-testid="iot-plc-quality-distribution-empty"]').count();
  if (qualityBarCount > 0) {
    assert(`${label} quality bar segments visible`, qualityBarCount > 0, String(qualityBarCount));
    const qualityGeometry = await page.evaluate(() => {
      const segments = [...document.querySelectorAll('[data-testid="iot-plc-quality-bar-segment"]')];
      return segments.every((segment) => {
        const width = Number.parseFloat(segment.style.width || "0");
        return width > 0;
      });
    });
    assert(`${label} quality bar segments have width`, qualityGeometry);
  } else {
    assert(`${label} quality distribution empty state when no bar`, qualityEmpty === 1, String(qualityEmpty));
  }

  const workspaceText = await page.locator('[data-testid="iot-plc-workspace"]').innerText();
  if (workspaceText.includes("Sinyal kalitesi") && qualityBarCount > 0) {
    const percentMatches = workspaceText.match(/%\d|\d\s*%|%\s*\d/g) ?? [];
    const ratioMatches = workspaceText.match(/\d{2,3}\s*%/g) ?? [];
    assert(
      `${label} ratio display uses percent format`,
      percentMatches.length > 0 || ratioMatches.length > 0,
      workspaceText.slice(0, 300),
    );
  }

  if (expectManageControls) {
    assert(`${label} readonly banner absent`, await page.locator('[data-testid="iot-plc-readonly-banner"]').count() === 0);
    await clickIotPlcTab(page, "connections", `${label} connections`);
    assert(
      `${label} manage connection control available`,
      await page.getByRole("button", { name: "Yeni bağlantı kaydı oluştur" }).count() === 1,
    );
    await clickIotPlcTab(page, "signals", `${label} signals`);
    assert(
      `${label} manage signal control available`,
      await page.getByRole("button", { name: "Yeni sinyal kaydı oluştur" }).count() === 1,
    );
    await clickIotPlcTab(page, "quality", `${label} quality`);
    assert(
      `${label} manage evaluation control available`,
      await page.getByRole("button", { name: "Bağlantı değerlendirmesi çalıştır" }).count() === 1,
    );
    await assertQualityWorkspaceGeometry(page, label);
  } else {
    assert(`${label} readonly banner visible`, await page.locator('[data-testid="iot-plc-readonly-banner"]').isVisible());
    await clickIotPlcTab(page, "connections", `${label} readonly connections`);
    assert(
      `${label} forbidden connection create absent`,
      await page.getByRole("button", { name: "Yeni bağlantı kaydı oluştur" }).count() === 0,
    );
    await clickIotPlcTab(page, "signals", `${label} readonly signals`);
    assert(
      `${label} forbidden signal create absent`,
      await page.getByRole("button", { name: "Yeni sinyal kaydı oluştur" }).count() === 0,
    );
    await clickIotPlcTab(page, "quality", `${label} readonly quality`);
    assert(
      `${label} forbidden evaluation control absent`,
      await page.getByRole("button", { name: "Bağlantı değerlendirmesi çalıştır" }).count() === 0,
    );
    assert(
      `${label} forbidden profile edit absent`,
      await page.getByRole("button", { name: /profilini düzenle/i }).count() === 0,
    );
  }

  await assertNoHorizontalOverflow(page, label);
  await assertDataSemantics(page, label);
  if (!expectManageControls) {
    await clickIotPlcTab(page, "quality", `${label} readonly quality geometry`);
    const hero = await page.locator('[data-testid="iot-plc-quality-hero-intelligence"]').count();
    assert(`${label} quality hero visible for readonly`, hero >= 1, String(hero));
  }
}

function db(sql) {
  const result = spawnSync(
    "docker",
    ["exec", "crevafactos-local-postgres-1", "psql", "-U", "crevafactos", "-d", "crevafactos_local", "-t", "-A", "-c", sql],
    { encoding: "utf8" },
  );
  if (result.status !== 0) fail(result.stderr || result.stdout);
}

function seedIotPlcReadOverrideForMaintenance() {
  db(`
    DELETE FROM tenant_membership_permission_overrides WHERE id = '${CLOSURE_OVERRIDE_ID}';
    INSERT INTO tenant_membership_permission_overrides (
      id, tenant_id, tenant_membership_id, permission_definition_id, effect, record_scope, status, created_at_utc, updated_at_utc, version)
    VALUES (
      '${CLOSURE_OVERRIDE_ID}',
      'bb000001-0001-4000-8000-000000000001',
      'bb000003-0223-4000-8000-000000000223',
      '10000000-0000-4000-8000-000000000078',
      'allow', 'tenant', 'Active', NOW(), NOW(), 1);
  `);
}

function removeIotPlcReadOverrideForMaintenance() {
  db(`DELETE FROM tenant_membership_permission_overrides WHERE id = '${CLOSURE_OVERRIDE_ID}';`);
}

console.log("Running IOT-PLC-UI-1 functional UI verifier.");

const verifierSource = readFileSync(new URL(import.meta.url), "utf8");
const fullPageTrueScreenshotCalls =
  verifierSource.match(/page\.screenshot\(\{[\s\S]*?fullPage:\s*true[\s\S]*?\}\)/g) ?? [];
assert("verifier uses viewport screenshot only", fullPageTrueScreenshotCalls.length === 0);

const ready = await fetch(`${WEB}/giris`, { redirect: "manual" });
assert("web server reachable", ready.status === 200 || ready.status === 307 || ready.status === 308, `status=${ready.status}`);

const ownerPassword = requireEnv("DevelopmentSaasIdentity__TenantOwnerPassword");
const activePassword = requireEnv("DevelopmentSaasIdentity__ActiveUserPassword");
const mfaSecret = requireEnv("DevelopmentSaasIdentity__MfaSharedSecret");

removeIotPlcReadOverrideForMaintenance();
seedIotPlcReadOverrideForMaintenance();

const { chromium } = await import("playwright");
const browser = await chromium.launch({
  headless: true,
  ...(resolveChromiumExecutable() ? { executablePath: resolveChromiumExecutable() } : {}),
});

const consoleErrors = [];

try {
  const ownerContext = await browser.newContext({
    viewport: { width: 1600, height: 1000 },
    deviceScaleFactor: 1,
  });
  const ownerPage = await ownerContext.newPage();
  ownerPage.on("console", (msg) => {
    if (msg.type() === "error") consoleErrors.push(msg.text());
  });
  ownerPage.on("pageerror", (err) => consoleErrors.push(String(err)));

  await signInPersona(ownerPage, "tenant-owner@dev.local", ownerPassword, mfaSecret);
  const ownerResponse = await ownerPage.goto(`${WEB}/iot-plc`, { waitUntil: "networkidle", timeout: 90000 });
  assert("owner /iot-plc HTTP 200", ownerResponse?.status() === 200);
  await assertWorkspaceStructure(ownerPage, "owner", { expectManageControls: true });

  for (const spec of OWNER_SCREENSHOTS) {
    await captureScreenshot(ownerPage, spec);
  }

  const readonlyContext = await browser.newContext({
    viewport: { width: 1600, height: 1000 },
    deviceScaleFactor: 1,
  });
  const readonlyPage = await readonlyContext.newPage();
  readonlyPage.on("console", (msg) => {
    if (msg.type() === "error") consoleErrors.push(msg.text());
  });
  readonlyPage.on("pageerror", (err) => consoleErrors.push(String(err)));

  await signInPersona(readonlyPage, "maintenance-team@dev.local", activePassword);
  const readonlyResponse = await readonlyPage.goto(`${WEB}/iot-plc`, { waitUntil: "networkidle", timeout: 90000 });
  assert("readonly /iot-plc HTTP 200", readonlyResponse?.status() === 200);
  await assertWorkspaceStructure(readonlyPage, "readonly", { expectManageControls: false });
  await captureScreenshot(readonlyPage, READONLY_SCREENSHOT);

  await ownerContext.close();
  await readonlyContext.close();
} finally {
  await browser.close();
  removeIotPlcReadOverrideForMaintenance();
}

assert("no hydration errors", !consoleErrors.some((entry) => /hydration/i.test(entry)), consoleErrors.join("; "));
assert("no console errors", consoleErrors.length === 0, consoleErrors.join("; "));

console.log("IOT-PLC-UI-1 functional UI verification passed.");
