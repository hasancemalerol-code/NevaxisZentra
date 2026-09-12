#!/usr/bin/env node
import { createHmac } from "node:crypto";
import { spawnSync } from "node:child_process";

const API = process.env.NEVAXIS_FACTOS_API_BASE_URL ?? "http://127.0.0.1:5000";
const WEB = process.env.NEVAXIS_FACTOS_WEB_BASE_URL ?? "http://127.0.0.1:3001";
const ORGANIZATION = "dev-pilot-tenant";
const FACILITY_A = "bb000001-0005-4000-8000-000000000005";
const FACILITY_B = "bb000001-0005-4000-8000-000000000099";
const CROSS_TENANT_ID = "aa000000-0000-4000-8000-000000000099";

function fail(message) {
  console.error(`APP-ACTION-1A permission verification failed: ${message}`);
  process.exit(1);
}

function assert(label, ok, detail = "") {
  if (!ok) fail(`${label}${detail ? `: ${detail}` : ""}`);
  console.log(`✓ ${label}`);
}

function requireEnv(name) {
  const value = process.env[name];
  if (!value) fail(`Missing required environment variable ${name}`);
  return value;
}

function generateTotp(secret) {
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
  return String(binaryCode % 1_000_000).padStart(6, "0");
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
  let payload = null;
  const contentType = response.headers.get("content-type") ?? "";
  if (contentType.includes("application/json")) {
    try {
      payload = await response.json();
    } catch {
      payload = null;
    }
  }
  return { status: response.status, payload };
}

async function signIn(email, password) {
  const jar = new CookieJar();
  const result = await api("POST", "/api/identity/sign-in", {
    organizationContext: ORGANIZATION,
    email,
    password,
    deviceLabel: "app-action-1a-permissions",
  }, jar);
  if (result.status !== 200) fail(`sign-in failed for ${email}`);
  return jar;
}

async function signInOwner() {
  const jar = await signIn(
    "admin@demo.nevaxisfactos.local",
    requireEnv("DevelopmentSaasIdentity__TenantOwnerPassword"),
  );
  const verify = await api("POST", "/api/identity/mfa/verify", {
    totpCode: generateTotp(requireEnv("DevelopmentSaasIdentity__MfaSharedSecret")),
  }, jar);
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

const createBody = {
  assetType: "machine",
  assetReference: "APP-ACTION-1A-DENIED",
  type: "fault",
  priority: "normal",
  title: "APP-ACTION-1A denied write",
  description: "Must not persist",
  source: "manual",
};

const password = requireEnv("DevelopmentSaasIdentity__ActiveUserPassword");
const owner = await signInOwner();
const maintenance = await signIn("maintenance-team@dev.local", password);
const machine = await signIn("machine-responsible@dev.local", password);
const materials = await signIn("materials-sales@dev.local", password);
const readOnly = await signIn("active-user@dev.local", password);

const ownerHome = await fetchWeb("/bakim", owner);
const ownerCreate = await fetchWeb("/bakim/arizalar/yeni", owner);
assert("tenant-owner/admin can open maintenance", ownerHome.status === 200);
assert("tenant-owner/admin can open create", ownerCreate.status === 200);
assert("tenant-owner/admin sees active create controls", ownerHome.html.includes('href="/bakim/arizalar/yeni"'));

const maintenanceHome = await fetchWeb("/bakim", maintenance);
const maintenanceCreate = await fetchWeb("/bakim/arizalar/yeni", maintenance);
const maintenancePlan = await fetchWeb("/bakim/planlama", maintenance);
assert("maintenance-only can read maintenance", maintenanceHome.status === 200 && maintenancePlan.status === 200);
assert("maintenance-only create route is denied", maintenanceCreate.status === 403);
assert(
  "maintenance-only create is disabled with reason",
  maintenanceHome.html.includes("Arıza kaydı oluşturma yetkiniz yok.")
    && !/href="\/bakim\/arizalar\/yeni"/.test(maintenanceHome.html),
);

for (const [name, jar] of [
  ["machine-responsible", machine],
  ["materials-only", materials],
]) {
  const home = await fetchWeb("/bakim", jar);
  const create = await fetchWeb("/bakim/arizalar/yeni", jar);
  const detail = await fetchWeb("/bakim/arizalar/00000000-0000-4000-8000-000000000001", jar);
  assert(`${name} maintenance home denied`, home.status === 403);
  assert(`${name} create route denied`, create.status === 403);
  assert(`${name} detail route denied`, detail.status === 403);
}

const readOnlyAccess = await api("GET", "/api/admin-access/me/effective-access", undefined, readOnly);
const readOnlyKeys = readOnlyAccess.payload?.grantedPermissionKeys ?? [];
const readOnlyCanRead = readOnlyKeys.includes("maintenance.read");
const readOnlyCanManage = readOnlyKeys.includes("maintenance.manage");
const readOnlyHome = await fetchWeb("/bakim", readOnly);
const readOnlyCreate = await fetchWeb("/bakim/arizalar/yeni", readOnly);
if (readOnlyCanRead) {
  assert("read-only GET allowed where granted", readOnlyHome.status === 200);
} else {
  assert("read-only GET denied where not granted", readOnlyHome.status === 403);
}
assert("read-only cannot manage", readOnlyCanManage === false);
assert("read-only create route denied", readOnlyCreate.status === 403);

function sqlCount() {
  const result = spawnSync(
    "docker",
    ["exec", "NevaxisFactOS-local-postgres-1", "psql", "-U", "crevafactos", "-d", "crevafactos_local", "-t", "-A", "-c", "SELECT count(*) FROM maintenance_requests;"],
    { encoding: "utf8" },
  );
  if (result.status !== 0) fail(result.stderr || result.stdout);
  return Number(result.stdout.trim());
}

async function assertDeniedWrite(label, jar) {
  const beforeCount = sqlCount();
  const write = await api("POST", `/api/maintenance/requests?facilityId=${FACILITY_A}`, createBody, jar);
  const afterCount = sqlCount();
  assert(`${label} mutation denied`, write.status === 401 || write.status === 403, `actual ${write.status}`);
  assert(`${label} created no records`, afterCount === beforeCount, `${beforeCount} -> ${afterCount}`);
  assert(`${label} mutated no records`, afterCount === beforeCount);
}

await assertDeniedWrite("maintenance-only", maintenance);
await assertDeniedWrite("machine-responsible", machine);
await assertDeniedWrite("materials-only", materials);
await assertDeniedWrite("read-only", readOnly);

const ownerFacilityB = await api("GET", `/api/maintenance/requests?facilityId=${FACILITY_B}`, undefined, owner);
assert("facility isolation denies unknown facility", ownerFacilityB.status === 403 || ownerFacilityB.status === 404);

const crossTenant = await api("GET", `/api/maintenance/requests/${CROSS_TENANT_ID}?facilityId=${FACILITY_A}`, undefined, owner);
assert("cross-tenant record is not disclosed", crossTenant.status === 404 || crossTenant.status === 403);

const ownerApiCreateDeniedShape = await api("POST", `/api/maintenance/requests?facilityId=${FACILITY_B}`, createBody, owner);
assert("owner cannot write into isolated facility", ownerApiCreateDeniedShape.status === 403 || ownerApiCreateDeniedShape.status === 400);

console.log("APP-ACTION-1A permission verification passed.");
