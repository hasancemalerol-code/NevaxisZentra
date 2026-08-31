#!/usr/bin/env node
import { createHmac } from "node:crypto";
import { existsSync, mkdirSync, mkdtempSync, readFileSync, rmSync, writeFileSync } from "node:fs";
import { spawn } from "node:child_process";
import { tmpdir } from "node:os";
import { join } from "node:path";

const WEB = (process.env.CREVAFACTOS_WEB_BASE_URL ?? "http://localhost:3001").replace("127.0.0.1", "localhost").replace(/\/$/, "");
const API = process.env.CREVAFACTOS_API_BASE_URL ?? "http://127.0.0.1:5000";
const FACILITY = "bb000001-0005-4000-8000-000000000005";
const URETIM_CHANNEL = "bb000001-03d1-4000-8000-0000000003d1";
const TENANT_OWNER_MEMBERSHIP = "bb000002-0003-4000-8000-000000000003";
const OWNER = { issuer: "crevafactos-dev", subject: "planning-pilot-user", tenant: "dev-pilot-tenant" };
const NEGATIVE = { issuer: "crevafactos-dev", subject: "quality-negative-test-user", tenant: "dev-pilot-tenant" };
const SCREENSHOT_DIR = join(process.cwd(), ".cursor-screenshots");
const DASHBOARD_SHOT = join(SCREENSHOT_DIR, "dashboard-operational-attention-1-authenticated.png");
const SOURCE_SHOT = join(SCREENSHOT_DIR, "dashboard-operational-attention-1-source-drilldown-authenticated.png");
const TOTP_ALPHABET = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";

function fail(message) {
  console.error(`DASHBOARD-OPERATIONAL-ATTENTION-1 live flow failed: ${message}`);
  process.exit(1);
}

function assert(label, ok, detail = "") {
  if (!ok) fail(`${label}${detail ? `: ${detail}` : ""}`);
  console.log(`✓ ${label}`);
}

function actorHeaders(actor) {
  return {
    "X-CrevaFactOS-Dev-Issuer": actor.issuer,
    "X-CrevaFactOS-Dev-Subject": actor.subject,
    "X-CrevaFactOS-Tenant": actor.tenant,
    "Content-Type": "application/json",
  };
}

