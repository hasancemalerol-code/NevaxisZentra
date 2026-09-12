#!/usr/bin/env node
import { createHash, createHmac, randomBytes, randomUUID } from "node:crypto";
import { spawnSync } from "node:child_process";
import { readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), "../../..");
const BASE = process.env.NEVAXIS_FACTOS_API_BASE_URL ?? "http://127.0.0.1:5000";
const ORG = "dev-pilot-tenant";
const TENANT_ID = "bb000001-0001-4000-8000-000000000001";
const CLOSURE_PREFIX = "SAASIDENT1-CLOSURE";
const RUN_SUFFIX = Date.now();
const SESSION_COOKIE = "NevaxisFactOS.session";
const CSRF_COOKIE = "NevaxisFactOS.csrf";
const CSRF_HEADER = "X-NevaxisFactOS-Csrf";
const ACTIVE_EMAIL = "active-user@dev.local";
const TENANT_OWNER_EMAIL = "tenant-owner@dev.local";
function fail(message) {
  console.error(`SAAS-IDENTITY-1 session-security verification failed: ${message}`);
  process.exit(1);
}

function assert(label, ok, detail = "") {
  if (!ok) fail(`${label}${detail ? `: ${detail}` : ""}`);
  console.log(`✓ ${label}`);
}

function readRequired(relativePath) {
  return readFileSync(resolve(ROOT, relativePath), "utf8").replaceAll("\r\n", "\n");
}

function forbidText(source, forbidden, label) {
  if (source.includes(forbidden)) fail(`${label} must not contain "${forbidden}"`);
}

function requireEnv(name) {
  const value = process.env[name];
  if (!value) fail(`Missing required environment variable ${name}`);
  return value;
}

