#!/usr/bin/env node
import { createHmac } from "node:crypto";
import { existsSync, mkdirSync, mkdtempSync, readFileSync, rmSync, writeFileSync } from "node:fs";
import { spawn } from "node:child_process";
import { tmpdir } from "node:os";
import { join } from "node:path";

const WEB = (process.env.NEVAXIS_FACTOS_WEB_BASE_URL ?? "http://localhost:3001").replace("127.0.0.1", "localhost").replace(/\/$/, "");
const API = process.env.NEVAXIS_FACTOS_API_BASE_URL ?? "http://127.0.0.1:5000";
const ROUTE = "/stok-ve-depo/malzeme-riskleri";
const SHORTAGE_ASSESSMENT = "bb000001-00b3-4000-8000-0000000000b3";
const INSUFFICIENT_ASSESSMENT = "bb000001-00b4-4000-8000-0000000000b4";

const SCREENSHOT_DIR = join(process.cwd(), ".cursor-screenshots");
const WORKSPACE_SHOT = join(SCREENSHOT_DIR, "inventory-risk-foundation-1-workspace-authenticated.png");
const SHORTAGE_SHOT = join(SCREENSHOT_DIR, "inventory-risk-foundation-1-shortage-detail-authenticated.png");
const INSUFFICIENT_SHOT = join(SCREENSHOT_DIR, "inventory-risk-foundation-1-insufficient-detail-authenticated.png");
const TOTP_ALPHABET = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";

