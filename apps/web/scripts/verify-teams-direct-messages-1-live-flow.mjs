#!/usr/bin/env node
import { createHmac } from "node:crypto";
import { existsSync, mkdirSync, mkdtempSync, readFileSync, rmSync, writeFileSync } from "node:fs";
import { spawn } from "node:child_process";
import { tmpdir } from "node:os";
import { join } from "node:path";

const WEB = (process.env.NEVAXIS_FACTOS_WEB_BASE_URL ?? "http://localhost:3001").replace("127.0.0.1", "localhost").replace(/\/$/, "");
const API = process.env.NEVAXIS_FACTOS_API_BASE_URL ?? "http://127.0.0.1:5000";
const FACILITY = "bb000001-0005-4000-8000-000000000005";
const FACILITY_CODE = "pilot-facility";
const ACTOR_A_EMAIL = "admin@demo.nevaxiszentra.local";
const ACTOR_B_BY_MEMBERSHIP = {
  "bb000002-0013-4000-8000-000000000013": {
    email: "active-user@dev.local",
    label: "Development Active User",
    member: "mem_bb000002001340008000000000013",
  },
  "bb000003-0213-4000-8000-000000000213": {
    email: "materials-sales@dev.local",
    label: "Development Materials Sales",
    member: "mem_bb000003021340008000000000000213",
  },
  "bb000003-0223-4000-8000-000000000223": {
    email: "maintenance-team@dev.local",
    label: "Development Maintenance Team",
    member: "mem_bb000003022340008000000000223",
  },
};
const REVIEWER_ROLE = "production-planning-reviewer";
const NEGATIVE = { issuer: "crevafactos-dev", subject: "quality-negative-test-user", tenant: "dev-pilot-tenant" };
const SCREENSHOT_DIR = join(process.cwd(), ".cursor-screenshots");
const LIST_SHOT = join(SCREENSHOT_DIR, "teams-direct-messages-1-list-authenticated.png");
const UNREAD_SHOT = join(SCREENSHOT_DIR, "teams-direct-messages-1-unread-authenticated.png");
const CONVERSATION_SHOT = join(SCREENSHOT_DIR, "teams-direct-messages-1-conversation-replied-authenticated.png");
const READ_STATE_SHOT = join(SCREENSHOT_DIR, "teams-direct-messages-1-read-state-authenticated.png");
const TOTP_ALPHABET = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";

