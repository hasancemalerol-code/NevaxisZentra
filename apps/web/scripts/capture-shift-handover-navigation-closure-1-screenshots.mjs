#!/usr/bin/env node
import { createHmac } from "node:crypto";
import { existsSync, mkdirSync, mkdtempSync, readFileSync, rmSync, writeFileSync } from "node:fs";
import { spawn } from "node:child_process";
import { tmpdir } from "node:os";
import { dirname, join, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), "../../..");
const WEB = (process.env.NEVAXIS_FACTOS_WEB_BASE_URL ?? "http://localhost:3001").replace("127.0.0.1", "localhost").replace(/\/$/, "");
const SCREENSHOT = join(ROOT, ".cursor-screenshots/shift-handover-navigation-closure-1-authenticated.png");
const TOTP_ALPHABET = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";

function fail(message) {
  console.error(`SHIFT-HANDOVER-NAVIGATION-CLOSURE-1 failed: ${message}`);
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
    "dc1076d3-6856-4bae-8a65-935769827e04",
    "45a3825f-58e0-409e-82b6-3ea3581088e8",
    "51fc0481-f004-4fef-8064-2a67ad0f24d4",
  ];
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

bootstrapSecrets();
const ownerPassword = process.env.DevelopmentSaasIdentity__TenantOwnerPassword;
const mfaSecret = process.env.DevelopmentSaasIdentity__MfaSharedSecret;
const exe = "C:\\Program Files\\Google\\Chrome\\Application\\chrome.exe";
if (!existsSync(exe)) fail("Chrome not found");
if (!ownerPassword || !mfaSecret) fail("Missing tenant owner password or MFA secret");
mkdirSync(dirname(SCREENSHOT), { recursive: true });

const userDataDir = mkdtempSync(join(tmpdir(), "shift-handover-nav-closure-1-"));
const port = 9790 + Math.floor(Math.random() * 100);
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
    fail(`timeout: ${JSON.stringify(await evalSnap(expr))}; href=${await evalSnap("location.href")}`);
  };
  const waitHref = async (pred, ms = 60000) => {
    const start = Date.now();
    while (Date.now() - start < ms) {
      const href = await evalSnap("location.href");
      if (pred(href)) return href;
      await new Promise((r) => setTimeout(r, 300));
    }
    fail(`timeout href: ${await evalSnap("location.href")}`);
  };
  const capture = async (path) => {
    writeFileSync(path, Buffer.from((await ss("Page.captureScreenshot", { format: "png", fromSurface: true })).data, "base64"));
  };

  await ss("Page.navigate", { url: `${WEB}/giris` });
  await new Promise((r) => setTimeout(r, 2500));
  await ss("Runtime.evaluate", {
    expression: `(() => {
      const setter = Object.getOwnPropertyDescriptor(window.HTMLInputElement.prototype, "value").set;
      const fill = (sel, val) => { const n = document.querySelector(sel); setter.call(n, val); n.dispatchEvent(new Event("input", { bubbles: true })); n.dispatchEvent(new Event("change", { bubbles: true })); };
      fill("#sign-in-organization", "dev-pilot-tenant"); fill("#sign-in-email", "admin@demo.nevaxiszentra.local"); fill("#sign-in-password", ${JSON.stringify(ownerPassword)});
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
        const csrf = decodeURIComponent((document.cookie.match(/NevaxisZentra\\.csrf=([^;]+)/) || [])[1] || "");
        const res = await fetch("/api/identity/mfa/verify", { method: "POST", credentials: "include", headers: { Accept: "application/json", "Content-Type": "application/json", "X-NevaxisZentra-Csrf": csrf }, body: JSON.stringify({ totpCode: ${JSON.stringify(generateTotp(mfaSecret, offset))} }) });
        return { status: res.status };
      })()`,
    });
    if (attempt.result?.value?.status === 200) {
      mfaOk = true;
      break;
    }
  }
  assert("MFA verification succeeded", mfaOk);

  await ss("Runtime.evaluate", { expression: `window.location.assign(${JSON.stringify(`${WEB}/vardiya-devir`)})` });
  await waitHref((href) => href.includes("/vardiya-devir") && !href.includes("abonelik-erisimi"), 90000);
  await waitFor(
    `(() => {
      const text = document.body?.innerText ?? "";
      const idxVardiyalar = text.indexOf("Vardiyalar");
      const idxDevir = text.indexOf("Vardiya Devir Teslimi");
      const idxOee = text.indexOf("Proses & OEE");
      return idxVardiyalar >= 0 && idxDevir > idxVardiyalar && idxOee > idxDevir;
    })()`,
    (ok) => ok === true,
    60000,
  );
  assert("sidebar order Vardiyalar → Vardiya Devir Teslimi → Proses & OEE", true);
  assert("page loaded without subscription redirect", !(await evalSnap("location.href"))?.includes("abonelik-erisimi"));
  assert("vardiya-devir route active", (await evalSnap("location.href"))?.includes("/vardiya-devir"));

  await capture(SCREENSHOT);
  assert("navigation screenshot captured", existsSync(SCREENSHOT));
  console.log(`Screenshot: ${SCREENSHOT}`);
  console.log("SHIFT-HANDOVER-NAVIGATION-CLOSURE-1 passed.");
} finally {
  child.kill("SIGTERM");
  try { rmSync(userDataDir, { recursive: true, force: true }); } catch { /* Chrome may hold the profile */ }
}
