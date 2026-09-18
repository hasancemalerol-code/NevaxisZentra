#!/usr/bin/env node
import { existsSync, mkdtempSync, rmSync } from "node:fs";
import { spawn } from "node:child_process";
import { tmpdir } from "node:os";
import { dirname, join, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), "../../..");
const API = process.env.NEVAXIS_FACTOS_API_BASE_URL ?? "http://127.0.0.1:5000";
const WEB = process.env.NEVAXIS_FACTOS_WEB_BASE_URL ?? "http://127.0.0.1:3001";
const ORGANIZATION = "dev-pilot-tenant";
const CLOSURE_PREFIX = "ADMINACCESS1-CLOSURE";
const SALES_MEMBER = "mem_bb000003021340008000000000000213";
const FACILITY_A_ID = "bb000001-0005-4000-8000-000000000005";
const FACILITY_B_ID = "bb000001-0005-4000-8000-000000000099";

function fail(message) {
  console.error(`ADMIN-ACCESS-1 runtime-navigation verification failed: ${message}`);
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

  get(name) {
    return this.cookies.get(name);
  }

  cookieHeader() {
    return [...this.cookies.entries()].map(([name, value]) => `${name}=${value}`).join("; ");
  }

  asPlaywrightCookies(baseUrl) {
    const url = new URL(baseUrl);
    return [...this.cookies.entries()].map(([name, value]) => ({
      name,
      value,
      domain: url.hostname,
      path: "/",
    }));
  }
}

async function api(method, path, body, jar, extraHeaders = {}) {
  const headers = { "Content-Type": "application/json", Accept: "application/json", ...extraHeaders };
  if (jar?.cookieHeader()) headers.Cookie = jar.cookieHeader();
  if (jar?.get("NevaxisZentra.session") && jar.get("NevaxisZentra.csrf") && method !== "GET") {
    headers["X-NevaxisZentra-Csrf"] = jar.get("NevaxisZentra.csrf");
  }
  const response = await fetch(`${API}${path}`, {
    method,
    headers,
    body: body ? JSON.stringify(body) : undefined,
  });
  jar?.ingest(response);
  const text = await response.text();
  let json = null;
  try {
    json = text ? JSON.parse(text) : null;
  } catch {
    json = { raw: text };
  }
  return { status: response.status, json, raw: text };
}

async function signIn(email, password) {
  const jar = new CookieJar();
  const result = await api("POST", "/api/identity/sign-in", {
    organizationContext: ORGANIZATION,
    email,
    password,
    deviceLabel: `${CLOSURE_PREFIX}-runtime`,
  }, jar);
  if (result.status !== 200) fail(`sign-in failed for ${email}: status=${result.status}`);
  return jar;
}

async function signInOwner() {
  const { createHmac } = await import("node:crypto");
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
  if (verify.status !== 200) fail(`owner MFA failed: status=${verify.status}`);
  return jar;
}

function assertPersona(label, access, expected) {
  assert(`${label} effective-access machines`, access.navigation?.machines === expected.machines, JSON.stringify(access.navigation));
  assert(`${label} effective-access materials`, access.navigation?.materials === expected.materials, JSON.stringify(access.navigation));
  assert(`${label} effective-access maintenance`, access.navigation?.maintenance === expected.maintenance, JSON.stringify(access.navigation));
}

async function assertApi(label, jar, path, expectedStatus) {
  const result = await api("GET", path, undefined, jar);
  assert(label, result.status === expectedStatus, `status=${result.status} path=${path}`);
  return result;
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
  fail(`Chrome DevTools was not ready at ${url}`);
}

async function createFlatSession(webSocketUrl, targetId) {
  const socket = new WebSocket(webSocketUrl);
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
    const result = new Promise((resolveResult, rejectResult) => {
      pending.set(id, { resolveResult, rejectResult });
    });
    socket.send(JSON.stringify(payload));
    return result;
  };
  const attached = await send("Target.attachToTarget", { targetId, flatten: true });
  const sessionId = attached.sessionId;
  return {
    async send(method, params = {}) {
      return send(method, params, sessionId);
    },
    waitFor(predicate, timeoutMs = 30000) {
      const started = Date.now();
      return new Promise((resolveEvent, rejectEvent) => {
        const timer = setInterval(() => {
          const match = events.find(predicate);
          if (match) {
            clearInterval(timer);
            resolveEvent(match);
            return;
          }
          if (Date.now() - started > timeoutMs) {
            clearInterval(timer);
            rejectEvent(new Error("Timed out waiting for Chrome DevTools event."));
          }
        }, 50);
      });
    },
    close() {
      socket.close();
    },
  };
}

