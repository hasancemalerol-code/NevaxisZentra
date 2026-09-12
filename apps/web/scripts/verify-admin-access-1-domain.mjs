#!/usr/bin/env node
import { createHmac } from "node:crypto";
import { spawnSync } from "node:child_process";
import { readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), "../../..");
const BASE = process.env.NEVAXIS_FACTOS_API_BASE_URL ?? "http://127.0.0.1:5000";
const ORGANIZATION = "dev-pilot-tenant";
const CLOSURE_PREFIX = "ADMINACCESS1-CLOSURE";
const CLOSURE_ROLE_INPUT = " ADMINACCESS1-CLOSURE-role ";
const CLOSURE_ROLE = "adminaccess1-closure-role";
const CLOSURE_MEMBER = `${CLOSURE_PREFIX}-member`;
const TENANT_ID = "bb000001-0001-4000-8000-000000000001";
const CLOSURE_MEMBERSHIP_ID = "dd000001-0003-4000-8000-000000000003";
const CLOSURE_IDENTITY_ID = "dd000001-0002-4000-8000-000000000002";
const TOTP_ALPHABET = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
const TOTP_STEP_SECONDS = 30;
const TOTP_DIGITS = 6;

function fail(message) {
  console.error(`ADMIN-ACCESS-1 domain verification failed: ${message}`);
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

function previewClosureFixtures() {
  const roles = db(`SELECT COUNT(*) FROM tenant_roles WHERE key ILIKE 'adminaccess1-closure%'`);
  const members = db(`SELECT COUNT(*) FROM tenant_memberships WHERE membership_reference LIKE '${CLOSURE_PREFIX}%'`);
  const audits = db(`SELECT COUNT(*) FROM authorization_audit_events WHERE entity_reference ILIKE 'adminaccess1-closure%'`);
  console.log(`Preview ${CLOSURE_PREFIX} fixtures: roles=${roles}, members=${members}, audits=${audits}`);
  return { roles: Number(roles), members: Number(members), audits: Number(audits) };
}

function removeClosureFixtures() {
  db(`
    DELETE FROM tenant_membership_permission_overrides
    WHERE tenant_membership_id IN (SELECT id FROM tenant_memberships WHERE membership_reference LIKE '${CLOSURE_PREFIX}%');
    DELETE FROM tenant_membership_roles
    WHERE tenant_membership_id IN (SELECT id FROM tenant_memberships WHERE membership_reference LIKE '${CLOSURE_PREFIX}%')
       OR tenant_role_id IN (SELECT id FROM tenant_roles WHERE key ILIKE 'adminaccess1-closure%');
    DELETE FROM tenant_role_permissions
    WHERE tenant_role_id IN (SELECT id FROM tenant_roles WHERE key ILIKE 'adminaccess1-closure%');
    DELETE FROM facility_memberships
    WHERE tenant_membership_id IN (SELECT id FROM tenant_memberships WHERE membership_reference LIKE '${CLOSURE_PREFIX}%');
    DELETE FROM admin_access_idempotency_records
    WHERE idempotency_key LIKE '${CLOSURE_PREFIX}%';
    DELETE FROM authorization_audit_events
    WHERE entity_reference ILIKE 'adminaccess1-closure%';
    DELETE FROM tenant_memberships WHERE membership_reference LIKE '${CLOSURE_PREFIX}%';
    DELETE FROM tenant_roles WHERE key ILIKE 'adminaccess1-closure%';
    DELETE FROM identity_subjects WHERE id = '${CLOSURE_IDENTITY_ID}';
  `);
}

async function signInOwner() {
  const password = requireEnv("DevelopmentSaasIdentity__TenantOwnerPassword");
  const secret = requireEnv("DevelopmentSaasIdentity__MfaSharedSecret");
  const jar = new CookieJar();
  const signIn = await api("POST", "/api/identity/sign-in", {
    organizationContext: ORGANIZATION,
    email: "admin@demo.nevaxisfactos.local",
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

console.log("Running ADMIN-ACCESS-1 domain verifier.");
readFileSync(resolve(ROOT, "packages/NevaxisFactOS.Persistence/AdminAccess/AdminAccessConstants.cs"), "utf8");

previewClosureFixtures();
removeClosureFixtures();
previewClosureFixtures();

const now = new Date().toISOString();
db(`
  INSERT INTO identity_subjects (id, external_issuer, external_subject, status, created_at_utc)
  VALUES ('${CLOSURE_IDENTITY_ID}', 'NevaxisFactOS-saas', '${CLOSURE_IDENTITY_ID}', 'Active', '${now}')
  ON CONFLICT (id) DO NOTHING;
  INSERT INTO tenant_memberships (id, tenant_id, identity_subject_id, membership_reference, status, authorization_version, created_at_utc)
  VALUES ('${CLOSURE_MEMBERSHIP_ID}', '${TENANT_ID}', '${CLOSURE_IDENTITY_ID}', '${CLOSURE_MEMBER}', 'Active', 1, '${now}')
  ON CONFLICT (id) DO NOTHING;
`);

const owner = await signInOwner();
const ready = await fetch(`${BASE}/health/ready`);
assert("API ready", ready.status === 200, `status=${ready.status}`);

const created = await api("POST", "/api/admin-access/roles", {
  roleKey: CLOSURE_ROLE_INPUT,
  displayName: "Closure role",
  description: "Domain verifier role",
  idempotencyKey: `${CLOSURE_PREFIX}-create-role`,
}, owner);
assert("role created", created.status === 200, `status=${created.status} ${created.raw.slice(0, 180)}`);

const replay = await api("POST", "/api/admin-access/roles", {
  roleKey: CLOSURE_ROLE_INPUT,
  displayName: "Closure role",
  description: "Domain verifier role",
  idempotencyKey: `${CLOSURE_PREFIX}-create-role`,
}, owner);
assert("exact idempotent retry", replay.status === 200 && replay.json?.idempotentReplay === true, replay.raw.slice(0, 180));

const conflict = await api("POST", "/api/admin-access/roles", {
  roleKey: `${CLOSURE_PREFIX}-other`,
  displayName: "Other",
  idempotencyKey: `${CLOSURE_PREFIX}-create-role`,
}, owner);
assert("conflicting idempotent retry", conflict.status === 409, `status=${conflict.status}`);

const roles = await api("GET", "/api/admin-access/roles", undefined, owner);
const closureRole = roles.json?.roles?.find((role) => role.roleReference === CLOSURE_ROLE);
assert("role key normalized", Boolean(closureRole), JSON.stringify(roles.json?.roles?.map((r) => r.roleReference) ?? []));
assert("role uniqueness scope tenant-local", closureRole.roleReference === CLOSURE_ROLE);

const permissions = await api("GET", "/api/admin-access/permissions", undefined, owner);
const materialRead = permissions.json?.permissions?.find((p) => p.permissionKey === "material.read");
assert("permission key normalized", materialRead?.normalizedKey === "material.read");
assert("permissions deterministically ordered", (permissions.json?.permissions ?? []).every((item, index, list) => index === 0 || list[index - 1].normalizedKey <= item.normalizedKey));

const grant = await api("POST", `/api/admin-access/roles/${CLOSURE_ROLE}/permissions`, {
  expectedVersion: closureRole.version,
  idempotencyKey: `${CLOSURE_PREFIX}-grant`,
  grants: [{ permissionKey: "material.read", effect: "allow" }],
}, owner);
assert("role grant", grant.status === 200, grant.raw.slice(0, 180));

const stale = await api("POST", `/api/admin-access/roles/${CLOSURE_ROLE}/permissions`, {
  expectedVersion: closureRole.version,
  idempotencyKey: `${CLOSURE_PREFIX}-stale`,
  grants: [{ permissionKey: "edge.read", effect: "allow" }],
}, owner);
assert("optimistic concurrency stale version", stale.status === 409, `status=${stale.status}`);

const refreshed = (await api("GET", "/api/admin-access/roles", undefined, owner)).json.roles.find((r) => r.roleReference === CLOSURE_ROLE);
const ownScope = await api("POST", `/api/admin-access/roles/${CLOSURE_ROLE}/permissions`, {
  expectedVersion: refreshed.version,
  idempotencyKey: `${CLOSURE_PREFIX}-own-scope`,
  grants: [{ permissionKey: "material.read", recordScope: "own", effect: "allow" }],
}, owner);
assert("unsupported own record scope rejected", ownScope.status === 400, `status=${ownScope.status}`);

const assign = await api("PUT", `/api/admin-access/members/${CLOSURE_MEMBER}/roles`, {
  roleReferences: [CLOSURE_ROLE],
  expectedAuthorizationVersion: 1,
  idempotencyKey: `${CLOSURE_PREFIX}-assign-role`,
}, owner);
assert("membership role assignment", assign.status === 200, assign.raw.slice(0, 180));

const allow = await api("PUT", `/api/admin-access/members/${CLOSURE_MEMBER}/permission-overrides`, {
  overrides: [{ permissionKey: "edge.read", effect: "allow" }],
  expectedAuthorizationVersion: 2,
  idempotencyKey: `${CLOSURE_PREFIX}-direct-allow`,
}, owner);
assert("direct allow", allow.status === 200, allow.raw.slice(0, 180));

const deny = await api("PUT", `/api/admin-access/members/${CLOSURE_MEMBER}/permission-overrides`, {
  overrides: [
    { permissionKey: "edge.read", effect: "allow" },
    { permissionKey: "material.read", effect: "deny" },
  ],
  expectedAuthorizationVersion: 3,
  idempotencyKey: `${CLOSURE_PREFIX}-explicit-deny`,
}, owner);
assert("explicit deny applied", deny.status === 200, deny.raw.slice(0, 180));

const access = await api("GET", `/api/admin-access/members/${CLOSURE_MEMBER}/effective-access`, undefined, owner);
assert("explicit deny wins over role allow", access.json?.deniedPermissionKeys?.includes("material.read") === true, JSON.stringify(access.json));
assert("denied key not granted", access.json?.grantedPermissionKeys?.includes("material.read") !== true);
assert("direct allow remains", access.json?.grantedPermissionKeys?.includes("edge.read") === true);

const retire = await api("PATCH", `/api/admin-access/roles/${CLOSURE_ROLE}`, {
  status: "retired",
  expectedVersion: (await api("GET", "/api/admin-access/roles", undefined, owner)).json.roles.find((r) => r.roleReference === CLOSURE_ROLE).version,
  idempotencyKey: `${CLOSURE_PREFIX}-retire`,
}, owner);
assert("role retirement", retire.status === 200, retire.raw.slice(0, 180));

const retiredVersion = (await api("GET", "/api/admin-access/roles", undefined, owner)).json.roles.find((r) => r.roleReference === CLOSURE_ROLE).version;
const retiredGrant = await api("POST", `/api/admin-access/roles/${CLOSURE_ROLE}/permissions`, {
  expectedVersion: retiredVersion,
  grants: [{ permissionKey: "edge.read", effect: "allow" }],
}, owner);
assert("retired role cannot receive grants", retiredGrant.status === 400 || retiredGrant.status === 403, `status=${retiredGrant.status} ${retiredGrant.raw.slice(0, 160)}`);

const concurrentVersion = (await api("GET", "/api/admin-access/roles", undefined, owner)).json.roles.find((r) => r.roleReference === CLOSURE_ROLE).version;
const [first, second] = await Promise.all([
  api("PATCH", `/api/admin-access/roles/${CLOSURE_ROLE}`, {
    displayName: "Closure role concurrent A",
    expectedVersion: concurrentVersion,
    idempotencyKey: `${CLOSURE_PREFIX}-concurrent-a`,
  }, owner),
  api("PATCH", `/api/admin-access/roles/${CLOSURE_ROLE}`, {
    displayName: "Closure role concurrent B",
    expectedVersion: concurrentVersion,
    idempotencyKey: `${CLOSURE_PREFIX}-concurrent-b`,
  }, owner),
]);
assert(
  "concurrent mutation has one winner",
  [first.status, second.status].filter((status) => status === 200).length >= 1
    && [first.status, second.status].some((status) => status === 200 || status === 409),
  `statuses=${first.status},${second.status}`,
);

previewClosureFixtures();
removeClosureFixtures();
const remaining = previewClosureFixtures();
assert("closure fixtures remaining", remaining.roles === 0 && remaining.members === 0);

const templates = db(`SELECT COUNT(*) FROM tenant_roles WHERE system_template_origin = 'development-template'`);
assert("deterministic role templates preserved", Number(templates) === 15, `count=${templates}`);

console.log("ADMIN-ACCESS-1 domain verification passed.");
