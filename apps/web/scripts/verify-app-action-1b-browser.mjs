#!/usr/bin/env node
import { createHmac } from "node:crypto";
import { existsSync, mkdtempSync, rmSync } from "node:fs";
import { spawn } from "node:child_process";
import { tmpdir } from "node:os";
import { join } from "node:path";

const API = process.env.NEVAXIS_FACTOS_API_BASE_URL ?? "http://127.0.0.1:5000";
const WEB = process.env.NEVAXIS_FACTOS_WEB_BASE_URL ?? "http://127.0.0.1:3001";
const ORGANIZATION = "dev-pilot-tenant";
const VIEWPORTS = [
  { width: 1600, height: 1000 },
  { width: 1024, height: 900 },
];

function fail(message) {
  console.error(`APP-ACTION-1B browser verification failed: ${message}`);
  process.exit(1);
}

function assert(label, ok, detail = "") {
  if (!ok) fail(`${label}${detail ? `: ${detail}` : ""}`);
  console.log(`✓ ${label}`);
}

function requireEnv(name) {
  const value = process.env[name];
  if (!value) fail(`Missing environment variable ${name}`);
  return value;
}

class CookieJar {
  constructor() {
    this.cookies = new Map();
  }

  ingest(response) {
    const setCookies = typeof response.headers.getSetCookie === "function"
      ? response.headers.getSetCookie()
      : [];
    for (const raw of setCookies) {
      const [pair] = raw.split(";");
      const separator = pair.indexOf("=");
      if (separator <= 0) continue;
      this.cookies.set(pair.slice(0, separator).trim(), pair.slice(separator + 1).trim());
    }
  }

  header() {
    return [...this.cookies.entries()].map(([name, value]) => `${name}=${value}`).join("; ");
  }
}

async function api(method, path, body, jar) {
  const headers = { Accept: "application/json" };
  if (body) headers["Content-Type"] = "application/json";
  if (jar?.header()) headers.Cookie = jar.header();
  if (jar?.cookies.get("NevaxisZentra.session") && jar.cookies.get("NevaxisZentra.csrf") && method !== "GET") {
    headers["X-NevaxisZentra-Csrf"] = jar.cookies.get("NevaxisZentra.csrf");
  }
  const response = await fetch(`${API}${path}`, {
    method,
    headers,
    body: body ? JSON.stringify(body) : undefined,
  });
  jar?.ingest(response);
  return { status: response.status };
}

async function signIn(email, password) {
  const jar = new CookieJar();
  const result = await api("POST", "/api/identity/sign-in", {
    organizationContext: ORGANIZATION,
    email,
    password,
    deviceLabel: "app-action-1b-browser",
  }, jar);
  if (result.status !== 200) fail("sign-in failed for persona");
  return jar;
}

async function signInOwner() {
  const secret = requireEnv("DevelopmentSaasIdentity__MfaSharedSecret");
  const jar = await signIn("admin@demo.nevaxiszentra.local", requireEnv("DevelopmentSaasIdentity__TenantOwnerPassword"));
  const alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
  const counter = Math.floor(Date.now() / 1000 / 30);
  const normalized = secret.trim().replace(/=+$/, "").toUpperCase();
  let buffer = 0;
  let bitsLeft = 0;
  const output = [];
  for (const character of normalized) {
    buffer = (buffer << 5) | alphabet.indexOf(character);
    bitsLeft += 5;
    if (bitsLeft >= 8) {
      output.push((buffer >> (bitsLeft - 8)) & 0xff);
      bitsLeft -= 8;
    }
  }
  const counterBytes = Buffer.alloc(8);
  counterBytes.writeBigUInt64BE(BigInt(counter));
  const hash = createHmac("sha1", Buffer.from(output)).update(counterBytes).digest();
  const offset = hash[hash.length - 1] & 0x0f;
  const binaryCode =
    ((hash[offset] & 0x7f) << 24)
    | ((hash[offset + 1] & 0xff) << 16)
    | ((hash[offset + 2] & 0xff) << 8)
    | (hash[offset + 3] & 0xff);
  const totp = String(binaryCode % 1_000_000).padStart(6, "0");
  const verify = await api("POST", "/api/identity/mfa/verify", { totpCode: totp }, jar);
  if (verify.status !== 200) fail("owner MFA failed");
  return jar;
}

function resolveChromiumExecutable() {
  const candidates = [
    process.env.PLAYWRIGHT_CHROMIUM_EXECUTABLE_PATH,
    process.env.CHROME_PATH,
    "C:\\Program Files\\Google\\Chrome\\Application\\chrome.exe",
    "C:\\Program Files (x86)\\Microsoft\\Edge\\Application\\msedge.exe",
    "C:\\Program Files\\Microsoft\\Edge\\Application\\msedge.exe",
  ].filter(Boolean);
  for (const candidate of candidates) {
    if (existsSync(candidate)) return candidate;
  }
  return undefined;
}

