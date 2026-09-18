#!/usr/bin/env node
/**
 * MP2 Throughput Intelligence — Visual QA capture (authenticated, CDP)
 */
import { createHmac } from "node:crypto";
import { existsSync, mkdirSync, readFileSync, writeFileSync } from "node:fs";
import { spawn } from "node:child_process";
import { tmpdir } from "node:os";
import { dirname, join, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), "../../..");
const FACILITY_ID = process.env.NEVAXIS_FACTOS_DEV_FACILITY_ID ?? "bb000001-0005-4000-8000-000000000005";
const WEB_PORT = Number(process.env.NEVAXIS_FACTOS_MP2_VISUAL_QA_WEB_PORT ?? "3021");
let WEB = (process.env.NEVAXIS_FACTOS_WEB_BASE_URL ?? `http://localhost:${WEB_PORT}`)
  .replace("127.0.0.1", "localhost")
  .replace(/\/$/, "");
const API = (process.env.NEVAXIS_FACTOS_API_BASE_URL ?? "http://127.0.0.1:5000").replace(/\/$/, "");
const SSR_RUNTIME_ENV = {
  NEVAXIS_FACTOS_API_BASE_URL: API,
  NEVAXIS_FACTOS_PRODUCTION_EXECUTION_RUNTIME_MODE: "development-api",
  NEVAXIS_FACTOS_DEV_AUTH_ISSUER: "crevafactos-dev",
  NEVAXIS_FACTOS_DEV_AUTH_SUBJECT: "planning-pilot-user",
  NEVAXIS_FACTOS_DEV_TENANT_KEY: "dev-pilot-tenant",
  NEVAXIS_FACTOS_DEV_FACILITY_ID: FACILITY_ID,
};
const OUT_DIR = join(ROOT, "artifacts", "mp2-throughput-intelligence");
const VIEWPORTS = [
  { width: 1920, height: 1080, label: "1920x1080" },
  { width: 1440, height: 900, label: "1440x900" },
  { width: 1366, height: 768, label: "1366x768" },
];
const TOTP_ALPHABET = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";

const MP2_CHECKS = [
  { id: "throughput-intelligence-panel", label: "Throughput Status / MP2 panel" },
  { id: "current-constraint", label: "Current Constraint" },
  { id: "throughput-topology-grid", label: "Line Topology metrics" },
  { id: "loss-tree-grid", label: "Throughput Loss Tree" },
  { id: "mixed-production-line-detail-view", label: "Line detail shell" },
  { id: "mixed-line-timeline", label: "Production Timeline" },
];

function fail(message) {
  console.error(`MP2 visual QA failed: ${message}`);
  process.exit(1);
}

function loadSecrets() {
  const envLocal = join(ROOT, "infrastructure", ".env.local");
  if (existsSync(envLocal)) {
    for (const line of readFileSync(envLocal, "utf8").split(/\r?\n/)) {
      const trimmed = line.trim();
      if (!trimmed || trimmed.startsWith("#")) continue;
      const eq = trimmed.indexOf("=");
      if (eq > 0 && !process.env[trimmed.slice(0, eq)]) {
        process.env[trimmed.slice(0, eq)] = trimmed.slice(eq + 1);
      }
    }
  }

  process.env.NEVAXIS_FACTOS_PRODUCTION_EXECUTION_RUNTIME_MODE ??= "development-api";
  process.env.NEVAXIS_FACTOS_DEV_AUTH_ISSUER ??= "crevafactos-dev";
  process.env.NEVAXIS_FACTOS_DEV_AUTH_SUBJECT ??= "planning-pilot-user";
  process.env.NEVAXIS_FACTOS_DEV_TENANT_KEY ??= "dev-pilot-tenant";
  process.env.NEVAXIS_FACTOS_DEV_FACILITY_ID ??= FACILITY_ID;
  Object.assign(process.env, SSR_RUNTIME_ENV);
}

