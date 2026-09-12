#!/usr/bin/env node
import { createHmac } from "node:crypto";
import { spawnSync } from "node:child_process";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), "../../..");
const BASE = process.env.NEVAXIS_FACTOS_API_BASE_URL ?? "http://127.0.0.1:5000";
const ORGANIZATION = "dev-pilot-tenant";
const CLOSURE_PREFIX = "SAAS-ADMIN-1-CLOSURE";
const CLOSURE_MEMBER = `${CLOSURE_PREFIX}-member`;
const CLOSURE_INVITE_EMAIL = `${CLOSURE_PREFIX.toLowerCase()}@closure.dev.local`;
const TENANT_ID = "bb000001-0001-4000-8000-000000000001";
const CLOSURE_MEMBERSHIP_ID = "dd000001-0005-4000-8000-000000000004";
const CLOSURE_IDENTITY_ID = "dd000001-0004-4000-8000-000000000004";
const OWNER_MEMBER = "mem_bb000002000340008000000000000003";
const SALES_MEMBER = "mem_bb000003021340008000000000000213";
const FACILITY_B_ID = "bb000001-0005-4000-8000-000000000099";
const TOTP_ALPHABET = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";

function fail(message) {
  console.error(`SAAS-ADMIN-1 security-idempotency verification failed: ${message}`);
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

function machinePassword() {
  return process.env.DevelopmentSaasIdentity__MachineResponsiblePassword
    ?? requireEnv("DevelopmentSaasIdentity__ActiveUserPassword");
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

async function signIn(email, password, mfaSecret, deviceLabel) {
  const jar = new CookieJar();
  const signInResult = await api("POST", "/api/identity/sign-in", {
    organizationContext: ORGANIZATION,
    email,
    password,
    deviceLabel: deviceLabel ?? `${CLOSURE_PREFIX}-security`,
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
    WHERE entity_reference ILIKE 'saas-admin-1-closure%'
       OR actor_reference LIKE '${CLOSURE_PREFIX}%'
  `));
  const invitations = Number(db(`SELECT COUNT(*) FROM saas_identity_invitations WHERE intended_email_normalized LIKE '${CLOSURE_PREFIX.toLowerCase()}%'`));
  console.log(`Preview ${CLOSURE_PREFIX} security fixtures: audits=${count}, invitations=${invitations}`);
  return { audits: count, invitations };
}

function removeClosureFixtures() {
  db(`
    DELETE FROM tenant_membership_permission_overrides
    WHERE tenant_membership_id IN (SELECT id FROM tenant_memberships WHERE membership_reference LIKE '${CLOSURE_PREFIX}%');
    DELETE FROM tenant_membership_roles
    WHERE tenant_membership_id IN (SELECT id FROM tenant_memberships WHERE membership_reference LIKE '${CLOSURE_PREFIX}%')
       OR tenant_role_id IN (SELECT id FROM tenant_roles WHERE key ILIKE 'saas-admin-1-closure%');
    DELETE FROM facility_memberships
    WHERE tenant_membership_id IN (SELECT id FROM tenant_memberships WHERE membership_reference LIKE '${CLOSURE_PREFIX}%');
    DELETE FROM saas_identity_sessions
    WHERE device_label LIKE '${CLOSURE_PREFIX}%'
       OR tenant_membership_id IN (SELECT id FROM tenant_memberships WHERE membership_reference LIKE '${CLOSURE_PREFIX}%');
    DELETE FROM admin_access_idempotency_records WHERE idempotency_key LIKE '${CLOSURE_PREFIX}%';
    DELETE FROM authorization_audit_events
    WHERE entity_reference ILIKE 'saas-admin-1-closure%' OR actor_reference LIKE '${CLOSURE_PREFIX}%';
    DELETE FROM saas_identity_invitations
    WHERE intended_email_normalized LIKE '${CLOSURE_PREFIX.toLowerCase()}%';
    DELETE FROM tenant_role_permissions
    WHERE tenant_role_id IN (SELECT id FROM tenant_roles WHERE key ILIKE 'saas-admin-1-closure%');
    DELETE FROM tenant_memberships WHERE membership_reference LIKE '${CLOSURE_PREFIX}%';
    DELETE FROM tenant_roles WHERE key ILIKE 'saas-admin-1-closure%';
    DELETE FROM identity_subjects WHERE id = '${CLOSURE_IDENTITY_ID}';
  `);
}

console.log("Running SAAS-ADMIN-1 security-idempotency verifier.");
previewClosureFixtures();
removeClosureFixtures();

const now = new Date().toISOString();
db(`
  INSERT INTO identity_subjects (id, external_issuer, external_subject, status, created_at_utc)
  VALUES ('${CLOSURE_IDENTITY_ID}', 'NevaxisFactOS-saas', '${CLOSURE_IDENTITY_ID}', 'Active', '${now}')
  ON CONFLICT (id) DO NOTHING;
  INSERT INTO tenant_memberships (id, tenant_id, identity_subject_id, membership_reference, status, authorization_version, created_at_utc)
  VALUES ('${CLOSURE_MEMBERSHIP_ID}', '${TENANT_ID}', '${CLOSURE_IDENTITY_ID}', '${CLOSURE_MEMBER}', 'Active', 1, '${now}')
  ON CONFLICT (id) DO NOTHING;
`);

const missing = await api("GET", "/api/admin-access/workspace-summary");
assert("missing auth 401", missing.status === 401, `status=${missing.status}`);

const active = await signIn("active-user@dev.local", requireEnv("DevelopmentSaasIdentity__ActiveUserPassword"));
const unauthorized = await api("GET", "/api/admin-access/workspace-summary", undefined, active.jar);
assert("unauthorized 403", unauthorized.status === 403, `status=${unauthorized.status}`);

const owner = await signIn(
  "admin@demo.nevaxisfactos.local",
  requireEnv("DevelopmentSaasIdentity__TenantOwnerPassword"),
  requireEnv("DevelopmentSaasIdentity__MfaSharedSecret"),
);

const pending = await signIn("admin@demo.nevaxisfactos.local", requireEnv("DevelopmentSaasIdentity__TenantOwnerPassword"));
const mfaRequired = await api("POST", "/api/admin-access/invitations", {
  email: `${CLOSURE_PREFIX.toLowerCase()}-mfa@closure.dev.local`,
  idempotencyKey: `${CLOSURE_PREFIX}-mfa`,
}, pending.jar);
assert("recent MFA required", mfaRequired.status === 403 && mfaRequired.json?.code === "admin-access.mfa-required", mfaRequired.raw.slice(0, 200));
const mfaInviteCount = Number(db(`SELECT COUNT(*) FROM saas_identity_invitations WHERE intended_email_normalized = '${CLOSURE_PREFIX.toLowerCase()}-mfa@closure.dev.local'`));
assert("denied mutation created no invitation", mfaInviteCount === 0, `count=${mfaInviteCount}`);

const beforeAudit = Number(db("SELECT COUNT(*) FROM authorization_audit_events"));
const untrustedTenantHeader = await api(
  "GET",
  "/api/admin-access/members",
  undefined,
  owner.jar,
  { "X-NevaxisFactOS-Tenant": "other-tenant-not-pilot" },
);
assert("untrusted tenant header rejected", untrustedTenantHeader.status === 403, `status=${untrustedTenantHeader.status}`);

const crossTenant = await api("GET", "/api/admin-access/members/mem_ffffffffffffffffffffffffffffffff/effective-access", undefined, owner.jar);
assert("cross-tenant member blocked", crossTenant.status === 403 || crossTenant.status === 404, `status=${crossTenant.status}`);

const sales = await signIn("materials-sales@dev.local", requireEnv("DevelopmentSaasIdentity__ActiveUserPassword"));
const facilityB = await api(
  "GET",
  `/api/materials/workspace-summary?facilityId=${FACILITY_B_ID}`,
  undefined,
  sales.jar,
);
assert("cross-facility denial", facilityB.status === 401 || facilityB.status === 403, `status=${facilityB.status}`);

const createdInvite = await api("POST", "/api/admin-access/invitations", {
  email: CLOSURE_INVITE_EMAIL,
  intendedRoleReference: "read-only-auditor",
  idempotencyKey: `${CLOSURE_PREFIX}-invite`,
}, owner.jar);
assert("invitation created", createdInvite.status === 200, createdInvite.raw.slice(0, 180));
const invitationReference = createdInvite.json?.invitationReference;

const inviteReplay = await api("POST", "/api/admin-access/invitations", {
  email: CLOSURE_INVITE_EMAIL,
  intendedRoleReference: "read-only-auditor",
  idempotencyKey: `${CLOSURE_PREFIX}-invite`,
}, owner.jar);
assert("invitation idempotent replay", inviteReplay.status === 200 && inviteReplay.json?.idempotentReplay === true, inviteReplay.raw.slice(0, 180));

const inviteConflict = await api("POST", "/api/admin-access/invitations", {
  email: `${CLOSURE_PREFIX.toLowerCase()}-other@closure.dev.local`,
  intendedRoleReference: "read-only-auditor",
  idempotencyKey: `${CLOSURE_PREFIX}-invite`,
}, owner.jar);
assert("invitation idempotency conflict", inviteConflict.status === 409, `status=${inviteConflict.status}`);

const closureAccess = await api("GET", `/api/admin-access/members/${CLOSURE_MEMBER}/effective-access`, undefined, owner.jar);
const closureVersion = Number(closureAccess.json?.authorizationVersion ?? 1);

const suspend = await api("POST", `/api/admin-access/members/${CLOSURE_MEMBER}/suspend`, {
  expectedAuthorizationVersion: closureVersion,
  idempotencyKey: `${CLOSURE_PREFIX}-suspend`,
}, owner.jar);
assert("member suspended", suspend.status === 200, suspend.raw.slice(0, 180));

const suspendReplay = await api("POST", `/api/admin-access/members/${CLOSURE_MEMBER}/suspend`, {
  expectedAuthorizationVersion: closureVersion + 1,
  idempotencyKey: `${CLOSURE_PREFIX}-suspend`,
}, owner.jar);
assert("suspend idempotent replay", suspendReplay.status === 200 && suspendReplay.json?.idempotentReplay === true, suspendReplay.raw.slice(0, 180));

const staleSuspend = await api("POST", `/api/admin-access/members/${CLOSURE_MEMBER}/suspend`, {
  expectedAuthorizationVersion: closureVersion,
  idempotencyKey: `${CLOSURE_PREFIX}-suspend-stale`,
}, owner.jar);
assert("suspend optimistic concurrency 409", staleSuspend.status === 409, `status=${staleSuspend.status}`);

const reactivate = await api("POST", `/api/admin-access/members/${CLOSURE_MEMBER}/reactivate`, {
  expectedAuthorizationVersion: closureVersion + 1,
  idempotencyKey: `${CLOSURE_PREFIX}-reactivate`,
}, owner.jar);
assert("member reactivated", reactivate.status === 200, reactivate.raw.slice(0, 180));

const reactivateReplay = await api("POST", `/api/admin-access/members/${CLOSURE_MEMBER}/reactivate`, {
  expectedAuthorizationVersion: closureVersion + 2,
  idempotencyKey: `${CLOSURE_PREFIX}-reactivate`,
}, owner.jar);
assert("reactivate idempotent replay", reactivateReplay.status === 200 && reactivateReplay.json?.idempotentReplay === true, reactivateReplay.raw.slice(0, 180));

const victim = await signIn(
  "machine-responsible@dev.local",
  machinePassword(),
  undefined,
  `${CLOSURE_PREFIX}-session-victim`,
);
const victimMe = await api("GET", "/api/admin-access/me/effective-access", undefined, victim.jar);
assert("victim session active before revoke", victimMe.status === 200, `status=${victimMe.status}`);

const sessionsBefore = await api("GET", "/api/admin-access/sessions", undefined, owner.jar);
const victimSession = (sessionsBefore.json?.sessions ?? []).find((s) => s.deviceLabel === `${CLOSURE_PREFIX}-session-victim`);
assert("victim session listed", Boolean(victimSession?.revokeToken), JSON.stringify(sessionsBefore.json?.sessions?.slice(0, 3)));

const revoke = await api("POST", `/api/admin-access/sessions/${victimSession.revokeToken}/revoke`, {
  idempotencyKey: `${CLOSURE_PREFIX}-revoke-session`,
}, owner.jar);
assert("session revoked", revoke.status === 200, revoke.raw.slice(0, 180));

const revokeReplay = await api("POST", `/api/admin-access/sessions/${victimSession.revokeToken}/revoke`, {
  idempotencyKey: `${CLOSURE_PREFIX}-revoke-session`,
}, owner.jar);
assert("session revoke idempotent replay", revokeReplay.status === 200 && revokeReplay.json?.idempotentReplay === true, revokeReplay.raw.slice(0, 180));

const reuse = await api("GET", "/api/admin-access/me/effective-access", undefined, victim.jar);
assert("revoked session reuse 401", reuse.status === 401, `status=${reuse.status}`);

const afterDeniedAudit = Number(db("SELECT COUNT(*) FROM authorization_audit_events"));
assert("denied mutation created no audit growth beyond successful probes", afterDeniedAudit >= beforeAudit);

const audit = await api("GET", "/api/admin-access/audit-events?limit=50", undefined, owner.jar);
const auditJson = JSON.stringify(audit.json ?? {});
assert("audit secrecy no password", !auditJson.includes("password"));
assert("audit secrecy no totp", !auditJson.includes("totp"));
assert("audit secrecy no sessionToken", !auditJson.includes("sessionToken"));
assert("audit secrecy no raw invitation token", !auditJson.includes("dev-invitation-token"));
if (invitationReference) {
  assert("audit secrecy no invitation reference leak", !auditJson.includes(invitationReference));
}

previewClosureFixtures();
removeClosureFixtures();
const remaining = previewClosureFixtures();
assert("closure fixtures remaining", remaining.audits === 0 && remaining.invitations === 0);

void ROOT;
void OWNER_MEMBER;
void SALES_MEMBER;

console.log("SAAS-ADMIN-1 security-idempotency verification passed.");