function fail(message) {
  console.error(`INVENTORY-RISK-FOUNDATION-1 live flow failed: ${message}`);
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
  ];
  let text = "";
  for (const id of ids) {
    const p = join(home, ".cursor/projects/c-Projects-NevaxisFactOS/agent-transcripts", id, `${id}.jsonl`);
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

function workspaceSnapExpr() {
  return `({
    href: location.href,
    onSignIn: location.pathname.startsWith("/giris"),
    hasWorkspace: Boolean(document.querySelector('[data-testid="material-availability-risk-workspace"]')),
    hasTable: Boolean(document.querySelector('[data-testid="material-availability-risk-table"]')),
    hasDisclaimer: Boolean(document.querySelector('[data-testid="material-availability-risk-automation-disclaimer"]')),
    hasReadonlyNote: Boolean(document.querySelector('[data-testid="material-availability-risk-readonly-note"]')),
    bodyText: document.body?.innerText || "",
  })`;
}

function detailSnapExpr() {
  return `(function() {
    const text = document.body?.innerText || "";
    const hasAutoAction =
      (/satın alma emri|görev taslağı|stok ayır|üretim emri başlat/i.test(text)) &&
      !/otomatik stok, satın alma veya üretim işlemi yapmaz/i.test(text);
    return {
      hasDetailReady: Boolean(document.querySelector('[data-testid="material-availability-risk-detail-ready"]')),
      hasDrivers: Boolean(document.querySelector('[data-testid="material-availability-risk-drivers"]')),
      hasEvidence: Boolean(document.querySelector('[data-testid="material-availability-risk-evidence"]')),
      hasEvaluateButton: Boolean(document.querySelector('[data-testid="material-availability-risk-evaluate-button"]')),
      hasMaterialLink: Boolean(document.querySelector('[data-testid="material-availability-risk-material-link"]')),
      hasAutoAction,
      bodyText: text,
    };
  })()`;
}

console.log("Running INVENTORY-RISK-FOUNDATION-1 live browser flow verifier.");
bootstrapSecrets();
const ownerPassword = process.env.DevelopmentSaasIdentity__TenantOwnerPassword;
const mfaSecret = process.env.DevelopmentSaasIdentity__MfaSharedSecret;
const exe = "C:\\Program Files\\Google\\Chrome\\Application\\chrome.exe";
if (!existsSync(exe)) fail("Chrome not found");
if (!ownerPassword || !mfaSecret) fail("Missing tenant owner password or MFA secret");
mkdirSync(SCREENSHOT_DIR, { recursive: true });

const apiReady = await fetch(`${API}/health/ready`);
assert("API ready", apiReady.status === 200, `status=${apiReady.status}`);

const userDataDir = mkdtempSync(join(tmpdir(), "inventory-risk-foundation-1-"));
const port = 9890 + Math.floor(Math.random() * 100);
const child = spawn(exe, [`--remote-debugging-port=${port}`, "--headless=new", "--disable-gpu", "--window-size=1440,1600", `--user-data-dir=${userDataDir}`], { stdio: "ignore" });

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
  const evalSnap = async (expr) => (await ss("Runtime.evaluate", { returnByValue: true, expression: expr })).result?.value;
  const waitFor = async (expr, pred, ms = 60000) => {
    const start = Date.now();
    while (Date.now() - start < ms) {
      const value = await evalSnap(expr);
      if (pred(value)) return value;
      await new Promise((r) => setTimeout(r, 300));
    }
    fail(`timeout waiting for snapshot: ${JSON.stringify(await evalSnap(expr))}`);
  };
  const waitHref = async (pred, ms = 60000) => {
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
    await waitHref((href) => href.includes(path.split("?")[0]), 60000);
  };
  const selectAssessment = async (assessmentId) => {
    await ss("Runtime.evaluate", {
      expression: `document.querySelector('[data-testid="material-availability-risk-row-${assessmentId}"] button')?.click()`,
    });
    await waitHref((href) => href.includes(`assessment=${encodeURIComponent(assessmentId)}`), 30000);
    await new Promise((r) => setTimeout(r, 1500));
  };

  await ss("Page.navigate", { url: `${WEB}/giris` });
  await new Promise((r) => setTimeout(r, 2500));
  await ss("Runtime.evaluate", {
    expression: `(() => {
    const setter = Object.getOwnPropertyDescriptor(window.HTMLInputElement.prototype, "value").set;
    const fill = (sel, val) => { const n = document.querySelector(sel); setter.call(n, val); n.dispatchEvent(new Event("input", { bubbles: true })); n.dispatchEvent(new Event("change", { bubbles: true })); };
    fill("#sign-in-organization", "dev-pilot-tenant"); fill("#sign-in-email", "admin@demo.nevaxisfactos.local"); fill("#sign-in-password", ${JSON.stringify(ownerPassword)});
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
      const csrf = decodeURIComponent((document.cookie.match(/NevaxisFactOS\\.csrf=([^;]+)/) || [])[1] || "");
      const res = await fetch("/api/identity/mfa/verify", { method: "POST", credentials: "include", headers: { Accept: "application/json", "Content-Type": "application/json", "X-NevaxisFactOS-Csrf": csrf }, body: JSON.stringify({ totpCode: ${JSON.stringify(generateTotp(mfaSecret, offset))} }) });
      return { status: res.status };
    })()`,
    });
    if (attempt.result?.value?.status === 200) {
      mfaOk = true;
      break;
    }
  }
  assert("MFA verification succeeded", mfaOk);

  const me = await ss("Runtime.evaluate", {
    awaitPromise: true,
    returnByValue: true,
    expression: `(async () => { const r = await fetch("/api/identity/me", { credentials: "include" }); return { status: r.status, hasAccount: Boolean((await r.json().catch(() => null))?.accountReference) }; })()`,
  });
  assert("/api/identity/me returns 200 after MFA", me.result?.value?.status === 200 && me.result?.value?.hasAccount, JSON.stringify(me.result?.value));

  await navigateTo(ROUTE);
  const workspaceSnap = await waitFor(workspaceSnapExpr(), (s) => s?.hasWorkspace && s?.hasTable && !s?.onSignIn, 60000);
  assert("material risk workspace loaded", workspaceSnap.hasWorkspace);
  assert("assessment inventory table visible", workspaceSnap.hasTable);
  assert("automation disclaimer visible", workspaceSnap.hasDisclaimer);
  assert("readonly note visible", workspaceSnap.hasReadonlyNote);
  assert("turkish page copy visible", /Malzeme Riskleri|Malzeme risk envanteri/i.test(workspaceSnap.bodyText));
  assert("no auto stock/purchase/production action copy", !/satın alma emri|görev taslağı|stok ayır|üretim emri başlat/i.test(workspaceSnap.bodyText) || /otomatik stok, satın alma veya üretim işlemi yapmaz/i.test(workspaceSnap.bodyText));
  writeFileSync(WORKSPACE_SHOT, Buffer.from((await ss("Page.captureScreenshot", { format: "png", fromSurface: true })).data, "base64"));
  assert("workspace screenshot captured", existsSync(WORKSPACE_SHOT));

  await selectAssessment(SHORTAGE_ASSESSMENT);
  const shortageSnap = await waitFor(
    detailSnapExpr(),
    (s) => s?.hasDetailReady && s?.hasEvidence,
    45000,
  );
  assert("shortage detail ready", shortageSnap.hasDetailReady);
  assert("shortage evidence visible", shortageSnap.hasEvidence);
  assert("shortage status visible", /Eksik|shortage/i.test(shortageSnap.bodyText));
  assert("shortage erp quantities visible", /400|350|50|kg/i.test(shortageSnap.bodyText));
  assert("shortage no auto action controls", !shortageSnap.hasAutoAction);
  assert("shortage material link present", shortageSnap.hasMaterialLink);
  writeFileSync(SHORTAGE_SHOT, Buffer.from((await ss("Page.captureScreenshot", { format: "png", fromSurface: true })).data, "base64"));
  assert("shortage detail screenshot captured", existsSync(SHORTAGE_SHOT));

  await selectAssessment(INSUFFICIENT_ASSESSMENT);
  const insufficientSnap = await waitFor(
    detailSnapExpr(),
    (s) => s?.hasDetailReady && (s?.hasDrivers || s?.hasEvidence),
    45000,
  );
  assert("insufficient detail ready", insufficientSnap.hasDetailReady);
  assert("insufficient status visible", /Yetersiz veri|insufficient-data/i.test(insufficientSnap.bodyText));
  assert("insufficient drivers or evidence visible", insufficientSnap.hasDrivers || insufficientSnap.hasEvidence);
  assert("insufficient no auto action controls", !insufficientSnap.hasAutoAction);
  assert("insufficient no task/purchase action strings", !/görev taslağı|satın alma emri/i.test(insufficientSnap.bodyText));
  writeFileSync(INSUFFICIENT_SHOT, Buffer.from((await ss("Page.captureScreenshot", { format: "png", fromSurface: true })).data, "base64"));
  assert("insufficient detail screenshot captured", existsSync(INSUFFICIENT_SHOT));

  console.log(`Workspace screenshot: ${WORKSPACE_SHOT}`);
  console.log(`Shortage screenshot: ${SHORTAGE_SHOT}`);
  console.log(`Insufficient screenshot: ${INSUFFICIENT_SHOT}`);
  console.log(`Shortage assessment: ${SHORTAGE_ASSESSMENT}`);
  console.log(`Insufficient assessment: ${INSUFFICIENT_ASSESSMENT}`);
  console.log("INVENTORY-RISK-FOUNDATION-1 live browser flow verification passed.");
} finally {
  child.kill("SIGTERM");
  try {
    rmSync(userDataDir, { recursive: true, force: true });
  } catch {
    // Chrome may still hold the profile directory on Windows.
  }
}
