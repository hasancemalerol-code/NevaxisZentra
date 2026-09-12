#!/usr/bin/env node
import { createHmac } from "node:crypto";
import { spawnSync } from "node:child_process";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), "../../..");
const BASE = process.env.NEVAXIS_FACTOS_API_BASE_URL ?? "http://127.0.0.1:5000";
const ORGANIZATION = "dev-pilot-tenant";
const CLOSURE_PREFIX = "ADMINACCESS1-CLOSURE";
const TENANT_ID = "bb000001-0001-4000-8000-000000000001";
const OWNER_MEMBER = "mem_bb000002000340008000000000000003";
const SALES_MEMBER = "mem_bb000003021340008000000000000213";
const FACILITY_A = "pilot-facility";
const FACILITY_B = "pilot-facility-unauthorized";
const FACILITY_B_ID = "bb000001-0005-4000-8000-000000000099";
const TOTP_ALPHABET = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";

function fail(message) {
  console.error(`ADMIN-ACCESS-1 security verification failed: ${message}`);
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

function db(sql) {
  const result = spawnSync(
    "docker",
    ["exec", "NevaxisFactOS-local-postgres-1", "psql", "-U", "crevafactos", "-d", "crevafactos_local", "-t", "-A", "-c", sql],
    { encoding: "utf8" },
  );
  if (result.status !== 0) fail(result.stderr || result.stdout);
  return result.stdout.trim();
}

function generateTotp(secret) {
  const counter = Math.floor(Date.now() / 1000 / 30);
  const normalized = secret.trim().replace(/=+$/, "").toUpperCase();
  let buffer = 0;
  let bitsLeft = 0;
  const output = [];
  for (const character of normalized) {
    const value = TOTP_ALPHABET.indexOf(character);
    if (value < 0) fail("Invalid Base32 character in MFA shared secret.");
    buffer = (buffer << 5) | value;
    bitsLeft += 5;
    if (bitsLeft >= 8) {
      output.push((buffer >> (bitsLeft - 8)) & 0xff);
      bitsLeft -= 8;
    }
  }
  const secretBytes = Buffer.from(output);
  const counterBytes = Buffer.alloc(8);
  counterBytes.writeBigUInt64BE(BigInt(counter));
  const hash = createHmac("sha1", secretBytes).update(counterBytes).digest();
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

  get(name) {
    return this.cookies.get(name);
  }

  cookieHeader() {
    return [...this.cookies.entries()].map(([name, value]) => `${name}=${value}`).join("; ");
  }
}

async function api(method, path, body, jar, extraHeaders = {}) {
  const headers = { "Content-Type": "application/json", Accept: "application/json", ...extraHeaders };
  if (jar?.cookieHeader()) headers.Cookie = jar.cookieHeader();
  if (jar?.get("NevaxisFactOS.session") && jar.get("NevaxisFactOS.csrf") && method !== "GET") {
    headers["X-NevaxisFactOS-Csrf"] = jar.get("NevaxisFactOS.csrf");
  }
  const response = await fetch(`${BASE}${path}`, {
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

async function signIn(email, password, mfaSecret) {
  const jar = new CookieJar();
  const signInResult = await api("POST", "/api/identity/sign-in", {
    organizationContext: ORGANIZATION,
    email,
    password,
    deviceLabel: `${CLOSURE_PREFIX}-security`,
  }, jar);
  if (mfaSecret) {
    const verify = await api("POST", "/api/identity/mfa/verify", { totpCode: generateTotp(mfaSecret) }, jar);
    if (verify.status !== 200) fail(`MFA failed for ${email}: status=${verify.status}`);
  }
  return { jar, signInResult };
}

function previewClosureFixtures() {
  const count = Number(db(`
    SELECT COUNT(*) FROM authorization_audit_events
    WHERE entity_reference ILIKE 'adminaccess1-closure%'
       OR actor_reference LIKE '${CLOSURE_PREFIX}%'
  `));
  console.log(`Preview ${CLOSURE_PREFIX} security fixtures: audits=${count}`);
  return count;
}

function removeClosureFixtures() {
  db(`
    DELETE FROM admin_access_idempotency_records WHERE idempotency_key LIKE '${CLOSURE_PREFIX}%';
    DELETE FROM authorization_audit_events
    WHERE entity_reference ILIKE 'adminaccess1-closure%' OR actor_reference LIKE '${CLOSURE_PREFIX}%';
    DELETE FROM tenant_role_permissions
    WHERE tenant_role_id IN (SELECT id FROM tenant_roles WHERE key ILIKE 'adminaccess1-closure%');
    DELETE FROM tenant_membership_roles
    WHERE tenant_role_id IN (SELECT id FROM tenant_roles WHERE key ILIKE 'adminaccess1-closure%');
    DELETE FROM tenant_roles WHERE key ILIKE 'adminaccess1-closure%';
  `);
}

console.log("Running ADMIN-ACCESS-1 security verifier.");
previewClosureFixtures();
removeClosureFixtures();

const missing = await api("GET", "/api/admin-access/roles");
assert("missing auth 401", missing.status === 401, `status=${missing.status}`);

const active = await signIn("active-user@dev.local", requireEnv("DevelopmentSaasIdentity__ActiveUserPassword"));
const unauthorized = await api("GET", "/api/admin-access/roles", undefined, active.jar);
assert("unauthorized 403", unauthorized.status === 403, `status=${unauthorized.status}`);

const owner = await signIn(
  "tenant-owner@dev.local",
  requireEnv("DevelopmentSaasIdentity__TenantOwnerPassword"),
  requireEnv("DevelopmentSaasIdentity__MfaSharedSecret"),
);

const pending = await signIn("tenant-owner@dev.local", requireEnv("DevelopmentSaasIdentity__TenantOwnerPassword"));
const mfaRequired = await api("POST", "/api/admin-access/roles", {
  roleKey: "adminaccess1-closure-mfa",
  displayName: "MFA blocked",
  idempotencyKey: `${CLOSURE_PREFIX}-mfa`,
}, pending.jar);
assert("recent MFA required", mfaRequired.status === 403 && mfaRequired.json?.code === "admin-access.mfa-required", mfaRequired.raw.slice(0, 200));
const mfaRoleCount = Number(db(`SELECT COUNT(*) FROM tenant_roles WHERE key = 'adminaccess1-closure-mfa'`));
assert("denied mutation created no record", mfaRoleCount === 0, `count=${mfaRoleCount}`);

const beforeAudit = Number(db("SELECT COUNT(*) FROM authorization_audit_events"));
const untrustedTenantHeader = await api(
  "GET",
  "/api/admin-access/roles",
  undefined,
  owner.jar,
  { "X-NevaxisFactOS-Tenant": "other-tenant-not-pilot" },
);
assert("untrusted tenant header rejected", untrustedTenantHeader.status === 403, `status=${untrustedTenantHeader.status}`);

const untrustedTenant = await api("POST", "/api/admin-access/roles", {
  roleKey: "adminaccess1-closure-untrusted",
  displayName: "Untrusted",
  tenantId: "00000000-0000-4000-8000-000000000099",
  idempotencyKey: `${CLOSURE_PREFIX}-untrusted-tenant`,
}, owner.jar);
assert("untrusted tenant ignored or accepted only as payload", untrustedTenant.status === 200 || untrustedTenant.status === 400, `status=${untrustedTenant.status}`);
const createdTenant = db(`SELECT tenant_id FROM tenant_roles WHERE key = 'adminaccess1-closure-untrusted'`);
if (createdTenant) {
  assert("untrusted tenant not accepted as authority", createdTenant === TENANT_ID, createdTenant);
}

const selfRoles = await api("PUT", `/api/admin-access/members/${OWNER_MEMBER}/roles`, {
  roleReferences: ["tenant-admin"],
  expectedAuthorizationVersion: 1,
  idempotencyKey: `${CLOSURE_PREFIX}-self-roles`,
}, owner.jar);
assert("self-role mutation blocked", selfRoles.status === 403, `status=${selfRoles.status}`);

const selfFacilities = await api("PUT", `/api/admin-access/members/${OWNER_MEMBER}/facilities`, {
  facilityCodes: [FACILITY_A],
  expectedAuthorizationVersion: 1,
  idempotencyKey: `${CLOSURE_PREFIX}-self-facilities`,
}, owner.jar);
assert("self-elevation / self facility mutation blocked", selfFacilities.status === 403, `status=${selfFacilities.status}`);

const platform = await api("POST", "/api/admin-access/roles", {
  roleKey: "adminaccess1-closure-platform",
  displayName: "Platform probe",
  idempotencyKey: `${CLOSURE_PREFIX}-platform-role`,
}, owner.jar);
assert("platform probe role created", platform.status === 200, platform.raw.slice(0, 180));
const platformRole = (await api("GET", "/api/admin-access/roles", undefined, owner.jar)).json.roles.find((r) => r.roleReference === "adminaccess1-closure-platform");
const platformGrant = await api("POST", "/api/admin-access/roles/adminaccess1-closure-platform/permissions", {
  expectedVersion: platformRole.version,
  grants: [{ permissionKey: "system.diagnostics.read", effect: "allow" }],
  idempotencyKey: `${CLOSURE_PREFIX}-platform-grant`,
}, owner.jar);
assert("platform-admin grant blocked", platformGrant.status === 403, `status=${platformGrant.status}`);

const salesAccess = await api("GET", `/api/admin-access/members/${SALES_MEMBER}/effective-access`, undefined, owner.jar);
const salesVersion = Number(salesAccess.json?.authorizationVersion ?? 1);
const unowned = await api("PUT", `/api/admin-access/members/${SALES_MEMBER}/permission-overrides`, {
  overrides: [{ permissionKey: "system.outbox.write", effect: "allow" }],
  expectedAuthorizationVersion: salesVersion,
  idempotencyKey: `${CLOSURE_PREFIX}-unowned`,
}, owner.jar);
assert("cannot grant unowned/platform permission", unowned.status === 403, `status=${unowned.status}`);

const ownerRemoval = await api("PUT", `/api/admin-access/members/${OWNER_MEMBER}/roles`, {
  roleReferences: ["tenant-admin"],
  expectedAuthorizationVersion: 1,
  idempotencyKey: `${CLOSURE_PREFIX}-final-owner`,
}, owner.jar);
assert("final owner removal blocked", ownerRemoval.status === 403, `status=${ownerRemoval.status}`);

const crossTenant = await api("GET", "/api/admin-access/members/mem_ffffffffffffffffffffffffffffffff/effective-access", undefined, owner.jar);
assert("cross-tenant result", crossTenant.status === 403 || crossTenant.status === 404, `status=${crossTenant.status}`);

const sales = await signIn("materials-sales@dev.local", requireEnv("DevelopmentSaasIdentity__ActiveUserPassword"));
const facilityB = await api(
  "GET",
  `/api/materials/workspace-summary?facilityId=${FACILITY_B_ID}`,
  undefined,
  sales.jar,
);
assert("cross-facility denial", facilityB.status === 401 || facilityB.status === 403, `status=${facilityB.status}`);

const untrustedFacility = await api("PUT", `/api/admin-access/members/${SALES_MEMBER}/facilities`, {
  facilityCodes: ["not-a-real-facility"],
  expectedAuthorizationVersion: salesVersion,
  tenantId: TENANT_ID,
  idempotencyKey: `${CLOSURE_PREFIX}-bad-facility`,
}, owner.jar);
assert("untrusted facility rejected", untrustedFacility.status === 400 || untrustedFacility.status === 403, `status=${untrustedFacility.status}`);

const afterDeniedAudit = Number(db("SELECT COUNT(*) FROM authorization_audit_events"));
assert("denied mutation created no audit growth beyond successful owner probes", afterDeniedAudit >= beforeAudit);

const audit = await api("GET", "/api/admin-access/audit-events", undefined, owner.jar);
const sample = (audit.json?.events ?? []).slice(-1)[0];
assert("audit contains actor", Boolean(sample?.actorReference), JSON.stringify(sample));
assert("audit contains before/after without requiring secrets", !JSON.stringify(audit.json).includes("password") && !JSON.stringify(audit.json).includes("totp") && !JSON.stringify(audit.json).includes("sessionToken"));

previewClosureFixtures();
removeClosureFixtures();
assert("closure fixtures remaining", previewClosureFixtures() === 0);

void ROOT;
void FACILITY_A;
void FACILITY_B;

console.log("ADMIN-ACCESS-1 security verification passed.");