async function waitForJson(url, timeoutMs = 20000) {
  const started = Date.now();
  while (Date.now() - started < timeoutMs) {
    try {
      const response = await fetch(url);
      if (response.ok) return await response.json();
    } catch {
      // Chrome DevTools is not listening yet.
    }
    await new Promise((resolvePromise) => setTimeout(resolvePromise, 200));
  }
  fail("Chrome DevTools was not ready.");
}

async function inspectPage(jar, viewport, path) {
  const executablePath = resolveChromiumExecutable();
  if (!executablePath) fail("Chrome executable was not found for the APP-ACTION-1B browser proof.");
  const userDataDir = mkdtempSync(join(tmpdir(), "app-action-1b-chrome-"));
  const port = 9433 + Math.floor(Math.random() * 200);
  const child = spawn(executablePath, [
    `--remote-debugging-port=${port}`,
    "--headless=new",
    "--disable-gpu",
    "--no-first-run",
    "--no-default-browser-check",
    `--window-size=${viewport.width},${viewport.height}`,
    `--user-data-dir=${userDataDir}`,
  ], { stdio: "ignore" });
  try {
    const version = await waitForJson(`http://127.0.0.1:${port}/json/version`);
    const created = await fetch(`http://127.0.0.1:${port}/json/new?about:blank`, { method: "PUT" }).then((r) => r.json());
    const socket = new WebSocket(version.webSocketDebuggerUrl);
    await new Promise((resolveOpen, rejectOpen) => {
      socket.addEventListener("open", resolveOpen);
      socket.addEventListener("error", () => rejectOpen(new Error("Chrome DevTools socket failed.")));
    });
    let nextId = 1;
    const pending = new Map();
    const events = [];
    socket.addEventListener("message", (event) => {
      const message = JSON.parse(String(event.data));
      if (message.id && pending.has(message.id)) {
        const { resolveResult, rejectResult } = pending.get(message.id);
        pending.delete(message.id);
        if (message.error) rejectResult(new Error(message.error.message));
        else resolveResult(message.result);
        return;
      }
      events.push(message);
    });
    const send = (method, params = {}, sessionId) => {
      const id = nextId++;
      const payload = { id, method, params };
      if (sessionId) payload.sessionId = sessionId;
      return new Promise((resolveResult, rejectResult) => {
        pending.set(id, { resolveResult, rejectResult });
        socket.send(JSON.stringify(payload));
      });
    };
    const attached = await send("Target.attachToTarget", { targetId: created.id, flatten: true });
    const sessionId = attached.sessionId;
    const sessionSend = (method, params = {}) => send(method, params, sessionId);
    await sessionSend("Network.enable");
    await sessionSend("Page.enable");
    await sessionSend("Runtime.enable");
    await sessionSend("Log.enable");
    await sessionSend("Emulation.setDeviceMetricsOverride", {
      width: viewport.width,
      height: viewport.height,
      deviceScaleFactor: 1,
      mobile: false,
    });
    const url = new URL(WEB);
    for (const [name, value] of jar.cookies.entries()) {
      await sessionSend("Network.setCookie", {
        name,
        value,
        domain: url.hostname,
        path: "/",
      });
    }
    const navigation = sessionSend("Page.navigate", { url: `${WEB}${path}` });
    await new Promise((resolveEvent, rejectEvent) => {
      const started = Date.now();
      const timer = setInterval(() => {
        const match = events.find((message) =>
          message.method === "Network.responseReceived"
          && message.params?.type === "Document"
          && String(message.params?.response?.url ?? "").split("?")[0] === `${WEB}${path}`);
        if (match) {
          clearInterval(timer);
          resolveEvent(match);
          return;
        }
        if (Date.now() - started > 20000) {
          clearInterval(timer);
          rejectEvent(new Error(`Timed out waiting for ${path} document.`));
        }
      }, 50);
    });
    await navigation;
    await new Promise((resolveWait) => setTimeout(resolveWait, 900));
    const evaluated = await sessionSend("Runtime.evaluate", {
      returnByValue: true,
      expression: `(() => {
        const workspace = document.querySelector(".workspace-selector");
        const search = document.querySelector(".search-placeholder");
        const notifications = document.querySelector(".shell-notifications");
        const account = document.querySelector(".account-placeholder");
        const prohibitedCursor = [...document.querySelectorAll(".dashboard-panel a, .dashboard-panel button")].some((node) => getComputedStyle(node).cursor === "not-allowed" && !node.disabled);
        const overflow = document.documentElement.scrollWidth > document.documentElement.clientWidth + 1;
        const rawUuid = /[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}/i.test(document.body?.innerText || "");
        const orderButtons = [...document.querySelectorAll(".order-draft-action-panel button")];
        return {
          workspaceText: workspace?.textContent?.trim() || "",
          searchText: search?.textContent?.trim() || "",
          notificationsHref: notifications?.getAttribute("href") || "",
          notificationsTitle: notifications?.getAttribute("title") || "",
          accountPresent: Boolean(account),
          prohibitedCursor,
          overflow,
          rawUuid,
          orderButtonCount: orderButtons.length,
          orderButtonsDisabled: orderButtons.every((button) => button.disabled),
          orderButtonTitles: orderButtons.map((button) => button.getAttribute("title") || ""),
          bodyText: (document.body?.innerText || "").slice(0, 5000),
        };
      })()`,
    });
    const consoleErrors = events
      .filter((message) =>
        (message.method === "Runtime.exceptionThrown")
        || (message.method === "Log.entryAdded" && ["error", "exception"].includes(message.params?.entry?.level)))
      .map((message) => String(message.params?.exceptionDetails?.text || message.params?.entry?.text || ""))
      .filter(Boolean);
    socket.close();
    return {
      documentStatus: events.find((message) =>
        message.method === "Network.responseReceived"
        && message.params?.type === "Document"
        && String(message.params?.response?.url ?? "").split("?")[0] === `${WEB}${path}`)
        ?.params?.response?.status,
      page: evaluated.result?.value,
      consoleErrors,
    };
  } finally {
    try {
      spawn("taskkill", ["/pid", String(child.pid), "/T", "/F"], { stdio: "ignore" });
    } catch {
      child.kill();
    }
    await new Promise((resolveWait) => setTimeout(resolveWait, 400));
    try {
      rmSync(userDataDir, { recursive: true, force: true, maxRetries: 3, retryDelay: 200 });
    } catch {
      // Chrome may keep a short lock on the temporary profile after exit.
    }
  }
}