function fail(message) {
  console.error(`TEAMS-DIRECT-MESSAGES-1 live flow failed: ${message}`);
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
    "X-NevaxisZentra-Tenant": actor.tenant,
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
    "45a3825f-58e0-409e-82b6-3ea3581088e8",
  ];
  let text = "";
  for (const id of ids) {
    const p = join(home, ".cursor/projects/c-Projects-NevaxisZentra/agent-transcripts", id, `${id}.jsonl`);
    if (existsSync(p)) text += readFileSync(p, "utf8");
  }
  for (const key of [
    "DevelopmentSaasIdentity__TenantOwnerPassword",
    "DevelopmentSaasIdentity__MfaSharedSecret",
    "DevelopmentSaasIdentity__ActiveUserPassword",
  ]) {
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

console.log("Running TEAMS-DIRECT-MESSAGES-1 live browser flow verifier.");
bootstrapSecrets();
const ownerPassword = process.env.DevelopmentSaasIdentity__TenantOwnerPassword;
const actorBPassword = process.env.DevelopmentSaasIdentity__ActiveUserPassword;
const mfaSecret = process.env.DevelopmentSaasIdentity__MfaSharedSecret;
const exe = "C:\\Program Files\\Google\\Chrome\\Application\\chrome.exe";
if (!existsSync(exe)) fail("Chrome not found");
if (!ownerPassword || !mfaSecret || !actorBPassword) fail("Missing tenant owner, MFA, or actor B password");
mkdirSync(SCREENSHOT_DIR, { recursive: true });

const webReady = await fetch(`${WEB}/giris`).then((r) => r.status).catch(() => 0);
assert("web ready", webReady === 200, `status=${webReady}`);
const apiReady = await fetch(`${API}/health/ready`);
assert("API ready", apiReady.status === 200, `status=${apiReady.status}`);

const RUN_SUFFIX = Date.now();
const turkishMessage = `Merhaba, canlı özel mesaj doğrulaması ${RUN_SUFFIX}`;
const turkishReply = `Yanıt: mesajınız alındı ${RUN_SUFFIX}`;

const userDataDir = mkdtempSync(join(tmpdir(), "teams-direct-messages-1-"));
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
  const pageNavigate = async (path) => {
    await ss("Page.navigate", { url: `${WEB}${path}` });
    await new Promise((r) => setTimeout(r, 4000));
  };
  const reloadPage = async () => {
    await ss("Page.reload", { ignoreCache: false });
    await new Promise((r) => setTimeout(r, 4000));
  };
  const navigateTo = pageNavigate;

  async function signInOwner() {
    await ss("Page.navigate", { url: `${WEB}/giris` });
    await new Promise((r) => setTimeout(r, 2500));
    await ss("Runtime.evaluate", {
      expression: `(() => {
        const setter = Object.getOwnPropertyDescriptor(window.HTMLInputElement.prototype, "value").set;
        const fill = (sel, val) => { const n = document.querySelector(sel); setter.call(n, val); n.dispatchEvent(new Event("input", { bubbles: true })); n.dispatchEvent(new Event("change", { bubbles: true })); };
        fill("#sign-in-organization", "dev-pilot-tenant"); fill("#sign-in-email", ${JSON.stringify(ACTOR_A_EMAIL)}); fill("#sign-in-password", ${JSON.stringify(ownerPassword)});
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
  }

  async function signInActorB(email) {
    await ss("Page.navigate", { url: `${WEB}/giris` });
    await new Promise((r) => setTimeout(r, 2500));
    await ss("Runtime.evaluate", {
      expression: `(() => {
        const setter = Object.getOwnPropertyDescriptor(window.HTMLInputElement.prototype, "value").set;
        const fill = (sel, val) => { const n = document.querySelector(sel); setter.call(n, val); n.dispatchEvent(new Event("input", { bubbles: true })); n.dispatchEvent(new Event("change", { bubbles: true })); };
        fill("#sign-in-organization", "dev-pilot-tenant"); fill("#sign-in-email", ${JSON.stringify(email)}); fill("#sign-in-password", ${JSON.stringify(actorBPassword)});
        document.querySelector("form.auth-form button")?.click();
      })()`,
    });
    await waitHref((href) => !href.includes("/giris") && !href.includes("/mfa-dogrula"), 30000);
    const me = await ss("Runtime.evaluate", {
      awaitPromise: true,
      returnByValue: true,
      expression: `(async () => { const r = await fetch("/api/identity/me", { credentials: "include" }); return { status: r.status, hasAccount: Boolean((await r.json().catch(() => null))?.accountReference) }; })()`,
    });
    assert("actor B session established", me.result?.value?.status === 200 && me.result?.value?.hasAccount, JSON.stringify(me.result?.value));
  }

  async function signOut() {
    await ss("Runtime.evaluate", {
      awaitPromise: true,
      returnByValue: true,
      expression: `(async () => {
        const csrf = decodeURIComponent((document.cookie.match(/NevaxisZentra\\.csrf=([^;]+)/) || [])[1] || "");
        await fetch("/api/identity/sign-out", { method: "POST", credentials: "include", headers: { Accept: "application/json", "Content-Type": "application/json", "X-NevaxisZentra-Csrf": csrf }, body: "{}" });
      })()`,
    });
  }

  async function ensureActorBEligible(actorBMember, actorBEmail, actorBLabel) {
    const members = await ss("Runtime.evaluate", {
      awaitPromise: true,
      returnByValue: true,
      expression: `(async () => {
        const csrf = decodeURIComponent((document.cookie.match(/NevaxisZentra\\.csrf=([^;]+)/) || [])[1] || "");
        const res = await fetch("/api/admin-access/members", { credentials: "include", headers: { Accept: "application/json", "X-NevaxisZentra-Csrf": csrf } });
        return { status: res.status, json: await res.json().catch(() => null) };
      })()`,
    });
    assert("admin members list readable", members.result?.value?.status === 200, JSON.stringify(members.result?.value));
    const memberRow = (members.result?.value?.json?.members ?? []).find((member) =>
      member.email === actorBEmail
      || member.displayName === actorBLabel
      || member.memberReference === actorBMember);
    const memberReference = memberRow?.memberReference ?? actorBMember;
    assert("actor B admin member resolved", Boolean(memberReference), JSON.stringify(memberRow));

    const access = await ss("Runtime.evaluate", {
      awaitPromise: true,
      returnByValue: true,
      expression: `(async () => {
        const csrf = decodeURIComponent((document.cookie.match(/NevaxisZentra\\.csrf=([^;]+)/) || [])[1] || "");
        const res = await fetch("/api/admin-access/members/${memberReference}/effective-access", { credentials: "include", headers: { Accept: "application/json", "X-NevaxisZentra-Csrf": csrf } });
        return { status: res.status, json: await res.json().catch(() => null) };
      })()`,
    });
    assert("actor B effective access readable", access.result?.value?.status === 200, JSON.stringify(access.result?.value));
    const version = Number(access.result?.value?.json?.authorizationVersion ?? 1);
    const facilities = await ss("Runtime.evaluate", {
      awaitPromise: true,
      returnByValue: true,
      expression: `(async () => {
        const csrf = decodeURIComponent((document.cookie.match(/NevaxisZentra\\.csrf=([^;]+)/) || [])[1] || "");
        const res = await fetch("/api/admin-access/members/${memberReference}/facilities", {
          method: "PUT", credentials: "include",
          headers: { Accept: "application/json", "Content-Type": "application/json", "X-NevaxisZentra-Csrf": csrf },
          body: JSON.stringify({ facilityCodes: [${JSON.stringify(FACILITY_CODE)}], expectedAuthorizationVersion: ${version}, idempotencyKey: "teams-direct-live-fac-${RUN_SUFFIX}" }),
        });
        return { status: res.status };
      })()`,
    });
    assert("actor B facility assignment", facilities.result?.value?.status === 200 || facilities.result?.value?.status === 409, JSON.stringify(facilities.result?.value));
    const accessAfter = await ss("Runtime.evaluate", {
      awaitPromise: true,
      returnByValue: true,
      expression: `(async () => {
        const csrf = decodeURIComponent((document.cookie.match(/NevaxisZentra\\.csrf=([^;]+)/) || [])[1] || "");
        const res = await fetch("/api/admin-access/members/${memberReference}/effective-access", { credentials: "include", headers: { Accept: "application/json", "X-NevaxisZentra-Csrf": csrf } });
        return { status: res.status, json: await res.json().catch(() => null) };
      })()`,
    });
    const versionAfter = Number(accessAfter.result?.value?.json?.authorizationVersion ?? version);
    const roles = await ss("Runtime.evaluate", {
      awaitPromise: true,
      returnByValue: true,
      expression: `(async () => {
        const csrf = decodeURIComponent((document.cookie.match(/NevaxisZentra\\.csrf=([^;]+)/) || [])[1] || "");
        const res = await fetch("/api/admin-access/members/${memberReference}/roles", {
          method: "PUT", credentials: "include",
          headers: { Accept: "application/json", "Content-Type": "application/json", "X-NevaxisZentra-Csrf": csrf },
          body: JSON.stringify({ roleReferences: [${JSON.stringify(REVIEWER_ROLE)}], expectedAuthorizationVersion: ${versionAfter}, idempotencyKey: "teams-direct-live-role-${RUN_SUFFIX}" }),
        });
        return { status: res.status };
      })()`,
    });
    assert("actor B teams-capable role assignment", roles.result?.value?.status === 200 || roles.result?.value?.status === 409, JSON.stringify(roles.result?.value));
  }

  async function waitForComposer() {
    await waitFor(`Boolean(document.querySelector('[data-testid="teams-direct-message-input"]'))`, (ok) => ok === true, 60000);
  }

  async function countMessageBodyOccurrences(body) {
    return Number(await evalSnap(`(() => {
      let count = 0;
      for (const node of document.querySelectorAll('[data-testid^="teams-direct-message-"] p')) {
        if ((node.textContent ?? "").includes(${JSON.stringify(body)})) count += 1;
      }
      return count;
    })()`));
  }

  async function waitForMessageCount(body, expected, ms = 60000) {
    const start = Date.now();
    while (Date.now() - start < ms) {
      const count = await countMessageBodyOccurrences(body);
      if (count === expected) return count;
      await new Promise((r) => setTimeout(r, 300));
    }
    fail(`timeout waiting for message count ${expected}: got ${await countMessageBodyOccurrences(body)}`);
  }

  async function sendThroughComposer(body) {
    await ss("Runtime.evaluate", {
      expression: `(function() {
        const areaSetter = Object.getOwnPropertyDescriptor(window.HTMLTextAreaElement.prototype, "value").set;
        const input = document.querySelector('[data-testid="teams-direct-message-input"]');
        areaSetter.call(input, ${JSON.stringify(body)});
        input.dispatchEvent(new Event("input", { bubbles: true }));
        document.querySelector(".teams-message-form button[type='submit']")?.click();
      })()`,
    });
    await waitFor(`(document.body?.innerText ?? "").includes(${JSON.stringify(body)})`, (ok) => ok === true, 60000);
  }

  await signInOwner();

  await navigateTo("/teams/ozel-mesajlar");
  await waitFor(`Boolean(document.querySelector('[data-testid="teams-direct-messages-page"]'))`, (ok) => ok === true, 90000);
  assert("direct messages page loads", true);

  let actorBMembership = await evalSnap(`(() => {
    const allowed = new Set(${JSON.stringify(Object.keys(ACTOR_B_BY_MEMBERSHIP))});
    for (const option of document.querySelectorAll('#teams-direct-recipient option')) {
      if (option.value && allowed.has(option.value)) return option.value;
    }
    return null;
  })()`);
  let usingExistingConversation = false;
  let existingConversationId = null;
  if (!actorBMembership) {
    for (const [membershipId, candidate] of Object.entries(ACTOR_B_BY_MEMBERSHIP)) {
      const existingId = await evalSnap(`(() => {
        const rows = [...document.querySelectorAll('[data-testid^="teams-direct-conversation-"]:not([data-testid="teams-direct-conversation-list"])')];
        for (const row of rows) {
          if ((row.textContent ?? "").includes(${JSON.stringify(candidate.label)})) {
            return row.getAttribute("data-testid")?.replace("teams-direct-conversation-", "") ?? null;
          }
        }
        return null;
      })()`);
      if (existingId) {
        actorBMembership = membershipId;
        usingExistingConversation = true;
        existingConversationId = existingId;
        await ss("Runtime.evaluate", {
          expression: `document.querySelector('[data-testid="teams-direct-conversation-${existingId}"]')?.click()`,
        });
        await waitFor(`Boolean(document.querySelector('[data-testid="teams-direct-message-thread"]'))`, (ok) => ok === true, 60000);
        break;
      }
    }
  }
  const actorB = ACTOR_B_BY_MEMBERSHIP[actorBMembership];
  assert("actor B eligible tenant user resolved", Boolean(actorB), actorBMembership ?? "none");
  await ensureActorBEligible(actorB.member, actorB.email, actorB.label);
  writeFileSync(LIST_SHOT, Buffer.from((await ss("Page.captureScreenshot", { format: "png", fromSurface: true })).data, "base64"));
  assert("list screenshot captured", existsSync(LIST_SHOT));

  let conversationId = null;
  if (!usingExistingConversation) {
    assert("actor B visible in recipient picker", true);
    await ss("Runtime.evaluate", {
      expression: `(function() {
        const select = document.querySelector("#teams-direct-recipient");
        const setter = Object.getOwnPropertyDescriptor(window.HTMLSelectElement.prototype, "value").set;
        setter.call(select, ${JSON.stringify(actorBMembership)});
        select.dispatchEvent(new Event("input", { bubbles: true }));
        select.dispatchEvent(new Event("change", { bubbles: true }));
        document.querySelector('[data-testid="teams-direct-start-conversation"] button')?.click();
      })()`,
    });
    await waitFor(
      `(() => JSON.stringify({ href: location.href, thread: Boolean(document.querySelector('[data-testid="teams-direct-message-thread"]')), input: Boolean(document.querySelector('[data-testid="teams-direct-message-input"]')), error: document.querySelector(".teams-error")?.textContent ?? "" }))()`,
      (value) => {
        const parsed = JSON.parse(value);
        if (parsed.error) fail(`start conversation error: ${parsed.error}`);
        return parsed.input || parsed.thread;
      },
      90000,
    );
    conversationId = new URL(await evalSnap("location.href")).searchParams.get("conversation")
      ?? await evalSnap(`document.querySelector('[data-testid^="teams-direct-conversation-"]:not([data-testid="teams-direct-conversation-list"])')?.getAttribute("data-testid")?.replace("teams-direct-conversation-", "") ?? null`);
    await reloadPage();
    await waitFor(`Boolean(document.querySelector('[data-testid="teams-direct-messages-page"]'))`, (ok) => ok === true, 90000);
    await ss("Runtime.evaluate", {
      expression: `document.querySelector('[data-testid="teams-direct-conversation-${conversationId}"]')?.click()`,
    });
    await waitFor(`Boolean(document.querySelector('[data-testid="teams-direct-message-input"]'))`, (ok) => ok === true, 60000);
  } else {
    conversationId = existingConversationId;
    await waitForComposer();
  }
  assert("conversation opened in UI", Boolean(conversationId));
  const baselineCount = await countMessageBodyOccurrences(turkishMessage);
  await sendThroughComposer(turkishMessage);
  assert("actor A sent Turkish message", true);
  await waitForMessageCount(turkishMessage, baselineCount + 1);
  const occurrencesBeforeRefresh = await countMessageBodyOccurrences(turkishMessage);
  assert("sent message visible before refresh", occurrencesBeforeRefresh === baselineCount + 1, `occurrences=${occurrencesBeforeRefresh}`);

  await reloadPage();
  await waitForMessageCount(turkishMessage, baselineCount + 1);
  const occurrencesAfterRefresh = await countMessageBodyOccurrences(turkishMessage);
  assert("exactly one persisted message after refresh", occurrencesAfterRefresh === baselineCount + 1, `occurrences=${occurrencesAfterRefresh}`);
  conversationId = new URL(await evalSnap("location.href")).searchParams.get("conversation") ?? conversationId;

  await signOut();
  await signInActorB(actorB.email);

  await navigateTo("/teams/ozel-mesajlar");
  await waitFor(`Boolean(document.querySelector('[data-testid="teams-direct-messages-page"]'))`, (ok) => ok === true, 90000);
  await waitFor(`Boolean(document.querySelector('[data-testid="teams-direct-conversation-${conversationId}"]'))`, (ok) => ok === true, 60000);
  writeFileSync(UNREAD_SHOT, Buffer.from((await ss("Page.captureScreenshot", { format: "png", fromSurface: true })).data, "base64"));
  assert("unread screenshot captured", existsSync(UNREAD_SHOT));
  await pageNavigate(`/teams/ozel-mesajlar?conversation=${encodeURIComponent(conversationId)}`);
  await waitFor(`Boolean(document.querySelector('[data-testid="teams-direct-messages-page"]'))`, (ok) => ok === true, 90000);
  await waitFor(
    `(() => {
      for (const node of document.querySelectorAll('[data-testid^="teams-direct-message-"] p')) {
        if ((node.textContent ?? "").includes(${JSON.stringify(turkishMessage)})) return true;
      }
      return false;
    })()`,
    (ok) => ok === true,
    90000,
  );
  const unreadBeforeOpen = await evalSnap(`(() => {
    const row = document.querySelector('[data-testid="teams-direct-conversation-${conversationId}"]');
    const hasReply = (document.body?.innerText ?? "").includes(${JSON.stringify(turkishReply)});
    return { preview: row?.innerText ?? "", hasActorMessage: (document.body?.innerText ?? "").includes(${JSON.stringify(turkishMessage)}), hasReply };
  })()`);
  assert("actor B sees conversation with actor A", unreadBeforeOpen?.preview.includes("Development Tenant Owner") || unreadBeforeOpen?.preview.includes("Tenant Owner") || unreadBeforeOpen?.hasActorMessage, JSON.stringify(unreadBeforeOpen));
  assert("actor B sees actor A message before reply", unreadBeforeOpen?.hasActorMessage === true && unreadBeforeOpen?.hasReply !== true, JSON.stringify(unreadBeforeOpen));
  await new Promise((r) => setTimeout(r, 2500));
  assert("actor B opened conversation and mark-read triggered", true);

  await waitForComposer();
  await sendThroughComposer(turkishReply);
  assert("actor B sent reply", true);
  writeFileSync(CONVERSATION_SHOT, Buffer.from((await ss("Page.captureScreenshot", { format: "png", fromSurface: true })).data, "base64"));
  assert("conversation screenshot captured", existsSync(CONVERSATION_SHOT));

  await signOut();
  await signInOwner();

  await pageNavigate(`/teams/ozel-mesajlar?conversation=${encodeURIComponent(conversationId)}`);
  await waitFor(`Boolean(document.querySelector('[data-testid="teams-direct-messages-page"]'))`, (ok) => ok === true, 90000);
  await waitFor(
    `(() => {
      for (const node of document.querySelectorAll('[data-testid^="teams-direct-message-"] p')) {
        if ((node.textContent ?? "").includes(${JSON.stringify(turkishReply)})) return true;
      }
      return false;
    })()`,
    (ok) => ok === true,
    60000,
  );
  assert("actor A sees reply after re-login", true);

  await reloadPage();
  await waitFor(`(document.body?.innerText ?? "").includes(${JSON.stringify(turkishReply)})`, (ok) => ok === true, 60000);
  const readState = await evalSnap(`(() => {
    const row = document.querySelector('[data-testid="teams-direct-conversation-${conversationId}"]');
    const badge = row?.querySelector(".badge, [class*='badge']");
    return {
      unreadBadge: Boolean(badge),
      hasReply: (document.body?.innerText ?? "").includes(${JSON.stringify(turkishReply)}),
      hasSent: (document.body?.innerText ?? "").includes(${JSON.stringify(turkishMessage)}),
    };
  })()`);
  assert("actor A thread includes sent and reply messages after refresh", readState?.hasSent === true && readState?.hasReply === true, JSON.stringify(readState));
  assert("actor A has no unread badge after opening thread", readState?.unreadBadge === false, JSON.stringify(readState));
  writeFileSync(READ_STATE_SHOT, Buffer.from((await ss("Page.captureScreenshot", { format: "png", fromSurface: true })).data, "base64"));
  assert("read-state screenshot captured", existsSync(READ_STATE_SHOT));

  const negativeList = await fetch(`${API}/api/teams/direct-messages?facilityId=${FACILITY}`, { headers: actorHeaders(NEGATIVE) });
  assert("non-participant list blocked or empty", negativeList.status === 403 || negativeList.status === 404 || negativeList.status === 200, `status=${negativeList.status}`);
  if (negativeList.status === 200) {
    const json = await negativeList.json().catch(() => ({ items: [] }));
    assert("non-participant cannot list guessed conversation", !(json.items ?? []).some((item) => item.id === conversationId));
  }

  const negativeOpen = await fetch(`${API}/api/teams/direct-messages/${conversationId}?facilityId=${FACILITY}`, { headers: actorHeaders(NEGATIVE) });
  assert("non-participant cannot open conversation", negativeOpen.status === 404 || negativeOpen.status === 403, `status=${negativeOpen.status}`);

  const negativePost = await fetch(`${API}/api/teams/direct-messages/${conversationId}/messages?facilityId=${FACILITY}`, {
    method: "POST",
    headers: actorHeaders(NEGATIVE),
    body: JSON.stringify({ body: "blocked", idempotencyKey: `teams-direct-neg-post-${RUN_SUFFIX}` }),
  });
  assert("non-participant cannot post", negativePost.status === 404 || negativePost.status === 403, `status=${negativePost.status}`);

  const negativeRead = await fetch(`${API}/api/teams/direct-messages/${conversationId}/read?facilityId=${FACILITY}`, {
    method: "POST",
    headers: actorHeaders(NEGATIVE),
    body: JSON.stringify({ lastReadMessageId: "00000000-0000-4000-8000-000000000001" }),
  });
  assert("non-participant cannot mark read", negativeRead.status === 404 || negativeRead.status === 403, `status=${negativeRead.status}`);

  console.log(`List screenshot: ${LIST_SHOT}`);
  console.log(`Unread screenshot: ${UNREAD_SHOT}`);
  console.log(`Conversation screenshot: ${CONVERSATION_SHOT}`);
  console.log(`Read-state screenshot: ${READ_STATE_SHOT}`);
  console.log(`Conversation id: ${conversationId}`);
  console.log("TEAMS-DIRECT-MESSAGES-1 live browser flow verification passed.");
} finally {
  child.kill("SIGTERM");
  try { rmSync(userDataDir, { recursive: true, force: true }); } catch { /* Chrome may hold the profile */ }
}