async function navigateAndStatus(session, url) {
  await session.send("Network.enable");
  await session.send("Page.enable");
  const navigation = session.send("Page.navigate", { url });
  const response = await session.waitFor((message) =>
    message.method === "Network.responseReceived"
    && message.params?.type === "Document"
    && String(message.params?.response?.url ?? "").split("?")[0] === url);
  await navigation;
  await session.send("Page.loadEventFired").catch(() => undefined);
  await new Promise((resolveWait) => setTimeout(resolveWait, 500));
  return Number(response.params.response.status);
}

async function countNavLink(session, name) {
  const result = await session.send("Runtime.evaluate", {
    expression: `document.querySelectorAll('a').length && Array.from(document.querySelectorAll('a')).filter((link) => (link.textContent || '').trim() === ${JSON.stringify(name)}).length`,
    returnByValue: true,
  });
  return Number(result.result?.value ?? 0);
}

async function browserProof(jar, expected) {
  const executablePath = resolveChromiumExecutable();
  if (!executablePath) fail("Chrome executable was not found for the 1600px browser proof.");
  const userDataDir = mkdtempSync(join(tmpdir(), "adminaccess1-chrome-"));
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
  let session;
  try {
    const version = await waitForJson(`http://127.0.0.1:${port}/json/version`);
    const created = await fetch(`http://127.0.0.1:${port}/json/new?about:blank`, { method: "PUT" }).then((r) => r.json());
    session = await createFlatSession(version.webSocketDebuggerUrl, created.id);
    await session.send("Network.enable");
    await session.send("Page.enable");
    await session.send("Emulation.setDeviceMetricsOverride", {
      width: 1600,
      height: 1000,
      deviceScaleFactor: 1,
      mobile: false,
    });
    const url = new URL(WEB);
    for (const cookie of jar.asPlaywrightCookies(WEB)) {
      await session.send("Network.setCookie", {
        name: cookie.name,
        value: cookie.value,
        domain: cookie.domain,
        path: cookie.path,
      });
    }
    await navigateAndStatus(session, `${WEB}/dashboard`);
    const machinesVisible = await countNavLink(session, "Makineler");
    const materialsVisible = await countNavLink(session, "Malzemeler");
    const maintenanceVisible = await countNavLink(session, "Bakım");
    assert("sidebar machines visibility", (machinesVisible > 0) === expected.machines, `count=${machinesVisible}`);
    assert("sidebar materials visibility", (materialsVisible > 0) === expected.materials, `count=${materialsVisible}`);
    assert("sidebar maintenance visibility", (maintenanceVisible > 0) === expected.maintenance, `count=${maintenanceVisible}`);

    const machineStatus = await navigateAndStatus(session, `${WEB}/makineler`);
    assert(expected.machines ? "direct /makineler allowed" : "direct /makineler denied", machineStatus === (expected.machines ? 200 : 403), `status=${machineStatus}`);
    const materialStatus = await navigateAndStatus(session, `${WEB}/malzemeler`);
    assert(expected.materials ? "direct /malzemeler allowed" : "direct /malzemeler denied", materialStatus === (expected.materials ? 200 : 403), `status=${materialStatus}`);
    const maintenanceStatus = await navigateAndStatus(session, `${WEB}/bakim`);
    assert(expected.maintenance ? "direct Maintenance route allowed" : "direct Maintenance route denied", maintenanceStatus === (expected.maintenance ? 200 : 403), `status=${maintenanceStatus}`);
    void url;
  } finally {
    session?.close();
    try {
      spawn("taskkill", ["/pid", String(child.pid), "/T", "/F"], { stdio: "ignore" });
    } catch {
      child.kill();
    }
    await new Promise((resolveWait) => setTimeout(resolveWait, 800));
    try {
      rmSync(userDataDir, { recursive: true, force: true, maxRetries: 3, retryDelay: 200 });
    } catch {
      // Chrome may keep a short lock on the temporary profile after exit.
    }
  }
}

console.log("Running ADMIN-ACCESS-1 runtime-navigation verifier.");
const password = requireEnv("DevelopmentSaasIdentity__ActiveUserPassword");
const runId = `${CLOSURE_PREFIX}-${Date.now()}`;
const owner = await signInOwner();
const startingSales = await api("GET", `/api/admin-access/members/${SALES_MEMBER}/effective-access`, undefined, owner);
const startingRestore = await api("PUT", `/api/admin-access/members/${SALES_MEMBER}/permission-overrides`, {
  overrides: [],
  expectedAuthorizationVersion: Number(startingSales.json?.authorizationVersion ?? 1),
  idempotencyKey: `${runId}-start-restore`,
}, owner);
if (startingRestore.status !== 200 && startingRestore.status !== 409) {
  fail(`could not restore sales overrides before proof: status=${startingRestore.status} ${startingRestore.raw.slice(0, 160)}`);
}