async function ensureWebDevServer() {
  if (process.env.NEVAXIS_FACTOS_WEB_BASE_URL) {
    return null;
  }

  try {
    const probe = await fetch(`${WEB}/giris`, { signal: AbortSignal.timeout(3000) });
    if (probe.ok) {
      return null;
    }
  } catch {
    // start a dedicated dev server for SSR visual QA
  }

  const child = spawn(
    "pnpm",
    ["--filter", "@nevaxiszentra/web", "dev", "--", "-p", String(WEB_PORT)],
    {
      cwd: ROOT,
      env: { ...process.env, ...SSR_RUNTIME_ENV },
      stdio: "ignore",
      shell: true,
    },
  );

  for (let attempt = 0; attempt < 90; attempt++) {
    try {
      const response = await fetch(`${WEB}/giris`, { signal: AbortSignal.timeout(5000) });
      if (response.ok) {
        console.log(`✓ Web dev server ready at ${WEB} (development-api SSR)`);
        return child;
      }
    } catch {
      await new Promise((resolve) => setTimeout(resolve, 2000));
    }
  }

  child.kill("SIGTERM");
  fail(`Web dev server did not become ready at ${WEB}`);
}

function generateTotp(secret, counterOffset = 0) {
  const counter = Math.floor(Date.now() / 1000 / 30) + counterOffset;
  const normalized = secret.trim().replace(/=+$/, "").toUpperCase();
  let buffer = 0;
  let bitsLeft = 0;
  const output = [];
  for (const character of normalized) {
    buffer = (buffer << 5) | TOTP_ALPHABET.indexOf(character);
    bitsLeft += 5;
    if (bitsLeft >= 8) {
      output.push((buffer >> (bitsLeft - 8)) & 0xff);
      bitsLeft -= 8;
    }
  }
  const counterBytes = Buffer.alloc(8);
  counterBytes.writeBigUInt64BE(BigInt(counter));
  const hash = createHmac("sha1", Buffer.from(output)).update(counterBytes).digest();
  const hashOffset = hash[hash.length - 1] & 0x0f;
  const binaryCode =
    ((hash[hashOffset] & 0x7f) << 24) |
    ((hash[hashOffset + 1] & 0xff) << 16) |
    ((hash[hashOffset + 2] & 0xff) << 8) |
    (hash[hashOffset + 3] & 0xff);
  return String(binaryCode % 1_000_000).padStart(6, "0");
}

async function createCdpSession(viewport) {
  const exe = "C:\\Program Files\\Google\\Chrome\\Application\\chrome.exe";
  if (!existsSync(exe)) fail("Chrome not found");

  const userDataDir = join(tmpdir(), `mp2-ui-qa-${Date.now()}`);
  const port = 9880 + Math.floor(Math.random() * 100);
  const child = spawn(exe, [
    `--remote-debugging-port=${port}`,
    "--headless=new",
    "--disable-gpu",
    `--window-size=${viewport.width},${viewport.height}`,
    `--user-data-dir=${userDataDir}`,
  ], { stdio: "ignore" });

  let version = null;
  for (let attempt = 0; attempt < 60; attempt++) {
    try {
      version = await fetch(`http://127.0.0.1:${port}/json/version`).then((r) => r.json());
      break;
    } catch {
      await new Promise((r) => setTimeout(r, 500));
    }
  }
  if (!version) {
    child.kill("SIGTERM");
    fail("Chrome DevTools not ready");
  }

  const created = await fetch(`http://127.0.0.1:${port}/json/new?about:blank`, { method: "PUT" }).then((r) => r.json());
  const ws = new WebSocket(version.webSocketDebuggerUrl);
  await new Promise((res, rej) => { ws.onopen = res; ws.onerror = rej; });

  let nextId = 1;
  const pending = new Map();
  ws.onmessage = (event) => {
    const message = JSON.parse(String(event.data));
    if (message.id && pending.has(message.id)) {
      const { resolve, reject } = pending.get(message.id);
      pending.delete(message.id);
      message.error ? reject(new Error(message.error.message)) : resolve(message.result);
    }
  };

  const send = (method, params = {}, sessionId) => new Promise((resolve, reject) => {
    const id = nextId++;
    pending.set(id, { resolve, reject });
    const payload = { id, method, params };
    if (sessionId) payload.sessionId = sessionId;
    ws.send(JSON.stringify(payload));
  });

  const attached = await send("Target.attachToTarget", { targetId: created.id, flatten: true });
  const ss = (method, params = {}) => send(method, params, attached.sessionId);

  await ss("Emulation.setDeviceMetricsOverride", {
    width: viewport.width,
    height: viewport.height,
    deviceScaleFactor: 1,
    mobile: false,
  });

  const dispose = () => {
    try { ws.close(); } catch { /* ignore */ }
    try { child.kill("SIGTERM"); } catch { /* ignore */ }
  };

  return { ss, dispose };
}

