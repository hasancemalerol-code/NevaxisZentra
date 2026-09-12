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
  console.error(`DASHBOARD action browser verification failed: ${message}`);
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
  if (jar?.cookies.get("NevaxisFactOS.session") && jar.cookies.get("NevaxisFactOS.csrf") && method !== "GET") {
    headers["X-NevaxisFactOS-Csrf"] = jar.cookies.get("NevaxisFactOS.csrf");
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
    deviceLabel: "dashboard-action-browser",
  }, jar);
  if (result.status !== 200) fail("sign-in failed for persona");
  return jar;
}

async function signInOwner() {
  const secret = requireEnv("DevelopmentSaasIdentity__MfaSharedSecret");
  const jar = await signIn("tenant-owner@dev.local", requireEnv("DevelopmentSaasIdentity__TenantOwnerPassword"));
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

async function inspectDashboard(jar, viewport) {
  const executablePath = resolveChromiumExecutable();
  if (!executablePath) fail("Chrome executable was not found for the dashboard browser proof.");
  const userDataDir = mkdtempSync(join(tmpdir(), "dashboard-action-chrome-"));
  const port = 9333 + Math.floor(Math.random() * 200);
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
    const navigation = sessionSend("Page.navigate", { url: `${WEB}/dashboard` });
    await new Promise((resolveEvent, rejectEvent) => {
      const started = Date.now();
      const timer = setInterval(() => {
        const match = events.find((message) =>
          message.method === "Network.responseReceived"
          && message.params?.type === "Document"
          && String(message.params?.response?.url ?? "").split("?")[0] === `${WEB}/dashboard`);
        if (match) {
          clearInterval(timer);
          resolveEvent(match);
          return;
        }
        if (Date.now() - started > 20000) {
          clearInterval(timer);
          rejectEvent(new Error("Timed out waiting for dashboard document."));
        }
      }, 50);
    });
    await navigation;
    await sessionSend("Page.loadEventFired").catch(() => undefined);
    await new Promise((resolveWait) => setTimeout(resolveWait, 800));
    await sessionSend("Runtime.evaluate", {
      expression: `(() => {
        const teams = document.querySelector(".command-center__teams-button");
        const focusable = [...document.querySelectorAll('a[href], button:not([disabled]), input, select, textarea, [tabindex]:not([tabindex="-1"])')];
        const teamsIndex = focusable.indexOf(teams);
        if (teamsIndex > 0) focusable[teamsIndex - 1].focus();
        else if (teams) teams.focus();
        return true;
      })()`,
    });
    await sessionSend("Input.dispatchKeyEvent", {
      type: "keyDown",
      key: "Tab",
      code: "Tab",
      windowsVirtualKeyCode: 9,
    });
    await sessionSend("Input.dispatchKeyEvent", {
      type: "keyUp",
      key: "Tab",
      code: "Tab",
      windowsVirtualKeyCode: 9,
    });
    await new Promise((resolveWait) => setTimeout(resolveWait, 200));
    const evaluated = await sessionSend("Runtime.evaluate", {
      returnByValue: true,
      expression: `(() => {
        const commandCenter = document.querySelector(".command-center");
        const aiButton = document.querySelector(".command-center__ai-task-button");
        const teams = document.querySelector(".command-center__teams-button");
        const quickHrefs = [...document.querySelectorAll("a.locked-quick-action")].map((node) => node.getAttribute("href"));
        const hashLinks = [...document.querySelectorAll("a[href='#']")].length;
        const inspectButtons = [...document.querySelectorAll(".dashboard-panel--ai-task-center button[disabled]")];
        const inspectLinks = [...document.querySelectorAll(".dashboard-panel--ai-task-center a[href^='/ai-gorevleri/']")].map((node) => node.getAttribute("href"));
        const operationalSummary = [...document.querySelectorAll("a")].find((node) => (node.textContent || "").includes("Detaylı özeti görüntüle"));
        const prohibitedCursor = [...document.querySelectorAll(".dashboard-panel--ai-task-center a, .dashboard-panel--ai-task-center button")].some((node) => getComputedStyle(node).cursor === "not-allowed" && !node.disabled);
        const footer = [...document.querySelectorAll("a")].find((node) => (node.textContent || "").includes("Tüm AI görevlerini görüntüle"));
        const overflow = document.documentElement.scrollWidth > document.documentElement.clientWidth + 1;
        const rawUuid = /[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}/i.test(commandCenter?.innerText || "");
        const rawEnum = /\\b(on-track|awaiting-approval|in-progress|needs-review)\\b/.test(commandCenter?.innerText || "");
        const teamsFocused = document.activeElement === teams;
        const focusVisible = Boolean(teams?.matches(":focus-visible"));
        const focusOutline = teams ? getComputedStyle(teams).outlineStyle : "none";
        const cssDefinesFocus = [...document.styleSheets].some((sheet) => {
          try {
            return [...sheet.cssRules].some((rule) => String(rule.selectorText || "").includes(".command-center__teams-button:focus-visible"));
          } catch {
            return false;
          }
        });
        return {
          title: document.querySelector("h1")?.textContent?.trim() || "",
          aiDisabled: Boolean(aiButton?.disabled || aiButton?.getAttribute("aria-disabled") === "true"),
          aiReason: (aiButton?.getAttribute("title") || "") + " " + (aiButton?.textContent || ""),
          teamsHref: teams?.getAttribute("href") || "",
          teamsFocused,
          focusVisible,
          focusOutline,
          cssDefinesFocus,
          quickHrefs,
          hashLinks,
          inspectDisabled: inspectButtons.length > 0,
          inspectLinks,
          operationalSummaryHref: operationalSummary?.getAttribute("href") || "",
          prohibitedCursor,
          footerHref: footer?.getAttribute("href") || "",
          overflow,
          rawUuid,
          rawEnum,
          bodyText: (commandCenter?.innerText || "").slice(0, 4000),
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
        && String(message.params?.response?.url ?? "").split("?")[0] === `${WEB}/dashboard`)
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
  { name: "tenant-owner", jar: await signInOwner(), allowedQuick: ["/", "/uretim", "/planlama", "/bakim", "/kalite", "/erp-entegrasyonlari/senkronizasyonlar"] },
  { name: "machine-responsible", jar: await signIn("machine-responsible@dev.local", password), allowedQuick: ["/"] },
  { name: "materials-only", jar: await signIn("materials-sales@dev.local", password), allowedQuick: ["/"] },
  { name: "maintenance-only", jar: await signIn("maintenance-team@dev.local", password), allowedQuick: ["/", "/bakim"] },
];

for (const persona of personas) {
  for (const viewport of VIEWPORTS) {
    const result = await inspectDashboard(persona.jar, viewport);
    const page = result.page;
    assert(`${persona.name} ${viewport.width}x${viewport.height} dashboard document is 200`, result.documentStatus === 200);
    assert(`${persona.name} ${viewport.width}x${viewport.height} heading is dashboard`, page.title.includes("AI Görev Merkezi"));
    assert(`${persona.name} ${viewport.width}x${viewport.height} AI create is disabled`, page.aiDisabled);
    assert(
      `${persona.name} ${viewport.width}x${viewport.height} AI reason is Turkish and honest`,
      page.aiReason.includes("AI görev oluşturma akışı henüz kullanıma hazır değil."),
    );
    assert(`${persona.name} ${viewport.width}x${viewport.height} Teams href is /teams`, page.teamsHref === "/teams");
    assert(`${persona.name} ${viewport.width}x${viewport.height} no href-hash`, page.hashLinks === 0);
    if (persona.name === "tenant-owner") {
      assert(`${persona.name} ${viewport.width}x${viewport.height} AI inspect links are canonical`, page.inspectLinks.length >= 3);
      assert(`${persona.name} ${viewport.width}x${viewport.height} no prohibited cursor on authorized navigation`, page.prohibitedCursor === false);
      assert(`${persona.name} ${viewport.width}x${viewport.height} operational summary links to proses-oee`, page.operationalSummaryHref === "/proses-oee");
    } else {
      assert(`${persona.name} ${viewport.width}x${viewport.height} AI inspect has no broken detail links`, page.inspectLinks.every((href) => href.startsWith("/ai-gorevleri/ai-task-")));
    }
    assert(`${persona.name} ${viewport.width}x${viewport.height} AI footer goes to Task Center`, page.footerHref === "/");
    const operational = page.quickHrefs.filter((href) =>
      ["/uretim", "/planlama", "/bakim", "/kalite", "/erp-entegrasyonlari/senkronizasyonlar"].includes(href));
    const unexpected = operational.filter((href) => !persona.allowedQuick.includes(href));
    assert(`${persona.name} ${viewport.width}x${viewport.height} no unauthorized operational quick action`, unexpected.length === 0, unexpected.join(", "));
    assert(`${persona.name} ${viewport.width}x${viewport.height} no horizontal overflow`, page.overflow === false);
    assert(`${persona.name} ${viewport.width}x${viewport.height} no raw UUID`, page.rawUuid === false);
    assert(`${persona.name} ${viewport.width}x${viewport.height} no raw enum`, page.rawEnum === false);
    const hydration = result.consoleErrors.some((text) => /hydration|minified react error|serialization/i.test(text));
    assert(`${persona.name} ${viewport.width}x${viewport.height} no hydration/serialization console error`, hydration === false);
    if (persona.name === "tenant-owner" && viewport.width === 1600) {
      assert("keyboard can focus Teams", page.teamsFocused === true);
      assert(
        "visible focus is present on Teams",
        page.focusVisible === true || (page.cssDefinesFocus === true && page.focusOutline !== "none"),
      );
    }
    assert(
      `${persona.name} ${viewport.width}x${viewport.height} no fake success`,
      !/görev oluşturuldu|işlem başarıyla/i.test(page.bodyText),
    );
  }
}

console.log("DASHBOARD action browser verification passed.");
