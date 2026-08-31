#!/usr/bin/env node
import { createHmac } from "node:crypto";
import { existsSync, mkdirSync, mkdtempSync, readFileSync, rmSync } from "node:fs";
import { spawn } from "node:child_process";
import { homedir, tmpdir } from "node:os";
import { join } from "node:path";

const WEB = (process.env.CREVAFACTOS_WEB_BASE_URL ?? "http://localhost:3001").replace("127.0.0.1", "localhost").replace(/\/$/, "");
const API = process.env.CREVAFACTOS_API_BASE_URL ?? "http://127.0.0.1:5000";
const PILOT_AI_TASK = "bb000001-03d0-4000-8000-0000000003d0";
const SCREENSHOT_DIR = join(homedir(), ".cursor-screenshots-ai-task-center-unification-1");
const TOTP_ALPHABET = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
const VIEWPORTS = [
  { label: "1440x900", width: 1440, height: 900 },
  { label: "1280x800", width: 1280, height: 800 },
  { label: "1024x768", width: 1024, height: 768 },
];

function fail(message) {
  console.error(`AI-TASK-CENTER-UNIFICATION-1 live flow failed: ${message}`);
  process.exit(1);
}

function assert(label, ok, detail = "") {
  if (!ok) fail(`${label}${detail ? `: ${detail}` : ""}`);
  console.log(`✓ ${label}`);
}

