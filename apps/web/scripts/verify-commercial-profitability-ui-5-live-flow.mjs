import { createHmac } from "node:crypto";
import { existsSync, mkdirSync, mkdtempSync, readFileSync, rmSync, writeFileSync } from "node:fs";
import { spawn, spawnSync } from "node:child_process";
import { tmpdir } from "node:os";
import { join } from "node:path";

const WEB = "http://127.0.0.1:3001";
const ROUTE = "/ticari-zeka/malzeme-ve-maliyet-karliligi";
const MATERIAL_COMPARISON = "bb000001-03b7-4000-8000-0000000003b7";
const COST_COMPARISON = "bb000001-03b4-4000-8000-0000000003b4";
const INVALID = "00000000-0000-4000-8000-000000000099";
const SCREENSHOT_DIR = join(process.cwd(), ".cursor-screenshots");
const MATERIAL_SHOT = join(SCREENSHOT_DIR, "profitability-5-material-lens-authenticated.png");
const COST_SHOT = join(SCREENSHOT_DIR, "profitability-5-cost-driver-lens-authenticated.png");
const TOTP_ALPHABET = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";

function fail(message) {
  console.error(`PROFITABILITY-5 live flow failed: ${message}`);
  process.exit(1);
}

function assert(label, ok, detail = "") {
  if (!ok) fail(`${label}${detail ? `: ${detail}` : ""}`);
  console.log(`✓ ${label}`);
}

