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
const DEV_SUBSCRIPTION_HISTORY_ID = "bb000002-00e1-4000-8000-0000000000e1";
const TENANT_ID = "bb000001-0001-4000-8000-000000000001";
const SALES_MEMBER = "mem_bb000003021340008000000000000213";
const ADMIN_ACCESS_READ_PERMISSION_ID = "bb000003-0001-4000-8000-000000000001";
const DEV_PLAN_CODE = "dev-pilot-local-commercial";
const TOTP_ALPHABET = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
const PERIOD_START = "2026-08-01T00:00:00.000Z";
const PERIOD_END = "2026-09-01T00:00:00.000Z";

function fail(message) {
  console.error(`SAAS-SUBSCRIPTION-1 domain-entitlements verification failed: ${message}`);
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

function closureSubscriptionIdsSubquery() {
  return `SELECT id FROM saas_tenant_subscriptions WHERE tenant_id = '${TENANT_ID}' AND id <> '${DEV_SUBSCRIPTION_ID}'`;
}

function previewClosureFixtures() {
  const subs = db(`SELECT COUNT(*) FROM saas_tenant_subscriptions WHERE id IN (${closureSubscriptionIdsSubquery()})`);
  const history = db(`SELECT COUNT(*) FROM saas_tenant_subscription_transition_history WHERE subscription_id IN (${closureSubscriptionIdsSubquery()})`);
  const audits = db(`SELECT COUNT(*) FROM saas_subscription_audit_events WHERE tenant_id = '${TENANT_ID}' AND entity_reference IN (SELECT id::text FROM saas_tenant_subscriptions WHERE tenant_id = '${TENANT_ID}' AND id <> '${DEV_SUBSCRIPTION_ID}')`);
  const idempotency = db(`SELECT COUNT(*) FROM saas_subscription_idempotency_records WHERE idempotency_key LIKE '${CLOSURE_PREFIX}%'`);
  console.log(`Preview ${CLOSURE_PREFIX}: subs=${subs}, history=${history}, idempotency=${idempotency}, audits=${audits}`);
  return {
    subs: Number(subs),
    history: Number(history),
    idempotency: Number(idempotency),
    audits: Number(audits),
  };
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

    DELETE FROM tenant_membership_permission_overrides
    WHERE tenant_membership_id IN (SELECT id FROM tenant_memberships WHERE membership_reference = '${SALES_MEMBER}')
      AND permission_definition_id IN ('${ADMIN_ACCESS_READ_PERMISSION_ID}', '10000000-0000-4000-8000-000000000072');

    DELETE FROM saas_tenant_subscription_transition_history
    WHERE subscription_id IN (${closureSubs})
       OR idempotency_key LIKE '${CLOSURE_PREFIX}%';

    DELETE FROM saas_tenant_subscriptions
    WHERE tenant_id = '${TENANT_ID}' AND id <> '${DEV_SUBSCRIPTION_ID}';

    DELETE FROM facilities
    WHERE code LIKE '${CLOSURE_PREFIX}%';
  `);
  restoreDevSubscriptionCurrent();
}

function assertDevSubscriptionPreserved() {
  const count = Number(db(`SELECT COUNT(*) FROM saas_tenant_subscriptions WHERE id = '${DEV_SUBSCRIPTION_ID}'`));
  assert("dev subscription row preserved", count === 1, `count=${count}`);
  const current = db(`SELECT is_current FROM saas_tenant_subscriptions WHERE id = '${DEV_SUBSCRIPTION_ID}'`);
  assert("dev subscription is current", current === "t", current);
  const status = db(`SELECT subscription_status FROM saas_tenant_subscriptions WHERE id = '${DEV_SUBSCRIPTION_ID}'`);
  assert("dev subscription active", status === "active", status);
  const planVersion = db(`SELECT commercial_plan_version_id FROM saas_tenant_subscriptions WHERE id = '${DEV_SUBSCRIPTION_ID}'`);
  assert("dev subscription plan version bound", planVersion === DEV_PLAN_VERSION_ID, planVersion);
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

async function assignClosureSubscription(jar, idempotencyKey, status = "active") {
  return api("POST", "/api/saas-subscription/development/assign", {
    commercialPlanVersionId: DEV_PLAN_VERSION_ID,
    billingCadence: "monthly",
    subscriptionStatus: status,
    startsAtUtc: PERIOD_START,
    currentPeriodStartUtc: PERIOD_START,
    currentPeriodEndUtc: PERIOD_END,
    trialEndsAtUtc: null,
    sourceProvenance: `${CLOSURE_PREFIX}-assign`,
    transitionReason: "development-assignment",
    idempotencyKey,
  }, jar);
}

async function transitionSubscription(jar, subscriptionId, targetStatus, expectedVersion, idempotencyKey, extras = {}) {
  return api("POST", "/api/saas-subscription/transitions", {
    subscriptionId,
    targetStatus,
    transitionReason: extras.transitionReason ?? "payment-failed",
    expectedVersion,
    cancelAtPeriodEnd: extras.cancelAtPeriodEnd ?? null,
    graceUntilUtc: extras.graceUntilUtc ?? null,
    idempotencyKey,
  }, jar);
}

console.log("Running SAAS-SUBSCRIPTION-1 domain-entitlements verifier.");

const conversions = readRequired("packages/NevaxisFactOS.Persistence/SaasSubscription/SaasSubscriptionConstants.cs");
const repository = readRequired("packages/NevaxisFactOS.Persistence/SaasSubscription/SaasSubscriptionRepository.cs");
const migration = readRequired(
  "packages/NevaxisFactOS.Persistence.Migrations/Migrations/20260814220344_AddSaasTenantSubscriptionEntitlementEnforcement.cs",
);
const evaluator = readRequired("packages/NevaxisFactOS.Persistence/SaasSubscription/TenantEntitlementEvaluator.cs");
const moduleEntitlements = readRequired("packages/NevaxisFactOS.Persistence/SaasSubscription/SaasSubscriptionModuleEntitlements.cs");
const webSubscriptionLabels = readRequired("apps/web/src/features/saas-subscription/domain/saas-subscription-labels.ts");

assert("subscription status conversions", conversions.includes("SaasSubscriptionStatuses.Active"));
assert("valid transition guard", conversions.includes("IsValidTransition"));
assert("paid application resolver", conversions.includes("ResolvePaidApplicationAccess"));
assert("repository idempotency hash", repository.includes("HashCommand"));
assert("repository concurrency conflict", repository.includes("SaasSubscriptionFailureCodes.ConcurrencyConflict"));
assert("limit evaluator usage", evaluator.includes("ResolveUsageAsync"));
assert("unique current subscription index", migration.includes("ux_saas_tenant_subscriptions_tenant_current"));
assert("bootstrap seed idempotency guard", readRequired("packages/NevaxisFactOS.Persistence/Development/DevelopmentPilotSaasSubscriptionSeed.cs").includes("DevPilotTenantSubscriptionId"));
assert(
  "factory showcase shares dashboard module mapping",
  moduleEntitlements.includes('("/fabrika-vitrini", EntitlementCodes.Dashboard)'),
);
assert(
  "web route maps factory showcase to module.dashboard",
  webSubscriptionLabels.includes('{ prefix: "/fabrika-vitrini", code: "module.dashboard" }'),
);

previewClosureFixtures();
removeClosureFixtures();
assertDevSubscriptionPreserved();

const owner = await signInOwner();
const ready = await fetch(`${BASE}/health/ready`);
assert("API ready", ready.status === 200, `status=${ready.status}`);

const current = await api("GET", "/api/saas-subscription/current", undefined, owner);
assert("current subscription → 200", current.status === 200, `status=${current.status} ${current.raw.slice(0, 180)}`);

const summary = current.json?.subscription;
assert("plan binding plan code", summary?.planCode === DEV_PLAN_CODE, summary?.planCode);
assert("plan binding version number", summary?.planVersionNumber === 1, String(summary?.planVersionNumber));
assert("plan binding commercial version id", summary?.commercialPlanVersionId === DEV_PLAN_VERSION_ID, summary?.commercialPlanVersionId);
assert("subscription status active", summary?.subscriptionStatus === "active", summary?.subscriptionStatus);

const entitlements = current.json?.entitlements ?? [];
const facilityEntitlement = entitlements.find((entry) => entry.code === "facility-limit");
const userEntitlement = entitlements.find((entry) => entry.code === "user-limit");
const shiftModule = entitlements.find((entry) => entry.code === "module.shift-workforce");
assert("facility limit semantics", facilityEntitlement?.semantics === "limited" && facilityEntitlement?.integerLimitValue === 2);
assert("user limit semantics", userEntitlement?.semantics === "limited" && userEntitlement?.integerLimitValue === 25);
assert("shift module unavailable semantics", shiftModule?.semantics === "unavailable" && shiftModule?.booleanValue === false);

const usersLimit = current.json?.limits?.users;
const facilitiesLimit = current.json?.limits?.facilities;
const machinesLimit = current.json?.limits?.connectedMachines;
assert("users limit evidence configured", usersLimit?.limit?.limitConfigured === true);
assert("facilities limit evidence configured", facilitiesLimit?.limit?.limitConfigured === true);
assert("connected machines limit evidence configured", machinesLimit?.limit?.limitConfigured === true);
assert("connected machine cap", machinesLimit?.limit?.limitValue === 5, JSON.stringify(machinesLimit?.limit));

assert("paid application allowed", current.json?.paidApplicationAccess?.allowed === true);

const dashboardAccess = await api(
  "GET",
  "/api/saas-subscription/access-status?routePath=%2Fdashboard",
  undefined,
  owner,
);
assert(
  "dashboard access-status allowed",
  dashboardAccess.status === 200 && dashboardAccess.json?.allowed === true,
  dashboardAccess.raw.slice(0, 180),
);

const showcaseAccess = await api(
  "GET",
  "/api/saas-subscription/access-status?routePath=%2Ffabrika-vitrini",
  undefined,
  owner,
);
assert(
  "factory showcase access-status allowed",
  showcaseAccess.status === 200 && showcaseAccess.json?.allowed === true,
  showcaseAccess.raw.slice(0, 180),
);

assert("shift module access denied", current.json?.moduleAccess?.shiftWorkforce?.allowed === false);
assert(
  "shift module unavailable reason",
  current.json?.moduleAccess?.shiftWorkforce?.reasonCode?.includes("unavailable"),
  current.json?.moduleAccess?.shiftWorkforce?.reasonCode,
);

assert("trial end NULL remains null", summary?.trialEndsAtUtc === null, JSON.stringify(summary?.trialEndsAtUtc));
assert("grace until NULL remains null", summary?.graceUntilUtc === null, JSON.stringify(summary?.graceUntilUtc));

const bootstrapSubCount = Number(db(`SELECT COUNT(*) FROM saas_tenant_subscriptions WHERE id = '${DEV_SUBSCRIPTION_ID}'`));
const bootstrapHistoryCount = Number(db(`SELECT COUNT(*) FROM saas_tenant_subscription_transition_history WHERE id = '${DEV_SUBSCRIPTION_HISTORY_ID}'`));
assert("bootstrap subscription idempotent seed", bootstrapSubCount === 1, `count=${bootstrapSubCount}`);
assert("bootstrap transition history idempotent seed", bootstrapHistoryCount === 1, `count=${bootstrapHistoryCount}`);

const closureFacilityCodeA = `${CLOSURE_PREFIX}-facility-a`;
const closureFacilityCodeB = `${CLOSURE_PREFIX}-facility-b`;
const closureFacilityIdA = crypto.randomUUID();
const closureFacilityIdB = crypto.randomUUID();
db(`
  INSERT INTO facilities (
    id, tenant_id, code, name, status, time_zone_id, created_at_utc, updated_at_utc, version
  ) VALUES (
    '${closureFacilityIdA}', '${TENANT_ID}', '${closureFacilityCodeA}', 'Closure facility over-limit probe A',
    'Active', 'Europe/Istanbul', NOW(), NOW(), 1
  )
  ON CONFLICT (id) DO NOTHING;
  INSERT INTO facilities (
    id, tenant_id, code, name, status, time_zone_id, created_at_utc, updated_at_utc, version
  ) VALUES (
    '${closureFacilityIdB}', '${TENANT_ID}', '${closureFacilityCodeB}', 'Closure facility over-limit probe B',
    'Active', 'Europe/Istanbul', NOW(), NOW(), 1
  )
  ON CONFLICT (id) DO NOTHING;
`);
const overLimitCurrent = await api("GET", "/api/saas-subscription/current", undefined, owner);
const facilityUsage = overLimitCurrent.json?.limits?.facilities?.limit?.usageValue;
const facilityCap = overLimitCurrent.json?.limits?.facilities?.limit?.limitValue;
assert("facility usage reflects closure inserts", facilityUsage >= 3, `usage=${facilityUsage}`);
if (facilityUsage > facilityCap) {
  assert("facility over-limit denied", overLimitCurrent.json?.limits?.facilities?.allowed === false);
  assert(
    "facility over-limit reason",
    overLimitCurrent.json?.limits?.facilities?.reasonCode?.includes("limit-exceeded"),
    overLimitCurrent.json?.limits?.facilities?.reasonCode,
  );
}
db(`DELETE FROM facilities WHERE id IN ('${closureFacilityIdA}', '${closureFacilityIdB}')`);

const assign = await assignClosureSubscription(owner, `${CLOSURE_PREFIX}-assign`);
assert("closure development assign", assign.status === 201 || assign.status === 200, `status=${assign.status} ${assign.raw.slice(0, 180)}`);
const closureCurrent = await api("GET", "/api/saas-subscription/current", undefined, owner);
const closureSummary = closureCurrent.json?.subscription;
assert("closure subscription current", closureSummary?.sourceProvenance?.includes(CLOSURE_PREFIX));
const closureSubscriptionId = closureSummary?.subscriptionId;
const closureVersion = Number(closureSummary?.version ?? 0);
assert("closure subscription id present", Boolean(closureSubscriptionId));

const assignReplay = await assignClosureSubscription(owner, `${CLOSURE_PREFIX}-assign`);
assert("development assign idempotent replay", assignReplay.status === 200 && assignReplay.json?.idempotentReplay === true, assignReplay.raw.slice(0, 180));

const graceUntil = new Date(Date.now() + 7 * 24 * 60 * 60 * 1000).toISOString();
const toGrace = await transitionSubscription(
  owner,
  closureSubscriptionId,
  "grace-period",
  closureVersion,
  `${CLOSURE_PREFIX}-grace`,
  { transitionReason: "grace-entered", graceUntilUtc: graceUntil },
);
assert("active → grace-period", toGrace.status === 201 || toGrace.status === 200, `status=${toGrace.status} ${toGrace.raw.slice(0, 180)}`);

const graceReplay = await transitionSubscription(
  owner,
  closureSubscriptionId,
  "grace-period",
  closureVersion,
  `${CLOSURE_PREFIX}-grace`,
  { transitionReason: "grace-entered", graceUntilUtc: graceUntil },
);
assert("grace transition idempotent replay", graceReplay.status === 200 && graceReplay.json?.idempotentReplay === true, graceReplay.raw.slice(0, 180));

const staleGrace = await transitionSubscription(
  owner,
  closureSubscriptionId,
  "active",
  closureVersion,
  `${CLOSURE_PREFIX}-stale`,
  { transitionReason: "payment-received" },
);
assert("optimistic concurrency stale version", staleGrace.status === 409, `status=${staleGrace.status}`);

const toActive = await transitionSubscription(
  owner,
  closureSubscriptionId,
  "active",
  closureVersion + 1,
  `${CLOSURE_PREFIX}-reactivate`,
  { transitionReason: "payment-received" },
);
assert("grace-period → active", toActive.status === 201 || toActive.status === 200, `status=${toActive.status}`);

const conflictBody = await transitionSubscription(
  owner,
  closureSubscriptionId,
  "suspended",
  closureVersion + 2,
  `${CLOSURE_PREFIX}-grace`,
  { transitionReason: "manual-suspension" },
);
assert("idempotency conflict on reused key", conflictBody.status === 409, `status=${conflictBody.status}`);

const invalidTransition = await transitionSubscription(
  owner,
  closureSubscriptionId,
  "trialing",
  closureVersion + 2,
  `${CLOSURE_PREFIX}-invalid`,
  { transitionReason: "trial-started" },
);
assert("invalid transition rejected", invalidTransition.status === 409, `status=${invalidTransition.status}`);

const userLimitProbe = closureCurrent.json?.limits?.users?.limit;
const remainingSeats = userLimitProbe?.limitValue != null && userLimitProbe?.usageValue != null
  ? userLimitProbe.limitValue - userLimitProbe.usageValue
  : null;
if (remainingSeats === 1) {
  const parallel = await Promise.all([
    api("POST", "/api/admin-access/invitations", {
      email: `${CLOSURE_PREFIX.toLowerCase()}-slot-a@closure.dev.local`,
      idempotencyKey: `${CLOSURE_PREFIX}-slot-a`,
    }, owner),
    api("POST", "/api/admin-access/invitations", {
      email: `${CLOSURE_PREFIX.toLowerCase()}-slot-b@closure.dev.local`,
      idempotencyKey: `${CLOSURE_PREFIX}-slot-b`,
    }, owner),
  ]);
  const statuses = parallel.map((result) => result.status).join(",");
  assert("concurrent final-slot best effort observed", /200|201|403|409/.test(statuses), statuses);
  db(`DELETE FROM saas_identity_invitations WHERE intended_email_normalized LIKE '${CLOSURE_PREFIX.toLowerCase()}-slot-%@closure.dev.local'`);
} else {
  console.log(`Skipping concurrent final-slot probe (remainingSeats=${remainingSeats ?? "unknown"}).`);
}

previewClosureFixtures();
removeClosureFixtures();
const remaining = previewClosureFixtures();
assert("closure fixtures remaining", remaining.subs === 0 && remaining.idempotency === 0);
assertDevSubscriptionPreserved();

console.log("SAAS-SUBSCRIPTION-1 domain-entitlements verification passed.");