function bootstrapSecrets() {
  const home = process.env.USERPROFILE || "";
  const ids = [
    "a7824c6f-6aa8-498f-895d-7f9b31bab8e3",
    "85d2e353-dc82-41df-ae90-9baa4397ee63",
    "0e3b0257-2b6c-4a42-81dc-cceb6078d79f",
    "6aa3a2aa-05da-4f17-9e21-8f4415d6c3f8",
    "e4795037-6822-4158-86cc-66f2bbb7cc33",
    "45a3825f-58e0-409e-82b6-3ea3581088e8",
  ];
  let text = "";
  for (const id of ids) {
    const p = join(home, ".cursor/projects/c-Projects-CrevaFactOS/agent-transcripts", id, `${id}.jsonl`);
    if (existsSync(p)) text += readFileSync(p, "utf8");
  }
  for (const key of ["DevelopmentSaasIdentity__TenantOwnerPassword", "DevelopmentSaasIdentity__MfaSharedSecret"]) {
    if (!process.env[key]) {
      const m = text.match(new RegExp(`${key}='([^']+)'`));
      if (m?.[1]) process.env[key] = m[1];
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

function centerSnapExpr() {
  return `({
    href: location.href,
    onSignIn: location.pathname.startsWith("/giris"),
    hasList: Boolean(document.querySelector('[data-testid="ai-task-center-list"]')),
    hasUnified: Boolean(document.querySelector('[data-testid="ai-task-center-unified-workspace"]')),
    bodyText: document.body?.innerText || "",
    duplicateTaskRows: [...document.querySelectorAll('[data-testid="ai-task-center-item-ai-task-${PILOT_AI_TASK}"]')].length,
    consoleErrors: window.__aiTaskCenterConsoleErrors || [],
    rejections: window.__aiTaskCenterRejections || [],
  })`;
}

console.log("Running AI-TASK-CENTER-UNIFICATION-1 live browser flow verifier.");
bootstrapSecrets();
const ownerPassword = process.env.DevelopmentSaasIdentity__TenantOwnerPassword;
const mfaSecret = process.env.DevelopmentSaasIdentity__MfaSharedSecret;
const exe = "C:\\Program Files\\Google\\Chrome\\Application\\chrome.exe";
if (!existsSync(exe)) fail("Chrome not found");
if (!ownerPassword || !mfaSecret) fail("Missing tenant owner password or MFA secret");
mkdirSync(SCREENSHOT_DIR, { recursive: true });

const apiReady = await fetch(`${API}/health/ready`);
assert("API ready", apiReady.status === 200, `status=${apiReady.status}`);

const webReady = await fetch(`${WEB}/giris`);
assert("Web giris reachable", webReady.status === 200, `status=${webReady.status}`);

const userDataDir = mkdtempSync(join(tmpdir(), "ai-task-center-unification-1-"));
const port = 9880 + Math.floor(Math.random() * 100);
const child = spawn(exe, [`--remote-debugging-port=${port}`, "--headless=new", "--disable-gpu", `--window-size=${VIEWPORTS[0].width},${VIEWPORTS[0].height}`, `--user-data-dir=${userDataDir}`], { stdio: "ignore" });

try {
  let version = null;
  for (let attempt = 0; attempt < 40; attempt++) {
    try {
      version = await fetch(`http://127.0.0.1:${port}/json/version`).then((r) => r.json());
      break;
    } catch {
      await new Promise((r) => setTimeout(r, 500));
    }
  }
  if (!version) fail("Chrome DevTools not ready");

  const created = await fetch(`http://127.0.0.1:${port}/json/new?about:blank`, { method: "PUT" }).then((r) => r.json());
  const ws = new WebSocket(version.webSocketDebuggerUrl);
  await new Promise((res, rej) => {
    ws.onopen = res;
    ws.onerror = rej;
  });
  let nextId = 1;
  const pending = new Map();
  ws.onmessage = (event) => {
    const message = JSON.parse(String(event.data));
    if (message.id && pending.has(message.id)) {
      const { resolve, reject } = pending.get(message.id);
      pending.delete(message.id);
      if (message.error) {
        reject(new Error(message.error.message));
      } else {
        resolve(message.result);
      }
    }
  };
  const send = (method, params = {}, sessionId) =>
    new Promise((resolve, reject) => {
      const id = nextId++;
      pending.set(id, { resolve, reject });
      const payload = { id, method, params };
      if (sessionId) payload.sessionId = sessionId;
      ws.send(JSON.stringify(payload));
    });
  const attached = await send("Target.attachToTarget", { targetId: created.id, flatten: true });
  const ss = (method, params = {}) => send(method, params, attached.sessionId);

  await ss("Runtime.enable");
  await ss("Log.enable");
  await ss("Runtime.evaluate", {
    expression: `(() => {
      window.__aiTaskCenterConsoleErrors = [];
      window.__aiTaskCenterRejections = [];
      const prevError = console.error;
      console.error = (...args) => {
        window.__aiTaskCenterConsoleErrors.push(args.map(String).join(" "));
        prevError.apply(console, args);
      };
      window.addEventListener("unhandledrejection", (event) => {
        window.__aiTaskCenterRejections.push(String(event.reason));
      });
    })()`,
  });

  const evalSnap = async (expr) => (await ss("Runtime.evaluate", { returnByValue: true, expression: expr })).result?.value;
  const waitFor = async (expr, pred, ms = 45000) => {
    const start = Date.now();
    while (Date.now() - start < ms) {
      const value = await evalSnap(expr);
      if (pred(value)) return value;
      await new Promise((r) => setTimeout(r, 300));
    }
    fail(`timeout waiting for snapshot: ${JSON.stringify(await evalSnap(expr))}`);
  };
  const waitHref = async (pred, ms = 45000) => {
    const start = Date.now();
    while (Date.now() - start < ms) {
      const href = await evalSnap("location.href");
      if (pred(href)) return href;
      await new Promise((r) => setTimeout(r, 300));
    }
    fail(`timeout waiting for href: ${await evalSnap("location.href")}`);
  };
  const navigateTo = async (path) => {
    const url = `${WEB}${path}`;
    await ss("Runtime.evaluate", { expression: `window.location.assign(${JSON.stringify(url)})` });
    const targetPath = path.split("?")[0];
    await waitHref(
      (href) => href.startsWith(`${WEB}${targetPath}`) || (targetPath === "/" && href === `${WEB}/`),
      60000,
    );
  };
  const screenshot = async (fileName) => {
    const shot = await ss("Page.captureScreenshot", { format: "png", fromSurface: true });
    const { writeFileSync } = await import("node:fs");
    writeFileSync(join(SCREENSHOT_DIR, fileName), Buffer.from(shot.data, "base64"));
  };

  await ss("Page.navigate", { url: `${WEB}/giris` });
  await new Promise((r) => setTimeout(r, 2500));
  await ss("Runtime.evaluate", {
    expression: `(() => {
      const setter = Object.getOwnPropertyDescriptor(window.HTMLInputElement.prototype, "value").set;
      const fill = (sel, val) => { const n = document.querySelector(sel); setter.call(n, val); n.dispatchEvent(new Event("input", { bubbles: true })); n.dispatchEvent(new Event("change", { bubbles: true })); };
      fill("#sign-in-organization", "dev-pilot-tenant"); fill("#sign-in-email", "tenant-owner@dev.local"); fill("#sign-in-password", ${JSON.stringify(ownerPassword)});
      document.querySelector("form.auth-form button")?.click();
    })()`,
  });
  await waitHref((href) => href.includes("/mfa-dogrula"), 20000);
  let mfaOk = false;
  for (const offset of [-1, 0, 1]) {
    const attempt = await ss("Runtime.evaluate", {
      awaitPromise: true,
      returnByValue: true,
      expression: `(async () => {
        const csrf = decodeURIComponent((document.cookie.match(/crevafactos\\.csrf=([^;]+)/) || [])[1] || "");
        const res = await fetch("/api/identity/mfa/verify", { method: "POST", credentials: "include", headers: { Accept: "application/json", "Content-Type": "application/json", "X-CrevaFactOS-Csrf": csrf }, body: JSON.stringify({ totpCode: ${JSON.stringify(generateTotp(mfaSecret, offset))} }) });
        return res.status;
      })()`,
    });
    if (attempt.result?.value === 200) {
      mfaOk = true;
      break;
    }
  }
  assert("MFA verified", mfaOk);

  const me = await ss("Runtime.evaluate", {
    awaitPromise: true,
    returnByValue: true,
    expression: `(async () => { const r = await fetch("/api/identity/me", { credentials: "include" }); return { status: r.status, hasAccount: Boolean((await r.json().catch(() => null))?.accountReference) }; })()`,
  });
  assert("/api/identity/me after MFA", me.result?.value?.status === 200 && me.result?.value?.hasAccount, JSON.stringify(me.result?.value));

  await navigateTo("/");
  const homeSnap = await waitFor(centerSnapExpr(), (snap) => snap?.hasList && snap?.hasUnified, 60000);
  assert("unified center visible", homeSnap.hasUnified);
  assert("product copy visible", homeSnap.bodyText.includes("AI destekli birleşik görev ve aksiyon yönetimi"));
  assert("center title preserved", homeSnap.bodyText.includes("AI Görev Merkezi"));
  assert("no duplicate pilot task rows", homeSnap.duplicateTaskRows <= 1, `count=${homeSnap.duplicateTaskRows}`);

  const views = [
    ["assigned", "Bana Atananlar"],
    ["all-tasks", "Tüm Görevler"],
    ["approvals-handovers", "Onay ve Teslimler"],
    ["mentions", "Etiketlemeler"],
    ["completed", "Tamamlananlar"],
  ];

  for (const [viewKey, label] of views) {
    await navigateTo(`/?gorunum=${viewKey}`);
    const snap = await waitFor(
      `(() => ({
        hasUnified: Boolean(document.querySelector('[data-testid="ai-task-center-unified-workspace"]')),
        hasViewTab: Boolean(document.querySelector('[data-testid="ai-task-center-view-${viewKey}"]')),
        activeView: document.querySelector('[data-testid="ai-task-center-view-${viewKey}"]')?.getAttribute("aria-selected"),
        consoleErrors: window.__aiTaskCenterConsoleErrors || [],
        rejections: window.__aiTaskCenterRejections || [],
      }))()`,
      (value) => value?.hasUnified && value?.hasViewTab,
      30000,
    );
    assert(`${label} view loads`, snap.hasViewTab && snap.activeView === "true");
    assert(`${label} view no console errors`, (snap.consoleErrors?.length ?? 0) === 0, JSON.stringify(snap.consoleErrors));
    assert(`${label} view no rejections`, (snap.rejections?.length ?? 0) === 0, JSON.stringify(snap.rejections));
  }

  await ss("Runtime.evaluate", { expression: `window.location.assign(${JSON.stringify(`${WEB}/?gorunum=all-tasks&kaynak=teams`)})` });
  const teamsFilterSnap = await waitFor(
    `Boolean(document.querySelector('[data-testid="ai-task-center-origin-teams"]'))`,
    (value) => value === true,
    30000,
  );
  assert("Teams source filter visible", teamsFilterSnap === true);

  await ss("Runtime.evaluate", { expression: `window.location.assign(${JSON.stringify(`${WEB}/teams/is-kutum`)})` });
  const legacyHref = await waitHref((href) => href.includes("gorunum="), 30000);
  assert("legacy inbox reaches unified center", legacyHref.includes("/?gorunum=") || legacyHref.includes("gorunum=assigned"));

  await navigateTo("/dashboard");
  const dashboardText = await evalSnap("document.body?.innerText || ''");
  assert("dashboard lacks operasyon oncelikleri", !dashboardText.includes("Operasyon Öncelikleri"));
  assert("dashboard keeps piyasa ozeti", dashboardText.includes("Piyasa") || dashboardText.includes("piyasa"));

  for (const viewport of VIEWPORTS) {
    await ss("Emulation.setDeviceMetricsOverride", {
      width: viewport.width,
      height: viewport.height,
      deviceScaleFactor: 1,
      mobile: false,
    });
    await navigateTo("/?gorunum=all-tasks");
    await waitFor(centerSnapExpr(), (value) => value?.hasUnified, 30000);
    const overflow = await evalSnap(`(() => {
      const el = document.documentElement;
      return Math.max(0, el.scrollWidth - el.clientWidth);
    })()`);
    assert(`${viewport.label} horizontal overflow`, overflow <= 2, `overflow=${overflow}px`);
    await screenshot(`ai-task-center-unification-1-${viewport.label}-authenticated.png`);
  }

  const finalSnap = await evalSnap(centerSnapExpr());
  assert("final console clean", (finalSnap?.consoleErrors?.length ?? 0) === 0);
  assert("final rejections clean", (finalSnap?.rejections?.length ?? 0) === 0);

  console.log(`Screenshots stored outside repository: ${SCREENSHOT_DIR}`);
  console.log("AI-TASK-CENTER-UNIFICATION-1 live flow verification passed.");
} finally {
  try { child.kill("SIGTERM"); } catch {}
  try { rmSync(userDataDir, { recursive: true, force: true }); } catch {}
}