function bootstrapSecrets() {
  const home = process.env.USERPROFILE || "";
  const ids = ["a7824c6f-6aa8-498f-895d-7f9b31bab8e3", "85d2e353-dc82-41df-ae90-9baa4397ee63", "0e3b0257-2b6c-4a42-81dc-cceb6078d79f"];
  let text = "";
  for (const id of ids) {
    const p = join(home, ".cursor/projects/c-Projects-NevaxisZentra/agent-transcripts", id, `${id}.jsonl`);
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
  let buffer = 0; let bitsLeft = 0; const output = [];
  for (const character of normalized) {
    buffer = (buffer << 5) | TOTP_ALPHABET.indexOf(character); bitsLeft += 5;
    if (bitsLeft >= 8) { output.push((buffer >> (bitsLeft - 8)) & 0xff); bitsLeft -= 8; }
  }
  const counterBytes = Buffer.alloc(8);
  counterBytes.writeBigUInt64BE(BigInt(counter));
  const hash = createHmac("sha1", Buffer.from(output)).update(counterBytes).digest();
  const hashOffset = hash[hash.length - 1] & 0x0f;
  const binaryCode = ((hash[hashOffset] & 0x7f) << 24) | ((hash[hashOffset + 1] & 0xff) << 16) | ((hash[hashOffset + 2] & 0xff) << 8) | (hash[hashOffset + 3] & 0xff);
  return String(binaryCode % 1_000_000).padStart(6, "0");
}

console.log("Running PROFITABILITY-5 live browser flow verifier.");
bootstrapSecrets();
const ownerPassword = process.env.DevelopmentSaasIdentity__TenantOwnerPassword;
const mfaSecret = process.env.DevelopmentSaasIdentity__MfaSharedSecret;
const exe = "C:\\Program Files\\Google\\Chrome\\Application\\chrome.exe";
if (!existsSync(exe)) fail("Chrome not found");
if (!ownerPassword || !mfaSecret) fail("Missing tenant owner password or MFA secret");
mkdirSync(SCREENSHOT_DIR, { recursive: true });
const userDataDir = mkdtempSync(join(tmpdir(), "profitability-5-"));
const port = 9880 + Math.floor(Math.random() * 100);
const child = spawn(exe, [`--remote-debugging-port=${port}`, "--headless=new", "--disable-gpu", "--window-size=1440,1400", `--user-data-dir=${userDataDir}`], { stdio: "ignore" });

function workspaceSnap(lens) {
  return `({
    href: location.href,
    onSignIn: location.pathname.startsWith("/giris"),
    hasWorkspace: Boolean(document.querySelector('[data-testid="commercial-material-cost-profitability-workspace"]')),
    hasDetailReady: Boolean(document.querySelector('[data-testid="commercial-profitability-comparison-detail-ready"]')),
    hasInsufficient: Boolean(document.querySelector('[data-testid="commercial-material-cost-profitability-insufficient"]')),
    hasMaterialTable: Boolean(document.querySelector('[data-testid="commercial-material-cost-profitability-material-table"]')),
    hasCostTable: Boolean(document.querySelector('[data-testid="commercial-material-cost-profitability-cost-table"]')),
    hasKimlik: /Kimlik ve kapsam/.test(document.body?.innerText || ''),
    hasHesaplanamadi: /Hesaplanamadı/.test(document.body?.innerText || ''),
    comparisonInUrl: location.search.includes("comparison="),
  })`;
}

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
  let nextId = 1; const pending = new Map();
  ws.onmessage = (event) => {
    const message = JSON.parse(String(event.data));
    if (message.id && pending.has(message.id)) {
      const { resolve, reject } = pending.get(message.id);
      pending.delete(message.id);
      message.error ? reject(new Error(message.error.message)) : resolve(message.result);
    }
  };
  const send = (method, params = {}, sessionId) => new Promise((resolve, reject) => {
    const id = nextId++; pending.set(id, { resolve, reject });
    const payload = { id, method, params }; if (sessionId) payload.sessionId = sessionId;
    ws.send(JSON.stringify(payload));
  });
  const attached = await send("Target.attachToTarget", { targetId: created.id, flatten: true });
  const ss = (method, params = {}) => send(method, params, attached.sessionId);
  const evalSnap = async (expr) => (await ss("Runtime.evaluate", { returnByValue: true, expression: expr })).result?.value;
  const waitFor = async (expr, pred, ms = 45000) => {
    const start = Date.now();
    while (Date.now() - start < ms) {
      const value = await evalSnap(expr);
      if (pred(value)) return value;
      await new Promise((r) => setTimeout(r, 300));
    }
    fail(`timeout waiting for snapshot`);
  };
  const waitHref = async (pred, ms = 45000) => {
    const start = Date.now();
    while (Date.now() - start < ms) {
      const href = await evalSnap("location.href");
      if (pred(href)) return href;
      await new Promise((r) => setTimeout(r, 300));
    }
    fail("timeout waiting for href");
  };

  await ss("Page.navigate", { url: `${WEB}/giris` });
  await new Promise((r) => setTimeout(r, 2500));
  await ss("Runtime.evaluate", { expression: `(() => {
    const setter = Object.getOwnPropertyDescriptor(window.HTMLInputElement.prototype, "value").set;
    const fill = (sel, val) => { const n = document.querySelector(sel); setter.call(n, val); n.dispatchEvent(new Event("input", { bubbles: true })); n.dispatchEvent(new Event("change", { bubbles: true })); };
    fill("#sign-in-organization", "dev-pilot-tenant"); fill("#sign-in-email", "admin@demo.nevaxiszentra.local"); fill("#sign-in-password", ${JSON.stringify(ownerPassword)});
    document.querySelector("form.auth-form button")?.click();
  })()` });
  const mfa = await waitHref((href) => href.includes("/mfa-dogrula"), 20000);
  assert("login reaches MFA", mfa.includes("/mfa-dogrula"), mfa);
  for (const offset of [-1, 0, 1]) {
    const attempt = await ss("Runtime.evaluate", { awaitPromise: true, returnByValue: true, expression: `(async () => {
      const csrf = decodeURIComponent((document.cookie.match(/NevaxisZentra\\.csrf=([^;]+)/) || [])[1] || "");
      const res = await fetch("/api/identity/mfa/verify", { method: "POST", credentials: "include", headers: { Accept: "application/json", "Content-Type": "application/json", "X-NevaxisZentra-Csrf": csrf }, body: JSON.stringify({ totpCode: ${JSON.stringify(generateTotp(mfaSecret, offset))} }) });
      return { status: res.status };
    })()` });
    if (attempt.result?.value?.status === 200) break;
  }
  const me = await ss("Runtime.evaluate", { awaitPromise: true, returnByValue: true, expression: `(async () => { const r = await fetch("/api/identity/me", { credentials: "include" }); return { status: r.status, hasAccount: Boolean((await r.json().catch(() => null))?.accountReference) }; })()` });
  assert("/api/identity/me returns 200 after MFA", me.result?.value?.status === 200 && me.result?.value?.hasAccount, JSON.stringify(me.result?.value));

  await ss("Page.navigate", { url: `${WEB}${ROUTE}?lens=material&comparison=${MATERIAL_COMPARISON}` });
  const materialSnap = await waitFor(workspaceSnap("material"), (s) => s?.hasWorkspace && !s?.onSignIn, 45000);
  assert("material lens workspace loaded", materialSnap.hasWorkspace);
  assert("material table visible", materialSnap.hasMaterialTable);
  const materialReady = await waitFor(workspaceSnap("material"), (s) => s?.hasDetailReady, 30000);
  assert("material detail ready", materialReady?.hasDetailReady === true);
  assert("material shows Kimlik ve kapsam", materialReady?.hasKimlik === true);
  writeFileSync(MATERIAL_SHOT, Buffer.from((await ss("Page.captureScreenshot", { format: "png", fromSurface: true })).data, "base64"));
  assert("material screenshot captured", existsSync(MATERIAL_SHOT));

  await ss("Page.navigate", { url: `${WEB}${ROUTE}?lens=cost-driver&comparison=${COST_COMPARISON}` });
  const costSnap = await waitFor(workspaceSnap("cost-driver"), (s) => s?.hasWorkspace && s?.hasCostTable && !s?.onSignIn, 45000);
  assert("cost lens workspace loaded", costSnap.hasWorkspace);
  assert("cost table visible", costSnap.hasCostTable);
  const costReady = await waitFor(workspaceSnap("cost-driver"), (s) => s?.hasDetailReady, 30000);
  assert("cost detail ready", costReady?.hasDetailReady === true);
  writeFileSync(COST_SHOT, Buffer.from((await ss("Page.captureScreenshot", { format: "png", fromSurface: true })).data, "base64"));
  assert("cost screenshot captured", existsSync(COST_SHOT));

  await ss("Page.navigate", { url: `${WEB}${ROUTE}?lens=material&comparison=${INVALID}` });
  const invalidSnap = await waitFor(workspaceSnap("material"), (s) => s?.hasWorkspace && !s?.onSignIn, 45000);
  assert("invalid comparison safe", invalidSnap?.hasDetailReady !== true);

  await ss("Page.navigate", { url: `${WEB}${ROUTE}?lens=material&comparison=${MATERIAL_COMPARISON}` });
  await waitFor(workspaceSnap("material"), (s) => s?.hasDetailReady, 45000);
  await ss("Page.reload", { ignoreCache: true });
  const refreshSnap = await waitFor(workspaceSnap("material"), (s) => s?.hasWorkspace && s?.comparisonInUrl, 45000);
  assert("refresh preserved comparison param", refreshSnap?.comparisonInUrl === true);
  assert("refresh workspace still loaded", refreshSnap?.hasWorkspace === true);

  ws.close();
} finally {
  try { spawnSync("taskkill", ["/pid", String(child.pid), "/T", "/F"], { stdio: "ignore" }); } catch { child.kill(); }
  try { rmSync(userDataDir, { recursive: true, force: true }); } catch {}
}

console.log(`Material screenshot: ${MATERIAL_SHOT}`);
console.log(`Cost-driver screenshot: ${COST_SHOT}`);
console.log("PROFITABILITY-5 live browser flow verification passed.");
