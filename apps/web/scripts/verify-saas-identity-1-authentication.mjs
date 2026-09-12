#!/usr/bin/env node
import { createHmac, createHash, randomUUID } from "node:crypto";
import { spawnSync } from "node:child_process";
import { readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), "../../..");
const BASE = process.env.NEVAXIS_FACTOS_API_BASE_URL ?? "http://127.0.0.1:5000";
const ORGANIZATION = "dev-pilot-tenant";
const CLOSURE_PREFIX = "SAASIDENT1-CLOSURE";
const TENANT_ID = "bb000001-0001-4000-8000-000000000001";
const DEV_TENANT_OWNER_MFA_METHOD_ID = "bb000002-0004-4000-8000-000000000004";
const DEV_ACTIVE_ACCOUNT_ID = "bb000002-0011-4000-8000-000000000011";
const MAX_FAILED_SIGN_IN_ATTEMPTS = 5;

const LOCKOUT_FIXTURE = {
  accountId: "cc000001-0001-4000-8000-000000000001",
  identitySubjectId: "cc000001-0002-4000-8000-000000000002",
  membershipId: "cc000001-0003-4000-8000-000000000003",
  credentialId: "cc000001-0004-4000-8000-000000000004",
  accountReference: `${CLOSURE_PREFIX}-lockout`,
  email: "saasident1-closure-lockout@dev.local",
};

const RECOVERY_FIXTURE = {
  accountId: "cc000001-0011-4000-8000-000000000011",
  identitySubjectId: "cc000001-0012-4000-8000-000000000012",
  membershipId: "cc000001-0013-4000-8000-000000000013",
  credentialId: "cc000001-0016-4000-8000-000000000016",
  mfaMethodId: "cc000001-0014-4000-8000-000000000014",
  recoveryCodeId: "cc000001-0015-4000-8000-000000000015",
  accountReference: `${CLOSURE_PREFIX}-recovery`,
  email: "saasident1-closure-recovery@dev.local",
  recoveryCode: "a1b2c3d4e5f67890",
};

const TOTP_ALPHABET = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
const TOTP_STEP_SECONDS = 30;
const TOTP_DIGITS = 6;

function fail(message) {
  console.error(`SAAS-IDENTITY-1 authentication verification failed: ${message}`);
  process.exit(1);
}

function assert(label, ok, detail = "") {
  if (!ok) fail(`${label}${detail ? `: ${detail}` : ""}`);
  console.log(`✓ ${label}`);
}

