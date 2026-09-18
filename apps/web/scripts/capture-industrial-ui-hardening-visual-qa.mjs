#!/usr/bin/env node
/**
 * Industrial Enterprise UI Hardening — Visual QA capture (authenticated, CDP)
 */
import { createHmac } from "node:crypto";
import { existsSync, mkdirSync, readFileSync, writeFileSync } from "node:fs";
import { spawn } from "node:child_process";
import { tmpdir } from "node:os";
import { dirname, join, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), "../../..");
const WEB = (process.env.NEVAXIS_FACTOS_WEB_BASE_URL ?? "http://localhost:3001").replace("127.0.0.1", "localhost").replace(/\/$/, "");
const API = (process.env.NEVAXIS_FACTOS_API_BASE_URL ?? "http://127.0.0.1:5000").replace(/\/$/, "");
const FACILITY_ID = process.env.NEVAXIS_FACTOS_DEV_FACILITY_ID ?? "bb000001-0005-4000-8000-000000000005";
const OUT_DIR = join(ROOT, "artifacts", "industrial-ui-hardening");
const VIEWPORTS = [
  { width: 1920, height: 1080, label: "1920x1080" },
  { width: 1440, height: 900, label: "1440x900" },
  { width: 1366, height: 768, label: "1366x768" },
];
const ROUTES = [
  { path: "/dashboard", name: "dashboard", testId: null },
  { path: "/uretim", name: "production", testId: null },
  { path: "/uretim/hatlar", name: "mixed-production-lines", testId: "mixed-production-lines-view" },
  { path: "/dijital-is-gucu", name: "digital-workforce", testId: null },
  { path: "/fabrika-karar-merkezi", name: "factory-decision-center", testId: "factory-decision-center-view" },
  { path: "/fabrika-zekasi", name: "factory-intelligence", testId: "factory-intelligence-view" },
  { path: "/vardiyalar", name: "shifts", testId: null },
  { path: "/bakim", name: "maintenance", testId: null },
  { path: "/kalite", name: "quality", testId: null },
  { path: "/malzemeler", name: "materials", testId: null },
  { path: "/kaliplar", name: "molds", testId: null },
];
const TOTP_ALPHABET = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";

function fail(message) {
  console.error(`Industrial UI visual QA failed: ${message}`);
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

  const userDataDir = join(tmpdir(), `industrial-ui-qa-${Date.now()}`);
  const port = 9780 + Math.floor(Math.random() * 100);
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
  const evalSnap = async (expr, awaitPromise = false) =>
    (await ss("Runtime.evaluate", { expression: expr, returnByValue: true, awaitPromise })).result?.value;

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

  return { ss, evalSnap, dispose };
}

async function signIn(ss, evalSnap, ownerPassword, mfaSecret) {
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
  if (outcome?.status !== 200 || !requiresMfa) {
    fail(`Sign-in failed: ${JSON.stringify(outcome)}`);
  }

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
  await ss("Page.navigate", { url: `${WEB}/dashboard` });
  await new Promise((r) => setTimeout(r, 2000));
}

async function seedMixLineFromNode() {
  const devIssuer = process.env.NEVAXIS_FACTOS_DEV_AUTH_ISSUER ?? "crevafactos-dev";
  const devSubject = process.env.NEVAXIS_FACTOS_DEV_AUTH_SUBJECT ?? "production-pilot-operator";
  const devTenantKey = process.env.NEVAXIS_FACTOS_DEV_TENANT_KEY ?? "dev-pilot-tenant";
  const response = await fetch(
    `${API}/api/mixed-production/dev/seed-mix-line-01?facilityId=${FACILITY_ID}`,
    {
      method: "POST",
      headers: {
        Accept: "application/json",
        "X-crevafactos-dev-Issuer": devIssuer,
        "X-crevafactos-dev-Subject": devSubject,
        "X-NevaxisZentra-Tenant": devTenantKey,
      },
    },
  );
  const body = await response.json().catch(() => ({}));
  return { status: response.status, body };
}

