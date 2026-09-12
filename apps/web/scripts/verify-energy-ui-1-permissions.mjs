#!/usr/bin/env node
import { createHmac } from "node:crypto";
import { spawnSync } from "node:child_process";
import { existsSync, readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const API = process.env.NEVAXIS_FACTOS_API_BASE_URL ?? "http://127.0.0.1:5000";
const WEB = process.env.NEVAXIS_FACTOS_WEB_BASE_URL ?? "http://localhost:3001";
const ORGANIZATION = "dev-pilot-tenant";
const FACILITY = "bb000001-0005-4000-8000-000000000005";
const ENERGY_READ_PERMISSION_ID = "10000000-0000-4000-8000-000000000076";
const ENERGY_MANAGE_PERMISSION_ID = "10000000-0000-4000-8000-000000000077";
const TENANT_ID = "bb000001-0001-4000-8000-000000000001";
const MAINTENANCE_MEMBERSHIP_ID = "bb000003-0223-4000-8000-000000000223";
const CLOSURE_PREFIX = "ENERGYUI1-CLOSURE";
const CLOSURE_OVERRIDE_ID = "dd000003-00e4-4000-8000-0000000000e4";
const MANAGER = {
  issuer: "crevafactos-dev",
  subject: "planning-pilot-user",
  tenant: "dev-pilot-tenant",
};
const READONLY = {
  issuer: "crevafactos-dev",
  subject: "planning-pilot-reviewer",
  tenant: "dev-pilot-tenant",
};
const TOTP_ALPHABET = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";

function fail(message) {
  console.error(`ENERGY-UI-1 permission verification failed: ${message}`);
  process.exit(1);
}

function assert(label, ok, detail = "") {
  if (!ok) fail(`${label}${detail ? `: ${detail}` : ""}`);
  console.log(`✓ ${label}`);
}

function requireEnv(name) {
  const value = process.env[name]?.trim();
  if (!value) fail(`Missing environment variable ${name}`);
  return value;
}

function generateTotp(secret) {
  const counter = Math.floor(Date.now() / 1000 / 30);
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

async function api(method, path, body, jar, headers = {}) {
  const requestHeaders = { Accept: "application/json", ...headers };
  if (body) requestHeaders["Content-Type"] = "application/json";
  if (jar?.header()) requestHeaders.Cookie = jar.header();
  if (jar?.cookies.get("NevaxisFactOS.session") && jar.cookies.get("NevaxisFactOS.csrf") && method !== "GET") {
    requestHeaders["X-NevaxisFactOS-Csrf"] = jar.cookies.get("NevaxisFactOS.csrf");
  }
  const response = await fetch(`${API}${path}`, {
    method,
    headers: requestHeaders,
    body: body ? JSON.stringify(body) : undefined,
  });
  jar?.ingest(response);
  let payload = null;
  try {
    payload = (await response.json());
  } catch {
    payload = null;
  }
  return { status: response.status, payload };
}

async function devApi(method, path, body, actor) {
  return api(method, path, body, null, {
    "Content-Type": "application/json",
    "X-crevafactos-dev-Issuer": actor.issuer,
    "X-crevafactos-dev-Subject": actor.subject,
    "X-NevaxisFactOS-Tenant": actor.tenant,
  });
}

async function signIn(email, password) {
  const jar = new CookieJar();
  const result = await api("POST", "/api/identity/sign-in", {
    organizationContext: ORGANIZATION,
    email,
    password,
    deviceLabel: `${CLOSURE_PREFIX}-permissions`,
  }, jar);
  if (result.status !== 200) fail(`sign-in failed for ${email}`);
  return jar;
}

async function signInOwner() {
  const jar = await signIn("tenant-owner@dev.local", requireEnv("DevelopmentSaasIdentity__TenantOwnerPassword"));
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

function countEnergySidebarLinks(html) {
  return (html.match(/href="\/enerji"/g) ?? []).length;
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

async function signInThroughForm(page, { organization, email, password }) {
  await page.goto(`${WEB}/giris`, { waitUntil: "networkidle", timeout: 90000 });
  await page.locator("#sign-in-organization").fill(organization);
  await page.locator("#sign-in-email").fill(email);
  await page.locator("#sign-in-password").fill(password);
  await page.getByRole("button", { name: "Oturum aç" }).click();
}

async function completeOwnerMfa(page, mfaSecret) {
  await page.waitForURL("**/mfa-dogrula", { timeout: 30000 });
  await page.locator("#mfa-totp").fill(generateTotp(mfaSecret));
  await page.getByRole("button", { name: "Doğrula" }).click();
  await page.waitForURL("**/dashboard", { timeout: 30000 });
}

async function assertOwnerEnergyPresentation() {
  const { chromium } = await import("playwright");
  const browser = await chromium.launch({
    headless: true,
    ...(resolveChromiumExecutable() ? { executablePath: resolveChromiumExecutable() } : {}),
  });

  try {
    const context = await browser.newContext({ viewport: { width: 1600, height: 1000 } });
    const page = await context.newPage();
    await signInThroughForm(page, {
      organization: ORGANIZATION,
      email: "tenant-owner@dev.local",
      password: requireEnv("DevelopmentSaasIdentity__TenantOwnerPassword"),
    });
    await completeOwnerMfa(page, requireEnv("DevelopmentSaasIdentity__MfaSharedSecret"));

    const response = await page.goto(`${WEB}/enerji`, { waitUntil: "networkidle", timeout: 90000 });
    assert("owner /enerji HTTP 200", response?.status() === 200);
    await page.locator('[data-testid="energy-workspace"]').waitFor({ state: "visible", timeout: 30000 });

    assert(
      "owner /enerji read-only banner absent",
      await page.locator('[data-testid="energy-readonly-banner"]').count() === 0,
    );

    const workspaceText = await page.locator('[data-testid="energy-workspace"]').innerText();
    assert("owner /enerji raw unit absent", !workspaceText.includes("1000-good-parts"));
    assert("owner /enerji duplicated currency absent", !/₺.*TRY|TRY.*₺/.test(workspaceText));
  } finally {
    await browser.close();
  }
}

function navigationSource() {
  return readFileSync(
    resolve(dirname(fileURLToPath(import.meta.url)), "../src/lib/navigation/primary-navigation.ts"),
    "utf8",
  );
}

function db(sql) {
  const result = spawnSync(
    "docker",
    ["exec", "NevaxisFactOS-local-postgres-1", "psql", "-U", "crevafactos", "-d", "crevafactos_local", "-t", "-A", "-c", sql],
    { encoding: "utf8" },
  );
  if (result.status !== 0) fail(result.stderr || result.stdout);
  return result.stdout.trim();
}

function targetCount() {
  return Number(db("SELECT count(*) FROM energy_targets;"));
}

function seedEnergyReadOverrideForMaintenance() {
  db(`
    DELETE FROM tenant_membership_permission_overrides
    WHERE id = '${CLOSURE_OVERRIDE_ID}';

    INSERT INTO tenant_membership_permission_overrides (
      id, tenant_id, tenant_membership_id, permission_definition_id, effect, record_scope, status, created_at_utc, updated_at_utc, version)
    VALUES (
      '${CLOSURE_OVERRIDE_ID}',
      '${TENANT_ID}',
      '${MAINTENANCE_MEMBERSHIP_ID}',
      '${ENERGY_READ_PERMISSION_ID}',
      'allow',
      'tenant',
      'Active',
      NOW(),
      NOW(),
      1);
  `);
}

function removeEnergyReadOverrideForMaintenance() {
  db(`
    DELETE FROM tenant_membership_permission_overrides
    WHERE id = '${CLOSURE_OVERRIDE_ID}';
  `);
}

function assertOwnerBootstrapEnergyGrants() {
  const rows = db(`
    SELECT pd.key
    FROM tenant_role_permissions trp
    JOIN permission_definitions pd ON pd.id = trp.permission_definition_id
    WHERE trp.tenant_id = '${TENANT_ID}'
      AND trp.tenant_role_id = 'bb000003-0101-4000-8000-000000000101'
      AND pd.key IN ('energy.read', 'energy.manage')
    ORDER BY pd.key;
  `);
  const keys = rows.split("\n").map((line) => line.trim()).filter(Boolean);
  assert("bootstrap tenant-owner energy.read grant", keys.includes("energy.read"), rows);
  assert("bootstrap tenant-owner energy.manage grant", keys.includes("energy.manage"), rows);
}

console.log("Running ENERGY-UI-1 permission verifier.");

removeEnergyReadOverrideForMaintenance();
assertOwnerBootstrapEnergyGrants();

const owner = await signInOwner();
const materials = await signIn("materials-sales@dev.local", requireEnv("DevelopmentSaasIdentity__ActiveUserPassword"));
seedEnergyReadOverrideForMaintenance();
const maintenance = await signIn("maintenance-team@dev.local", requireEnv("DevelopmentSaasIdentity__ActiveUserPassword"));

const ownerRoute = await fetchWeb("/dashboard", owner);
const materialsRoute = await fetchWeb("/dashboard", materials);
const maintenanceRoute = await fetchWeb("/dashboard", maintenance);
const ownerEnergyRoute = await fetchWeb("/enerji", owner);
const materialsEnergyRoute = await fetchWeb("/enerji", materials);
const maintenanceEnergyRoute = await fetchWeb("/enerji", maintenance);

assert("owner/admin /enerji permitted", ownerEnergyRoute.status === 200);
await assertOwnerEnergyPresentation();
assert("unauthorized user /enerji denied", materialsEnergyRoute.status === 403);
assert("energy-read user /enerji permitted", maintenanceEnergyRoute.status === 200);

assert("owner sidebar Enerji visible once", countEnergySidebarLinks(ownerRoute.html) === 1);
assert("unauthorized sidebar Enerji hidden", countEnergySidebarLinks(materialsRoute.html) === 0);
assert("energy-read sidebar Enerji visible once", countEnergySidebarLinks(maintenanceRoute.html) === 1);

const nav = navigationSource();
const kaliteIndex = nav.indexOf('href: "/kalite"');
const enerjiIndex = nav.indexOf('href: "/enerji"');
const iotIndex = nav.indexOf('href: "/iot-plc"');
assert("navigation order Kalite -> Enerji -> IoT", kaliteIndex > 0 && enerjiIndex > kaliteIndex && iotIndex > enerjiIndex);
assert("navigation permission energy.read", nav.includes('permissionKey: "energy.read"'));

const ownerWorkspace = await api("GET", `/api/energy/workspace?facilityId=${FACILITY}`, undefined, owner);
assert("owner energy workspace API", ownerWorkspace.status === 200);
assert("owner canManage from API", ownerWorkspace.payload?.capabilities?.canManage === true);

const managerWorkspace = await devApi("GET", `/api/energy/workspace?facilityId=${FACILITY}`, undefined, MANAGER);
assert("energy manager dev actor read", managerWorkspace.status === 200);

const readonlyWorkspace = await devApi("GET", `/api/energy/workspace?facilityId=${FACILITY}`, undefined, READONLY);
assert("energy.read reviewer dev actor read", readonlyWorkspace.status === 200);

const readonlyIntelligence = await devApi("GET", `/api/energy/intelligence/workspace?facilityId=${FACILITY}`, undefined, READONLY);
assert("energy.read intelligence workspace", readonlyIntelligence.status === 200);

const beforeTargets = targetCount();
const deniedCreate = await devApi(
  "POST",
  `/api/energy/targets?facilityId=${FACILITY}`,
  {
    targetReference: `${CLOSURE_PREFIX}-denied-target`,
    targetScope: "machine",
    scopeReference: "EM-INJ-001",
    targetMetric: "consumed-kwh",
    targetValue: 99,
    unit: "kWh",
    tolerance: 0.1,
    effectiveFromUtc: new Date().toISOString(),
    effectiveToUtc: new Date(Date.now() + 86400000).toISOString(),
    sourceProvenance: "permission-test",
    idempotencyKey: `${CLOSURE_PREFIX}-denied-create`,
  },
  READONLY,
);
const afterTargets = targetCount();
assert("energy.read without manage mutation denied", deniedCreate.status === 403, `status=${deniedCreate.status}`);
assert("denied mutation created no records", afterTargets === beforeTargets, `${beforeTargets} -> ${afterTargets}`);

const ownerAccess = await api("GET", "/api/admin-access/me/effective-access", undefined, owner);
const granted = ownerAccess.payload?.grantedPermissionKeys ?? [];
assert("owner effective energy.read", granted.includes("energy.read"));
assert("owner has energy.manage", granted.includes("energy.manage"));

const maintenanceAccess = await api("GET", "/api/admin-access/me/effective-access", undefined, maintenance);
const maintenanceGranted = maintenanceAccess.payload?.grantedPermissionKeys ?? [];
assert("energy-read user effective energy.read", maintenanceGranted.includes("energy.read"));
assert("energy-read user lacks energy.manage", !maintenanceGranted.includes("energy.manage"));

assert("direct-route permission agreement owner", ownerEnergyRoute.status === 200 && granted.includes("energy.read"));
assert("direct-route permission agreement unauthorized", materialsEnergyRoute.status === 403 && !materialsRoute.html.includes('href="/enerji"'));

const isolatedFacility = await devApi("GET", `/api/energy/workspace?facilityId=bb000001-0005-4000-8000-000000000099`, undefined, MANAGER);
assert("facility isolation enforced", isolatedFacility.status === 403 || isolatedFacility.status === 404);

db(`
  DELETE FROM energy_targets
  WHERE target_reference LIKE '${CLOSURE_PREFIX}%'
     OR create_idempotency_key LIKE '${CLOSURE_PREFIX}%';
`);

removeEnergyReadOverrideForMaintenance();

console.log("ENERGY-UI-1 permission verification passed.");