function readRequired(relativePath) {
  return readFileSync(resolve(ROOT, relativePath), "utf8").replaceAll("\r\n", "\n");
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

function requireEnv(name) {
  const value = process.env[name];
  if (!value) fail(`Missing required environment variable ${name}`);
  return value;
}

function hashToken(rawToken) {
  return createHash("sha256").update(rawToken, "utf8").digest("hex").toLowerCase();
}

function normalizeEmail(email) {
  return email.trim().toLowerCase();
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

function computeTotp(secret, counter) {
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

function generateTotp(secret, utcNow = new Date()) {
  const counterBase = Math.floor(utcNow.getTime() / 1000 / TOTP_STEP_SECONDS);
  return computeTotp(secret, counterBase);
}

function generateInvalidTotp(secret, utcNow = new Date()) {
  const valid = generateTotp(secret, utcNow);
  const invalid = String((Number(valid) + 1) % 10 ** TOTP_DIGITS).padStart(TOTP_DIGITS, "0");
  return invalid === valid ? "000000" : invalid;
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
      const name = pair.slice(0, separator).trim();
      const value = pair.slice(separator + 1).trim();
      this.cookies.set(name, value);
    }
  }

  get(name) {
    return this.cookies.get(name);
  }

  cookieHeader() {
    return [...this.cookies.entries()].map(([name, value]) => `${name}=${value}`).join("; ");
  }

  clear() {
    this.cookies.clear();
  }
}

async function identityRequest(method, path, body, jar = new CookieJar()) {
  const headers = { "Content-Type": "application/json" };
  const cookieHeader = jar.cookieHeader();
  if (cookieHeader) headers.Cookie = cookieHeader;

  if (
    jar.get("NevaxisFactOS.session")
    && jar.get("NevaxisFactOS.csrf")
    && method !== "GET"
    && !["/api/identity/sign-in", "/api/identity/password/forgot", "/api/identity/password/reset", "/api/identity/email/verify", "/api/identity/invitations/accept"].includes(path)
  ) {
    headers["X-NevaxisFactOS-Csrf"] = jar.get("NevaxisFactOS.csrf");
  }

  const response = await fetch(`${BASE}${path}`, {
    method,
    headers,
    body: body ? JSON.stringify(body) : undefined,
  });
  jar.ingest(response);

  const text = await response.text();
  let json = null;
  try {
    json = text ? JSON.parse(text) : null;
  } catch {
    json = { raw: text };
  }

  return { status: response.status, json, raw: text, jar };
}

function signInBody(email, password, organizationContext = ORGANIZATION) {
  return {
    organizationContext,
    email,
    password,
    deviceLabel: "saas-identity-1-verifier",
  };
}

function closureAccountIdsSubquery() {
  return `SELECT id FROM saas_identity_accounts WHERE account_reference LIKE '${CLOSURE_PREFIX}%'`;
}

function closureIdentitySubjectIdsSubquery() {
  return `
    SELECT identity_subject_id
    FROM saas_identity_accounts
    WHERE account_reference LIKE '${CLOSURE_PREFIX}%'
  `;
}

function previewClosureFixtures() {
  return db(`
    SELECT account_reference, email_normalized, lifecycle_status
    FROM saas_identity_accounts
    WHERE account_reference LIKE '${CLOSURE_PREFIX}%'
    ORDER BY account_reference
  `);
}

function removeClosureFixtures() {
  db(`
    DELETE FROM saas_identity_sessions
    WHERE account_id IN (${closureAccountIdsSubquery()});

    DELETE FROM saas_identity_recovery_codes
    WHERE mfa_method_id IN (
      SELECT id FROM saas_identity_mfa_methods
      WHERE account_id IN (${closureAccountIdsSubquery()})
    );

    DELETE FROM saas_identity_mfa_methods
    WHERE account_id IN (${closureAccountIdsSubquery()});

    DELETE FROM saas_identity_password_credentials
    WHERE account_id IN (${closureAccountIdsSubquery()});

    DELETE FROM saas_identity_password_reset_tokens
    WHERE account_id IN (${closureAccountIdsSubquery()});

    DELETE FROM saas_identity_invitations
    WHERE invitation_reference LIKE '${CLOSURE_PREFIX}%';

    DELETE FROM saas_identity_security_audit_events
    WHERE account_id IN (${closureAccountIdsSubquery()});

    DELETE FROM tenant_memberships
    WHERE identity_subject_id IN (${closureIdentitySubjectIdsSubquery()});

    DELETE FROM saas_identity_accounts
    WHERE account_reference LIKE '${CLOSURE_PREFIX}%';

    DELETE FROM identity_subjects
    WHERE id IN (${closureIdentitySubjectIdsSubquery()});
  `);
}

function insertClosureAccountFixture(fixture, displayName, lifecycleStatus, includeMembership) {
  const now = new Date().toISOString();
  const activePasswordHash = db(`
    SELECT password_hash
    FROM saas_identity_password_credentials
    WHERE account_id = '${DEV_ACTIVE_ACCOUNT_ID}'
  `);
  assert("development active-user password hash available", activePasswordHash.length > 0);

  db(`
    INSERT INTO identity_subjects (id, external_issuer, external_subject, status, created_at_utc)
    VALUES (
      '${fixture.identitySubjectId}',
      'NevaxisFactOS-saas',
      '${fixture.accountId}',
      'Active',
      '${now}'
    )
    ON CONFLICT (id) DO NOTHING;

    INSERT INTO saas_identity_accounts (
      id, account_reference, email_normalized, display_name, lifecycle_status,
      email_verified_at_utc, failed_sign_in_count, lockout_until_utc, security_stamp,
      identity_subject_id, created_at_utc, updated_at_utc, version
    ) VALUES (
      '${fixture.accountId}',
      '${fixture.accountReference}',
      '${normalizeEmail(fixture.email)}',
      '${displayName}',
      '${lifecycleStatus}',
      '${now}',
      0,
      NULL,
      '${randomUUID()}',
      '${fixture.identitySubjectId}',
      '${now}',
      '${now}',
      1
    )
    ON CONFLICT (id) DO UPDATE SET
      lifecycle_status = EXCLUDED.lifecycle_status,
      failed_sign_in_count = 0,
      lockout_until_utc = NULL,
      updated_at_utc = EXCLUDED.updated_at_utc;

    INSERT INTO saas_identity_password_credentials (
      id, account_id, password_hash, password_hash_version, created_at_utc, updated_at_utc
    ) VALUES (
      '${fixture.credentialId ?? randomUUID()}',
      '${fixture.accountId}',
      '${activePasswordHash.replaceAll("'", "''")}',
      1,
      '${now}',
      '${now}'
    )
    ON CONFLICT DO NOTHING;
  `);

  if (includeMembership) {
    db(`
      INSERT INTO tenant_memberships (id, tenant_id, identity_subject_id, status, created_at_utc)
      VALUES (
        '${fixture.membershipId}',
        '${TENANT_ID}',
        '${fixture.identitySubjectId}',
        'Active',
        '${now}'
      )
      ON CONFLICT (id) DO NOTHING;
    `);
  }
}

function insertRecoveryClosureFixture() {
  insertClosureAccountFixture(RECOVERY_FIXTURE, "SAASIDENT1 Closure Recovery MFA User", "Active", true);

  const encryptedSecret = db(`
    SELECT encrypted_secret
    FROM saas_identity_mfa_methods
    WHERE id = '${DEV_TENANT_OWNER_MFA_METHOD_ID}'
  `);
  assert("development tenant-owner MFA secret available", encryptedSecret.length > 0);

  const now = new Date().toISOString();
  db(`
    INSERT INTO saas_identity_mfa_methods (
      id, account_id, encrypted_secret, is_required, enrolled_at_utc
    ) VALUES (
      '${RECOVERY_FIXTURE.mfaMethodId}',
      '${RECOVERY_FIXTURE.accountId}',
      '${encryptedSecret.replaceAll("'", "''")}',
      TRUE,
      '${now}'
    )
    ON CONFLICT (id) DO UPDATE SET
      encrypted_secret = EXCLUDED.encrypted_secret,
      is_required = TRUE;

    INSERT INTO saas_identity_recovery_codes (
      id, mfa_method_id, code_hash, consumed_at_utc, created_at_utc
    ) VALUES (
      '${RECOVERY_FIXTURE.recoveryCodeId}',
      '${RECOVERY_FIXTURE.mfaMethodId}',
      '${hashToken(RECOVERY_FIXTURE.recoveryCode)}',
      NULL,
      '${now}'
    )
    ON CONFLICT (id) DO UPDATE SET
      code_hash = EXCLUDED.code_hash,
      consumed_at_utc = NULL;
  `);
}

function assertGenericInvalidCredentials(response) {
  assert(
    "generic invalid credentials response",
    response.status === 401 && response.json?.title === "Invalid credentials.",
    `status=${response.status}, title=${response.json?.title ?? response.raw.slice(0, 120)}`,
  );
}

function assertAuditPayloadsExcludeSecrets() {
  const payloads = db(`
    SELECT payload_json
    FROM saas_identity_security_audit_events
    ORDER BY occurred_at_utc DESC
    LIMIT 500
  `);

  if (!payloads) return;

  const forbiddenPatterns = [
    /"password"\s*:/i,
    /passwordHash/i,
    /\bsharedSecret\b/i,
    /\bmfaSecret\b/i,
    /\btotpSecret\b/i,
    /"token"\s*:/i,
    /"recoveryCode"\s*:/i,
  ];

  for (const row of payloads.split("\n").filter(Boolean)) {
    for (const pattern of forbiddenPatterns) {
      assert(`audit payload excludes ${pattern}`, !pattern.test(row), row.slice(0, 160));
    }
  }
}

console.log("Running SAAS-IDENTITY-1 authentication verifier.");

const constants = readRequired("packages/NevaxisFactOS.Persistence/SaasIdentity/SaasIdentityConstants.cs");
const migration = readRequired(
  "packages/NevaxisFactOS.Persistence.Migrations/Migrations/20260814055239_AddSaasIdentityAccountAndSessionFoundation.cs",
);

assert("MaxFailedSignInAttempts = 5", constants.includes("public const int MaxFailedSignInAttempts = 5;"));
assert("LockoutDuration = 15 minutes", constants.includes("TimeSpan.FromMinutes(15)"));
assert("password hash column present", migration.includes("password_hash = table.Column<string>"));
assert("plaintext password column absent", !migration.includes("password = table.Column<string>"));

const activeUserPassword = requireEnv("DevelopmentSaasIdentity__ActiveUserPassword");
requireEnv("DevelopmentSaasIdentity__TenantOwnerPassword");
requireEnv("DevelopmentSaasIdentity__DeactivatedUserPassword");
requireEnv("DevelopmentSaasIdentity__NoMembershipUserPassword");
const mfaSharedSecret = requireEnv("DevelopmentSaasIdentity__MfaSharedSecret");

const ready = await fetch(`${BASE}/health/ready`);
assert("API ready", ready.status === 200, `status=${ready.status}`);

removeClosureFixtures();
insertClosureAccountFixture(LOCKOUT_FIXTURE, "SAASIDENT1 Closure Lockout User", "Active", true);
insertRecoveryClosureFixture();

const passwordHashCount = Number(db(`
  SELECT COUNT(*)
  FROM saas_identity_password_credentials
  WHERE password_hash IS NOT NULL
    AND btrim(password_hash) <> ''
`));
assert("password hashes stored for credentials", passwordHashCount > 0, `count=${passwordHashCount}`);

const plaintextPasswordColumnCount = Number(db(`
  SELECT COUNT(*)
  FROM information_schema.columns
  WHERE table_schema = 'public'
    AND table_name = 'saas_identity_password_credentials'
    AND column_name = 'password'
`));
assert("plaintext password column absent", plaintextPasswordColumnCount === 0, `count=${plaintextPasswordColumnCount}`);

const validLogin = await identityRequest(
  "POST",
  "/api/identity/sign-in",
  signInBody("Active-User@Dev.Local", activeUserPassword),
);
assert("valid normalized active-user login → 200", validLogin.status === 200, `status=${validLogin.status}`);
assert("valid login does not require MFA", validLogin.json?.requiresMfa === false);
assert("session cookie issued", Boolean(validLogin.jar.get("NevaxisFactOS.session")));
assert("csrf cookie issued", Boolean(validLogin.jar.get("NevaxisFactOS.csrf")));

const validMe = await identityRequest("GET", "/api/identity/me", null, validLogin.jar);
assert("valid login /me → 200", validMe.status === 200, `status=${validMe.status}`);
assert("valid login /me mfaPending false", validMe.json?.mfaPending === false);

const invalidPassword = await identityRequest(
  "POST",
  "/api/identity/sign-in",
  signInBody("active-user@dev.local", "definitely-not-the-password"),
);
assertGenericInvalidCredentials(invalidPassword);

const unknownAccount = await identityRequest(
  "POST",
  "/api/identity/sign-in",
  signInBody("unknown-account@dev.local", activeUserPassword),
);
assertGenericInvalidCredentials(unknownAccount);
assert(
  "invalid password and unknown account expose same external response",
  invalidPassword.status === unknownAccount.status
    && invalidPassword.json?.title === unknownAccount.json?.title,
  `${invalidPassword.status}/${invalidPassword.json?.title} vs ${unknownAccount.status}/${unknownAccount.json?.title}`,
);

const deactivatedLogin = await identityRequest(
  "POST",
  "/api/identity/sign-in",
  signInBody("deactivated-user@dev.local", process.env.DevelopmentSaasIdentity__DeactivatedUserPassword),
);
assertGenericInvalidCredentials(deactivatedLogin);

const noMembershipLogin = await identityRequest(
  "POST",
  "/api/identity/sign-in",
  signInBody("no-membership@dev.local", process.env.DevelopmentSaasIdentity__NoMembershipUserPassword),
);
assertGenericInvalidCredentials(noMembershipLogin);

for (let attempt = 1; attempt <= MAX_FAILED_SIGN_IN_ATTEMPTS; attempt += 1) {
  const badAttempt = await identityRequest(
    "POST",
    "/api/identity/sign-in",
    signInBody(LOCKOUT_FIXTURE.email, "wrong-password-value"),
  );
  assertGenericInvalidCredentials(badAttempt);
}

const failedCount = Number(db(`
  SELECT failed_sign_in_count
  FROM saas_identity_accounts
  WHERE account_reference = '${LOCKOUT_FIXTURE.accountReference}'
`));
assert(
  "lockout fixture reached failed attempt threshold",
  failedCount >= MAX_FAILED_SIGN_IN_ATTEMPTS,
  `failedSignInCount=${failedCount}`,
);

const lockoutState = db(`
  SELECT lifecycle_status, lockout_until_utc IS NOT NULL
  FROM saas_identity_accounts
  WHERE account_reference = '${LOCKOUT_FIXTURE.accountReference}'
`).split("|");
assert("lockout fixture lifecycle Locked", lockoutState[0] === "Locked", lockoutState[0]);
assert("lockout fixture lockout_until_utc set", lockoutState[1] === "t", lockoutState[1]);

const lockedCorrectPassword = await identityRequest(
  "POST",
  "/api/identity/sign-in",
  signInBody(LOCKOUT_FIXTURE.email, activeUserPassword),
);
assertGenericInvalidCredentials(lockedCorrectPassword);

const tenantOwnerPassword = process.env.DevelopmentSaasIdentity__TenantOwnerPassword;
const pendingMfaLogin = await identityRequest(
  "POST",
  "/api/identity/sign-in",
  signInBody("admin@demo.nevaxisfactos.local", tenantOwnerPassword),
);
assert("tenant-owner login → 200", pendingMfaLogin.status === 200, `status=${pendingMfaLogin.status}`);
assert("tenant-owner login requires MFA", pendingMfaLogin.json?.requiresMfa === true);

const pendingMe = await identityRequest("GET", "/api/identity/me", null, pendingMfaLogin.jar);
assert("pending MFA /me → 403", pendingMe.status === 403, `status=${pendingMe.status}`);
assert(
  "pending MFA /me title",
  pendingMe.json?.title === "Multi-factor authentication is required.",
  pendingMe.json?.title ?? pendingMe.raw.slice(0, 120),
);
if (pendingMe.json?.mfaPending !== undefined) {
  assert("pending MFA /me mfaPending true", pendingMe.json.mfaPending === true);
}

const validTotp = generateTotp(mfaSharedSecret);
const validMfa = await identityRequest(
  "POST",
  "/api/identity/mfa/verify",
  { totpCode: validTotp },
  pendingMfaLogin.jar,
);
assert("valid TOTP verify → 200", validMfa.status === 200, `status=${validMfa.status}`);

const completedMe = await identityRequest("GET", "/api/identity/me", null, validMfa.jar);
assert("completed MFA /me → 200", completedMe.status === 200, `status=${completedMe.status}`);
assert("completed MFA /me mfaPending false", completedMe.json?.mfaPending === false);

const invalidTotpLogin = await identityRequest(
  "POST",
  "/api/identity/sign-in",
  signInBody("admin@demo.nevaxisfactos.local", tenantOwnerPassword),
);
assert("tenant-owner re-login for invalid TOTP → 200", invalidTotpLogin.status === 200);

const invalidTotp = generateInvalidTotp(mfaSharedSecret);
const invalidMfa = await identityRequest(
  "POST",
  "/api/identity/mfa/verify",
  { totpCode: invalidTotp },
  invalidTotpLogin.jar,
);
assert("invalid TOTP verify → 401", invalidMfa.status === 401, `status=${invalidMfa.status}`);
assert(
  "invalid TOTP title",
  invalidMfa.json?.title === "Multi-factor authentication failed.",
  invalidMfa.json?.title ?? invalidMfa.raw.slice(0, 120),
);

const recoveryLogin = await identityRequest(
  "POST",
  "/api/identity/sign-in",
  signInBody(RECOVERY_FIXTURE.email, activeUserPassword),
);
assert("recovery fixture login → 200", recoveryLogin.status === 200, `status=${recoveryLogin.status}`);
assert("recovery fixture login requires MFA", recoveryLogin.json?.requiresMfa === true);

const consumeRecovery = await identityRequest(
  "POST",
  "/api/identity/mfa/recovery/consume",
  { recoveryCode: RECOVERY_FIXTURE.recoveryCode },
  recoveryLogin.jar,
);
assert("recovery code first consume → 200", consumeRecovery.status === 200, `status=${consumeRecovery.status}`);

const recoveryMe = await identityRequest("GET", "/api/identity/me", null, consumeRecovery.jar);
assert("recovery code session /me → 200", recoveryMe.status === 200, `status=${recoveryMe.status}`);

const recoveryLoginAgain = await identityRequest(
  "POST",
  "/api/identity/sign-in",
  signInBody(RECOVERY_FIXTURE.email, activeUserPassword),
);
const reuseRecovery = await identityRequest(
  "POST",
  "/api/identity/mfa/recovery/consume",
  { recoveryCode: RECOVERY_FIXTURE.recoveryCode },
  recoveryLoginAgain.jar,
);
assert("recovery code second consume → 401", reuseRecovery.status === 401, `status=${reuseRecovery.status}`);
assert(
  "recovery code second consume title",
  reuseRecovery.json?.title === "Recovery code validation failed.",
  reuseRecovery.json?.title ?? reuseRecovery.raw.slice(0, 120),
);

assertAuditPayloadsExcludeSecrets();

console.log("Preview closure fixtures before cleanup:");
const closurePreviewCount = Number(db(`
  SELECT COUNT(*)
  FROM saas_identity_accounts
  WHERE account_reference LIKE '${CLOSURE_PREFIX}%'
`));
console.log(previewClosureFixtures() || "(none)");
console.log(`${CLOSURE_PREFIX} fixture account count=${closurePreviewCount}`);
removeClosureFixtures();

const remainingClosureAccounts = Number(db(`
  SELECT COUNT(*)
  FROM saas_identity_accounts
  WHERE account_reference LIKE '${CLOSURE_PREFIX}%'
`));
assert("closure fixtures remaining = 0", remainingClosureAccounts === 0, `count=${remainingClosureAccounts}`);

const devFixtureCount = Number(db(`
  SELECT COUNT(*)
  FROM saas_identity_accounts
  WHERE account_reference LIKE 'acc_dev_%'
`));
assert("development acc_dev_* fixtures preserved", devFixtureCount > 0, `count=${devFixtureCount}`);

console.log("SAAS-IDENTITY-1 authentication verification passed.");
