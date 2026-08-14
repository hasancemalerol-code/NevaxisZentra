#!/usr/bin/env node
import { createHmac } from "node:crypto";
import { existsSync } from "node:fs";
import { spawn } from "node:child_process";
import { mkdtempSync, rmSync } from "node:fs";
import { tmpdir } from "node:os";
import { join } from "node:path";

const API = process.env.CREVAFACTOS_API_BASE_URL ?? "http://127.0.0.1:5000";
const WEB = process.env.CREVAFACTOS_WEB_BASE_URL ?? "http://127.0.0.1:3001";
const ORGANIZATION = "dev-pilot-tenant";
const FACILITY_A_ID = "bb000001-0005-4000-8000-000000000005";
const FACILITY_B_ID = "bb000001-0005-4000-8000-000000000099";

function fail(message) {
  console.error(`APP-ACTION permission matrix verification failed: ${message}`);
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
  if (jar?.cookies.get("crevafactos.session") && jar.cookies.get("crevafactos.csrf") && method !== "GET") {
    headers["X-CrevaFactOS-Csrf"] = jar.cookies.get("crevafactos.csrf");
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
    deviceLabel: "app-action-security-closure",
  }, jar);
  if (result.status !== 200) fail(`sign-in failed for persona`);
  return jar;
}

async function signInOwner() {
  const { createHmac: hmac } = await import("node:crypto");
  void hmac;
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

async function fetchWeb(path, jar) {
  const response = await fetch(`${WEB}${path}`, {
    headers: { Cookie: jar.header(), Accept: "text/html" },
    redirect: "manual",
  });
  const html = response.status >= 300 && response.status < 400 ? "" : await response.text();
  return { status: response.status, html };
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

async function browserStatus(jar, path) {
  const executablePath = resolveChromiumExecutable();
  if (!executablePath) fail("Chrome executable was not found for the 1600x1000 proof.");
  const userDataDir = mkdtempSync(join(tmpdir(), "app-action-chrome-"));
  const port = 9333 + Math.floor(Math.random() * 200);
  const child = spawn(executablePath, [
    `--remote-debugging-port=${port}`,
    "--headless=new",
    "--disable-gpu",
    "--no-first-run",
    "--no-default-browser-check",
    "--window-size=1600,1000",
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
    await sessionSend("Emulation.setDeviceMetricsOverride", {
      width: 1600,
      height: 1000,
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
    const response = await new Promise((resolveEvent, rejectEvent) => {
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
          rejectEvent(new Error("Timed out waiting for document response."));
        }
      }, 50);
    });
    await navigation;
    socket.close();
    return Number(response.params.response.status);
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
const owner = await signInOwner();
const machine = await signIn("machine-responsible@dev.local", password);
const sales = await signIn("materials-sales@dev.local", password);
const maintenance = await signIn("maintenance-team@dev.local", password);

const protectedRoutes = ["/bakim/planlama", "/kaliplar", "/vardiyalar", "/proses-oee"];

const ownerPlanlama = await fetchWeb("/bakim/planlama", owner);
const ownerMolds = await fetchWeb("/kaliplar", owner);
const ownerShifts = await fetchWeb("/vardiyalar", owner);
const ownerOee = await fetchWeb("/proses-oee", owner);
assert("tenant-owner /bakim/planlama allowed", ownerPlanlama.status === 200);
assert("tenant-owner /kaliplar allowed", ownerMolds.status === 200);
assert("tenant-owner /vardiyalar allowed", ownerShifts.status === 200);
assert("tenant-owner /proses-oee allowed", ownerOee.status === 200);

const machineAllowed = await fetchWeb("/makineler", machine);
assert("machine-responsible /makineler allowed", machineAllowed.status === 200);
for (const path of protectedRoutes) {
  const result = await fetchWeb(path, machine);
  assert(`machine-responsible ${path} denied`, result.status === 403);
  assert(
    `machine-responsible ${path} does not render the protected workspace`,
    result.html.includes("Erişim reddedildi") || !result.html.includes("command-center"),
  );
}

const salesAllowed = await fetchWeb("/malzemeler", sales);
assert("materials-only /malzemeler allowed", salesAllowed.status === 200);
for (const path of protectedRoutes) {
  const result = await fetchWeb(path, sales);
  assert(`materials-only ${path} denied`, result.status === 403);
}

const maintenanceHome = await fetchWeb("/bakim", maintenance);
const maintenancePlan = await fetchWeb("/bakim/planlama", maintenance);
assert("maintenance-only /bakim allowed", maintenanceHome.status === 200);
assert("maintenance-only /bakim/planlama allowed", maintenancePlan.status === 200);
for (const path of ["/kaliplar", "/vardiyalar", "/proses-oee"]) {
  const result = await fetchWeb(path, maintenance);
  assert(`maintenance-only ${path} denied`, result.status === 403);
}

const machineFacilityA = await api("GET", `/api/edge/machines?facilityId=${FACILITY_A_ID}`, undefined, machine);
const machineFacilityB = await api("GET", `/api/edge/machines?facilityId=${FACILITY_B_ID}`, undefined, machine);
assert("facility A remains allowed for machine-responsible", machineFacilityA.status === 200);
assert("facility B remains denied", machineFacilityB.status === 403);

const machineMoldsApi = await api("GET", `/api/molds?facilityId=${FACILITY_A_ID}`, undefined, machine);
assert("machine-responsible molds API denied", machineMoldsApi.status === 403 || machineMoldsApi.status === 404);

const browserDenied = await browserStatus(machine, "/bakim/planlama");
assert("direct browser entry /bakim/planlama is 403", browserDenied === 403);
const browserAllowed = await browserStatus(machine, "/makineler");
assert("direct browser entry /makineler is 200", browserAllowed === 200);

console.log("APP-ACTION permission matrix verification passed.");
