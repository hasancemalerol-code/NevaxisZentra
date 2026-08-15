#!/usr/bin/env node
import { createHmac } from "node:crypto";
import { spawnSync } from "node:child_process";
import { readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), "../../..");
const BASE = process.env.CREVAFACTOS_API_BASE_URL ?? "http://127.0.0.1:5000";
const ORGANIZATION = "dev-pilot-tenant";
const CLOSURE_PREFIX = "SAAS-ADMIN-1-CLOSURE";
const CLOSURE_ROLE_INPUT = " SAAS-ADMIN-1-CLOSURE-role ";
const CLOSURE_ROLE = "saas-admin-1-closure-role";
const CLOSURE_MEMBER = `${CLOSURE_PREFIX}-member`;
const CLOSURE_INVITE_EMAIL = `${CLOSURE_PREFIX.toLowerCase()}@closure.dev.local`;
const TENANT_ID = "bb000001-0001-4000-8000-000000000001";
const CLOSURE_MEMBERSHIP_ID = "dd000001-0005-4000-8000-000000000004";
const CLOSURE_IDENTITY_ID = "dd000001-0004-4000-8000-000000000004";
const OWNER_MEMBER = "mem_bb000002000340008000000000000003";
const SALES_MEMBER = "mem_bb000003021340008000000000000213";
const MACHINE_MEMBER = "mem_bb000003020340008000000000000203";
const FACILITY_A = "pilot-facility";
const FACILITY_B = "pilot-facility-unauthorized";
const TOTP_ALPHABET = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
const TOTP_STEP_SECONDS = 30;
const TOTP_DIGITS = 6;

function fail(message) {
  console.error(`SAAS-ADMIN-1 domain-permissions verification failed: ${message}`);
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
    ["exec", "crevafactos-local-postgres-1", "psql", "-U", "crevafactos", "-d", "crevafactos_local", "-t", "-A", "-c", sql],
    { encoding: "utf8" },
  );
  if (result.status !== 0) fail(result.stderr || result.stdout);
  return result.stdout.trim();
}

function base32Decode(encoded) {
  const normalized = encoded.trim().replace(/=+$/, "").toUpperCase();
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
  return Buffer.from(output);
}

