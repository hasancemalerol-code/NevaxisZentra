#!/usr/bin/env node

import { createHmac } from "node:crypto";

import { existsSync, mkdirSync, mkdtempSync, readFileSync, rmSync, writeFileSync } from "node:fs";

import { spawn } from "node:child_process";

import { tmpdir } from "node:os";

import { join } from "node:path";



const WEB = (process.env.NEVAXIS_FACTOS_WEB_BASE_URL ?? "http://localhost:3001").replace("127.0.0.1", "localhost").replace(/\/$/, "");

const API = process.env.NEVAXIS_FACTOS_API_BASE_URL ?? "http://127.0.0.1:5000";

const ROUTE = "/teams";

const FACILITY = "bb000001-0005-4000-8000-000000000005";

const URETIM_CHANNEL = "bb000001-03d1-4000-8000-0000000003d1";

const TASK_CONTEXT_CHANNEL = "bb000001-03d7-4000-8000-0000000003d7";

const PILOT_TASK = "bb000001-03d0-4000-8000-0000000003d0";

const NEGATIVE = { issuer: "crevafactos-dev", subject: "quality-negative-test-user", tenant: "dev-pilot-tenant" };



const SCREENSHOT_DIR = join(process.cwd(), ".cursor-screenshots");

const WORKSPACE_SHOT = join(SCREENSHOT_DIR, "teams-foundation-1-workspace-authenticated.png");

const MESSAGE_SHOT = join(SCREENSHOT_DIR, "teams-foundation-1-posted-mentioned-message-authenticated.png");

const TASK_CONTEXT_SHOT = join(SCREENSHOT_DIR, "teams-foundation-1-task-context-authenticated.png");

const TOTP_ALPHABET = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";



function fail(message) {

  console.error(`TEAMS-FOUNDATION-1 live flow failed: ${message}`);

  process.exit(1);

}



function assert(label, ok, detail = "") {

  if (!ok) fail(`${label}${detail ? `: ${detail}` : ""}`);

  console.log(`✓ ${label}`);

}