const machineJar = await signIn("machine-responsible@dev.local", password);
const machineAccess = (await api("GET", "/api/admin-access/me/effective-access", undefined, machineJar)).json;
assertPersona("machine-responsible", machineAccess, { machines: true, materials: false, maintenance: false });
await assertApi("machine-responsible /api/edge/machines", machineJar, `/api/edge/machines?facilityId=${FACILITY_A_ID}`, 200);
await assertApi("machine-responsible denied Materials API", machineJar, `/api/materials/workspace-summary?facilityId=${FACILITY_A_ID}`, 403);
await assertApi("machine-responsible denied Maintenance API", machineJar, `/api/maintenance/workspace-summary?facilityId=${FACILITY_A_ID}`, 403);

const salesJar = await signIn("materials-sales@dev.local", password);
const salesAccess = (await api("GET", "/api/admin-access/me/effective-access", undefined, salesJar)).json;
assertPersona("materials-only", salesAccess, { machines: false, materials: true, maintenance: false });
await assertApi("materials-only /api/materials", salesJar, `/api/materials/workspace-summary?facilityId=${FACILITY_A_ID}`, 200);
await assertApi("materials-only denied Machines API", salesJar, `/api/edge/machines?facilityId=${FACILITY_A_ID}`, 403);
await assertApi("materials-only denied Maintenance API", salesJar, `/api/maintenance/workspace-summary?facilityId=${FACILITY_A_ID}`, 403);
await assertApi("facility A result", salesJar, `/api/materials/workspace-summary?facilityId=${FACILITY_A_ID}`, 200);
await assertApi("facility B result", salesJar, `/api/materials/workspace-summary?facilityId=${FACILITY_B_ID}`, 403);

const maintenanceJar = await signIn("maintenance-team@dev.local", password);
const maintenanceAccess = (await api("GET", "/api/admin-access/me/effective-access", undefined, maintenanceJar)).json;
assertPersona("maintenance-only", maintenanceAccess, { machines: false, materials: false, maintenance: true });
await assertApi("maintenance-only Maintenance API", maintenanceJar, `/api/maintenance/workspace-summary?facilityId=${FACILITY_A_ID}`, 200);
await assertApi("maintenance-only denied Machines API", maintenanceJar, `/api/edge/machines?facilityId=${FACILITY_A_ID}`, 403);
await assertApi("maintenance-only denied Materials API", maintenanceJar, `/api/materials/workspace-summary?facilityId=${FACILITY_A_ID}`, 403);

const currentSales = await api("GET", `/api/admin-access/members/${SALES_MEMBER}/effective-access`, undefined, owner);
const deny = await api("PUT", `/api/admin-access/members/${SALES_MEMBER}/permission-overrides`, {
  overrides: [{ permissionKey: "material.read", effect: "deny" }],
  expectedAuthorizationVersion: Number(currentSales.json?.authorizationVersion ?? 1),
  idempotencyKey: `${runId}-runtime-deny`,
}, owner);
assert("explicit deny mutation", deny.status === 200, deny.raw.slice(0, 180));

const deniedSales = (await api("GET", "/api/admin-access/me/effective-access", undefined, salesJar)).json;
assert("explicit-deny navigation payload", deniedSales.navigation?.materials === false, JSON.stringify(deniedSales.navigation));
await assertApi("explicit-deny API", salesJar, `/api/materials/workspace-summary?facilityId=${FACILITY_A_ID}`, 403);
assert("revocation effective without restart", deniedSales.grantedPermissionKeys?.includes("material.read") !== true);

await browserProof(machineJar, { machines: true, materials: false, maintenance: false });
await browserProof(salesJar, { machines: false, materials: false, maintenance: false });
await browserProof(maintenanceJar, { machines: false, materials: false, maintenance: true });

const restore = await api("PUT", `/api/admin-access/members/${SALES_MEMBER}/permission-overrides`, {
  overrides: [],
  expectedAuthorizationVersion: Number((await api("GET", `/api/admin-access/members/${SALES_MEMBER}/effective-access`, undefined, owner)).json?.authorizationVersion ?? 1),
  idempotencyKey: `${runId}-runtime-restore`,
}, owner);
assert("explicit deny restored", restore.status === 200, restore.raw.slice(0, 180));

void ROOT;
console.log("ADMIN-ACCESS-1 runtime-navigation verification passed.");