function sqlEscape(value) {
  return String(value).replace(/'/g, "''");
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

function hashToken(rawToken) {
  return createHash("sha256").update(rawToken, "utf8").digest("hex");
}

function generateOpaqueToken(byteLength = 32) {
  return randomBytes(byteLength)
    .toString("base64")
    .replace(/\+/g, "-")
    .replace(/\//g, "_")
    .replace(/=+$/g, "");
}

const BASE32_ALPHABET = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";

function base32Decode(encoded) {
  const normalized = encoded.trim().replace(/=+$/g, "").toUpperCase();
  const output = [];
  let buffer = 0;
  let bitsLeft = 0;
  for (const character of normalized) {
    const value = BASE32_ALPHABET.indexOf(character);
    if (value < 0) fail("Invalid Base32 character in MFA secret");
    buffer = (buffer << 5) | value;
    bitsLeft += 5;
    if (bitsLeft >= 8) {
      output.push((buffer >> (bitsLeft - 8)) & 0xff);
      bitsLeft -= 8;
    }
  }
  return Buffer.from(output);
}

function computeTotp(secret, counter) {
  const secretBytes = base32Decode(secret);
  const counterBuffer = Buffer.alloc(8);
  counterBuffer.writeBigInt64BE(BigInt(counter));
  const hash = createHmac("sha1", secretBytes).update(counterBuffer).digest();
  const offset = hash[hash.length - 1] & 0x0f;
  const binaryCode =
    ((hash[offset] & 0x7f) << 24)
    | ((hash[offset + 1] & 0xff) << 16)
    | ((hash[offset + 2] & 0xff) << 8)
    | (hash[offset + 3] & 0xff);
  const otp = binaryCode % 1_000_000;
  return otp.toString().padStart(6, "0");
}

function generateTotp(secret, utcNow = new Date()) {
  const counterBase = Math.floor(utcNow.getTime() / 1000 / 30);
  return computeTotp(secret, counterBase);
}

class CookieJar {
  constructor() {
    this.cookies = new Map();
  }

  absorb(response) {
    const setCookies = typeof response.headers.getSetCookie === "function"
      ? response.headers.getSetCookie()
      : [];
    for (const entry of setCookies) {
      const [pair] = entry.split(";");
      const separator = pair.indexOf("=");
      if (separator <= 0) continue;
      const name = pair.slice(0, separator).trim();
      const value = pair.slice(separator + 1).trim();
      if (value) this.cookies.set(name, value);
    }
  }

  get(name) {
    return this.cookies.get(name);
  }

  header() {
    return [...this.cookies.entries()].map(([name, value]) => `${name}=${value}`).join("; ");
  }

  clone() {
    const copy = new CookieJar();
    copy.cookies = new Map(this.cookies);
    return copy;
  }
}

async function identityRequest(method, path, jar, body, options = {}) {
  const { csrf = true, includeCsrf = csrf } = options;
  const headers = { "Content-Type": "application/json" };
  const cookieHeader = jar.header();
  if (cookieHeader) headers.Cookie = cookieHeader;
  if (includeCsrf && jar.get(CSRF_COOKIE)) {
    headers[CSRF_HEADER] = jar.get(CSRF_COOKIE);
  }

  const response = await fetch(`${BASE}${path}`, {
    method,
    headers,
    body: body === undefined ? undefined : JSON.stringify(body),
  });
  jar.absorb(response);

  const text = await response.text();
  let json = null;
  try {
    json = text ? JSON.parse(text) : null;
  } catch {
    json = { raw: text };
  }

  return { status: response.status, json, text };
}

async function signIn(organizationContext, email, password, jar, deviceLabel) {
  const body = { organizationContext, email, password };
  if (deviceLabel) body.deviceLabel = deviceLabel;
  return identityRequest("POST", "/api/identity/sign-in", jar, body, { csrf: false });
}

async function getMe(jar) {
  return identityRequest("GET", "/api/identity/me", jar, undefined, { csrf: false });
}

function sessionTokenHashFromJar(jar) {
  const token = jar.get(SESSION_COOKIE);
  if (!token) fail("Expected session cookie to be present");
  return hashToken(token);
}

function sessionTokenFromJar(jar) {
  const token = jar.get(SESSION_COOKIE);
  if (!token) fail("Expected session cookie to be present");
  return token;
}

function tokenHashForSessionReference(sessionReference) {
  return db(`
    SELECT token_hash
    FROM saas_identity_sessions
    WHERE session_reference = '${sqlEscape(sessionReference)}'
  `);
}

function assertRawTokenNotPersisted(rawToken, label) {
  const escaped = sqlEscape(rawToken);
  const hits = Number(db(`
    SELECT (
      (SELECT COUNT(*) FROM saas_identity_security_audit_events WHERE payload_json LIKE '%${escaped}%')
      + (SELECT COUNT(*) FROM saas_identity_sessions WHERE token_hash = '${escaped}')
      + (SELECT COUNT(*) FROM saas_identity_password_reset_tokens WHERE token_hash = '${escaped}')
      + (SELECT COUNT(*) FROM saas_identity_invitations WHERE token_hash = '${escaped}')
      + (SELECT COUNT(*) FROM saas_identity_email_verification_tokens WHERE token_hash = '${escaped}')
    )
  `));
  assert(`${label}: raw token not persisted in DB`, hits === 0, `hits=${hits}`);
}

function assertNoRawSecurityTokensInStores(label) {
  const forbiddenPatterns = [
    /"password"\s*:/i,
    /"token"\s*:\s*"(?!.*hash)/i,
    /"sharedSecret"/i,
    /"recoveryCode"/i,
    /otpauth:\/\//i,
  ];

  const auditPayloads = db(`
    SELECT payload_json
    FROM saas_identity_security_audit_events
    ORDER BY occurred_at_utc DESC
    LIMIT 200
  `);
  for (const pattern of forbiddenPatterns) {
    assert(`${label}: audit payloads exclude ${pattern}`, !pattern.test(auditPayloads));
  }

  const badHashes = Number(db(`
    SELECT COUNT(*) FROM (
      SELECT token_hash AS value FROM saas_identity_sessions
      UNION ALL
      SELECT token_hash FROM saas_identity_password_reset_tokens
      UNION ALL
      SELECT token_hash FROM saas_identity_invitations
      UNION ALL
      SELECT token_hash FROM saas_identity_email_verification_tokens
      UNION ALL
      SELECT code_hash FROM saas_identity_recovery_codes
    ) token_values
    WHERE value IS NULL OR value !~ '^[0-9a-f]{64}$'
  `));
  assert(`${label}: token fields store SHA-256 hashes only`, badHashes === 0, `bad=${badHashes}`);
}

function closureAccountPredicate(alias = "") {
  const prefix = alias ? `${alias}.` : "";
  return `${prefix}account_reference LIKE '${CLOSURE_PREFIX}%'`;
}

function closureInvitationPredicate(alias = "") {
  const prefix = alias ? `${alias}.` : "";
  return `${prefix}invitation_reference LIKE '${CLOSURE_PREFIX}%'`;
}

function previewClosureFixtures() {
  return db(`
    SELECT 'account' AS kind, account_reference AS ref
    FROM saas_identity_accounts
    WHERE ${closureAccountPredicate()}
    UNION ALL
    SELECT 'invitation', invitation_reference
    FROM saas_identity_invitations
    WHERE ${closureInvitationPredicate()}
    UNION ALL
    SELECT 'tenant', tenant_key
    FROM tenants
    WHERE tenant_key LIKE '${CLOSURE_PREFIX.toLowerCase()}%'
    ORDER BY kind, ref
  `);
}

function removeClosureFixtures() {
  const accountPredicate = closureAccountPredicate();
  const subjectIdRows = db(`
    SELECT identity_subject_id::text
    FROM saas_identity_accounts
    WHERE ${accountPredicate}
  `);
  const subjectIdList = subjectIdRows
    .split("\n")
    .map((value) => value.trim())
    .filter(Boolean)
    .map((value) => `'${sqlEscape(value)}'`)
    .join(", ");

  const accountIds = `SELECT id FROM saas_identity_accounts WHERE ${accountPredicate}`;
  const mfaMethodIds = `SELECT id FROM saas_identity_mfa_methods WHERE account_id IN (${accountIds})`;

  db(`
    DELETE FROM saas_identity_recovery_codes WHERE mfa_method_id IN (${mfaMethodIds});
    DELETE FROM saas_identity_sessions WHERE account_id IN (${accountIds});
    DELETE FROM saas_identity_password_reset_tokens WHERE account_id IN (${accountIds});
    DELETE FROM saas_identity_email_verification_tokens WHERE account_id IN (${accountIds});
    DELETE FROM saas_identity_mfa_methods WHERE account_id IN (${accountIds});
    DELETE FROM saas_identity_password_credentials WHERE account_id IN (${accountIds});
    DELETE FROM saas_identity_security_audit_events WHERE account_id IN (${accountIds});
    DELETE FROM saas_identity_invitations WHERE ${closureInvitationPredicate()};
    ${subjectIdList ? `DELETE FROM tenant_memberships WHERE identity_subject_id IN (${subjectIdList});` : ""}
    DELETE FROM saas_identity_accounts WHERE ${accountPredicate};
    ${subjectIdList ? `DELETE FROM identity_subjects WHERE id IN (${subjectIdList});` : ""}
    DELETE FROM tenants WHERE tenant_key LIKE '${CLOSURE_PREFIX.toLowerCase()}%';
  `);
}

function insertClosureAccount({ suffix, email, lifecycleStatus = "Active" }) {
  const accountId = randomUUID();
  const subjectId = randomUUID();
  const membershipId = randomUUID();
  const accountRef = `${CLOSURE_PREFIX}-${suffix}-${RUN_SUFFIX}`;
  const normalizedEmail = email.trim().toLowerCase();
  const now = new Date().toISOString();

  db(`
    INSERT INTO identity_subjects (id, external_issuer, external_subject, status, created_at_utc)
    VALUES ('${subjectId}', 'NevaxisFactOS-saas', '${accountId}', 'Active', '${now}');
    INSERT INTO saas_identity_accounts (
      id, account_reference, email_normalized, display_name, lifecycle_status,
      email_verified_at_utc, failed_sign_in_count, lockout_until_utc, security_stamp,
      identity_subject_id, created_at_utc, updated_at_utc, version
    ) VALUES (
      '${accountId}', '${sqlEscape(accountRef)}', '${sqlEscape(normalizedEmail)}',
      'Closure Session User', '${lifecycleStatus}', '${now}', 0, NULL, '${randomUUID()}',
      '${subjectId}', '${now}', '${now}', 1
    );
    INSERT INTO tenant_memberships (id, tenant_id, identity_subject_id, status, created_at_utc)
    VALUES ('${membershipId}', '${TENANT_ID}', '${subjectId}', 'Active', '${now}');
  `);

  return { accountId, subjectId, membershipId, accountRef, email: normalizedEmail };
}

function insertPasswordResetToken(accountId, rawToken) {
  const tokenId = randomUUID();
  const tokenHash = hashToken(rawToken);
  const expiresAt = new Date(Date.now() + 60 * 60 * 1000).toISOString();
  const createdAt = new Date().toISOString();
  db(`
    INSERT INTO saas_identity_password_reset_tokens (
      id, account_id, token_hash, expires_at_utc, created_at_utc
    ) VALUES (
      '${tokenId}', '${accountId}', '${tokenHash}', '${expiresAt}', '${createdAt}'
    )
  `);
  return rawToken;
}

function insertClosureInvitation({ suffix, email, rawToken }) {
  const invitationId = randomUUID();
  const invitationRef = `${CLOSURE_PREFIX}-INV-${suffix}-${RUN_SUFFIX}`;
  const tokenHash = hashToken(rawToken);
  const normalizedEmail = email.trim().toLowerCase();
  const expiresAt = new Date(Date.now() + 7 * 24 * 60 * 60 * 1000).toISOString();
  const createdAt = new Date().toISOString();

  db(`
    INSERT INTO saas_identity_invitations (
      id, invitation_reference, token_hash, tenant_id, intended_email_normalized,
      expires_at_utc, created_at_utc
    ) VALUES (
      '${invitationId}', '${sqlEscape(invitationRef)}', '${tokenHash}', '${TENANT_ID}',
      '${sqlEscape(normalizedEmail)}', '${expiresAt}', '${createdAt}'
    )
  `);

  return { invitationRef, rawToken };
}

async function setClosureAccountPassword(accountId, password) {
  const activePasswordHash = db(`
    SELECT password_hash
    FROM saas_identity_password_credentials
    WHERE account_id = 'bb000002-0011-4000-8000-000000000011'
  `);
  assert("development active-user password hash available for closure bootstrap", activePasswordHash.length > 0);

  const now = new Date().toISOString();
  db(`
    INSERT INTO saas_identity_password_credentials (
      id, account_id, password_hash, password_hash_version, created_at_utc, updated_at_utc
    ) VALUES (
      '${randomUUID()}', '${accountId}', '${sqlEscape(activePasswordHash)}', 1, '${now}', '${now}'
    )
    ON CONFLICT DO NOTHING;
  `);
}

async function createClosureAccountWithPassword(suffix) {
  const email = `${CLOSURE_PREFIX.toLowerCase()}-${suffix}-${RUN_SUFFIX}@closure.local`;
  const account = insertClosureAccount({ suffix, email });
  await setClosureAccountPassword(account.accountId, closurePassword);
  return account;
}

function insertClosureTenant(suffix) {
  const tenantId = randomUUID();
  const tenantKey = `${CLOSURE_PREFIX.toLowerCase()}-other-${suffix}-${RUN_SUFFIX}`;
  const createdAt = new Date().toISOString();
  db(`
    INSERT INTO tenants (id, tenant_key, display_name, status, created_at_utc)
    VALUES ('${tenantId}', '${sqlEscape(tenantKey)}', 'Closure Other Tenant', 'Active', '${createdAt}')
  `);
  return tenantKey;
}

const activeUserPassword = requireEnv("DevelopmentSaasIdentity__ActiveUserPassword");
const tenantOwnerPassword = requireEnv("DevelopmentSaasIdentity__TenantOwnerPassword");
const mfaSharedSecret = requireEnv("DevelopmentSaasIdentity__MfaSharedSecret");
const closurePassword = `ClosurePass1!${RUN_SUFFIX}`;

console.log("Running SAAS-IDENTITY-1 session-security verifier.");

const migration = readRequired(
  "packages/NevaxisFactOS.Persistence.Migrations/Migrations/20260814055239_AddSaasIdentityAccountAndSessionFoundation.cs",
);
assert("sessions table defines token_hash", migration.includes("token_hash = table.Column<string>"));
forbidText(migration, "session_token = table.Column", "sessions migration");
forbidText(migration, "raw_token = table.Column", "sessions migration");

const ready = await fetch(`${BASE}/health/ready`);
assert("API ready", ready.status === 200, `status=${ready.status}`);

removeClosureFixtures();

const sessionTokenColumns = db(`
  SELECT column_name
  FROM information_schema.columns
  WHERE table_schema = 'public'
    AND table_name = 'saas_identity_sessions'
    AND column_name ILIKE '%token%'
  ORDER BY column_name
`);
assert(
  "saas_identity_sessions exposes token_hash only",
  sessionTokenColumns === "token_hash",
  sessionTokenColumns,
);

const activeJar = new CookieJar();
const activeSignIn = await signIn(ORG, ACTIVE_EMAIL, activeUserPassword, activeJar, "session-security-active");
assert("active user sign-in → 200", activeSignIn.status === 200, `status=${activeSignIn.status}`);
assert("active user requiresMfa=false", activeSignIn.json?.requiresMfa === false);

const activeSessionToken = sessionTokenFromJar(activeJar);
const activeSessionReference = activeSignIn.json?.sessionReference;
assert("active sign-in returns sessionReference", Boolean(activeSessionReference));

const activeTokenHash = hashToken(activeSessionToken);
const persistedHash = tokenHashForSessionReference(activeSessionReference);
assert("session hash persisted in database", persistedHash === activeTokenHash, `${persistedHash} vs ${activeTokenHash}`);
assertRawTokenNotPersisted(activeSessionToken, "active sign-in");

const activeMe = await getMe(activeJar);
assert("active /me after authentication → 200", activeMe.status === 200, `status=${activeMe.status}`);
assert("active /me returns accountReference", Boolean(activeMe.json?.accountReference));

const signOutAccount = await createClosureAccountWithPassword("signout");
const signOutJar = new CookieJar();
const signOutSignIn = await signIn(ORG, signOutAccount.email, activeUserPassword, signOutJar, "signout-device");
assert("closure sign-out sign-in → 200", signOutSignIn.status === 200, `status=${signOutSignIn.status}`);

const signOut = await identityRequest("POST", "/api/identity/sign-out", signOutJar, {}, { csrf: true });
assert("sign-out → 200", signOut.status === 200, `status=${signOut.status}`);

const revokedAt = db(`
  SELECT revoked_at_utc IS NOT NULL
  FROM saas_identity_sessions
  WHERE session_reference = '${sqlEscape(signOutSignIn.json.sessionReference)}'
`);
assert("sign-out revokes current session", revokedAt === "t", revokedAt);

const revokedMe = await getMe(signOutJar);
assert("revoked session /me → 401", revokedMe.status === 401, `status=${revokedMe.status}`);

const signOutAllAccount = await createClosureAccountWithPassword("signout-all");
const jarA = new CookieJar();
const jarB = new CookieJar();
const signInA = await signIn(ORG, signOutAllAccount.email, activeUserPassword, jarA, "device-a");
const signInB = await signIn(ORG, signOutAllAccount.email, activeUserPassword, jarB, "device-b");
assert("sign-out-all session A → 200", signInA.status === 200, `status=${signInA.status}`);
assert("sign-out-all session B → 200", signInB.status === 200, `status=${signInB.status}`);

const signOutAll = await identityRequest("POST", "/api/identity/sign-out-all", jarA, {}, { csrf: true });
assert("sign-out-all → 200", signOutAll.status === 200, `status=${signOutAll.status}`);

const activeSessionsRemaining = Number(db(`
  SELECT COUNT(*)
  FROM saas_identity_sessions
  WHERE account_id = '${signOutAllAccount.accountId}'
    AND revoked_at_utc IS NULL
`));
assert("sign-out-all revokes all sessions", activeSessionsRemaining === 0, `remaining=${activeSessionsRemaining}`);
assert("sign-out-all session A /me → 401", (await getMe(jarA)).status === 401);
assert("sign-out-all session B /me → 401", (await getMe(jarB)).status === 401);

const stampAccount = await createClosureAccountWithPassword("stamp");
const stampJar = new CookieJar();
const stampSignIn = await signIn(ORG, stampAccount.email, activeUserPassword, stampJar, "stamp-device");
assert("security-stamp sign-in → 200", stampSignIn.status === 200, `status=${stampSignIn.status}`);
assert("security-stamp /me before change → 200", (await getMe(stampJar)).status === 200);

db(`
  UPDATE saas_identity_accounts
  SET security_stamp = '${randomUUID()}', updated_at_utc = NOW()
  WHERE id = '${stampAccount.accountId}'
`);
assert("security-stamp change invalidates session /me → 401", (await getMe(stampJar)).status === 401);

const deactivateAccount = await createClosureAccountWithPassword("deactivate");
const deactivateJar = new CookieJar();
const deactivateSignIn = await signIn(ORG, deactivateAccount.email, activeUserPassword, deactivateJar, "deactivate-device");
assert("deactivation sign-in → 200", deactivateSignIn.status === 200, `status=${deactivateSignIn.status}`);

db(`
  UPDATE saas_identity_accounts
  SET lifecycle_status = 'Deactivated', updated_at_utc = NOW()
  WHERE id = '${deactivateAccount.accountId}'
`);
assert("deactivation invalidates session /me → 401", (await getMe(deactivateJar)).status === 401);

const idleAccount = await createClosureAccountWithPassword("idle");
const idleJar = new CookieJar();
const idleSignIn = await signIn(ORG, idleAccount.email, activeUserPassword, idleJar, "idle-device");
assert("idle expiry sign-in → 200", idleSignIn.status === 200, `status=${idleSignIn.status}`);

db(`
  UPDATE saas_identity_sessions
  SET idle_expires_at_utc = NOW() - INTERVAL '1 minute'
  WHERE session_reference = '${sqlEscape(idleSignIn.json.sessionReference)}'
`);
assert("idle expiry /me → 401", (await getMe(idleJar)).status === 401);

const absoluteAccount = await createClosureAccountWithPassword("absolute");
const absoluteJar = new CookieJar();
const absoluteSignIn = await signIn(ORG, absoluteAccount.email, activeUserPassword, absoluteJar, "absolute-device");
assert("absolute expiry sign-in → 200", absoluteSignIn.status === 200, `status=${absoluteSignIn.status}`);

db(`
  UPDATE saas_identity_sessions
  SET absolute_expires_at_utc = NOW() - INTERVAL '1 minute'
  WHERE session_reference = '${sqlEscape(absoluteSignIn.json.sessionReference)}'
`);
assert("absolute expiry /me → 401", (await getMe(absoluteJar)).status === 401);

const adminJar = new CookieJar();
const adminSignIn = await signIn(ORG, TENANT_OWNER_EMAIL, tenantOwnerPassword, adminJar, "admin-mfa");
assert("admin sign-in → 200", adminSignIn.status === 200, `status=${adminSignIn.status}`);
assert("admin sign-in requiresMfa=true", adminSignIn.json?.requiresMfa === true);

const adminSessionReference = adminSignIn.json?.sessionReference;
const hashBeforeMfa = tokenHashForSessionReference(adminSessionReference);
const tokenBeforeMfa = sessionTokenFromJar(adminJar);

const mfaVerify = await identityRequest(
  "POST",
  "/api/identity/mfa/verify",
  adminJar,
  { totpCode: generateTotp(mfaSharedSecret) },
  { csrf: true },
);
assert("admin MFA verify → 200", mfaVerify.status === 200, `status=${mfaVerify.status}`);

const hashAfterMfa = tokenHashForSessionReference(adminSessionReference);
const tokenAfterMfa = sessionTokenFromJar(adminJar);
assert("MFA verify rotates session token hash", hashAfterMfa !== hashBeforeMfa, `${hashBeforeMfa} unchanged`);
assert("MFA verify issues new session cookie", tokenAfterMfa !== tokenBeforeMfa);
assertRawTokenNotPersisted(tokenAfterMfa, "admin MFA session");

const oldAdminJar = new CookieJar();
oldAdminJar.cookies.set(SESSION_COOKIE, tokenBeforeMfa);
assert("rotated old session /me → 401", (await getMe(oldAdminJar)).status === 401);
assert("rotated new session /me → 200", (await getMe(adminJar)).status === 200);

const csrfJar = new CookieJar();
const csrfSignIn = await signIn(ORG, ACTIVE_EMAIL, activeUserPassword, csrfJar, "csrf-device");
assert("csrf sign-in → 200", csrfSignIn.status === 200, `status=${csrfSignIn.status}`);

const csrfFailure = await identityRequest(
  "POST",
  "/api/identity/sign-out",
  csrfJar,
  {},
  { includeCsrf: false },
);
assert("sign-out without CSRF header → 403", csrfFailure.status === 403, `status=${csrfFailure.status}`);

const crossTenantKey = insertClosureTenant("cross");
const crossTenant = await signIn(crossTenantKey, ACTIVE_EMAIL, activeUserPassword, new CookieJar(), "cross-tenant");
assert("cross-tenant login denied → 401", crossTenant.status === 401, `status=${crossTenant.status}`);
assert(
  "cross-tenant login uses generic title",
  crossTenant.json?.title === "Invalid credentials.",
  crossTenant.json?.title ?? crossTenant.text,
);

const untrustedOrg = `${CLOSURE_PREFIX.toLowerCase()}-missing-${RUN_SUFFIX}`;
const untrustedTenant = await signIn(untrustedOrg, ACTIVE_EMAIL, activeUserPassword, new CookieJar(), "untrusted");
assert("untrusted tenant context denied → 401", untrustedTenant.status === 401, `status=${untrustedTenant.status}`);
assert(
  "untrusted tenant uses generic title",
  untrustedTenant.json?.title === "Invalid credentials.",
  untrustedTenant.json?.title ?? untrustedTenant.text,
);

const resetAccount = await createClosureAccountWithPassword("reset");
const rawResetToken = generateOpaqueToken();
insertPasswordResetToken(resetAccount.accountId, rawResetToken);
const newResetPassword = `ResetPass1!${RUN_SUFFIX}`;

const resetFirst = await identityRequest(
  "POST",
  "/api/identity/password/reset",
  new CookieJar(),
  { token: rawResetToken, newPassword: newResetPassword },
  { csrf: false },
);
assert("reset token first use → 200", resetFirst.status === 200, `status=${resetFirst.status}`);

const resetSecond = await identityRequest(
  "POST",
  "/api/identity/password/reset",
  new CookieJar(),
  { token: rawResetToken, newPassword: newResetPassword },
  { csrf: false },
);
assert("reset token second use → 400", resetSecond.status === 400, `status=${resetSecond.status}`);
assertRawTokenNotPersisted(rawResetToken, "password reset token");

const invitationEmail = `${CLOSURE_PREFIX.toLowerCase()}-invite-${RUN_SUFFIX}@closure.local`;
const rawInvitationToken = generateOpaqueToken();
insertClosureInvitation({ suffix: "invite", email: invitationEmail, rawToken: rawInvitationToken });
const invitationPassword = `InvitePass1!${RUN_SUFFIX}`;
const invitationBody = {
  token: rawInvitationToken,
  displayName: "Closure Invitee",
  password: invitationPassword,
};

const invitationFirst = await identityRequest(
  "POST",
  "/api/identity/invitations/accept",
  new CookieJar(),
  invitationBody,
  { csrf: false },
);
assert("invitation token first accept → 200", invitationFirst.status === 200, `status=${invitationFirst.status}`);

const invitationSecond = await identityRequest(
  "POST",
  "/api/identity/invitations/accept",
  new CookieJar(),
  invitationBody,
  { csrf: false },
);
assert("invitation token second accept → 400", invitationSecond.status === 400, `status=${invitationSecond.status}`);

const concurrentEmail = `${CLOSURE_PREFIX.toLowerCase()}-concurrent-${RUN_SUFFIX}@closure.local`;
const concurrentToken = generateOpaqueToken();
insertClosureInvitation({ suffix: "concurrent", email: concurrentEmail, rawToken: concurrentToken });
const concurrentBody = {
  token: concurrentToken,
  displayName: "Closure Concurrent Invitee",
  password: `Concurrent1!${RUN_SUFFIX}`,
};

const [concurrentA, concurrentB] = await Promise.all([
  identityRequest("POST", "/api/identity/invitations/accept", new CookieJar(), concurrentBody, { csrf: false }),
  identityRequest("POST", "/api/identity/invitations/accept", new CookieJar(), concurrentBody, { csrf: false }),
]);
const concurrentSuccesses = [concurrentA, concurrentB].filter((response) => response.status === 200).length;
assert("invitation concurrent accept has one winner", concurrentSuccesses === 1, `successes=${concurrentSuccesses}`);
assertRawTokenNotPersisted(rawInvitationToken, "invitation token");
assertRawTokenNotPersisted(concurrentToken, "concurrent invitation token");

assertNoRawSecurityTokensInStores("identity security stores");

console.log("Preview closure fixtures:");
console.log(previewClosureFixtures() || "(none)");
removeClosureFixtures();
const remainingClosureAccounts = Number(db(`SELECT COUNT(*) FROM saas_identity_accounts WHERE ${closureAccountPredicate()}`));
const remainingClosureInvitations = Number(db(`SELECT COUNT(*) FROM saas_identity_invitations WHERE ${closureInvitationPredicate()}`));
const remainingClosureTenants = Number(db(`SELECT COUNT(*) FROM tenants WHERE tenant_key LIKE '${CLOSURE_PREFIX.toLowerCase()}%'`));
assert(
  "closure fixtures remaining = 0",
  remainingClosureAccounts === 0 && remainingClosureInvitations === 0 && remainingClosureTenants === 0,
  `accounts=${remainingClosureAccounts}, invitations=${remainingClosureInvitations}, tenants=${remainingClosureTenants}`,
);

const devAccountsPreserved = Number(db(`SELECT COUNT(*) FROM saas_identity_accounts WHERE account_reference LIKE 'acc_dev_%'`));
assert("development acc_dev_* fixtures preserved", devAccountsPreserved >= 4, `count=${devAccountsPreserved}`);

console.log("SAAS-IDENTITY-1 session-security verification passed.");