function generateTotp(secret) {
  const counter = Math.floor(Date.now() / 1000 / TOTP_STEP_SECONDS);
  const secretBytes = base32Decode(secret);
  const counterBytes = Buffer.alloc(8);
  counterBytes.writeBigUInt64BE(BigInt(counter));
  const hash = createHmac("sha1", secretBytes).update(counterBytes).digest();
  const offset = hash[hash.length - 1] & 0x0f;
  const binaryCode =
    ((hash[offset] & 0x7f) << 24)
    | ((hash[offset + 1] & 0xff) << 16)
    | ((hash[offset + 2] & 0xff) << 8)
    | (hash[offset + 3] & 0xff);
  return String(binaryCode % 10 ** TOTP_DIGITS).padStart(TOTP_DIGITS, "0");
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

async function api(method, path, body, jar) {
  const headers = { "Content-Type": "application/json", Accept: "application/json" };
  if (jar?.cookieHeader()) headers.Cookie = jar.cookieHeader();
  if (jar?.get("crevafactos.session") && jar.get("crevafactos.csrf") && method !== "GET") {
    headers["X-CrevaFactOS-Csrf"] = jar.get("crevafactos.csrf");
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

function previewClosureFixtures() {
  const roles = db(`SELECT COUNT(*) FROM tenant_roles WHERE key ILIKE 'saas-admin-1-closure%'`);
  const members = db(`SELECT COUNT(*) FROM tenant_memberships WHERE membership_reference LIKE '${CLOSURE_PREFIX}%'`);
  const invitations = db(`SELECT COUNT(*) FROM saas_identity_invitations WHERE intended_email_normalized LIKE '${CLOSURE_PREFIX.toLowerCase()}%'`);
  const audits = db(`SELECT COUNT(*) FROM authorization_audit_events WHERE entity_reference ILIKE 'saas-admin-1-closure%'`);
  console.log(`Preview ${CLOSURE_PREFIX} fixtures: roles=${roles}, members=${members}, invitations=${invitations}, audits=${audits}`);
  return {
    roles: Number(roles),
    members: Number(members),
    invitations: Number(invitations),
    audits: Number(audits),
  };
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
    DELETE FROM admin_access_idempotency_records
    WHERE idempotency_key LIKE '${CLOSURE_PREFIX}%';
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

async function signInOwner() {
  const password = requireEnv("DevelopmentSaasIdentity__TenantOwnerPassword");
  const secret = requireEnv("DevelopmentSaasIdentity__MfaSharedSecret");
  const jar = new CookieJar();
  const signIn = await api("POST", "/api/identity/sign-in", {
    organizationContext: ORGANIZATION,
    email: "tenant-owner@dev.local",
    password,
    deviceLabel: `${CLOSURE_PREFIX}-domain`,
  }, jar);
  if (signIn.status !== 200 || !signIn.json?.requiresMfa) {
    fail(`tenant-owner sign-in did not require MFA: status=${signIn.status}`);
  }
  const verify = await api("POST", "/api/identity/mfa/verify", { totpCode: generateTotp(secret) }, jar);
  if (verify.status !== 200) fail(`tenant-owner MFA failed: status=${verify.status}`);
  return jar;
}

console.log("Running SAAS-ADMIN-1 domain-permissions verifier.");
readFileSync(resolve(ROOT, "packages/CrevaFactOS.Persistence/AdminAccess/AdminAccessConstants.cs"), "utf8");

previewClosureFixtures();
removeClosureFixtures();
previewClosureFixtures();

const now = new Date().toISOString();
db(`
  INSERT INTO identity_subjects (id, external_issuer, external_subject, status, created_at_utc)
  VALUES ('${CLOSURE_IDENTITY_ID}', 'crevafactos-saas', '${CLOSURE_IDENTITY_ID}', 'Active', '${now}')
  ON CONFLICT (id) DO NOTHING;
  INSERT INTO tenant_memberships (id, tenant_id, identity_subject_id, membership_reference, status, authorization_version, created_at_utc)
  VALUES ('${CLOSURE_MEMBERSHIP_ID}', '${TENANT_ID}', '${CLOSURE_IDENTITY_ID}', '${CLOSURE_MEMBER}', 'Active', 1, '${now}')
  ON CONFLICT (id) DO NOTHING;
`);

const owner = await signInOwner();
const ready = await fetch(`${BASE}/health/ready`);
assert("API ready", ready.status === 200, `status=${ready.status}`);

const summary = await api("GET", "/api/admin-access/workspace-summary", undefined, owner);
assert("workspace-summary", summary.status === 200 && summary.json?.tenantKey === ORGANIZATION, summary.raw.slice(0, 180));
assert("workspace-summary counts", typeof summary.json?.activeMembers === "number");

const membersList = await api("GET", "/api/admin-access/members", undefined, owner);
assert("members list", membersList.status === 200 && Array.isArray(membersList.json?.members), membersList.raw.slice(0, 180));
const closureMemberRow = membersList.json.members.find((m) => m.memberReference === CLOSURE_MEMBER);
assert("closure member listed", Boolean(closureMemberRow));

const invitationsBefore = await api("GET", "/api/admin-access/invitations", undefined, owner);
assert("invitations list", invitationsBefore.status === 200 && Array.isArray(invitationsBefore.json?.invitations));

const facilities = await api("GET", "/api/admin-access/facilities", undefined, owner);
assert("facilities list", facilities.status === 200 && facilities.json?.facilities?.length >= 1, facilities.raw.slice(0, 180));
assert("facility A present", facilities.json.facilities.some((f) => f.facilityCode === FACILITY_A));

const sessions = await api("GET", "/api/admin-access/sessions", undefined, owner);
assert("sessions list", sessions.status === 200 && Array.isArray(sessions.json?.sessions));

const audit = await api("GET", "/api/admin-access/audit-events?limit=50", undefined, owner);
assert("audit-events limit", audit.status === 200 && Array.isArray(audit.json?.events));
assert("audit-events bounded", audit.json.events.length <= 50);

const created = await api("POST", "/api/admin-access/roles", {
  roleKey: CLOSURE_ROLE_INPUT,
  displayName: "Closure role",
  description: "Domain verifier role",
  idempotencyKey: `${CLOSURE_PREFIX}-create-role`,
}, owner);
assert("role created", created.status === 200, `status=${created.status} ${created.raw.slice(0, 180)}`);

const roles = await api("GET", "/api/admin-access/roles", undefined, owner);
const closureRole = roles.json?.roles?.find((role) => role.roleReference === CLOSURE_ROLE);
assert("role key normalized", Boolean(closureRole), JSON.stringify(roles.json?.roles?.map((r) => r.roleReference) ?? []));

const permissions = await api("GET", "/api/admin-access/permissions", undefined, owner);
const materialRead = permissions.json?.permissions?.find((p) => p.permissionKey === "material.read");
assert("permission matrix material.read", materialRead?.normalizedKey === "material.read");
assert("permissions deterministically ordered", (permissions.json?.permissions ?? []).every((item, index, list) => index === 0 || list[index - 1].normalizedKey <= item.normalizedKey));

const grant = await api("POST", `/api/admin-access/roles/${CLOSURE_ROLE}/permissions`, {
  expectedVersion: closureRole.version,
  idempotencyKey: `${CLOSURE_PREFIX}-grant`,
  grants: [{ permissionKey: "material.read", effect: "allow" }],
}, owner);
assert("role grant", grant.status === 200, grant.raw.slice(0, 180));

const assign = await api("PUT", `/api/admin-access/members/${CLOSURE_MEMBER}/roles`, {
  roleReferences: [CLOSURE_ROLE],
  expectedAuthorizationVersion: 1,
  idempotencyKey: `${CLOSURE_PREFIX}-assign-role`,
}, owner);
assert("membership role assignment", assign.status === 200, assign.raw.slice(0, 180));

const facilityAssign = await api("PUT", `/api/admin-access/members/${CLOSURE_MEMBER}/facilities`, {
  facilityCodes: [FACILITY_A],
  expectedAuthorizationVersion: 2,
  idempotencyKey: `${CLOSURE_PREFIX}-assign-facility`,
}, owner);
assert("facility access assignment", facilityAssign.status === 200, facilityAssign.raw.slice(0, 180));

const allow = await api("PUT", `/api/admin-access/members/${CLOSURE_MEMBER}/permission-overrides`, {
  overrides: [{ permissionKey: "edge.read", effect: "allow" }],
  expectedAuthorizationVersion: 3,
  idempotencyKey: `${CLOSURE_PREFIX}-direct-allow`,
}, owner);
assert("direct allow", allow.status === 200, allow.raw.slice(0, 180));

const deny = await api("PUT", `/api/admin-access/members/${CLOSURE_MEMBER}/permission-overrides`, {
  overrides: [
    { permissionKey: "edge.read", effect: "allow" },
    { permissionKey: "material.read", effect: "deny" },
  ],
  expectedAuthorizationVersion: 4,
  idempotencyKey: `${CLOSURE_PREFIX}-explicit-deny`,
}, owner);
assert("explicit deny applied", deny.status === 200, deny.raw.slice(0, 180));

const access = await api("GET", `/api/admin-access/members/${CLOSURE_MEMBER}/effective-access`, undefined, owner);
assert("explicit deny wins over role allow", access.json?.deniedPermissionKeys?.includes("material.read") === true, JSON.stringify(access.json));
assert("denied key not granted", access.json?.grantedPermissionKeys?.includes("material.read") !== true);
assert("direct allow remains", access.json?.grantedPermissionKeys?.includes("edge.read") === true);
assert("facility codes on effective-access", access.json?.facilityCodes?.includes(FACILITY_A) === true);

const stale = await api("POST", `/api/admin-access/roles/${CLOSURE_ROLE}/permissions`, {
  expectedVersion: closureRole.version,
  idempotencyKey: `${CLOSURE_PREFIX}-stale`,
  grants: [{ permissionKey: "edge.read", effect: "allow" }],
}, owner);
assert("optimistic concurrency stale version", stale.status === 409, `status=${stale.status}`);

const unowned = await api("PUT", `/api/admin-access/members/${SALES_MEMBER}/permission-overrides`, {
  overrides: [{ permissionKey: "system.outbox.write", effect: "allow" }],
  expectedAuthorizationVersion: Number((await api("GET", `/api/admin-access/members/${SALES_MEMBER}/effective-access`, undefined, owner)).json?.authorizationVersion ?? 1),
  idempotencyKey: `${CLOSURE_PREFIX}-unowned`,
}, owner);
assert("cannot grant unowned/platform permission", unowned.status === 403, `status=${unowned.status}`);

const selfRoles = await api("PUT", `/api/admin-access/members/${OWNER_MEMBER}/roles`, {
  roleReferences: ["tenant-admin"],
  expectedAuthorizationVersion: 1,
  idempotencyKey: `${CLOSURE_PREFIX}-self-roles`,
}, owner);
assert("self-role mutation blocked", selfRoles.status === 403, `status=${selfRoles.status}`);

const selfFacilities = await api("PUT", `/api/admin-access/members/${OWNER_MEMBER}/facilities`, {
  facilityCodes: [FACILITY_A],
  expectedAuthorizationVersion: 1,
  idempotencyKey: `${CLOSURE_PREFIX}-self-facilities`,
}, owner);
assert("self-elevation / self facility mutation blocked", selfFacilities.status === 403, `status=${selfFacilities.status}`);

const ownerRemoval = await api("PUT", `/api/admin-access/members/${OWNER_MEMBER}/roles`, {
  roleReferences: ["tenant-admin"],
  expectedAuthorizationVersion: 1,
  idempotencyKey: `${CLOSURE_PREFIX}-final-owner`,
}, owner);
assert("final owner removal blocked", ownerRemoval.status === 403, `status=${ownerRemoval.status}`);

const untrustedFacility = await api("PUT", `/api/admin-access/members/${MACHINE_MEMBER}/facilities`, {
  facilityCodes: [FACILITY_B],
  expectedAuthorizationVersion: Number((await api("GET", `/api/admin-access/members/${MACHINE_MEMBER}/effective-access`, undefined, owner)).json?.authorizationVersion ?? 1),
  idempotencyKey: `${CLOSURE_PREFIX}-bad-facility`,
}, owner);
assert("untrusted facility rejected", untrustedFacility.status === 400 || untrustedFacility.status === 403, `status=${untrustedFacility.status}`);

const invite = await api("POST", "/api/admin-access/invitations", {
  email: CLOSURE_INVITE_EMAIL,
  intendedRoleReference: CLOSURE_ROLE,
  idempotencyKey: `${CLOSURE_PREFIX}-invite`,
}, owner);
assert("invitation created", invite.status === 200 && invite.json?.invitationReference, invite.raw.slice(0, 180));

const invitationsAfter = await api("GET", "/api/admin-access/invitations", undefined, owner);
assert("invitation listed", invitationsAfter.json?.invitations?.some((i) => i.emailMasked?.includes("@")), JSON.stringify(invitationsAfter.json?.invitations?.slice(0, 2)));

const roleChange = await api("PUT", `/api/admin-access/members/${CLOSURE_MEMBER}/roles`, {
  roleReferences: [CLOSURE_ROLE, "read-only-auditor"],
  expectedAuthorizationVersion: Number(access.json?.authorizationVersion ?? 5),
  idempotencyKey: `${CLOSURE_PREFIX}-role-change`,
}, owner);
assert("effective-access role change", roleChange.status === 200, roleChange.raw.slice(0, 180));

const changedAccess = await api("GET", `/api/admin-access/members/${CLOSURE_MEMBER}/effective-access`, undefined, owner);
assert("effective-access reflects role change", changedAccess.json?.roleReferences?.includes("read-only-auditor") === true, JSON.stringify(changedAccess.json?.roleReferences));

previewClosureFixtures();
removeClosureFixtures();
const remaining = previewClosureFixtures();
assert("closure fixtures remaining", remaining.roles === 0 && remaining.members === 0 && remaining.invitations === 0);

const templates = db(`SELECT COUNT(*) FROM tenant_roles WHERE system_template_origin = 'development-template'`);
assert("deterministic role templates preserved", Number(templates) === 15, `count=${templates}`);

void machinePassword;
console.log("SAAS-ADMIN-1 domain-permissions verification passed.");