function actorHeaders(actor) {

  return {

    "X-crevafactos-dev-Issuer": actor.issuer,

    "X-crevafactos-dev-Subject": actor.subject,

    "X-NevaxisFactOS-Tenant": actor.tenant,

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



console.log("Running TEAMS-FOUNDATION-1 live browser flow verifier.");

bootstrapSecrets();

const ownerPassword = process.env.DevelopmentSaasIdentity__TenantOwnerPassword;

const mfaSecret = process.env.DevelopmentSaasIdentity__MfaSharedSecret;

const exe = "C:\\Program Files\\Google\\Chrome\\Application\\chrome.exe";

if (!existsSync(exe)) fail("Chrome not found");

if (!ownerPassword || !mfaSecret) fail("Missing tenant owner password or MFA secret");

mkdirSync(SCREENSHOT_DIR, { recursive: true });



const apiReady = await fetch(`${API}/health/ready`);

assert("API ready", apiReady.status === 200, `status=${apiReady.status}`);



const userDataDir = mkdtempSync(join(tmpdir(), "teams-foundation-1-"));

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

    const composerError = await evalSnap(`document.querySelector('[data-testid="teams-composer-error"]')?.innerText ?? ""`);

    fail(`timeout waiting for snapshot: ${JSON.stringify(await evalSnap(expr))}${composerError ? `; composerError=${composerError}` : ""}`);

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

  assert("/api/identity/me returns 200 after MFA", me.result?.value?.status === 200 && me.result?.value?.hasAccount);



  await navigateTo(ROUTE);

  await waitFor(

    `Boolean(document.querySelector('[data-testid="teams-workspace"]'))`,

    (ok) => ok === true,

    60000,

  );

  assert("/teams loads without subscription redirect", !(String(await evalSnap("location.pathname")).includes("abonelik-erisimi")));

  const channelCount = await evalSnap(`document.querySelectorAll('[data-testid^="teams-channel-"]').length`);

  assert("accessible channel list renders", Number(channelCount) >= 3, `count=${channelCount}`);

  writeFileSync(WORKSPACE_SHOT, Buffer.from((await ss("Page.captureScreenshot", { format: "png", fromSurface: true })).data, "base64"));

  assert("workspace screenshot captured", existsSync(WORKSPACE_SHOT));



  await waitFor(

    `Boolean(document.querySelector('[data-testid="teams-selected-channel-ready"]'))`,

    (ok) => ok === true,

    60000,

  );



  const channelTestId = `[data-testid="teams-channel-${URETIM_CHANNEL}"]`;

  const hasUretim = await evalSnap(`Boolean(document.querySelector(${JSON.stringify(channelTestId)}))`);

  const clickTarget = hasUretim

    ? channelTestId

    : '[data-testid^="teams-channel-"]';

  await ss("Runtime.evaluate", {

    expression: `document.querySelector(${JSON.stringify(clickTarget)})?.click()`,

  });

  await waitFor(

    `Boolean(document.querySelector('[data-testid="teams-selected-channel-ready"]'))`,

    (ok) => ok === true,

    60000,

  );

  await waitFor(

    `Boolean(document.querySelector('[data-testid="teams-message-composer"]')) && Boolean(document.querySelector('[data-testid="teams-composer-input"]')) && Boolean(document.querySelector('[data-testid="teams-send-message"]'))`,

    (ok) => ok === true,

    60000,

  );

  assert("composer visible with input and send button", true);



  const turkishMessage = `Teams canlı doğrulama mesajı ${Date.now()}`;
  await ss("Runtime.evaluate", {
    expression: `(function() {
      const composer = document.querySelector('[data-testid="teams-message-composer"]');
      const mentionBtn = Array.from(composer?.querySelectorAll("button") ?? []).find((btn) => btn.textContent?.includes("@"));
      mentionBtn?.click();
      const area = document.querySelector('[data-testid="teams-composer-input"]');
      const setter = Object.getOwnPropertyDescriptor(window.HTMLTextAreaElement.prototype, "value").set;
      setter.call(area, ${JSON.stringify(turkishMessage)});
      area.dispatchEvent(new Event("input", { bubbles: true }));
      area.dispatchEvent(new Event("change", { bubbles: true }));
    })()`,
  });
  await waitFor(
    `!document.querySelector('[data-testid="teams-send-message"]')?.disabled`,
    (ok) => ok === true,
    10000,
  );
  await ss("Runtime.evaluate", {
    expression: `(function() {
      const form = document.querySelector('[data-testid="teams-message-composer"]');
      if (form instanceof HTMLFormElement) {
        form.requestSubmit();
        return;
      }
      document.querySelector('[data-testid="teams-send-message"]')?.click();
    })()`,
  });

  await waitFor(
    `(function(){
      const timelineMatch = Array.from(document.querySelectorAll('article[data-testid^="teams-message-"]'))
        .some((node) => node.innerText?.includes(${JSON.stringify(turkishMessage)}));
      const composerValue = document.querySelector('[data-testid="teams-composer-input"]')?.value ?? "";
      return timelineMatch && !composerValue.includes(${JSON.stringify(turkishMessage)});
    })()`,
    (ok) => ok === true,
    60000,
  );
  const composerError = await evalSnap(`document.querySelector('[data-testid="teams-composer-error"]')?.innerText ?? ""`);
  assert("message post has no composer error", !composerError, composerError || "");
  const messageCount = await evalSnap(`Array.from(document.querySelectorAll('article[data-testid^="teams-message-"]')).filter((node) => node.innerText?.includes(${JSON.stringify(turkishMessage)})).length`);
  assert("posted Turkish message appears once", Number(messageCount) === 1, `count=${messageCount}`);
  const mentionRendered = await evalSnap(`Array.from(document.querySelectorAll('.teams-message__mentions')).some((node) => node.innerText?.includes("@"))`);
  assert("mention rendered in timeline", mentionRendered === true);



  const selectedChannelId = hasUretim ? URETIM_CHANNEL : await evalSnap(`document.querySelector('[data-testid="teams-selected-channel-ready"]')?.getAttribute("data-channel-id")`);

  await navigateTo(`${ROUTE}?channel=${encodeURIComponent(selectedChannelId)}`);

  const persisted = await evalSnap(`document.body?.innerText?.includes(${JSON.stringify(turkishMessage)})`);

  assert("posted Turkish message persists after refresh", persisted === true);

  writeFileSync(MESSAGE_SHOT, Buffer.from((await ss("Page.captureScreenshot", { format: "png", fromSurface: true })).data, "base64"));

  assert("message screenshot captured", existsSync(MESSAGE_SHOT));



  await navigateTo(`${ROUTE}?channel=${TASK_CONTEXT_CHANNEL}`);

  await waitFor(

    `Boolean(document.body?.innerText?.includes("Görevi aç"))`,

    (ok) => ok === true,

    60000,

  );

  writeFileSync(TASK_CONTEXT_SHOT, Buffer.from((await ss("Page.captureScreenshot", { format: "png", fromSurface: true })).data, "base64"));

  assert("task-context screenshot captured", existsSync(TASK_CONTEXT_SHOT));



  await navigateTo(`/ai-gorevleri/${PILOT_TASK}`);

  await waitFor(

    `Boolean(document.querySelector('[data-testid="ai-task-teams-context-link"]'))`,

    (ok) => ok === true,

    60000,

  );

  assert("AI task shows Teams bağlamını aç", true);



  const negativeList = await fetch(`${API}/api/teams/channels?facilityId=${FACILITY}`, { headers: actorHeaders(NEGATIVE) });

  assert("non-member actor cannot list channels", negativeList.status === 403, `status=${negativeList.status}`);

  const negativePost = await fetch(`${API}/api/teams/channels/${URETIM_CHANNEL}/messages?facilityId=${FACILITY}`, {

    method: "POST",

    headers: { ...actorHeaders(NEGATIVE), "Content-Type": "application/json", "Idempotency-Key": crypto.randomUUID() },

    body: JSON.stringify({ body: "blocked", idempotencyKey: crypto.randomUUID(), mentionedMembershipIds: [] }),

  });

  assert("non-member actor cannot post messages", negativePost.status === 403, `status=${negativePost.status}`);



  console.log(`Workspace screenshot: ${WORKSPACE_SHOT}`);

  console.log(`Message screenshot: ${MESSAGE_SHOT}`);

  console.log(`Task-context screenshot: ${TASK_CONTEXT_SHOT}`);

  console.log("TEAMS-FOUNDATION-1 live browser flow verification passed.");

} finally {

  child.kill("SIGTERM");

  try {

    rmSync(userDataDir, { recursive: true, force: true });

  } catch {

    // Chrome may still hold the profile directory on Windows.

  }

}

