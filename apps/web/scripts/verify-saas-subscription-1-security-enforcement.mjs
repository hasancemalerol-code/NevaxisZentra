#!/usr/bin/env node
import { createHmac } from "node:crypto";
import { spawnSync } from "node:child_process";
import { readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), "../../..");
const BASE = process.env.NEVAXIS_FACTOS_API_BASE_URL ?? "http://127.0.0.1:5000";
const ORGANIZATION = "dev-pilot-tenant";
const CLOSURE_PREFIX = "SAAS-SUBSCRIPTION-1-CLOSURE";
const DEV_PLAN_VERSION_ID = "bb000001-00e2-4000-8000-0000000000e2";
const DEV_SUBSCRIPTION_ID = "bb000002-00e0-4000-8000-0000000000e0";
const TENANT_ID = "bb000001-0001-4000-8000-000000000001";
const SALES_MEMBER = "mem_bb000003021340008000000000000213";
const ADMIN_ACCESS_READ_PERMISSION_ID = "bb000003-0001-4000-8000-000000000001";
const PERIOD_START = "2026-08-01T00:00:00.000Z";
const PERIOD_END = "2026-09-01T00:00:00.000Z";
const TOTP_ALPHABET = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";

function fail(message) {
  console.error(`SAAS-SUBSCRIPTION-1 security-enforcement verification failed: ${message}`);
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

function readRequired(relativePath) {
  return readFileSync(resolve(ROOT, relativePath), "utf8").replaceAll("\r\n", "\n");
}

function forbidText(source, forbidden, label) {
  if (source.includes(forbidden)) fail(`${label} must not contain "${forbidden}"`);
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

function closureSubscriptionIdsSubquery() {
  return `SELECT id FROM saas_tenant_subscriptions WHERE tenant_id = '${TENANT_ID}' AND id <> '${DEV_SUBSCRIPTION_ID}'`;
}

function previewClosureFixtures() {
  const idempotency = Number(db(`SELECT COUNT(*) FROM saas_subscription_idempotency_records WHERE idempotency_key LIKE '${CLOSURE_PREFIX}%'`));
  const subs = Number(db(`SELECT COUNT(*) FROM saas_tenant_subscriptions WHERE id IN (${closureSubscriptionIdsSubquery()})`));
  console.log(`Preview ${CLOSURE_PREFIX} security fixtures: subs=${subs}, idempotency=${idempotency}`);
  return { subs, idempotency };
}

function restoreDevSubscriptionCurrent() {
  db(`
    UPDATE saas_tenant_subscriptions
    SET is_current = false, updated_at_utc = NOW()
    WHERE tenant_id = '${TENANT_ID}' AND id <> '${DEV_SUBSCRIPTION_ID}' AND is_current = true;

    UPDATE saas_tenant_subscriptions
    SET is_current = true,
        subscription_status = 'active',
        commercial_plan_version_id = '${DEV_PLAN_VERSION_ID}',
        billing_cadence = 'monthly',
        grace_until_utc = NULL,
        suspended_at_utc = NULL,
        cancelled_at_utc = NULL,
        cancel_at_period_end = false,
        updated_at_utc = NOW()
    WHERE id = '${DEV_SUBSCRIPTION_ID}';
  `);
}

function removeClosureFixtures() {
  const closureSubs = closureSubscriptionIdsSubquery();
  db(`
    DELETE FROM saas_subscription_audit_events
    WHERE tenant_id = '${TENANT_ID}'
      AND entity_reference IN (SELECT id::text FROM saas_tenant_subscriptions WHERE tenant_id = '${TENANT_ID}' AND id <> '${DEV_SUBSCRIPTION_ID}');

    DELETE FROM saas_subscription_idempotency_records
    WHERE idempotency_key LIKE '${CLOSURE_PREFIX}%';

    DELETE FROM admin_access_idempotency_records
    WHERE idempotency_key LIKE '${CLOSURE_PREFIX}%';

    DELETE FROM saas_tenant_subscription_transition_history
    WHERE subscription_id IN (${closureSubs})
       OR idempotency_key LIKE '${CLOSURE_PREFIX}%';

    DELETE FROM saas_tenant_subscriptions
    WHERE tenant_id = '${TENANT_ID}' AND id <> '${DEV_SUBSCRIPTION_ID}';

    DELETE FROM tenant_membership_permission_overrides
    WHERE tenant_membership_id IN (SELECT id FROM tenant_memberships WHERE membership_reference = '${SALES_MEMBER}')
      AND permission_definition_id IN ('${ADMIN_ACCESS_READ_PERMISSION_ID}', '10000000-0000-4000-8000-000000000072');
  `);
  restoreDevSubscriptionCurrent();
}

function assertDevSubscriptionPreserved() {
  const count = Number(db(`SELECT COUNT(*) FROM saas_tenant_subscriptions WHERE id = '${DEV_SUBSCRIPTION_ID}'`));
  assert("dev subscription row preserved", count === 1, `count=${count}`);
  const current = db(`SELECT is_current FROM saas_tenant_subscriptions WHERE id = '${DEV_SUBSCRIPTION_ID}'`);
  assert("dev subscription is current", current === "t", current);
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

async function transitionDevSubscription(jar, targetStatus, idempotencyKey, extras = {}) {
  const current = await api("GET", "/api/saas-subscription/current", undefined, jar);
  const subscription = current.json?.subscription;
  return api("POST", "/api/saas-subscription/transitions", {
    subscriptionId: subscription?.subscriptionId ?? DEV_SUBSCRIPTION_ID,
    targetStatus,
    transitionReason: extras.transitionReason ?? "manual-suspension",
    expectedVersion: subscription?.version ?? 1,
    cancelAtPeriodEnd: extras.cancelAtPeriodEnd ?? null,
    graceUntilUtc: extras.graceUntilUtc ?? null,
    idempotencyKey,
  }, jar);
}

console.log("Running SAAS-SUBSCRIPTION-1 security-enforcement verifier.");

const endpoints = readRequired("apps/api/SaasSubscription/SaasSubscriptionEndpointRouteBuilderExtensions.cs");
const helpers = readRequired("apps/api/SaasSubscription/SaasSubscriptionEndpointHelpers.cs");
const middleware = readRequired("apps/api/SaasSubscription/TenantSubscriptionAccessMiddleware.cs");
const migration = readRequired(
  "packages/NevaxisFactOS.Persistence.Migrations/Migrations/20260814220344_AddSaasTenantSubscriptionEntitlementEnforcement.cs",
);

assert("endpoints require auth", endpoints.includes("RequireAuthorization"));
assert("read access helper", endpoints.includes("ResolveReadAccessAsync"));
assert("manage access helper", endpoints.includes("ResolveManageAccessAsync"));
assert("helpers return 401 without identity", helpers.includes("TypedResults.Unauthorized()"));
assert("middleware paid access gate", middleware.includes("EvaluatePaidApplicationAccessAsync"));
assert("middleware module entitlement gate", middleware.includes("EvaluateModuleAccessAsync"));
assert("shift workforce recovery exempt", middleware.includes("/api/saas-subscription"));
assert("audit append-only grant", migration.includes("GRANT SELECT, INSERT ON TABLE saas_subscription_audit_events"));
assert("transition history append-only grant", migration.includes("GRANT SELECT, INSERT ON TABLE saas_tenant_subscription_transition_history"));
forbidText(migration, "GRANT DELETE ON TABLE saas_subscription_audit_events", "audit events migration");
forbidText(migration, "GRANT DELETE ON TABLE saas_tenant_subscriptions", "subscriptions migration");
forbidText(migration, "GRANT DELETE ON TABLE saas_tenant_subscription_transition_history", "transition history migration");
forbidText(migration, "GRANT DELETE ON TABLE saas_subscription_idempotency_records", "idempotency migration");

const rlsTables = [
  "saas_tenant_subscriptions",
  "saas_tenant_subscription_transition_history",
  "saas_subscription_idempotency_records",
  "saas_subscription_audit_events",
];
for (const table of rlsTables) {
  const forced = Number(db(`
    SELECT COUNT(*) FROM pg_class c
    JOIN pg_namespace n ON n.oid = c.relnamespace
    WHERE n.nspname = 'public' AND c.relname = '${table}' AND c.relrowsecurity = true AND c.relforcerowsecurity = true
  `));
  assert(`${table} FORCE RLS enabled`, forced === 1, `count=${forced}`);
  const policies = Number(db(`
    SELECT COUNT(*) FROM pg_policies WHERE schemaname = 'public' AND tablename = '${table}'
  `));
  assert(`${table} tenant policies present`, policies >= 2, `count=${policies}`);
}

previewClosureFixtures();
removeClosureFixtures();
assertDevSubscriptionPreserved();

const ready = await fetch(`${BASE}/health/ready`);
assert("API ready", ready.status === 200, `status=${ready.status}`);

const missing = await api("GET", "/api/saas-subscription/current");
assert("missing auth → 401", missing.status === 401, `status=${missing.status}`);

const salesPassword = requireEnv("DevelopmentSaasIdentity__ActiveUserPassword");
const restricted = await signIn("machine-responsible@dev.local", machinePassword());
const restrictedDenied = await api("GET", "/api/saas-subscription/current", undefined, restricted.jar);
assert("unauthorized subscription read → 403", restrictedDenied.status === 403, `status=${restrictedDenied.status}`);

const sales = await signIn("materials-sales@dev.local", salesPassword);

const owner = await signIn(
  "tenant-owner@dev.local",
  requireEnv("DevelopmentSaasIdentity__TenantOwnerPassword"),
  requireEnv("DevelopmentSaasIdentity__MfaSharedSecret"),
);

const salesAccess = await api("GET", `/api/admin-access/members/${SALES_MEMBER}/effective-access`, undefined, owner.jar);
const salesVersion = Number(salesAccess.json?.authorizationVersion ?? 1);
const grantRead = await api("PUT", `/api/admin-access/members/${SALES_MEMBER}/permission-overrides`, {
  overrides: [{ permissionKey: "admin-access.read", effect: "allow" }],
  expectedAuthorizationVersion: salesVersion,
  idempotencyKey: `${CLOSURE_PREFIX}-grant-read`,
}, owner.jar);
assert("grant admin-access.read override for subscription read probe", grantRead.status === 200, grantRead.raw.slice(0, 180));

const salesRead = await signIn("materials-sales@dev.local", salesPassword, undefined, `${CLOSURE_PREFIX}-sales-read`);
const salesCurrent = await api("GET", "/api/saas-subscription/current", undefined, salesRead.jar);
assert("read-only user can read current", salesCurrent.status === 200, `status=${salesCurrent.status} ${salesCurrent.raw.slice(0, 200)}`);

const idempotencyBefore = Number(db(`SELECT COUNT(*) FROM saas_subscription_idempotency_records WHERE idempotency_key LIKE '${CLOSURE_PREFIX}%'`));
const readOnlyMutation = await api("POST", "/api/saas-subscription/transitions", {
  subscriptionId: DEV_SUBSCRIPTION_ID,
  targetStatus: "suspended",
  transitionReason: "manual-suspension",
  expectedVersion: 1,
  idempotencyKey: `${CLOSURE_PREFIX}-read-only-mutation`,
}, salesRead.jar);
assert("read-only mutation → 403", readOnlyMutation.status === 403, `status=${readOnlyMutation.status}`);
const idempotencyAfterDenied = Number(db(`SELECT COUNT(*) FROM saas_subscription_idempotency_records WHERE idempotency_key LIKE '${CLOSURE_PREFIX}%'`));
assert("denied mutation created no idempotency record", idempotencyAfterDenied === idempotencyBefore, `${idempotencyBefore}→${idempotencyAfterDenied}`);

const untrustedTenant = await api(
  "GET",
  "/api/saas-subscription/current",
  undefined,
  owner.jar,
  { "X-NevaxisFactOS-Tenant": "other-tenant-not-pilot" },
);
assert("untrusted tenant header rejected", untrustedTenant.status === 403, `status=${untrustedTenant.status}`);

const suspend = await transitionDevSubscription(owner.jar, "suspended", `${CLOSURE_PREFIX}-suspend`);
assert("subscription suspended for middleware probe", suspend.status === 201 || suspend.status === 200, suspend.raw.slice(0, 180));

const suspendedMaterials = await api("GET", "/api/materials/workspace-summary", undefined, sales.jar);
assert(
  "middleware blocks suspended subscription",
  suspendedMaterials.status === 403,
  `status=${suspendedMaterials.status} ${suspendedMaterials.raw.slice(0, 180)}`,
);
assert(
  "suspended middleware reason paid-application-denied",
  suspendedMaterials.json?.failureCode?.includes("paid-application-denied"),
  suspendedMaterials.json?.failureCode,
);

const reactivateFromSuspended = await transitionDevSubscription(owner.jar, "active", `${CLOSURE_PREFIX}-reactivate-suspended`, {
  transitionReason: "manual-reactivation",
});
assert("suspended → active for grace probe", reactivateFromSuspended.status === 201 || reactivateFromSuspended.status === 200, reactivateFromSuspended.raw.slice(0, 180));

const graceUntil = new Date(Date.now() + 5 * 24 * 60 * 60 * 1000).toISOString();
const toGrace = await transitionDevSubscription(owner.jar, "grace-period", `${CLOSURE_PREFIX}-grace`, {
  transitionReason: "grace-entered",
  graceUntilUtc: graceUntil,
});
assert("subscription moved to grace-period", toGrace.status === 201 || toGrace.status === 200, toGrace.raw.slice(0, 180));

const graceMaterials = await api("GET", "/api/saas-subscription/current", undefined, owner.jar);
assert("grace-period retains paid application access", graceMaterials.status === 200, `status=${graceMaterials.status}`);
assert("grace-period paid access allowed", graceMaterials.json?.paidApplicationAccess?.allowed === true);

const machine = await signIn("machine-responsible@dev.local", salesPassword, undefined, `${CLOSURE_PREFIX}-shift`);
const shiftDenied = await api("GET", "/api/shift-workforce/overview", undefined, machine.jar);
assert(
  "permission+entitlement intersection blocks vardiyalar API",
  shiftDenied.status === 403,
  `status=${shiftDenied.status} ${shiftDenied.raw.slice(0, 180)}`,
);
assert(
  "vardiyalar denial entitlement unavailable",
  shiftDenied.json?.failureCode?.includes("unavailable") || shiftDenied.json?.entitlementCode === "module.shift-workforce",
  JSON.stringify(shiftDenied.json),
);

const auditBefore = Number(db(`SELECT COUNT(*) FROM saas_subscription_audit_events WHERE tenant_id = '${TENANT_ID}'`));
const deniedTransition = await api("POST", "/api/saas-subscription/transitions", {
  subscriptionId: DEV_SUBSCRIPTION_ID,
  targetStatus: "expired",
  transitionReason: "subscription-expired",
  expectedVersion: 1,
  idempotencyKey: `${CLOSURE_PREFIX}-denied-transition`,
}, salesRead.jar);
assert("denied transition mutation blocked", deniedTransition.status === 403, `status=${deniedTransition.status}`);
const auditAfterDenied = Number(db(`SELECT COUNT(*) FROM saas_subscription_audit_events WHERE tenant_id = '${TENANT_ID}'`));
assert("denied mutation created no audit growth", auditAfterDenied === auditBefore, `${auditBefore}→${auditAfterDenied}`);

const toActive = await transitionDevSubscription(owner.jar, "active", `${CLOSURE_PREFIX}-reactivate`, {
  transitionReason: "payment-received",
});
assert("subscription reactivated after probes", toActive.status === 201 || toActive.status === 200, toActive.raw.slice(0, 180));

const auditPayloads = db(`
  SELECT payload_json FROM saas_subscription_audit_events
  WHERE tenant_id = '${TENANT_ID}'
  ORDER BY occurred_at_utc DESC
  LIMIT 20
`);
const auditText = auditPayloads.toLowerCase();
assert("audit secrecy no password", !auditText.includes("password"));
assert("audit secrecy no totp", !auditText.includes("totp"));
assert("audit secrecy no sessiontoken", !auditText.includes("sessiontoken"));

previewClosureFixtures();
removeClosureFixtures();
const remaining = previewClosureFixtures();
assert("closure fixtures remaining", remaining.subs === 0 && remaining.idempotency === 0);
assertDevSubscriptionPreserved();

void ROOT;
console.log("SAAS-SUBSCRIPTION-1 security-enforcement verification passed.");