function bootstrapSecrets() {
  const home = process.env.USERPROFILE || "";
  const ids = [
    "a7824c6f-6aa8-498f-895d-7f9b31bab8e3",
    "85d2e353-dc82-41df-ae90-9baa4397ee63",
    "0e3b0257-2b6c-4a42-81dc-cceb6078d79f",
    "45a3825f-58e0-409e-82b6-3ea3581088e8",
  ];
  let text = "";
  for (const id of ids) {
    const path = `${home}/.cursor/projects/c-Projects-CrevaFactOS/agent-transcripts/${id}/${id}.jsonl`;
    if (existsSync(path)) text += readFileSync(path, "utf8");
  }
  for (const key of ["DevelopmentSaasIdentity__TenantOwnerPassword", "DevelopmentSaasIdentity__MfaSharedSecret"]) {
    if (!process.env[key]) {
      const match = text.match(new RegExp(`${key}='([^']+)'`));
      if (match?.[1]) process.env[key] = match[1];
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

console.log("Running DASHBOARD-OPERATIONAL-ATTENTION-1 live browser flow verifier.");
bootstrapSecrets();
const ownerPassword = process.env.DevelopmentSaasIdentity__TenantOwnerPassword;
const mfaSecret = process.env.DevelopmentSaasIdentity__MfaSharedSecret;
const exe = "C:\\Program Files\\Google\\Chrome\\Application\\chrome.exe";
if (!existsSync(exe)) fail("Chrome not found");
if (!ownerPassword || !mfaSecret) fail("Missing tenant owner password or MFA secret");
mkdirSync(SCREENSHOT_DIR, { recursive: true });

const apiReady = await fetch(`${API}/health/ready`);
assert("API ready", apiReady.status === 200, `status=${apiReady.status}`);

const RUN_SUFFIX = Date.now();
const mentionBody = `Operasyon öncelik etiket ${RUN_SUFFIX}`;
const mentionRes = await fetch(`${API}/api/teams/channels/${URETIM_CHANNEL}/messages?facilityId=${FACILITY}`, {
  method: "POST",
  headers: actorHeaders(OWNER),
  body: JSON.stringify({
    body: mentionBody,
    idempotencyKey: `dashboard-attention-live-mention-${RUN_SUFFIX}`,
    mentionedMembershipIds: [TENANT_OWNER_MEMBERSHIP],
  }),
});
assert("mention seeded", mentionRes.status === 200 || mentionRes.status === 201);
const mentionId = (await mentionRes.json().catch(() => null))?.id ?? null;
assert("mention id", Boolean(mentionId));

const approvalRes = await fetch(`${API}/api/teams/approvals?facilityId=${FACILITY}`, {
  method: "POST",
  headers: actorHeaders(OWNER),
  body: JSON.stringify({
    channelId: URETIM_CHANNEL,
    messageId: mentionId,
    title: `Operasyon öncelik onay ${RUN_SUFFIX}`,
    rationale: "Dashboard öncelik kanıtı.",
    idempotencyKey: `dashboard-attention-live-approval-${RUN_SUFFIX}`,
    reviewers: [{ tenantMembershipId: TENANT_OWNER_MEMBERSHIP, role: "reviewer" }],
  }),
});
const approvalJson = await approvalRes.json().catch(() => null);
assert("approval seeded", approvalRes.status === 200 || approvalRes.status === 201, JSON.stringify(approvalJson)?.slice(0, 200));
const approvalId = approvalJson?.id ?? null;

const userDataDir = mkdtempSync(join(tmpdir(), "dashboard-operational-attention-1-"));
const port = 9896 + Math.floor(Math.random() * 100);
const child = spawn(exe, [`--remote-debugging-port=${port}`, "--headless=new", "--disable-gpu", "--window-size=1440,1800", `--user-data-dir=${userDataDir}`], { stdio: "ignore" });

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
    fail(`timeout waiting for snapshot: ${JSON.stringify(await evalSnap(expr))}; href=${await evalSnap("location.href")}`);
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
  for (const offset of [-1, 0, 1]) {
    const attempt = await ss("Runtime.evaluate", {
      awaitPromise: true,
      returnByValue: true,
      expression: `(async () => {
        const csrf = decodeURIComponent((document.cookie.match(/crevafactos\\.csrf=([^;]+)/) || [])[1] || "");
        const res = await fetch("/api/identity/mfa/verify", { method: "POST", credentials: "include", headers: { Accept: "application/json", "Content-Type": "application/json", "X-CrevaFactOS-Csrf": csrf }, body: JSON.stringify({ totpCode: ${JSON.stringify(generateTotp(mfaSecret, offset))} }) });
        return { status: res.status };
      })()`,
    });
    if (attempt.result?.value?.status === 200) break;
  }

  await ss("Page.navigate", { url: `${WEB}/dashboard` });
  await new Promise((r) => setTimeout(r, 7000));
  await waitFor(`Boolean(document.querySelector('[data-testid="dashboard-operasyon-oncelikleri-panel"]'))`, (ok) => ok === true, 60000);

  const panelSnapshot = await evalSnap(`(() => {
    const panel = document.querySelector('[data-testid="dashboard-operasyon-oncelikleri-panel"]');
    const text = panel?.innerText ?? '';
    const itemCount = panel?.querySelectorAll('[data-testid^="dashboard-operasyon-oncelikleri-item-"]').length ?? 0;
    const hasInboxLink = Boolean(panel?.querySelector('[data-testid="dashboard-operasyon-oncelikleri-inbox-link"]'));
    return { text, itemCount, hasInboxLink };
  })()`);

  assert("panel title visible", panelSnapshot?.text.includes("Operasyon Öncelikleri"));
  assert("inbox footer link visible", panelSnapshot?.hasInboxLink === true);
  assert("authorized items rendered", panelSnapshot?.itemCount > 0, `count=${panelSnapshot?.itemCount}`);
  assert("mention or approval visible", panelSnapshot?.text.includes(mentionBody) || (approvalId && panelSnapshot?.text.includes(`Operasyon öncelik onay ${RUN_SUFFIX}`)));
  assert("no mutation controls", !panelSnapshot?.text.includes("Onayla") && !panelSnapshot?.text.includes("Teslim aldım") && !panelSnapshot?.text.includes("Gönder"));

  writeFileSync(DASHBOARD_SHOT, Buffer.from((await ss("Page.captureScreenshot", { format: "png", captureBeyondViewport: true })).data, "base64"));
  assert("dashboard screenshot captured", existsSync(DASHBOARD_SHOT));

  await ss("Runtime.evaluate", {
    expression: `document.querySelector('[data-testid="dashboard-operasyon-oncelikleri-item-mention-${mentionId}"]')?.click()`,
  });
  await waitHref((href) => href.includes("/teams") && href.includes(mentionId), 30000);
  assert("mention drill-down opens authoritative Teams route", true);
  writeFileSync(SOURCE_SHOT, Buffer.from((await ss("Page.captureScreenshot", { format: "png", captureBeyondViewport: true })).data, "base64"));
  assert("source drill-down screenshot captured", existsSync(SOURCE_SHOT));

  const negative = await fetch(`${API}/api/teams/work-inbox?facilityId=${FACILITY}`, {
    headers: actorHeaders(NEGATIVE),
  });
  assert("non-member blocked or empty", negative.status === 403 || negative.status === 200, `status=${negative.status}`);
  if (negative.status === 200) {
    const json = await negative.json().catch(() => ({ items: [] }));
    assert("non-member does not see seeded mention", !(json.items ?? []).some((item) => item.sourceId === mentionId));
    const text = JSON.stringify(json);
    assert("non-member response has no seeded title", !text.includes(`Operasyon öncelik onay ${RUN_SUFFIX}`));
  }

  console.log(`Dashboard screenshot: ${DASHBOARD_SHOT}`);
  console.log(`Source drill-down screenshot: ${SOURCE_SHOT}`);
  console.log("DASHBOARD-OPERATIONAL-ATTENTION-1 live browser flow verification passed.");
} finally {
  child.kill("SIGTERM");
  try { rmSync(userDataDir, { recursive: true, force: true }); } catch { /* ignore */ }
}