async function signIn(ss, ownerPassword, mfaSecret) {
  await ss("Page.navigate", { url: `${WEB}/giris` });
  await new Promise((r) => setTimeout(r, 2000));

  const signInResult = await ss("Runtime.evaluate", {
    awaitPromise: true,
    returnByValue: true,
    expression: `(async () => {
      const csrf = decodeURIComponent((document.cookie.match(/NevaxisZentra\\.csrf=([^;]+)/) || [])[1] || "");
      const res = await fetch("/api/identity/sign-in", {
        method: "POST",
        credentials: "include",
        headers: {
          Accept: "application/json",
          "Content-Type": "application/json",
          "X-NevaxisZentra-Csrf": csrf,
        },
        body: JSON.stringify({
          organizationContext: "dev-pilot-tenant",
          email: "admin@demo.nevaxiszentra.local",
          password: ${JSON.stringify(ownerPassword)},
        }),
      });
      const body = await res.json().catch(() => ({}));
      return { status: res.status, body };
    })()`,
  });

  const outcome = signInResult.result?.value;
  const requiresMfa = outcome?.body?.requiresMfa === true || outcome?.body?.outcome === "mfa_required";
  if (outcome?.status !== 200 || !requiresMfa) fail(`Sign-in failed: ${JSON.stringify(outcome)}`);

  let mfaOk = false;
  for (const offset of [-1, 0, 1]) {
    const code = generateTotp(mfaSecret, offset);
    const attempt = await ss("Runtime.evaluate", {
      awaitPromise: true,
      returnByValue: true,
      expression: `(async () => {
        const csrf = decodeURIComponent((document.cookie.match(/NevaxisZentra\\.csrf=([^;]+)/) || [])[1] || "");
        const res = await fetch("/api/identity/mfa/verify", {
          method: "POST",
          credentials: "include",
          headers: {
            Accept: "application/json",
            "Content-Type": "application/json",
            "X-NevaxisZentra-Csrf": csrf,
          },
          body: JSON.stringify({ totpCode: ${JSON.stringify(code)} }),
        });
        const body = await res.json().catch(() => ({}));
        return { status: res.status, body };
      })()`,
    });
    const mfaOutcome = attempt.result?.value;
    const authenticated =
      mfaOutcome?.status === 200 &&
      (mfaOutcome?.body?.outcome === "authenticated" || Boolean(mfaOutcome?.body?.sessionReference));
    if (authenticated) {
      mfaOk = true;
      break;
    }
  }
  if (!mfaOk) fail("MFA verification failed");
}

async function seedMixLineFromApi() {
  const response = await fetch(
    `${API}/api/mixed-production/dev/seed-mix-line-01?facilityId=${FACILITY_ID}`,
    {
      method: "POST",
      headers: {
        Accept: "application/json",
        "X-crevafactos-dev-Issuer": process.env.NEVAXIS_FACTOS_DEV_AUTH_ISSUER,
        "X-crevafactos-dev-Subject": "production-pilot-operator",
        "X-NevaxisZentra-Tenant": process.env.NEVAXIS_FACTOS_DEV_TENANT_KEY,
      },
    },
  );
  const body = await response.json().catch(() => ({}));
  return { status: response.status, body };
}

