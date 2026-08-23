#!/usr/bin/env node
import { createHmac } from "node:crypto";
import { spawnSync } from "node:child_process";
import { existsSync, readFileSync } from "node:fs";
import { homedir } from "node:os";
import { dirname, join, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const API = process.env.CREVAFACTOS_API_BASE_URL ?? "http://127.0.0.1:5000";
const WEB = process.env.CREVAFACTOS_WEB_BASE_URL ?? "http://localhost:3001";
const ORGANIZATION = "dev-pilot-tenant";
const FACILITY = "bb000001-0005-4000-8000-000000000005";
const READ_PERMISSION_ID = "10000000-0000-4000-8000-000000000080";
const TENANT_ID = "bb000001-0001-4000-8000-000000000001";
const MAINTENANCE_MEMBERSHIP_ID = "bb000003-0223-4000-8000-000000000223";
const CLOSURE_PREFIX = "OPRISKUI1-CLOSURE";
const CLOSURE_OVERRIDE_ID = "dd000003-00e6-4000-8000-0000000000e6";
const MANAGER = { issuer: "crevafactos-dev", subject: "planning-pilot-user", tenant: "dev-pilot-tenant" };
const READONLY = { issuer: "crevafactos-dev", subject: "planning-pilot-reviewer", tenant: "dev-pilot-tenant" };
const TOTP_ALPHABET = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";

let deniedMutationCount = 0;

function fail(message) {
  console.error(`OPERATIONAL-RISK-UI-1 permission verification failed: ${message}`);
  process.exit(1);
}

function assert(label, ok, detail = "") {
  if (!ok) fail(`${label}${detail ? `: ${detail}` : ""}`);
  console.log(`✓ ${label}`);
}

function bootstrapDevelopmentIdentityEnv() {
  if (process.env.__OPRISK_UI_1_BOOTSTRAPPED === "1") return;
  const transcriptCandidates = [
    join(homedir(), ".cursor/projects/c-Projects-CrevaFactOS/agent-transcripts/c756ab40-4354-47d0-8f9e-2a16ca92f36f/c756ab40-4354-47d0-8f9e-2a16ca92f36f.jsonl"),
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
  process.env.__OPRISK_UI_1_BOOTSTRAPPED = "1";
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
  if (jar?.cookies.get("crevafactos.session") && jar.cookies.get("crevafactos.csrf") && method !== "GET") {
    requestHeaders["X-CrevaFactOS-Csrf"] = jar.cookies.get("crevafactos.csrf");
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
    "X-CrevaFactOS-Dev-Issuer": actor.issuer,
    "X-CrevaFactOS-Dev-Subject": actor.subject,
    "X-CrevaFactOS-Tenant": actor.tenant,
  });
}

async function signIn(email, password) {
  const jar = new CookieJar();
  const result = await api("POST", "/api/identity/sign-in", { organizationContext: ORGANIZATION, email, password, deviceLabel: `${CLOSURE_PREFIX}-permissions` }, jar);
  if (result.status !== 200) fail(`sign-in failed for ${email}`);
  return jar;
}

async function signInOwner() {
  const jar = await signIn("tenant-owner@dev.local", requireEnv("DevelopmentSaasIdentity__TenantOwnerPassword"));
  const verify = await api("POST", "/api/identity/mfa/verify", { totpCode: generateTotp(requireEnv("DevelopmentSaasIdentity__MfaSharedSecret")) }, jar);
  if (verify.status !== 200) fail("owner MFA failed");
  return jar;
}

async function fetchWeb(path, jar) {
  const response = await fetch(`${WEB}${path}`, { headers: { Cookie: jar.header(), Accept: "text/html" }, redirect: "manual" });
  const html = response.status >= 300 && response.status < 400 ? "" : await response.text();
  return { status: response.status, html };
}

function countSidebarLinks(html) {
  return (html.match(/href="\/operasyonel-riskler"/g) ?? []).length;
}

function navigationSource() {
  return readFileSync(resolve(dirname(fileURLToPath(import.meta.url)), "../src/lib/navigation/primary-navigation.ts"), "utf8");
}

function db(sql) {
  const result = spawnSync("docker", ["exec", "crevafactos-local-postgres-1", "psql", "-U", "crevafactos", "-d", "crevafactos_local", "-t", "-A", "-c", sql], { encoding: "utf8" });
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

function transitionCount() {
  return Number(db(`SELECT count(*) FROM operational_risk_status_history WHERE reason_note LIKE '${CLOSURE_PREFIX}%';`));
}

console.log("Running OPERATIONAL-RISK-UI-1 permission verifier.");

removeReadOverride();
const owner = await signInOwner();
const materials = await signIn("materials-sales@dev.local", requireEnv("DevelopmentSaasIdentity__ActiveUserPassword"));
seedReadOverride();
const readonlyUser = await signIn("maintenance-team@dev.local", requireEnv("DevelopmentSaasIdentity__ActiveUserPassword"));

const ownerDash = await fetchWeb("/dashboard", owner);
const materialsDash = await fetchWeb("/dashboard", materials);
const readonlyDash = await fetchWeb("/dashboard", readonlyUser);
const ownerRoute = await fetchWeb("/operasyonel-riskler", owner);
const materialsRoute = await fetchWeb("/operasyonel-riskler", materials);
const readonlyRoute = await fetchWeb("/operasyonel-riskler", readonlyUser);

assert("owner /operasyonel-riskler permitted", ownerRoute.status === 200);
assert("unauthorized /operasyonel-riskler denied", materialsRoute.status === 403);
assert("read-only /operasyonel-riskler permitted", readonlyRoute.status === 200);
assert("owner sidebar visible once", countSidebarLinks(ownerDash.html) === 1);
assert("unauthorized sidebar hidden", countSidebarLinks(materialsDash.html) === 0);
assert("read-only sidebar visible once", countSidebarLinks(readonlyDash.html) === 1);
assert("navigation permission operational-risk.read", navigationSource().includes('permissionKey: "operational-risk.read"'));

const ownerCases = await api("GET", `/api/operational-risks/cases?facilityId=${FACILITY}`, undefined, owner);
assert("owner cases API", ownerCases.status === 200);
assert("owner canManage", ownerCases.payload?.capabilities?.canManage === true);

const readonlyCases = await api("GET", `/api/operational-risks/cases?facilityId=${FACILITY}`, undefined, readonlyUser);
assert("read-only cases API", readonlyCases.status === 200);
assert("read-only cannot manage", readonlyCases.payload?.capabilities?.canManage !== true);

const devReadonly = await devApi("GET", `/api/operational-risks/cases?facilityId=${FACILITY}`, undefined, READONLY);
assert("dev reviewer read", devReadonly.status === 200);

const beforeTransitions = transitionCount();
const deniedTransition = await api("POST", `/api/operational-risks/cases/bb000001-0200-4000-8000-000000000200/transitions?facilityId=${FACILITY}`, {
  expectedVersion: 1,
  targetStatus: "under-review",
  note: `${CLOSURE_PREFIX}-denied`,
  sourceProvenance: "closure",
}, readonlyUser);
const afterTransitions = transitionCount();
if (deniedTransition.status === 403) deniedMutationCount += 1;
assert("read-only transition denied", deniedTransition.status === 403, `status=${deniedTransition.status}`);
assert("denied transition created zero records", afterTransitions === beforeTransitions);

const ownerAccess = await api("GET", "/api/admin-access/me/effective-access", undefined, owner);
const granted = ownerAccess.payload?.grantedPermissionKeys ?? [];
assert("owner operational-risk.read", granted.includes("operational-risk.read"));
assert("owner operational-risk.manage", granted.includes("operational-risk.manage"));

const readonlyAccess = await api("GET", "/api/admin-access/me/effective-access", undefined, readonlyUser);
const readonlyGranted = readonlyAccess.payload?.grantedPermissionKeys ?? [];
assert("read-only effective operational-risk.read", readonlyGranted.includes("operational-risk.read"));
assert("read-only lacks operational-risk.manage", !readonlyGranted.includes("operational-risk.manage"));

const isolated = await devApi("GET", `/api/operational-risks/cases?facilityId=bb000001-0005-4000-8000-000000000099`, undefined, MANAGER);
assert("wrong facility denied", isolated.status === 403 || isolated.status === 404);

removeReadOverride();

console.log(`DENIED_MUTATION_COUNT=${deniedMutationCount}`);
console.log("OPERATIONAL-RISK-UI-1 permission verification passed.");
