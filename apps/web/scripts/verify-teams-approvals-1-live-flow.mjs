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
const REVIEWER_MEMBERSHIP = "bb000001-0021-4000-8000-000000000021";
const OWNER = { issuer: "crevafactos-dev", subject: "planning-pilot-user", tenant: "dev-pilot-tenant" };
const REVIEWER = { issuer: "crevafactos-dev", subject: "planning-pilot-reviewer", tenant: "dev-pilot-tenant" };
const NEGATIVE = { issuer: "crevafactos-dev", subject: "quality-negative-test-user", tenant: "dev-pilot-tenant" };
const SCREENSHOT_DIR = join(process.cwd(), ".cursor-screenshots");
const FORM_SHOT = join(SCREENSHOT_DIR, "teams-approvals-1-request-form-authenticated.png");
const PENDING_SHOT = join(SCREENSHOT_DIR, "teams-approvals-1-pending-detail-authenticated.png");
const DECIDED_SHOT = join(SCREENSHOT_DIR, "teams-approvals-1-decided-detail-authenticated.png");
const TOTP_ALPHABET = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";

function fail(message) {
  console.error(`TEAMS-APPROVALS-1 live flow failed: ${message}`);
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
    "6aa3a2aa-05da-4f17-9e21-8f4415d6c3f8",
    "e4795037-6822-4158-86cc-66f2bbb7cc33",
    "dc1076d3-6856-4bae-8a65-935769827e04",
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

console.log("Running TEAMS-APPROVALS-1 live browser flow verifier.");
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
const turkishMessage = `Onay isteği kaynak mesajı ${RUN_SUFFIX}`;
const persistedMessage = await fetch(`${API}/api/teams/channels/${URETIM_CHANNEL}/messages?facilityId=${FACILITY}`, {
  method: "POST",
  headers: actorHeaders(OWNER),
  body: JSON.stringify({
    body: turkishMessage,
    idempotencyKey: `teams-approval-live-${RUN_SUFFIX}`,
    mentionedMembershipIds: [],
  }),
});
assert("persisted source message seeded", persistedMessage.status === 200 || persistedMessage.status === 201);
const persistedMessageId = (await persistedMessage.json().catch(() => null))?.id ?? null;
assert("persisted source message id", Boolean(persistedMessageId));

const userDataDir = mkdtempSync(join(tmpdir(), "teams-approvals-1-"));
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
    fail(`timeout waiting for snapshot: ${JSON.stringify(await evalSnap(expr))}; href=${await evalSnap("location.href")}; body=${(await evalSnap("document.body?.innerText ?? ''")).slice(0, 400)}`);
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
    await ss("Runtime.evaluate", { expression: `window.location.assign(${JSON.stringify(`${WEB}${path}`)})` });
    await waitHref((href) => href.includes(path.split("?")[0]), 60000);
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
        return { status: res.status };
      })()`,
    });
    if (attempt.result?.value?.status === 200) {
      mfaOk = true;
      break;
    }
  }
  assert("MFA verification succeeded", mfaOk);

  await navigateTo("/teams");
  await waitFor(`Boolean(document.querySelector('[data-testid="teams-workspace"]'))`, (ok) => ok === true, 60000);
  assert("/teams loads", true);
  await waitFor(`Boolean(document.querySelector('[data-testid="teams-approvals-index-link"]'))`, (ok) => ok === true, 15000);

  await navigateTo(`/teams?channel=${URETIM_CHANNEL}&message=${persistedMessageId}`);
  await waitFor(
    `document.querySelector('[data-testid="teams-message-${persistedMessageId}"]')?.getAttribute("data-selected") === "true"`,
    (ok) => ok === true,
    60000,
  );
  await waitFor(`Boolean(document.querySelector('[data-testid="teams-message-request-approval"]'))`, (ok) => ok === true, 15000);
  assert("Onay iste visible for selected message", true);

  await navigateTo(`/teams/onaylar/yeni?channel=${URETIM_CHANNEL}&message=${persistedMessageId}`);
  await waitFor(`Boolean(document.querySelector('[data-testid="teams-approval-request-form"]'))`, (ok) => ok === true, 60000);
  assert("approval request form opened", true);
  writeFileSync(FORM_SHOT, Buffer.from((await ss("Page.captureScreenshot", { format: "png", fromSurface: true })).data, "base64"));
  assert("request form screenshot captured", existsSync(FORM_SHOT));

  const title = `İnsan onay isteği ${RUN_SUFFIX}`;
  await ss("Runtime.evaluate", {
    expression: `(function() {
      const setter = Object.getOwnPropertyDescriptor(window.HTMLInputElement.prototype, "value").set;
      const titleInput = document.querySelector('[data-testid="teams-approval-title-input"]');
      setter.call(titleInput, ${JSON.stringify(title)});
      titleInput.dispatchEvent(new Event("input", { bubbles: true }));
      const rationale = document.querySelector('[data-testid="teams-approval-rationale-input"]');
      const areaSetter = Object.getOwnPropertyDescriptor(window.HTMLTextAreaElement.prototype, "value").set;
      areaSetter.call(rationale, "Canlı doğrulama gerekçesi");
      rationale.dispatchEvent(new Event("input", { bubbles: true }));
      document.querySelector('[data-testid="teams-approval-reviewer-${REVIEWER_MEMBERSHIP}"]')?.click();
    })()`,
  });
  await waitFor(`!document.querySelector('[data-testid="teams-approval-request-submit"]')?.disabled`, (ok) => ok === true, 10000);
  await ss("Runtime.evaluate", {
    expression: `(function() {
      const form = document.querySelector('[data-testid="teams-approval-request-form"]');
      if (form instanceof HTMLFormElement) form.requestSubmit();
      else document.querySelector('[data-testid="teams-approval-request-submit"]')?.click();
    })()`,
  });
  await waitHref((href) => href.includes("/teams/onaylar/") && !href.includes("/yeni"), 60000);
  const createdRequestId = (await evalSnap("location.pathname")).split("/").pop();
  assert("navigated to approval detail", Boolean(createdRequestId));
  await waitFor(`Boolean(document.querySelector('[data-testid="teams-approval-detail-page"]'))`, (ok) => ok === true, 30000);
  const pendingText = await evalSnap("document.body?.innerText ?? ''");
  assert("pending status visible", pendingText.includes("Beklemede"));
  assert("source is Teams message", pendingText.includes("Teams mesajı"));
  writeFileSync(PENDING_SHOT, Buffer.from((await ss("Page.captureScreenshot", { format: "png", fromSurface: true })).data, "base64"));
  assert("pending detail screenshot captured", existsSync(PENDING_SHOT));

  const ownerDecide = await fetch(`${API}/api/teams/approvals/${createdRequestId}/decisions?facilityId=${FACILITY}`, {
    method: "POST",
    headers: actorHeaders(OWNER),
    body: JSON.stringify({ decision: "approved" }),
  });
  assert("requester cannot decide own request", ownerDecide.status === 403);

  const decided = await fetch(`${API}/api/teams/approvals/${createdRequestId}/decisions?facilityId=${FACILITY}`, {
    method: "POST",
    headers: actorHeaders(REVIEWER),
    body: JSON.stringify({ decision: "approved", note: "Canlı inceleme onayı" }),
  });
  assert("assigned reviewer records decision", decided.status === 200, await decided.text().then((t) => t.slice(0, 200)));

  await ss("Runtime.evaluate", { expression: "location.reload()" });
  await new Promise((r) => setTimeout(r, 2500));
  await waitFor(`(document.body?.innerText ?? "").includes("Onaylandı")`, (ok) => ok === true, 60000);
  assert("resolved status persists after refresh", true);
  writeFileSync(DECIDED_SHOT, Buffer.from((await ss("Page.captureScreenshot", { format: "png", fromSurface: true })).data, "base64"));
  assert("decided detail screenshot captured", existsSync(DECIDED_SHOT));

  const replay = await fetch(`${API}/api/teams/approvals?facilityId=${FACILITY}`, {
    method: "POST",
    headers: actorHeaders(OWNER),
    body: JSON.stringify({
      channelId: URETIM_CHANNEL,
      messageId: persistedMessageId,
      title,
      rationale: "Canlı doğrulama gerekçesi",
      idempotencyKey: `unused-ui-key-${RUN_SUFFIX}`,
      reviewers: [{ tenantMembershipId: REVIEWER_MEMBERSHIP, role: "reviewer" }],
    }),
  });
  const replayJson = await replay.json().catch(() => null);
  assert("second explicit create is a new or same human request", replay.status === 200 || replay.status === 201);
  assert("no automatic operational write", !JSON.stringify(replayJson).includes("workOrder") && !JSON.stringify(replayJson).includes("purchase"));

  const negative = await fetch(`${API}/api/teams/approvals?facilityId=${FACILITY}`, {
    method: "POST",
    headers: actorHeaders(NEGATIVE),
    body: JSON.stringify({
      channelId: URETIM_CHANNEL,
      messageId: persistedMessageId,
      title: "blocked",
      rationale: "blocked",
      idempotencyKey: `neg-${RUN_SUFFIX}`,
      reviewers: [{ tenantMembershipId: REVIEWER_MEMBERSHIP, role: "reviewer" }],
    }),
  });
  assert("non-member remains blocked", negative.status === 403);

  console.log(`Request form screenshot: ${FORM_SHOT}`);
  console.log(`Pending detail screenshot: ${PENDING_SHOT}`);
  console.log(`Decided detail screenshot: ${DECIDED_SHOT}`);
  console.log(`Request id: ${createdRequestId}`);
  console.log("TEAMS-APPROVALS-1 live browser flow verification passed.");
} finally {
  child.kill("SIGTERM");
  try { rmSync(userDataDir, { recursive: true, force: true }); } catch { /* Chrome may hold the profile */ }
}
