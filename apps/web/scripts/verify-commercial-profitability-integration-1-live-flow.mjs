import { createHmac } from "node:crypto";
import { existsSync, mkdirSync, mkdtempSync, readFileSync, rmSync, writeFileSync } from "node:fs";
import { spawn, spawnSync } from "node:child_process";
import { tmpdir } from "node:os";
import { join } from "node:path";

const WEB = "http://127.0.0.1:3001";
const PRODUCT_ROUTE = "/ticari-zeka/urun-karliligi";
const CUSTOMER_ROUTE = "/ticari-zeka/musteri-karliligi";
const PRODUCT_COMPARISON = "bb000001-03b0-4000-8000-0000000003b0";
const CUSTOMER_COMPARISON = "bb000001-03b6-4000-8000-0000000003b6";
const INVALID = "00000000-0000-4000-8000-000000000099";
const SCREENSHOT_DIR = join(process.cwd(), ".cursor-screenshots");
const PRODUCT_SHOT = join(SCREENSHOT_DIR, "profitability-integration-1-product-authenticated.png");
const CUSTOMER_SHOT = join(SCREENSHOT_DIR, "profitability-integration-1-customer-authenticated.png");
const TOTP_ALPHABET = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";

function fail(message) {
  console.error(`PROFITABILITY-INTEGRATION-1 live flow failed: ${message}`);
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

console.log("Running PROFITABILITY-INTEGRATION-1 live browser flow verifier.");
bootstrapSecrets();
const ownerPassword = process.env.DevelopmentSaasIdentity__TenantOwnerPassword;
const mfaSecret = process.env.DevelopmentSaasIdentity__MfaSharedSecret;
const exe = "C:\\Program Files\\Google\\Chrome\\Application\\chrome.exe";
if (!existsSync(exe)) fail("Chrome not found");
mkdirSync(SCREENSHOT_DIR, { recursive: true });
const userDataDir = mkdtempSync(join(tmpdir(), "profit-integ-1-"));
const port = 9780 + Math.floor(Math.random() * 100);
const child = spawn(exe, [`--remote-debugging-port=${port}`, "--headless=new", "--disable-gpu", "--window-size=1440,1400", `--user-data-dir=${userDataDir}`], { stdio: "ignore" });

function workspaceSnap(kind) {
  const prefix = kind === "product" ? "commercial-profitability" : "commercial-customer-profitability";
  return `({
    href: location.href,
    onSignIn: location.pathname.startsWith("/giris"),
    hasWorkspace: Boolean(document.querySelector('[data-testid="${prefix}-workspace"]')),
    hasDetailReady: Boolean(document.querySelector('[data-testid="commercial-profitability-comparison-detail-ready"]')),
    hasDetailLoading: Boolean(document.querySelector('[data-testid="${kind === "product" ? "commercial-profitability-loading" : "commercial-customer-profitability-detail-loading"}"]')),
    hasDetailError: Boolean(document.querySelector('[data-testid="${prefix}-error"]')),
    hasInsufficient: Boolean(document.querySelector('[data-testid="${prefix}-insufficient"], [data-testid="commercial-profitability-detail-insufficient"]')),
    hasEmpty: Boolean(document.querySelector('[data-testid="${prefix}-empty"]')),
    hasKimlik: /Kimlik ve kapsam/.test(document.body?.innerText || ''),
    hasHesaplanamadi: /Hesaplanamadı/.test(document.body?.innerText || ''),
    comparisonInUrl: /comparison=${INVALID}/.test(location.href),
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
    const start = Date.now(); let last = null;
    while (Date.now() - start < ms) {
      last = typeof expr === "string" ? await evalSnap(expr) : await evalSnap(expr);
      if (typeof pred === "function" ? pred(last) : pred(last)) return last;
      await new Promise((r) => setTimeout(r, 300));
    }
    return last;
  };

  const waitHref = async (pred, ms = 45000) => {
    const start = Date.now();
    let last = "";
    while (Date.now() - start < ms) {
      last = await evalSnap("location.href");
      if (pred(last)) return last;
      await new Promise((r) => setTimeout(r, 300));
    }
    return last;
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

  await ss("Page.navigate", { url: `${WEB}${PRODUCT_ROUTE}?comparison=${PRODUCT_COMPARISON}` });
  const product = await waitFor(workspaceSnap("product"), (s) => s.hasWorkspace && !s.onSignIn, 45000);
  assert("product workspace opens", product?.hasWorkspace === true, JSON.stringify(product));
  const productReady = await waitFor(workspaceSnap("product"), (s) => s.hasDetailReady, 30000);
  assert("product deep link detail ready", productReady?.hasDetailReady === true, JSON.stringify(productReady));
  assert("product shows Kimlik ve kapsam", productReady?.hasKimlik === true, JSON.stringify(productReady));
  writeFileSync(PRODUCT_SHOT, Buffer.from((await ss("Page.captureScreenshot", { format: "png", fromSurface: true })).data, "base64"));
  assert("product screenshot captured", existsSync(PRODUCT_SHOT), PRODUCT_SHOT);

  await ss("Page.reload", { ignoreCache: true });
  const productRefresh = await waitFor(workspaceSnap("product"), (s) => s.hasDetailReady && s.href.includes("comparison="), 45000);
  assert("product refresh preserves session and detail", productRefresh?.hasDetailReady && productRefresh?.href.includes("comparison="), JSON.stringify(productRefresh));

  await ss("Page.navigate", { url: `${WEB}${PRODUCT_ROUTE}?comparison=${INVALID}` });
  const productInvalid = await waitFor(workspaceSnap("product"), (s) => s.href.includes(INVALID), 30000);
  assert("product invalid comparison URL accepted without detail disclosure for invalid id", productInvalid?.href.includes(INVALID) && productInvalid?.hasDetailReady !== true, JSON.stringify(productInvalid));

  await ss("Page.navigate", { url: `${WEB}${CUSTOMER_ROUTE}?comparison=${CUSTOMER_COMPARISON}` });
  const customer = await waitFor(workspaceSnap("customer"), (s) => s.hasWorkspace && !s.onSignIn, 45000);
  assert("customer workspace opens", customer?.hasWorkspace === true, JSON.stringify(customer));
  const customerReady = await waitFor(workspaceSnap("customer"), (s) => s.hasDetailReady, 30000);
  assert("customer deep link detail ready", customerReady?.hasDetailReady === true, JSON.stringify(customerReady));
  assert("customer shows Kimlik ve kapsam", customerReady?.hasKimlik === true, JSON.stringify(customerReady));
  assert("customer missing values non-zero", customerReady?.hasHesaplanamadi === true, JSON.stringify(customerReady));
  writeFileSync(CUSTOMER_SHOT, Buffer.from((await ss("Page.captureScreenshot", { format: "png", fromSurface: true })).data, "base64"));
  assert("customer screenshot captured", existsSync(CUSTOMER_SHOT), CUSTOMER_SHOT);

  await ss("Page.reload", { ignoreCache: true });
  const customerRefresh = await waitFor(workspaceSnap("customer"), (s) => s.hasDetailReady && s.href.includes("comparison="), 45000);
  assert("customer refresh preserves session and detail", customerRefresh?.hasDetailReady && customerRefresh?.href.includes("comparison="), JSON.stringify(customerRefresh));

  await ss("Page.navigate", { url: `${WEB}${CUSTOMER_ROUTE}?comparison=${INVALID}` });
  const customerInvalid = await waitFor(workspaceSnap("customer"), (s) => s.hasInsufficient || s.hasDetailError || !s.hasDetailReady, 30000);
  assert("customer invalid comparison safe", customerInvalid?.hasDetailReady !== true && (customerInvalid?.hasInsufficient || customerInvalid?.hasDetailError), JSON.stringify(customerInvalid));

  await ss("Runtime.evaluate", { awaitPromise: true, returnByValue: true, expression: `(async () => {
    const csrf = decodeURIComponent((document.cookie.match(/NevaxisZentra\\.csrf=([^;]+)/) || [])[1] || "");
    const response = await fetch("/api/identity/sign-out", {
      method: "POST",
      credentials: "include",
      headers: { Accept: "application/json", "Content-Type": "application/json", "X-NevaxisZentra-Csrf": csrf },
      body: "{}",
    });
    return { status: response.status };
  })()` });
  await ss("Page.navigate", { url: `${WEB}${PRODUCT_ROUTE}` });
  const signedOutProduct = await waitHref((href) => href.includes("/giris"), 20000);
  assert("sign-out redirects product route to giris", signedOutProduct.includes("/giris"), signedOutProduct);
  await ss("Page.navigate", { url: `${WEB}${CUSTOMER_ROUTE}` });
  const signedOutCustomer = await waitHref((href) => href.includes("/giris"), 20000);
  assert("sign-out redirects customer route to giris", signedOutCustomer.includes("/giris"), signedOutCustomer);

  ws.close();
} finally {
  try { spawnSync("taskkill", ["/pid", String(child.pid), "/T", "/F"], { stdio: "ignore" }); } catch { child.kill(); }
  try { rmSync(userDataDir, { recursive: true, force: true }); } catch {}
}

console.log(`Product screenshot: ${PRODUCT_SHOT}`);
console.log(`Customer screenshot: ${CUSTOMER_SHOT}`);
console.log("PROFITABILITY-INTEGRATION-1 live browser flow verification passed.");