const password = requireEnv("DevelopmentSaasIdentity__ActiveUserPassword");
const personas = [
  { name: "tenant-owner", jar: await signInOwner(), path: "/dashboard" },
  { name: "machine-responsible", jar: await signIn("machine-responsible@dev.local", password), path: "/dashboard" },
  { name: "materials-only", jar: await signIn("materials-sales@dev.local", password), path: "/dashboard" },
  { name: "maintenance-only", jar: await signIn("maintenance-team@dev.local", password), path: "/dashboard" },
  { name: "normal-user", jar: await signIn("active-user@dev.local", password), path: "/dashboard" },
];

for (const persona of personas) {
  for (const viewport of VIEWPORTS) {
    const result = await inspectPage(persona.jar, viewport, persona.path);
    const page = result.page;
    assert(`${persona.name} ${viewport.width}x${viewport.height} document is 200`, result.documentStatus === 200);
    assert(`${persona.name} ${viewport.width}x${viewport.height} workspace selector visible`, page.workspaceText.length > 0);
    assert(`${persona.name} ${viewport.width}x${viewport.height} search label honest`, page.searchText.includes("Modül veya sayfa ara"));
    assert(`${persona.name} ${viewport.width}x${viewport.height} notifications link`, page.notificationsHref.includes("/teams/bildirimler"));
    assert(
      `${persona.name} ${viewport.width}x${viewport.height} notifications title`,
      page.notificationsTitle.includes("Bildirim merkezi"),
    );
    assert(`${persona.name} ${viewport.width}x${viewport.height} account menu present`, page.accountPresent);
    assert(`${persona.name} ${viewport.width}x${viewport.height} no prohibited cursor on dashboard navigation`, page.prohibitedCursor === false);
    assert(`${persona.name} ${viewport.width}x${viewport.height} no horizontal overflow`, page.overflow === false);
    assert(`${persona.name} ${viewport.width}x${viewport.height} no raw UUID`, page.rawUuid === false);
    const hydration = result.consoleErrors.some((text) => /hydration|minified react error|serialization/i.test(text));
    assert(`${persona.name} ${viewport.width}x${viewport.height} no hydration/serialization console error`, hydration === false);
  }
}

const ownerOrder = await inspectPage(await signInOwner(), VIEWPORTS[0], "/siparisler/order-draft-001");
assert("order detail document is 200", ownerOrder.documentStatus === 200);
assert("order action buttons are disabled", ownerOrder.page.orderButtonsDisabled === true);
assert(
  "order unavailable reasons are honest",
  ownerOrder.page.orderButtonTitles.some((title) => title.includes("Onay gönderme servisi henüz kullanıma hazır değil."))
    || ownerOrder.page.orderButtonTitles.some((title) => title.includes("ERP aktarım servisi henüz kullanıma hazır değil.")),
);

console.log("APP-ACTION-1B browser verification passed.");