async function captureViewport(ss, viewport, lineDetailPath) {
  await ss("Page.navigate", { url: `${WEB}${lineDetailPath}` });
  await new Promise((r) => setTimeout(r, 4500));

  const metrics = (await ss("Runtime.evaluate", {
    returnByValue: true,
    expression: `(() => {
      const horizontalOverflow = document.documentElement.scrollWidth > document.documentElement.clientWidth + 2;
      const bodyText = document.body?.innerText ?? "";
      const checks = ${JSON.stringify(MP2_CHECKS)}.map((check) => ({
        id: check.id,
        label: check.label,
        present: Boolean(document.querySelector('[data-testid="' + check.id + '"]')),
      }));
      const hasGradientRegression = Boolean(
        document.querySelector('[class*="gradient"], [style*="linear-gradient"]')
      );
      const hasFactWorkspace = Boolean(document.querySelector(".fact-workspace"));
      const constraintText = bodyText.includes("Mevcut kısıt") || bodyText.includes("ST-30");
      const lossTreeText = bodyText.includes("Verim kayıp") || bodyText.includes("kayıp ağacı");
      const throughputText = bodyText.includes("GOOD/saat") || bodyText.includes("GOOD");
      const scenarioCompare = Boolean(document.querySelector('[data-testid="scenario-compare"]'));
      return {
        horizontalOverflow,
        onLogin: location.pathname.includes("/giris"),
        isErrorState: bodyText.includes("yüklenemedi") || bodyText.includes("yetkiniz yok"),
        checks,
        hasGradientRegression,
        hasFactWorkspace,
        constraintText,
        lossTreeText,
        throughputText,
        scenarioComparePresent: scenarioCompare,
        path: location.pathname,
      };
    })()`,
  })).result?.value;

  const fileName = `mp2-line-detail-${viewport.label}.png`;
  const filePath = join(OUT_DIR, fileName);
  const shot = await ss("Page.captureScreenshot", { format: "png", captureBeyondViewport: true, fromSurface: true });
  writeFileSync(filePath, Buffer.from(shot.data, "base64"));

  const requiredChecks = metrics.checks.filter((c) => c.id !== "scenario-compare");
  const checksPass = requiredChecks.every((c) => c.present);
  const pass =
    !metrics.onLogin &&
    !metrics.isErrorState &&
    !metrics.horizontalOverflow &&
    !metrics.hasGradientRegression &&
    metrics.hasFactWorkspace &&
    checksPass &&
    metrics.constraintText &&
    metrics.lossTreeText &&
    metrics.throughputText;

  return {
    viewport: viewport.label,
    screenshot: filePath,
    ...metrics,
    pass,
  };
}

async function main() {
  loadSecrets();
  const webChild = await ensureWebDevServer();

  const apiReady = await fetch(`${API}/health/ready`).catch(() => null);
  if (!apiReady?.ok) fail(`API not ready at ${API}/health/ready`);
  const webReady = await fetch(`${WEB}/giris`).catch(() => null);
  if (!webReady?.ok) fail(`Web not reachable at ${WEB}`);

  mkdirSync(OUT_DIR, { recursive: true });
  const seed = await seedMixLineFromApi();
  if (seed?.status !== 200 || !seed.body?.lineId) {
    fail(`MIX-LINE-01 seed failed: ${JSON.stringify(seed)}`);
  }
  const lineDetailPath = `/uretim/hatlar/${seed.body.lineId}`;
  console.log(`✓ MIX-LINE-01 seeded → ${lineDetailPath}`);

  const report = [];
  for (const viewport of VIEWPORTS) {
    const { ss, dispose } = await createCdpSession(viewport);
    try {
      const entry = await captureViewport(ss, viewport, lineDetailPath);
      report.push(entry);
      console.log(`${entry.pass ? "✓" : "✗"} ${viewport.label} — checks=${entry.checks.filter((c) => c.present).length}/${entry.checks.length}${entry.horizontalOverflow ? " [overflow]" : ""}`);
      if (!entry.pass) {
        console.log(`  missing: ${entry.checks.filter((c) => !c.present).map((c) => c.id).join(", ")}`);
      }
    } finally {
      dispose();
    }
  }

  writeFileSync(join(OUT_DIR, "mp2-visual-qa-report.json"), JSON.stringify(report, null, 2));
  const failures = report.filter((e) => !e.pass);
  if (failures.length > 0) fail(`${failures.length} viewport(s) failed — see mp2-visual-qa-report.json`);
  console.log(`Output: ${OUT_DIR}`);
  console.log("MP2 Visual QA PASS");
  if (webChild) {
    webChild.kill("SIGTERM");
  }
}

main().catch((error) => {
  fail(error.message);
});
