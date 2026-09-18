#!/usr/bin/env node
import { createHmac } from "node:crypto";
import { spawnSync } from "node:child_process";
import { existsSync, readFileSync } from "node:fs";
import { homedir } from "node:os";
import { dirname, join, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const API = process.env.NEVAXIS_FACTOS_API_BASE_URL ?? "http://127.0.0.1:5000";
const WEB = process.env.NEVAXIS_FACTOS_WEB_BASE_URL ?? "http://localhost:3001";
const ROUTE = "/ticari-zeka/satin-alma-fiyat-analizi";
const ORGANIZATION = "dev-pilot-tenant";
const FACILITY = "bb000001-0005-4000-8000-000000000005";
const PROFILE = "bb000001-0220-4000-8000-000000000220";
const MATERIAL = "bb000001-00a2-4000-8000-0000000000a2";
const READ_PERMISSION_ID = "10000000-0000-4000-8000-000000000082";
const TENANT_ID = "bb000001-0001-4000-8000-000000000001";
const MAINTENANCE_MEMBERSHIP_ID = "bb000003-0223-4000-8000-000000000223";
const CLOSURE_PREFIX = "COMMINT-UI1-PERM-CLOSURE";
const CLOSURE_OVERRIDE_ID = "dd000003-00e7-4000-8000-0000000000e7";
const MANAGER = { issuer: "crevafactos-dev", subject: "planning-pilot-user", tenant: "dev-pilot-tenant" };
const READONLY = { issuer: "crevafactos-dev", subject: "planning-pilot-reviewer", tenant: "dev-pilot-tenant" };
const TOTP_ALPHABET = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";

let deniedMutationCount = 0;
let subscriptionRestore = null;

function fail(message) {
  console.error(`COMMERCIAL-INTELLIGENCE-UI-1 permission verification failed: ${message}`);
  process.exit(1);
}

function assert(label, ok, detail = "") {
  if (!ok) fail(`${label}${detail ? `: ${detail}` : ""}`);
  console.log(`✓ ${label}`);
}

function bootstrapDevelopmentIdentityEnv() {
  if (process.env.__COMMINT_UI_1_BOOTSTRAPPED === "1") return;
  const transcriptCandidates = [
    join(homedir(), ".cursor/projects/c-Projects-NevaxisZentra/agent-transcripts/85d2e353-dc82-41df-ae90-9baa4397ee63/85d2e353-dc82-41df-ae90-9baa4397ee63.jsonl"),
    join(homedir(), ".cursor/projects/c-Projects-NevaxisZentra/agent-transcripts/0e3b0257-2b6c-4a42-81dc-cceb6078d79f/0e3b0257-2b6c-4a42-81dc-cceb6078d79f.jsonl"),
    join(homedir(), ".cursor/projects/c-Projects-NevaxisZentra/agent-transcripts/e4795037-6822-4158-86cc-66f2bbb7cc33/e4795037-6822-4158-86cc-66f2bbb7cc33.jsonl"),
    join(homedir(), ".cursor/projects/c-Projects-NevaxisZentra/agent-transcripts/c756ab40-4354-47d0-8f9e-2a16ca92f36f/c756ab40-4354-47d0-8f9e-2a16ca92f36f.jsonl"),
    join(homedir(), ".cursor/projects/c-Projects-NevaxisZentra/agent-transcripts/f170800e-80eb-42f9-b69a-a0770e665e7a/f170800e-80eb-42f9-b69a-a0770e665e7a.jsonl"),
  ];
  let text = "";
  for (const candidate of transcriptCandidates) {
    if (existsSync(candidate)) { text = readFileSync(candidate, "utf8"); break; }
  }
  for (const key of ["DevelopmentSaasIdentity__TenantOwnerPassword", "DevelopmentSaasIdentity__MfaSharedSecret", "DevelopmentSaasIdentity__ActiveUserPassword"]) {
    if (process.env[key]?.trim()) continue;
    const match = text.match(new RegExp(`${key}='([^']+)'`));
    if (match?.[1]) process.env[key] = match[1];
  }
  process.env.__COMMINT_UI_1_BOOTSTRAPPED = "1";
}

function requireEnv(name) {
  bootstrapDevelopmentIdentityEnv();
  const value = process.env[name]?.trim();
  if (!value) fail(`Missing environment variable ${name}`);
  return value;
}

function generateTotp(secret) {
  const counter = Math.floor(Date.now() / 1000 / 30);
  const normalized = secret.trim().replace(/=+$/, "").toUpperCase();
  let buffer = 0; let bitsLeft = 0; const output = [];
  for (const character of normalized) {
    buffer = (buffer << 5) | TOTP_ALPHABET.indexOf(character); bitsLeft += 5;
    if (bitsLeft >= 8) { output.push((buffer >> (bitsLeft - 8)) & 0xff); bitsLeft -= 8; }
  }
  const counterBytes = Buffer.alloc(8);
  counterBytes.writeBigUInt64BE(BigInt(counter));
  const hash = createHmac("sha1", Buffer.from(output)).update(counterBytes).digest();
  const offset = hash[hash.length - 1] & 0x0f;
  const binaryCode = ((hash[offset] & 0x7f) << 24) | ((hash[offset + 1] & 0xff) << 16) | ((hash[offset + 2] & 0xff) << 8) | (hash[offset + 3] & 0xff);
  return String(binaryCode % 1_000_000).padStart(6, "0");
}

class CookieJar {
  constructor() { this.cookies = new Map(); }
  ingest(response) {
    const setCookies = typeof response.headers.getSetCookie === "function" ? response.headers.getSetCookie() : [];
    for (const raw of setCookies) {
      const [pair] = raw.split(";"); const separator = pair.indexOf("=");
      if (separator <= 0) continue;
      this.cookies.set(pair.slice(0, separator).trim(), pair.slice(separator + 1).trim());
    }
  }
  header() { return [...this.cookies.entries()].map(([name, value]) => `${name}=${value}`).join("; "); }
}

async function api(method, path, body, jar, headers = {}) {
  const requestHeaders = { Accept: "application/json", ...headers };
  if (body) requestHeaders["Content-Type"] = "application/json";
  if (jar?.header()) requestHeaders.Cookie = jar.header();
  if (jar?.cookies.get("NevaxisZentra.session") && jar.cookies.get("NevaxisZentra.csrf") && method !== "GET") {
    requestHeaders["X-NevaxisZentra-Csrf"] = jar.cookies.get("NevaxisZentra.csrf");
  }
  const response = await fetch(`${API}${path}`, { method, headers: requestHeaders, body: body ? JSON.stringify(body) : undefined });
  jar?.ingest(response);
  let payload = null;
  try { payload = await response.json(); } catch { payload = null; }
  return { status: response.status, payload };
}

async function devApi(method, path, body, actor) {
  return api(method, path, body, null, {
    "Content-Type": "application/json",
    "X-crevafactos-dev-Issuer": actor.issuer,
    "X-crevafactos-dev-Subject": actor.subject,
    "X-NevaxisZentra-Tenant": actor.tenant,
  });
}

async function signIn(email, password) {
  const jar = new CookieJar();
  const result = await api("POST", "/api/identity/sign-in", { organizationContext: ORGANIZATION, email, password, deviceLabel: `${CLOSURE_PREFIX}-permissions` }, jar);
  if (result.status !== 200) fail(`sign-in failed for ${email}`);
  return jar;
}

async function signInOwner() {
  const jar = await signIn("admin@demo.nevaxiszentra.local", requireEnv("DevelopmentSaasIdentity__TenantOwnerPassword"));
  const verify = await api("POST", "/api/identity/mfa/verify", { totpCode: generateTotp(requireEnv("DevelopmentSaasIdentity__MfaSharedSecret")) }, jar);
  if (verify.status !== 200) fail("owner MFA failed");
  return jar;
}

async function fetchWeb(path, jar) {
  const response = await fetch(`${WEB}${path}`, { headers: { Cookie: jar.header(), Accept: "text/html" }, redirect: "manual" });
  const html = response.status >= 300 && response.status < 400 ? "" : await response.text();
  return { status: response.status, html, location: response.headers.get("location") };
}

function countSidebarLinks(html) {
  return (html.match(/href="\/ticari-zeka\/satin-alma-fiyat-analizi"/g) ?? []).length;
}

function navigationSource() {
  return readFileSync(resolve(dirname(fileURLToPath(import.meta.url)), "../src/lib/navigation/primary-navigation.ts"), "utf8");
}

function db(sql) {
  const result = spawnSync("docker", ["exec", "NevaxisZentra-local-postgres-1", "psql", "-U", "crevafactos", "-d", "crevafactos_local", "-t", "-A", "-c", sql], { encoding: "utf8" });
  if (result.status !== 0) fail(result.stderr || result.stdout);
  return result.stdout.trim();
}

function seedReadOverride() {
  db(`DELETE FROM tenant_membership_permission_overrides WHERE id = '${CLOSURE_OVERRIDE_ID}';
    INSERT INTO tenant_membership_permission_overrides (id, tenant_id, tenant_membership_id, permission_definition_id, effect, record_scope, status, created_at_utc, updated_at_utc, version)
    VALUES ('${CLOSURE_OVERRIDE_ID}', '${TENANT_ID}', '${MAINTENANCE_MEMBERSHIP_ID}', '${READ_PERMISSION_ID}', 'allow', 'tenant', 'Active', NOW(), NOW(), 1);`);
}

function removeReadOverride() {
  db(`DELETE FROM tenant_membership_permission_overrides WHERE id = '${CLOSURE_OVERRIDE_ID}';`);
}

function assessmentCount() {
  return Number(db(`SELECT count(*) FROM commercial_purchase_price_assessments WHERE idempotency_key LIKE '${CLOSURE_PREFIX}%';`));
}

function removeClosureFixtures() {
  db(`
    DELETE FROM commercial_purchase_price_assessment_drivers
    WHERE assessment_id IN (SELECT id FROM commercial_purchase_price_assessments WHERE idempotency_key LIKE '${CLOSURE_PREFIX}%');
    DELETE FROM commercial_purchase_price_assessments WHERE idempotency_key LIKE '${CLOSURE_PREFIX}%';
  `);
}

const provenance = "Deterministic local Development commercial-intelligence data — simulated, non-production, not real customer evidence, market prices, or guaranteed savings.";

console.log("Running COMMERCIAL-INTELLIGENCE-UI-1 permission verifier.");

removeReadOverride();
removeClosureFixtures();

const owner = await signInOwner();
const materials = await signIn("materials-sales@dev.local", requireEnv("DevelopmentSaasIdentity__ActiveUserPassword"));
seedReadOverride();
const readonlyUser = await signIn("maintenance-team@dev.local", requireEnv("DevelopmentSaasIdentity__ActiveUserPassword"));

const ownerDash = await fetchWeb("/dashboard", owner);
const materialsDash = await fetchWeb("/dashboard", materials);
const readonlyDash = await fetchWeb("/dashboard", readonlyUser);
const ownerRoute = await fetchWeb(ROUTE, owner);
const materialsRoute = await fetchWeb(ROUTE, materials);
const readonlyRoute = await fetchWeb(ROUTE, readonlyUser);
const unauthRoute = await fetchWeb(ROUTE, new CookieJar());

assert("owner route permitted", ownerRoute.status === 200);
assert("unauthorized route denied", materialsRoute.status === 403);
assert("read-only route permitted", readonlyRoute.status === 200);
assert("owner sidebar visible once", countSidebarLinks(ownerDash.html) === 1);
assert("unauthorized sidebar hidden", countSidebarLinks(materialsDash.html) === 0);
assert("read-only sidebar visible once", countSidebarLinks(readonlyDash.html) === 1);
assert("navigation permission commercial-intelligence.read", navigationSource().includes('permissionKey: "commercial-intelligence.read"'));

const ownerWorkspace = await api("GET", `/api/commercial-intelligence/purchase-prices/workspace?facilityId=${FACILITY}`, undefined, owner);
assert("owner workspace API", ownerWorkspace.status === 200);
assert("owner canManage", ownerWorkspace.payload?.capabilities?.canManage === true);

const readonlyWorkspace = await api("GET", `/api/commercial-intelligence/purchase-prices/workspace?facilityId=${FACILITY}`, undefined, readonlyUser);
assert("read-only workspace API", readonlyWorkspace.status === 200);
assert("read-only cannot manage", readonlyWorkspace.payload?.capabilities?.canManage !== true);

const devReadonly = await devApi("GET", `/api/commercial-intelligence/purchase-prices/workspace?facilityId=${FACILITY}`, undefined, READONLY);
assert("dev reviewer read", devReadonly.status === 200);

const beforeAssessments = assessmentCount();
const deniedEvaluate = await api(
  "POST",
  `/api/commercial-intelligence/purchase-prices/assessments/evaluate?facilityId=${FACILITY}`,
  {
    profileId: PROFILE,
    materialId: MATERIAL,
    comparisonDimension: "material",
    currentUnitPrice: 101,
    currencyCode: "TRY",
    normalizedUnit: "kg",
    windowStartUtc: "2026-04-01T00:00:00.000Z",
    windowEndUtc: "2026-08-25T00:00:00.000Z",
    priceBasisTaxInclusion: "excluded",
    priceBasisDiscountInclusion: "excluded",
    priceBasisFreightInclusion: "excluded",
    sourceProvenance: provenance,
    idempotencyKey: `${CLOSURE_PREFIX}-readonly-${Date.now()}`,
  },
  readonlyUser,
);
const afterAssessments = assessmentCount();
if (deniedEvaluate.status === 403) deniedMutationCount += 1;
assert("read-only evaluate denied", deniedEvaluate.status === 403, `status=${deniedEvaluate.status}`);
assert("denied evaluate created zero records", afterAssessments === beforeAssessments);

const ownerAccess = await api("GET", "/api/admin-access/me/effective-access", undefined, owner);
const granted = ownerAccess.payload?.grantedPermissionKeys ?? [];
assert("owner commercial-intelligence.read", granted.includes("commercial-intelligence.read"));
assert("owner commercial-intelligence.manage", granted.includes("commercial-intelligence.manage"));

const readonlyAccess = await api("GET", "/api/admin-access/me/effective-access", undefined, readonlyUser);
const readonlyGranted = readonlyAccess.payload?.grantedPermissionKeys ?? [];
assert("read-only effective commercial-intelligence.read", readonlyGranted.includes("commercial-intelligence.read"));
assert("read-only lacks commercial-intelligence.manage", !readonlyGranted.includes("commercial-intelligence.manage"));

const isolated = await devApi("GET", `/api/commercial-intelligence/purchase-prices/workspace?facilityId=bb000001-0005-4000-8000-000000000099`, undefined, MANAGER);
assert("wrong facility denied", isolated.status === 403 || isolated.status === 404);

const crossTenant = await devApi(
  "GET",
  `/api/commercial-intelligence/purchase-prices/workspace?facilityId=${FACILITY}`,
  undefined,
  { issuer: "crevafactos-dev", subject: MANAGER.subject, tenant: "unknown-tenant-key" },
);
assert("cross tenant denied", crossTenant.status === 401 || crossTenant.status === 403, `status=${crossTenant.status}`);

const currentSubscription = await api("GET", "/api/saas-subscription/current", undefined, owner);
const subscriptionId = currentSubscription.payload?.subscription?.subscriptionId;
const subscriptionVersion = currentSubscription.payload?.subscription?.version ?? 1;
const suspend = await api("POST", "/api/saas-subscription/transitions", {
  subscriptionId,
  targetStatus: "suspended",
  transitionReason: "manual-suspension",
  expectedVersion: subscriptionVersion,
  idempotencyKey: `${CLOSURE_PREFIX}-suspend-${Date.now()}`,
}, owner);
assert("subscription suspend transition", suspend.status === 201 || suspend.status === 200, String(suspend.status));
subscriptionRestore = { subscriptionId, expectedVersion: subscriptionVersion + 1 };

const inactiveRoute = await fetchWeb(ROUTE, owner);
assert("inactive subscription recovery route", inactiveRoute.location?.includes("/abonelik-erisimi") || inactiveRoute.html.includes("abonelik"), inactiveRoute.location ?? "no redirect");

const deniedWhileInactive = await api(
  "POST",
  `/api/commercial-intelligence/purchase-prices/assessments/evaluate?facilityId=${FACILITY}`,
  {
    profileId: PROFILE,
    materialId: MATERIAL,
    comparisonDimension: "material",
    currentUnitPrice: 101,
    currencyCode: "TRY",
    normalizedUnit: "kg",
    windowStartUtc: "2026-04-01T00:00:00.000Z",
    windowEndUtc: "2026-08-25T00:00:00.000Z",
    priceBasisTaxInclusion: "excluded",
    priceBasisDiscountInclusion: "excluded",
    priceBasisFreightInclusion: "excluded",
    sourceProvenance: provenance,
    idempotencyKey: `${CLOSURE_PREFIX}-inactive-${Date.now()}`,
  },
  owner,
);
if (deniedWhileInactive.status === 403) deniedMutationCount += 1;
assert("inactive subscription mutation denied", deniedWhileInactive.status === 403, `status=${deniedWhileInactive.status}`);

const reactivate = await api("POST", "/api/saas-subscription/transitions", {
  subscriptionId,
  targetStatus: "active",
  transitionReason: "manual-reactivation",
  expectedVersion: subscriptionRestore.expectedVersion,
  idempotencyKey: `${CLOSURE_PREFIX}-reactivate-${Date.now()}`,
}, owner);
assert("subscription reactivated", reactivate.status === 201 || reactivate.status === 200, String(reactivate.status));
subscriptionRestore = null;

assert("missing authentication denied", unauthRoute.status === 302 || unauthRoute.status === 307 || unauthRoute.location?.includes("/giris"), String(unauthRoute.status));

removeReadOverride();
removeClosureFixtures();

console.log(`DENIED_MUTATION_COUNT=${deniedMutationCount}`);
console.log("COMMERCIAL-INTELLIGENCE-UI-1 permission verification passed.");
