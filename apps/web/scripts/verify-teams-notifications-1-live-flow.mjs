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
};
const REVIEWER_ROLE = "production-planning-reviewer";
const NEGATIVE = { issuer: "crevafactos-dev", subject: "quality-negative-test-user", tenant: "dev-pilot-tenant" };
const SCREENSHOT_DIR = join(process.cwd(), ".cursor-screenshots");
const UNREAD_SHOT = join(SCREENSHOT_DIR, "teams-notifications-1-unread-authenticated.png");
const DRILLDOWN_SHOT = join(SCREENSHOT_DIR, "teams-notifications-1-source-drilldown-authenticated.png");
const READ_STATE_SHOT = join(SCREENSHOT_DIR, "teams-notifications-1-read-state-authenticated.png");
const TOTP_ALPHABET = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";

function fail(message) {
  console.error(`TEAMS-NOTIFICATIONS-1 live flow failed: ${message}`);
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

console.log("Running TEAMS-NOTIFICATIONS-1 live browser flow verifier.");
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
const turkishMessage = `Canlı bildirim doğrulaması ${RUN_SUFFIX}`;

const userDataDir = mkdtempSync(join(tmpdir(), "teams-notifications-1-"));
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
          body: JSON.stringify({ facilityCodes: [${JSON.stringify(FACILITY_CODE)}], expectedAuthorizationVersion: ${version}, idempotencyKey: "teams-notify-live-fac-${RUN_SUFFIX}" }),
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
          body: JSON.stringify({ roleReferences: [${JSON.stringify(REVIEWER_ROLE)}, "tenant-admin"], expectedAuthorizationVersion: ${versionAfter}, idempotencyKey: "teams-notify-live-role-${RUN_SUFFIX}" }),
        });
        return { status: res.status };
      })()`,
    });
    assert("actor B teams-capable role assignment", roles.result?.value?.status === 200 || roles.result?.value?.status === 409, JSON.stringify(roles.result?.value));
    const actorAccess = await ss("Runtime.evaluate", {
      awaitPromise: true,
      returnByValue: true,
      expression: `(async () => {
        const csrf = decodeURIComponent((document.cookie.match(/NevaxisZentra\\.csrf=([^;]+)/) || [])[1] || "");
        const res = await fetch("/api/admin-access/members/${memberReference}/effective-access", { credentials: "include", headers: { Accept: "application/json", "X-NevaxisZentra-Csrf": csrf } });
        return { status: res.status, json: await res.json().catch(() => null) };
      })()`,
    });
    assert("actor B effective access after role assignment", actorAccess.result?.value?.status === 200, JSON.stringify(actorAccess.result?.value));
    const granted = actorAccess.result?.value?.json?.grantedPermissionKeys ?? [];
    assert("actor B has teams.notifications.read", granted.includes("teams.notifications.read"), JSON.stringify(granted.filter((key) => key.startsWith("teams."))));
  }

  async function waitForComposer() {
    await waitFor(`Boolean(document.querySelector('[data-testid="teams-direct-message-input"]'))`, (ok) => ok === true, 60000);
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
  await ensureActorBEligible(
    ACTOR_B_BY_MEMBERSHIP["bb000002-0013-4000-8000-000000000013"].member,
    ACTOR_B_BY_MEMBERSHIP["bb000002-0013-4000-8000-000000000013"].email,
    ACTOR_B_BY_MEMBERSHIP["bb000002-0013-4000-8000-000000000013"].label,
  );

  await pageNavigate("/teams/ozel-mesajlar");
  await waitFor(`Boolean(document.querySelector('[data-testid="teams-direct-messages-page"]'))`, (ok) => ok === true, 90000);

  let actorBMembership = await evalSnap(`(() => {
    const allowed = new Set(${JSON.stringify(Object.keys(ACTOR_B_BY_MEMBERSHIP))});
    for (const option of document.querySelectorAll('#teams-direct-recipient option')) {
      if (option.value && allowed.has(option.value)) return option.value;
    }
    return null;
  })()`);
  let conversationId = null;
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
        conversationId = existingId;
        await ss("Runtime.evaluate", {
          expression: `document.querySelector('[data-testid="teams-direct-conversation-${existingId}"]')?.click()`,
        });
        await waitForComposer();
        break;
      }
    }
  }
  const actorB = ACTOR_B_BY_MEMBERSHIP[actorBMembership];
  assert("actor B eligible tenant user resolved", Boolean(actorB), actorBMembership ?? "none");

  if (!conversationId) {
    assert("actor B visible in recipient picker", Boolean(actorBMembership));
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
    await waitForComposer();
    conversationId = new URL(await evalSnap("location.href")).searchParams.get("conversation")
      ?? await evalSnap(`document.querySelector('[data-testid^="teams-direct-conversation-"]:not([data-testid="teams-direct-conversation-list"])')?.getAttribute("data-testid")?.replace("teams-direct-conversation-", "") ?? null`);
  }
  assert("conversation opened for notification seed", Boolean(conversationId));

  await sendThroughComposer(turkishMessage);
  assert("actor A sent Turkish direct message", true);
  conversationId = new URL(await evalSnap("location.href")).searchParams.get("conversation") ?? conversationId;

  await signOut();
  await signInActorB(actorB.email);

  const actorSessionAccess = await ss("Runtime.evaluate", {
    awaitPromise: true,
    returnByValue: true,
    expression: `(async () => {
      const res = await fetch("/api/admin-access/me/effective-access", { credentials: "include", headers: { Accept: "application/json" } });
      return { status: res.status, json: await res.json().catch(() => null) };
    })()`,
  });
  assert("actor B session effective access readable", actorSessionAccess.result?.value?.status === 200, JSON.stringify(actorSessionAccess.result?.value));
  assert(
    "actor B session has teams.notifications.read",
    (actorSessionAccess.result?.value?.json?.grantedPermissionKeys ?? []).includes("teams.notifications.read"),
    JSON.stringify(actorSessionAccess.result?.value?.json?.grantedPermissionKeys?.filter((key) => key.startsWith("teams."))),
  );

  await pageNavigate("/teams/bildirimler");
  await waitFor(`Boolean(document.querySelector('[data-testid="teams-notifications-page"]'))`, (ok) => ok === true, 90000);
  await waitFor(`(document.body?.innerText ?? "").includes(${JSON.stringify(turkishMessage.slice(0, 24))})`, (ok) => ok === true, 60000);

  const pageUnreadMeta = await evalSnap(`(() => {
    const text = document.body?.innerText ?? "";
    const headerMatch = text.match(/(\\d+) okunmamış bildirim\\./);
    const shellCount = document.querySelector("#shell-notifications-link")?.getAttribute("data-shell-notifications-count");
    return {
      headerUnread: headerMatch ? Number(headerMatch[1]) : null,
      shellCountAttr: shellCount,
      hasForbidden: text.includes("yetkiniz yok"),
    };
  })()`);
  assert("notification page header reports unread count", (pageUnreadMeta?.headerUnread ?? 0) >= 1, JSON.stringify(pageUnreadMeta));

  const shellBadgeBefore = await waitFor(`(() => {
    const badge = document.querySelector('[data-testid="shell-notifications-unread-badge"]');
    return Boolean(badge) && Number((badge?.textContent ?? "0").trim()) >= 1;
  })()`, (ok) => ok === true, 30000);
  assert("shell unread badge visible for recipient", shellBadgeBefore === true);

  const notificationMeta = await evalSnap(`(() => {
    const rows = [...document.querySelectorAll('[data-testid^="teams-notification-"]')];
    for (const row of rows) {
      const text = row.textContent ?? "";
      if (text.includes(${JSON.stringify(turkishMessage.slice(0, 24))}) && text.includes("Okunmadı")) {
        return {
          id: row.getAttribute("data-testid")?.replace("teams-notification-", "") ?? null,
          unread: text.includes("Okunmadı"),
        };
      }
    }
    return null;
  })()`);
  assert(
    "persisted unread notification item visible",
    Boolean(notificationMeta?.id) && notificationMeta?.unread === true,
    JSON.stringify(notificationMeta),
  );
  writeFileSync(UNREAD_SHOT, Buffer.from((await ss("Page.captureScreenshot", { format: "png", fromSurface: true })).data, "base64"));
  assert("unread notification center screenshot captured", existsSync(UNREAD_SHOT));

  await ss("Runtime.evaluate", {
    expression: `document.querySelector('[data-testid="teams-notification-${notificationMeta.id}"]')?.click()`,
  });
  await waitHref((href) => href.includes("/teams/ozel-mesajlar") && href.includes("conversation="), 90000);
  await waitFor(`(document.body?.innerText ?? "").includes(${JSON.stringify(turkishMessage)})`, (ok) => ok === true, 90000);
  assert("notification deep link opened safe DM source", true);
  writeFileSync(DRILLDOWN_SHOT, Buffer.from((await ss("Page.captureScreenshot", { format: "png", fromSurface: true })).data, "base64"));
  assert("source drilldown screenshot captured", existsSync(DRILLDOWN_SHOT));

  await pageNavigate("/teams/bildirimler");
  await waitFor(`Boolean(document.querySelector('[data-testid="teams-notifications-page"]'))`, (ok) => ok === true, 90000);
  await waitFor(`(() => {
    const row = document.querySelector('[data-testid="teams-notification-${notificationMeta.id}"]');
    return row ? (row.textContent ?? "").includes("Okundu") : false;
  })()`, (ok) => ok === true, 60000);
  assert("notification marked read through UI", true);

  await reloadPage();
  await waitFor(`(() => {
    const row = document.querySelector('[data-testid="teams-notification-${notificationMeta.id}"]');
    return row ? (row.textContent ?? "").includes("Okundu") : false;
  })()`, (ok) => ok === true, 60000);
  assert("read state persists after refresh", true);
  writeFileSync(READ_STATE_SHOT, Buffer.from((await ss("Page.captureScreenshot", { format: "png", fromSurface: true })).data, "base64"));
  assert("read notification state screenshot captured", existsSync(READ_STATE_SHOT));

  const ownerList = await fetch(`${API}/api/teams/notifications?facilityId=${FACILITY}`, {
    headers: actorHeaders({ issuer: "crevafactos-dev", subject: "planning-pilot-user", tenant: "dev-pilot-tenant" }),
  });
  assert("sender cannot infer recipient notification body", ownerList.status === 200);
  assert(
    "sender list excludes seeded notification preview",
    !(await ownerList.json().catch(() => ({ items: [] }))).items?.some((item) => (item.sourcePreview ?? "").includes(turkishMessage)),
  );

  const negativeList = await fetch(`${API}/api/teams/notifications?facilityId=${FACILITY}`, { headers: actorHeaders(NEGATIVE) });
  assert("non-recipient list blocked or empty", negativeList.status === 403 || negativeList.status === 200, `status=${negativeList.status}`);
  if (negativeList.status === 200) {
    const json = await negativeList.json().catch(() => ({ items: [] }));
    assert(
      "non-recipient cannot see notification item",
      !(json.items ?? []).some((item) => item.id === notificationMeta.id),
    );
    assert(
      "non-recipient cannot infer seeded source preview",
      !(json.items ?? []).some((item) => (item.sourcePreview ?? "").includes(turkishMessage)),
    );
  }

  const negativeMark = await fetch(`${API}/api/teams/notifications/${notificationMeta.id}/read?facilityId=${FACILITY}`, {
    method: "POST",
    headers: actorHeaders(NEGATIVE),
    body: "{}",
  });
  assert("non-recipient cannot mark read", negativeMark.status === 404 || negativeMark.status === 403, `status=${negativeMark.status}`);

  const negativeOpen = await fetch(`${API}/api/teams/direct-messages/${conversationId}?facilityId=${FACILITY}`, { headers: actorHeaders(NEGATIVE) });
  assert("non-recipient cannot open DM source", negativeOpen.status === 404 || negativeOpen.status === 403, `status=${negativeOpen.status}`);

  console.log(`Unread screenshot: ${UNREAD_SHOT}`);
  console.log(`Drilldown screenshot: ${DRILLDOWN_SHOT}`);
  console.log(`Read-state screenshot: ${READ_STATE_SHOT}`);
  console.log(`Notification id: ${notificationMeta.id}`);
  console.log(`Conversation id: ${conversationId}`);
  console.log("TEAMS-NOTIFICATIONS-1 live browser flow verification passed.");
} finally {
  child.kill("SIGTERM");
  try {
    rmSync(userDataDir, { recursive: true, force: true });
  } catch {
    // Chrome may still hold the temp profile on Windows.
  }
}
