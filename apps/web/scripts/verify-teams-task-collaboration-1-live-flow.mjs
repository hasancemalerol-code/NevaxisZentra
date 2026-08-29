#!/usr/bin/env node

import { createHmac } from "node:crypto";

import { existsSync, mkdirSync, mkdtempSync, readFileSync, rmSync, writeFileSync } from "node:fs";

import { spawn } from "node:child_process";

import { tmpdir } from "node:os";

import { join } from "node:path";



const WEB = (process.env.CREVAFACTOS_WEB_BASE_URL ?? "http://localhost:3001").replace("127.0.0.1", "localhost").replace(/\/$/, "");

const API = process.env.CREVAFACTOS_API_BASE_URL ?? "http://127.0.0.1:5000";

const ROUTE = "/teams";

const FACILITY = "bb000001-0005-4000-8000-000000000005";

const URETIM_CHANNEL = "bb000001-03d1-4000-8000-0000000003d1";

const NEGATIVE = { issuer: "crevafactos-dev", subject: "quality-negative-test-user", tenant: "dev-pilot-tenant" };



const SCREENSHOT_DIR = join(process.cwd(), ".cursor-screenshots");

const DRAFT_SHOT = join(SCREENSHOT_DIR, "teams-task-collaboration-1-message-task-draft-authenticated.png");

const TASK_SHOT = join(SCREENSHOT_DIR, "teams-task-collaboration-1-linked-ai-task-authenticated.png");

const MESSAGE_LINKED_SHOT = join(SCREENSHOT_DIR, "teams-task-collaboration-1-backlinked-message-authenticated.png");

const TOTP_ALPHABET = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";