async function captureRoute(ss, evalSnap, route, viewport, lineDetailPath) {
  const paths = route.name === "mixed-production-lines" && lineDetailPath
    ? [route.path, lineDetailPath]
    : [route.path];

  const captures = [];
  for (const path of paths) {
    const name = path.includes("/hatlar/") && path !== "/uretim/hatlar"
      ? "mixed-production-line-detail"
      : route.name;

    await ss("Page.navigate", { url: `${WEB}${path}` });
    await new Promise((r) => setTimeout(r, 3500));

    const metrics = await evalSnap(`(() => {
      const horizontalOverflow = document.documentElement.scrollWidth > document.documentElement.clientWidth + 2;
      const onLogin = location.pathname.includes("/giris") || location.pathname.includes("/mfa-dogrula");
      const hasShell = Boolean(document.querySelector(".app-shell, .fact-workspace, main"));
      const testId = ${JSON.stringify(path)}.includes("/hatlar/") && ${JSON.stringify(path)} !== "/uretim/hatlar"
        ? "mixed-production-line-detail-view"
        : ${JSON.stringify(route.testId)};
      const hasExpected = testId ? Boolean(document.querySelector('[data-testid="' + testId + '"]')) : hasShell;
      const bodyText = document.body?.innerText ?? "";
      return {
        horizontalOverflow,
        onLogin,
        hasShell,
        hasExpectedSurface: hasExpected,
        path: location.pathname,
        isErrorState: bodyText.includes("yüklenemedi") || bodyText.includes("yetkiniz yok"),
      };
    })()`);

    const fileName = `${name}-${viewport.label}.png`;
    const filePath = join(OUT_DIR, fileName);
    const shot = await ss("Page.captureScreenshot", { format: "png", captureBeyondViewport: true, fromSurface: true });
    writeFileSync(filePath, Buffer.from(shot.data, "base64"));

    captures.push({
      route: path,
      name,
      viewport: viewport.label,
      screenshot: filePath,
      ...metrics,
      pass: !metrics.onLogin && metrics.hasShell && !metrics.isErrorState && (route.testId ? metrics.hasExpectedSurface : true),
    });
  }
  return captures;
}

async function main() {
  loadSecrets();
  const ownerPassword = process.env.DevelopmentSaasIdentity__TenantOwnerPassword;
  const mfaSecret = process.env.DevelopmentSaasIdentity__MfaSharedSecret;
  if (!ownerPassword || !mfaSecret) fail("Missing DevelopmentSaasIdentity secrets");

  const apiReady = await fetch(`${API}/health/ready`).catch(() => null);
  if (!apiReady?.ok) fail(`API not ready at ${API}/health/ready`);
  const webReady = await fetch(WEB).catch(() => null);
  if (!webReady) fail(`Web not reachable at ${WEB}`);

  mkdirSync(OUT_DIR, { recursive: true });
  const report = [];
  let lineDetailPath = null;

  const seed = await seedMixLineFromNode();
  if (seed?.status === 200 && seed.body?.lineId) {
    lineDetailPath = `/uretim/hatlar/${seed.body.lineId}`;
    console.log(`✓ MIX-LINE-01 seeded → ${lineDetailPath}`);
  }

  for (const viewport of VIEWPORTS) {
    const { ss, evalSnap, dispose } = await createCdpSession(viewport);
    try {
      await signIn(ss, evalSnap, ownerPassword, mfaSecret);
      for (const route of ROUTES) {
        const captures = await captureRoute(ss, evalSnap, route, viewport, lineDetailPath);
        for (const entry of captures) {
          report.push(entry);
          console.log(`${entry.pass ? "✓" : "✗"} ${entry.route} @ ${viewport.label}${entry.horizontalOverflow ? " [overflow]" : ""}`);
        }
      }
    } finally {
      dispose();
    }
  }

  writeFileSync(join(OUT_DIR, "visual-qa-report.json"), JSON.stringify(report, null, 2));
  const coreCaptures = report.filter((e) => e.name !== "mixed-production-line-detail");
  const lineDetailCaptures = report.filter((e) => e.name === "mixed-production-line-detail");
  const coreFailures = coreCaptures.filter((e) => !e.pass);
  const lineDetailFailures = lineDetailCaptures.filter((e) => !e.pass);
  console.log(`\nCore captures: ${coreCaptures.length} (expected 33)`);
  console.log(`Supplemental line-detail: ${lineDetailCaptures.length} (expected 3)`);
  if (coreCaptures.length !== 33) fail(`Expected 33 core screenshots, got ${coreCaptures.length}`);
  if (lineDetailCaptures.length !== 3) fail(`Expected 3 line-detail screenshots, got ${lineDetailCaptures.length}`);
  if (coreFailures.length > 0) fail(`${coreFailures.length} core captures failed — see visual-qa-report.json`);
  if (lineDetailFailures.length > 0) fail(`${lineDetailFailures.length} line-detail captures failed — see visual-qa-report.json`);
  console.log(`Output: ${OUT_DIR}`);
  console.log("Visual QA capture PASS");
}

main().catch((error) => fail(error.message));