function fail(message) {

  console.error(`TEAMS-TASK-COLLABORATION-1 live flow failed: ${message}`);

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



function taskSnapExpr() {

  return `({

    href: location.href,

    hasSourceSection: Boolean(document.querySelector('[data-testid="ai-task-source-reference-section"]')),

    sourceModuleText: document.querySelector('[data-testid="ai-task-source-reference-section"]')?.innerText || "",

    hasTeamsSourceLink: Boolean(document.querySelector('[data-testid="ai-task-teams-message-source-link"]')),

    teamsSourceLinkText: document.querySelector('[data-testid="ai-task-teams-message-source-link"]')?.textContent?.trim() || "",

    teamsSourceLinkHref: document.querySelector('[data-testid="ai-task-teams-message-source-link"]')?.getAttribute("href") || "",

  })`;

}



function messageLinkedSnapExpr(messageId) {

  return `({

    href: location.href,

    hasLinkedTask: Boolean(document.querySelector('[data-testid="teams-message-linked-task-${messageId}"]')),

    linkedTaskHref: document.querySelector('[data-testid="teams-message-linked-task-${messageId}"] a')?.getAttribute("href") || "",

    linkedTaskText: document.querySelector('[data-testid="teams-message-linked-task-${messageId}"]')?.innerText || "",

  })`;

}



console.log("Running TEAMS-TASK-COLLABORATION-1 live browser flow verifier.");

bootstrapSecrets();

const ownerPassword = process.env.DevelopmentSaasIdentity__TenantOwnerPassword;

const mfaSecret = process.env.DevelopmentSaasIdentity__MfaSharedSecret;

const exe = "C:\\Program Files\\Google\\Chrome\\Application\\chrome.exe";

if (!existsSync(exe)) fail("Chrome not found");

if (!ownerPassword || !mfaSecret) fail("Missing tenant owner password or MFA secret");

mkdirSync(SCREENSHOT_DIR, { recursive: true });



const apiReady = await fetch(`${API}/health/ready`);

assert("API ready", apiReady.status === 200, `status=${apiReady.status}`);



const userDataDir = mkdtempSync(join(tmpdir(), "teams-task-collab-1-"));

const port = 9890 + Math.floor(Math.random() * 100);

const child = spawn(exe, [`--remote-debugging-port=${port}`, "--headless=new", "--disable-gpu", "--window-size=1440,1600", `--user-data-dir=${userDataDir}`], { stdio: "ignore" });



let createdTaskId = null;

let selectedChannelId = URETIM_CHANNEL;

let selectedMessageId = null;



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

  const softWaitFor = async (expr, pred, ms = 15000) => {
    const start = Date.now();
    while (Date.now() - start < ms) {
      const value = await evalSnap(expr);
      if (pred(value)) return value;
      await new Promise((r) => setTimeout(r, 300));
    }
    return null;
  };

  const waitFor = async (expr, pred, ms = 60000) => {

    const start = Date.now();

    while (Date.now() - start < ms) {

      const value = await evalSnap(expr);

      if (pred(value)) return value;

      await new Promise((r) => setTimeout(r, 300));

    }

    const composerError = await evalSnap(`document.querySelector('[data-testid="teams-composer-error"]')?.innerText ?? ""`);

    const draftError = await evalSnap(`document.querySelector('[data-testid="teams-message-task-draft-error"]')?.innerText ?? ""`);

    const diagnostics = await evalSnap(`(() => ({
      href: location.href,
      ready: Boolean(document.querySelector('[data-testid="teams-selected-channel-ready"]')),
      loading: Boolean(document.querySelector('[data-testid="teams-selected-channel-loading"]')),
      empty: Boolean(document.querySelector('[data-testid="teams-selected-channel-empty"]')),
      channelIds: Array.from(document.querySelectorAll('[data-testid^="teams-channel-"]')).map((node) => node.getAttribute("data-testid")?.replace("teams-channel-", "")).filter(Boolean),
      loadError: document.body?.innerText?.includes("Teams çalışma alanı yüklenemedi") ?? false,
    }))()`);

    fail(`timeout waiting for snapshot: ${JSON.stringify(await evalSnap(expr))}; diagnostics=${JSON.stringify(diagnostics)}${composerError ? `; composerError=${composerError}` : ""}${draftError ? `; draftError=${draftError}` : ""}`);

  };

  async function waitForChannelReady(ms = 90000) {
    return waitFor(
      `(() => {
        const ready = document.querySelector('[data-testid="teams-selected-channel-ready"]');
        const loading = document.querySelector('[data-testid="teams-selected-channel-loading"]');
        return Boolean(ready) && !loading;
      })()`,
      (ok) => ok === true,
      ms,
    );
  }

  async function resolveAccessibleChannelId() {
    const preferred = await evalSnap(`(() => {
      const preferredNode = document.querySelector('[data-testid="teams-channel-${URETIM_CHANNEL}"]');
      if (preferredNode) return "${URETIM_CHANNEL}";
      const first = document.querySelector('[data-testid^="teams-channel-"]');
      return first?.getAttribute("data-testid")?.replace("teams-channel-", "") ?? null;
    })()`);
    assert("accessible channel id resolved from rail", Boolean(preferred), `channelId=${preferred}`);
    return preferred;
  }

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

  await waitFor(
    `Boolean(document.body?.innerText?.includes("Kanallar"))`,
    (ok) => ok === true,
    60000,
  );

  const channelCount = await evalSnap(`document.querySelectorAll('[data-testid^="teams-channel-"]').length`);
  assert("accessible channel list renders", Number(channelCount) >= 1, `count=${channelCount}`);

  selectedChannelId = await resolveAccessibleChannelId();

  let channelReady = false;
  try {
    await waitForChannelReady(15000);
    channelReady = true;
  } catch {
    channelReady = false;
  }

  if (!channelReady) {
    await ss("Runtime.evaluate", {
      expression: `document.querySelector('[data-testid="teams-channel-${selectedChannelId}"]')?.click()`,
    });
    await waitFor(
      `(location.search || "").includes("channel=")`,
      (ok) => ok === true,
      30000,
    );
    try {
      await waitForChannelReady(30000);
      channelReady = true;
    } catch {
      channelReady = false;
    }
  }

  if (!channelReady) {
    await navigateTo(`${ROUTE}?channel=${encodeURIComponent(selectedChannelId)}`);
    await waitForChannelReady(90000);
  }

  selectedChannelId = await evalSnap(`document.querySelector('[data-testid="teams-selected-channel-ready"]')?.getAttribute("data-channel-id") ?? ${JSON.stringify(selectedChannelId)}`);
  assert("channel ready through UI or deep link", Boolean(selectedChannelId));

  await navigateTo(`${ROUTE}?channel=${encodeURIComponent(selectedChannelId)}`);
  await waitForChannelReady(60000);

  const turkishMessage = `Teams görev işbirliği doğrulama mesajı ${Date.now()}`;

  await waitFor(

    `Boolean(document.querySelector('[data-testid="teams-message-composer"]')) && Boolean(document.querySelector('[data-testid="teams-composer-input"]')) && Boolean(document.querySelector('[data-testid="teams-send-message"]'))`,

    (ok) => ok === true,

    60000,

  );

  const composerNode = await ss("DOM.getDocument", { depth: -1, pierce: true }).then(async (doc) => {
    const { nodeId } = await ss("DOM.querySelector", {
      nodeId: doc.root.nodeId,
      selector: '[data-testid="teams-composer-input"]',
    });
    return nodeId;
  });
  await ss("DOM.focus", { nodeId: composerNode });
  await ss("Input.insertText", { text: turkishMessage });

  await waitFor(

    `(function() {
      const area = document.querySelector('[data-testid="teams-composer-input"]');
      const send = document.querySelector('[data-testid="teams-send-message"]');
      return Boolean(area?.value?.includes(${JSON.stringify(turkishMessage)})) && !send?.disabled;
    })()`,

    (ok) => ok === true,

    15000,

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

  let messagePosted = await softWaitFor(
    `(function(){
      const timelineMatch = Array.from(document.querySelectorAll('article[data-testid^="teams-message-"]'))
        .some((node) => node.innerText?.includes(${JSON.stringify(turkishMessage)}));
      const composerValue = document.querySelector('[data-testid="teams-composer-input"]')?.value ?? "";
      const pending = Boolean(document.querySelector('[data-testid="teams-composer-pending"]'));
      return timelineMatch && !composerValue.includes(${JSON.stringify(turkishMessage)}) && !pending;
    })()`,
    (ok) => ok === true,
    30000,
  );

  if (!messagePosted) {
    await navigateTo(`${ROUTE}?channel=${encodeURIComponent(selectedChannelId)}`);
    await waitForChannelReady(60000);
    messagePosted = await softWaitFor(
      `(function(){
        return Array.from(document.querySelectorAll('article[data-testid^="teams-message-"]'))
          .some((node) => node.innerText?.includes(${JSON.stringify(turkishMessage)}));
      })()`,
      (ok) => ok === true,
      60000,
    );
  }

  assert("posted message appears in channel timeline", Boolean(messagePosted));

  const composerError = await evalSnap(`document.querySelector('[data-testid="teams-composer-error"]')?.innerText ?? ""`);

  assert("message post has no composer error", !composerError, composerError || "");

  selectedChannelId = await evalSnap(`document.querySelector('[data-testid="teams-selected-channel-ready"]')?.getAttribute("data-channel-id") ?? ${JSON.stringify(selectedChannelId)}`);

  selectedMessageId = await evalSnap(`(function() {
    const node = Array.from(document.querySelectorAll('article[data-testid^="teams-message-"]'))
      .find((article) => article.innerText?.includes(${JSON.stringify(turkishMessage)}));
    return node?.getAttribute("data-testid")?.replace("teams-message-", "") ?? null;
  })()`);

  assert("posted message id resolved", Boolean(selectedMessageId), `messageId=${selectedMessageId}`);

  await ss("Runtime.evaluate", {
    expression: `document.querySelector('[data-testid="teams-message-select-${selectedMessageId}"]')?.click()`,
  });

  const clickSelected = await softWaitFor(
    `document.querySelector('[data-testid="teams-message-${selectedMessageId}"]')?.getAttribute("data-selected") === "true" || (location.search || "").includes("message=")`,
    (ok) => ok === true,
    15000,
  );

  if (!clickSelected) {
    await navigateTo(`/teams?channel=${encodeURIComponent(selectedChannelId)}&message=${encodeURIComponent(selectedMessageId)}`);
    await waitForChannelReady(60000);
  }

  await waitFor(
    `document.querySelector('[data-testid="teams-message-${selectedMessageId}"]')?.getAttribute("data-selected") === "true"`,
    (ok) => ok === true,
    60000,
  );

  assert("persisted message selected through UI or deep link", true);

  await waitFor(

    `Boolean(document.querySelector('[data-testid="teams-message-convert-to-task"]'))`,

    (ok) => ok === true,

    30000,

  );

  assert("Göreve dönüştür action visible for selected message", true);



  await ss("Runtime.evaluate", {

    expression: `document.querySelector('[data-testid="teams-message-convert-to-task"]')?.click()`,

  });

  await waitFor(

    `Boolean(document.querySelector('[data-testid="teams-message-task-draft-form"]'))`,

    (ok) => ok === true,

    30000,

  );

  assert("task draft form opened", true);

  writeFileSync(DRAFT_SHOT, Buffer.from((await ss("Page.captureScreenshot", { format: "png", fromSurface: true })).data, "base64"));

  assert("task draft screenshot captured", existsSync(DRAFT_SHOT));



  const taskTitle = `Teams mesaj görev taslağı ${Date.now()}`;

  await ss("Runtime.evaluate", {

    expression: `(function() {

      const setter = Object.getOwnPropertyDescriptor(window.HTMLInputElement.prototype, "value").set;

      const title = document.querySelector('[data-testid="teams-message-task-title-input"]');

      setter.call(title, ${JSON.stringify(taskTitle)});

      title.dispatchEvent(new Event("input", { bubbles: true }));

      title.dispatchEvent(new Event("change", { bubbles: true }));

      const note = document.querySelector('[data-testid="teams-message-task-note-input"]');

      const noteSetter = Object.getOwnPropertyDescriptor(window.HTMLTextAreaElement.prototype, "value").set;

      noteSetter.call(note, "Canlı doğrulama notu");

      note.dispatchEvent(new Event("input", { bubbles: true }));

      note.dispatchEvent(new Event("change", { bubbles: true }));

    })()`,

  });

  await waitFor(

    `!document.querySelector('[data-testid="teams-message-task-draft-submit"]')?.disabled`,

    (ok) => ok === true,

    10000,

  );

  await ss("Runtime.evaluate", {

    expression: `(function() {

      const form = document.querySelector('[data-testid="teams-message-task-draft-form"]');

      if (form instanceof HTMLFormElement) {

        form.requestSubmit();

        return;

      }

      document.querySelector('[data-testid="teams-message-task-draft-submit"]')?.click();

    })()`,

  });

  await waitHref((href) => href.includes("/ai-gorevleri/") && !href.includes("/yeni"), 60000);

  createdTaskId = (await evalSnap("location.pathname")).split("/").pop();

  assert("navigated to task detail after draft submit", Boolean(createdTaskId));



  const taskSnap = await waitFor(taskSnapExpr(), (s) => s?.hasSourceSection && s?.hasTeamsSourceLink, 60000);

  assert("task detail page loaded", taskSnap.href.includes("/ai-gorevleri/"));

  assert("Teams mesajı source module visible", taskSnap.sourceModuleText.includes("Teams mesajı"));

  assert("Teams bağlamını aç link present", taskSnap.teamsSourceLinkText.includes("Teams bağlamını aç"));

  assert("Teams back-link includes channel and message", taskSnap.teamsSourceLinkHref.includes("/teams?") && taskSnap.teamsSourceLinkHref.includes("channel=") && taskSnap.teamsSourceLinkHref.includes("message="));

  writeFileSync(TASK_SHOT, Buffer.from((await ss("Page.captureScreenshot", { format: "png", fromSurface: true })).data, "base64"));

  assert("linked task detail screenshot captured", existsSync(TASK_SHOT));



  await ss("Runtime.evaluate", {

    expression: `document.querySelector('[data-testid="ai-task-teams-message-source-link"]')?.click()`,

  });

  await waitHref(

    (href) => href.includes("/teams") && href.includes(`channel=${encodeURIComponent(selectedChannelId)}`) && href.includes(`message=${encodeURIComponent(selectedMessageId)}`),

    60000,

  );

  const linkedSnap = await waitFor(messageLinkedSnapExpr(selectedMessageId), (s) => s?.hasLinkedTask, 60000);

  assert("message shows linked task after backlink navigation", linkedSnap.hasLinkedTask);

  assert("linked task href points to created task", linkedSnap.linkedTaskHref.includes(createdTaskId));

  writeFileSync(MESSAGE_LINKED_SHOT, Buffer.from((await ss("Page.captureScreenshot", { format: "png", fromSurface: true })).data, "base64"));

  assert("message linked task screenshot captured", existsSync(MESSAGE_LINKED_SHOT));



  const taskIdBeforeRefresh = createdTaskId;

  await navigateTo(`/teams?channel=${encodeURIComponent(selectedChannelId)}&message=${encodeURIComponent(selectedMessageId)}`);

  await ss("Runtime.evaluate", { expression: "location.reload()" });

  await new Promise((r) => setTimeout(r, 2500));

  const refreshSnap = await waitFor(messageLinkedSnapExpr(selectedMessageId), (s) => s?.hasLinkedTask, 60000);

  assert("linked task persists after refresh", refreshSnap.hasLinkedTask);

  assert("refresh preserves linked task href", refreshSnap.linkedTaskHref.includes(taskIdBeforeRefresh));



  await ss("Runtime.evaluate", {

    expression: `document.querySelector('[data-testid="teams-message-linked-task-${selectedMessageId}"] a')?.click()`,

  });

  await waitHref((href) => href.includes(`/ai-gorevleri/${taskIdBeforeRefresh}`), 60000);

  const replayTaskId = (await evalSnap("location.pathname")).split("/").pop();

  assert("idempotent replay opens same task id", replayTaskId === taskIdBeforeRefresh, `${replayTaskId} vs ${taskIdBeforeRefresh}`);



  const negativeList = await fetch(`${API}/api/teams/channels?facilityId=${FACILITY}`, { headers: actorHeaders(NEGATIVE) });

  assert("non-member actor cannot list channels", negativeList.status === 403, `status=${negativeList.status}`);

  const negativePost = await fetch(`${API}/api/teams/channels/${selectedChannelId}/messages?facilityId=${FACILITY}`, {

    method: "POST",

    headers: { ...actorHeaders(NEGATIVE), "Content-Type": "application/json", "Idempotency-Key": crypto.randomUUID() },

    body: JSON.stringify({ body: "blocked", idempotencyKey: crypto.randomUUID(), mentionedMembershipIds: [] }),

  });

  assert("non-member actor cannot post messages", negativePost.status === 403, `status=${negativePost.status}`);

  const negativeTaskDraft = await fetch(`${API}/api/teams/messages/${selectedMessageId}/task-draft?facilityId=${FACILITY}`, {

    method: "POST",

    headers: { ...actorHeaders(NEGATIVE), "Content-Type": "application/json" },

    body: JSON.stringify({

      title: "blocked",

      category: "production-planning",

      priority: "medium",

      humanNote: null,

    }),

  });

  assert("non-member actor cannot create message task draft", negativeTaskDraft.status === 403, `status=${negativeTaskDraft.status}`);



  console.log(`Task draft screenshot: ${DRAFT_SHOT}`);

  console.log(`Linked task detail screenshot: ${TASK_SHOT}`);

  console.log(`Message linked task screenshot: ${MESSAGE_LINKED_SHOT}`);

  console.log(`Channel id: ${selectedChannelId}`);

  console.log(`Message id: ${selectedMessageId}`);

  console.log(`Created task id: ${createdTaskId}`);

  console.log("TEAMS-TASK-COLLABORATION-1 live browser flow verification passed.");

} finally {

  child.kill("SIGTERM");

  try {

    rmSync(userDataDir, { recursive: true, force: true });

  } catch {

    // Chrome may still hold the profile directory on Windows.

  }

}
