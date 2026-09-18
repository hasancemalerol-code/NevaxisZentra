#!/usr/bin/env node
import { spawnSync } from "node:child_process";
import { readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), "../../..");
const BASE = process.env.NEVAXIS_FACTOS_API_BASE_URL ?? "http://127.0.0.1:5000";
const DEV_PLAN_CODE = "dev-pilot-local-commercial";
const DEV_PLAN_ID = "bb000001-00e0-4000-8000-0000000000e0";
const DEV_MONTHLY_VERSION_ID = "bb000001-00e2-4000-8000-0000000000e2";
const CLOSURE_PREFIX = "SAASCOMM1-CLOSURE";
const RUN_SUFFIX = Date.now();
const MANAGER = {
  issuer: "crevafactos-dev",
  subject: "planning-pilot-user",
  tenant: "dev-pilot-tenant",
};

function fail(message) {
  console.error(`SAAS-COMMERCIAL-1 domain verification failed: ${message}`);
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
    ["exec", "NevaxisZentra-local-postgres-1", "psql", "-U", "crevafactos", "-d", "crevafactos_local", "-t", "-A", "-c", sql],
    { encoding: "utf8" },
  );
  if (result.status !== 0) fail(result.stderr || result.stdout);
  return result.stdout.trim();
}

function dbExpectFailure(sql) {
  const result = spawnSync(
    "docker",
    [
      "exec",
      "NevaxisZentra-local-postgres-1",
      "psql",
      "-U",
      "crevafactos",
      "-d",
      "crevafactos_local",
      "-v",
      "ON_ERROR_STOP=1",
      "-c",
      sql,
    ],
    { encoding: "utf8" },
  );
  return { ok: result.status === 0, output: `${result.stdout}${result.stderr}` };
}

function headers(actor = MANAGER) {
  return {
    "Content-Type": "application/json",
    "X-crevafactos-dev-Issuer": actor.issuer,
    "X-crevafactos-dev-Subject": actor.subject,
    "X-NevaxisZentra-Tenant": actor.tenant,
  };
}

async function request(method, path, body, actor = MANAGER) {
  const response = await fetch(`${BASE}${path}`, {
    method,
    headers: headers(actor),
    body: body ? JSON.stringify(body) : undefined,
  });
  const text = await response.text();
  let json = null;
  try {
    json = text ? JSON.parse(text) : null;
  } catch {
    json = { raw: text };
  }
  return { status: response.status, json, raw: text };
}

function closurePlanPredicate(alias = "p") {
  return `${alias}.plan_code LIKE '${CLOSURE_PREFIX}%'`;
}

function closurePlanIdsSubquery() {
  return `SELECT id FROM saas_commercial_plans p WHERE ${closurePlanPredicate("p")}`;
}

function closureVersionIdsSubquery() {
  return `SELECT id FROM saas_commercial_plan_versions v WHERE v.plan_id IN (${closurePlanIdsSubquery()})`;
}

function previewClosureFixtures() {
  return db(`
    SELECT p.id, p.plan_code
    FROM saas_commercial_plans p
    WHERE ${closurePlanPredicate("p")}
    ORDER BY p.plan_code
  `);
}

function removeClosureFixtures() {
  db(`
    DELETE FROM saas_commercial_audit_events
    WHERE entity_id IN (${closurePlanIdsSubquery()})
       OR entity_id IN (${closureVersionIdsSubquery()});

    DELETE FROM saas_commercial_grace_period_policies
    WHERE plan_version_id IN (${closureVersionIdsSubquery()});

    DELETE FROM saas_commercial_payment_method_capabilities
    WHERE plan_version_id IN (${closureVersionIdsSubquery()});

    DELETE FROM saas_commercial_plan_version_entitlements
    WHERE plan_version_id IN (${closureVersionIdsSubquery()});

    DELETE FROM saas_commercial_price_components
    WHERE plan_version_id IN (${closureVersionIdsSubquery()});

    DELETE FROM saas_commercial_plan_versions
    WHERE plan_id IN (${closurePlanIdsSubquery()});

    DELETE FROM saas_commercial_plans p
    WHERE ${closurePlanPredicate("p")};

    DELETE FROM saas_commercial_entitlement_definitions
    WHERE code LIKE '${CLOSURE_PREFIX.toLowerCase()}%';
  `);
}

function assertDevCatalogPreserved() {
  const planCount = Number(db(`SELECT COUNT(*) FROM saas_commercial_plans WHERE id = '${DEV_PLAN_ID}'`));
  assert("dev-pilot-local-commercial plan preserved", planCount === 1, `count=${planCount}`);

  const versionCount = Number(db(`
    SELECT COUNT(*) FROM saas_commercial_plan_versions
    WHERE plan_id = '${DEV_PLAN_ID}'
  `));
  assert("dev pilot monthly/annual versions preserved", versionCount === 2, `count=${versionCount}`);
}

function insertClosurePlan(planId, planCode, displayName) {
  const normalized = planCode.trim().toLowerCase();
  const now = new Date().toISOString();
  db(`
    INSERT INTO saas_commercial_plans (
      id, plan_code, plan_code_normalized, display_name_tr, description_tr,
      lifecycle_state, availability_state, created_at_utc, updated_at_utc, version
    ) VALUES (
      '${planId}', '${planCode}', '${normalized}', '${displayName}', 'Closure fixture',
      'draft', 'draft', '${now}', '${now}', 1
    )
  `);
  return normalized;
}

function insertClosureVersion(versionId, planId, versionNumber, cadence, effectiveFrom, effectiveUntil, publicationStatus) {
  const now = new Date().toISOString();
  db(`
    INSERT INTO saas_commercial_plan_versions (
      id, plan_id, version_number, effective_from_utc, effective_until_utc,
      billing_cadence, trial_days, currency_code, sellable, publication_status,
      commercial_notes, provenance, created_at_utc, version
    ) VALUES (
      '${versionId}', '${planId}', ${versionNumber}, '${effectiveFrom}', ${effectiveUntil ? `'${effectiveUntil}'` : "NULL"},
      '${cadence}', 0, 'TRY', false, '${publicationStatus}',
      'Closure version', 'closure-fixture', '${now}', 1
    )
  `);
}

console.log("Running SAAS-COMMERCIAL-1 domain verifier.");

const conversions = readRequired("packages/NevaxisZentra.Persistence/SaasCommercial/SaasCommercialConstants.cs");
const repository = readRequired("packages/NevaxisZentra.Persistence/SaasCommercial/SaasCommercialRepository.cs");
const migration = readRequired(
  "packages/NevaxisZentra.Persistence.Migrations/Migrations/20260814051728_AddSaasCommercialPlanAndEntitlementFoundation.cs",
);

assert("plan code normalizer", conversions.includes("NormalizePlanCode"));
assert("plan code normalization lowercases", conversions.includes("ToLowerInvariant"));
assert("billing cadence monthly/annual", conversions.includes("SaasCommercialBillingCadences.Monthly"));
assert("published immutability guard", conversions.includes("IsPublishedImmutable"));
assert("repository overlap detection", repository.includes("PeriodsOverlap"));
assert("DB exclusion constraint declared", migration.includes("ex_sc_plan_versions_no_overlap"));
assert("unique normalized plan code index", migration.includes("ix_saas_commercial_plans_plan_code_normalized"));

const ready = await fetch(`${BASE}/health/ready`);
assert("API ready", ready.status === 200, `status=${ready.status}`);

removeClosureFixtures();
assertDevCatalogPreserved();

// Plan-code normalization and uniqueness
const normPlanId = crypto.randomUUID();
const normPlanCode = `${CLOSURE_PREFIX}-NORM-${RUN_SUFFIX}`;
insertClosurePlan(normPlanId, normPlanCode, `${CLOSURE_PREFIX} Normalized Plan`);
const storedNormalized = db(`SELECT plan_code_normalized FROM saas_commercial_plans WHERE id = '${normPlanId}'`);
assert("plan code stored normalized", storedNormalized === normPlanCode.toLowerCase(), storedNormalized);

const duplicateCase = dbExpectFailure(`
  INSERT INTO saas_commercial_plans (
    id, plan_code, plan_code_normalized, display_name_tr, description_tr,
    lifecycle_state, availability_state, created_at_utc, updated_at_utc, version
  ) VALUES (
    '${crypto.randomUUID()}', '${normPlanCode.toUpperCase()}', '${normPlanCode.toLowerCase()}',
    'Duplicate normalized plan', NULL, 'draft', 'draft', NOW(), NOW(), 1
  )
`);
assert("duplicate normalized plan code rejected", !duplicateCase.ok);
assert(
  "duplicate normalized plan code unique violation",
  /unique|duplicate key|ix_saas_commercial_plans_plan_code_normalized/i.test(duplicateCase.output),
  duplicateCase.output,
);

const cadencePlanId = crypto.randomUUID();
const cadencePlanCode = `${CLOSURE_PREFIX}-CADENCE-${RUN_SUFFIX}`;
insertClosurePlan(cadencePlanId, cadencePlanCode, `${CLOSURE_PREFIX} Cadence Plan`);
const monthlyVersionId = crypto.randomUUID();
const annualVersionId = crypto.randomUUID();
insertClosureVersion(
  monthlyVersionId,
  cadencePlanId,
  1,
  "monthly",
  "2026-03-01T00:00:00.000Z",
  null,
  "draft",
);
insertClosureVersion(
  annualVersionId,
  cadencePlanId,
  2,
  "annual",
  "2026-03-01T00:00:00.000Z",
  null,
  "draft",
);

const planDetail = await request("GET", `/api/saas-commercial/catalog/${cadencePlanCode}`);
assert("cadence plan detail → 200", planDetail.status === 200, `status=${planDetail.status}`);
const cadences = (planDetail.json?.versions ?? []).map((v) => v.billingCadence).sort();
assert("monthly and annual cadence both present", cadences.join(",") === "annual,monthly", cadences.join(","));

// Immutable published version (repository contract + readable published state)
const publishedPlanId = crypto.randomUUID();
const publishedPlanCode = `${CLOSURE_PREFIX}-PUBLISHED-${RUN_SUFFIX}`;
insertClosurePlan(publishedPlanId, publishedPlanCode, `${CLOSURE_PREFIX} Published Plan`);
const publishedVersionId = crypto.randomUUID();
insertClosureVersion(
  publishedVersionId,
  publishedPlanId,
  1,
  "monthly",
  "2026-04-01T00:00:00.000Z",
  null,
  "published",
);

const publishedRead = await request("GET", `/api/saas-commercial/catalog/${publishedPlanCode}/versions/1`);
assert("published version readable", publishedRead.status === 200, `status=${publishedRead.status}`);
assert("published status exposed", publishedRead.json?.publicationStatus === "published");

const overlapPlanId = crypto.randomUUID();
const overlapPlanCode = `${CLOSURE_PREFIX}-OVERLAP-${RUN_SUFFIX}`;
insertClosurePlan(overlapPlanId, overlapPlanCode, `${CLOSURE_PREFIX} Overlap Plan`);
const overlapBaseVersionId = crypto.randomUUID();
insertClosureVersion(
  overlapBaseVersionId,
  overlapPlanId,
  1,
  "monthly",
  "2026-05-01T00:00:00.000Z",
  null,
  "draft",
);

const overlapInsert = dbExpectFailure(`
  INSERT INTO saas_commercial_plan_versions (
    id, plan_id, version_number, effective_from_utc, effective_until_utc,
    billing_cadence, trial_days, currency_code, sellable, publication_status,
    commercial_notes, provenance, created_at_utc, version
  ) VALUES (
    '${crypto.randomUUID()}', '${overlapPlanId}', 2, '2026-05-15T00:00:00.000Z', NULL,
    'monthly', 0, 'TRY', false, 'draft', 'Overlap attempt', 'closure-fixture', NOW(), 1
  )
`);
assert("overlapping effective range insert rejected", !overlapInsert.ok);
assert(
  "overlap rejection uses exclusion constraint",
  /ex_sc_plan_versions_no_overlap|exclusion|conflicting key/i.test(overlapInsert.output),
  overlapInsert.output,
);

const exclusionExists = Number(db(`
  SELECT COUNT(*) FROM pg_constraint WHERE conname = 'ex_sc_plan_versions_no_overlap'
`));
assert("ex_sc_plan_versions_no_overlap constraint present", exclusionExists === 1, `count=${exclusionExists}`);

// Entitlement semantics via API on dev catalog
const entitlements = await request("GET", `/api/saas-commercial/catalog/${DEV_PLAN_CODE}/entitlements?version=1`);
assert("entitlements endpoint → 200", entitlements.status === 200, `status=${entitlements.status}`);
const entitlementRows = entitlements.json?.entitlements ?? [];
assert("entitlement rows present", entitlementRows.length >= 4, `count=${entitlementRows.length}`);

const facilityLimit = entitlementRows.find((e) => e.code === "facility-limit");
const moduleAccess = entitlementRows.find((e) => e.code === "module.shift-workforce");
assert("limited entitlement semantics", facilityLimit?.semantics === "limited" && facilityLimit?.integerLimitValue === 2);
assert("unavailable module entitlement semantics", moduleAccess?.semantics === "unavailable" && moduleAccess?.booleanValue === false);

const entitlementCodes = entitlementRows.map((e) => e.code);
assert(
  "entitlements deterministically ordered by code",
  entitlementCodes.join(",") === [...entitlementCodes].sort((a, b) => a.localeCompare(b)).join(","),
  entitlementCodes.join(","),
);

// NULL amount must not become zero in API read model
const devMonthly = await request("GET", `/api/saas-commercial/catalog/${DEV_PLAN_CODE}/versions/1`);
assert("dev monthly version → 200", devMonthly.status === 200, `status=${devMonthly.status}`);
const tenantBase = (devMonthly.json?.priceComponents ?? []).find((c) => c.componentType === "tenant-base");
assert("tenant-base price component present", Boolean(tenantBase));
assert("NULL amount remains null in JSON", tenantBase.amount === null, JSON.stringify(tenantBase));
assert("NULL amount not coerced to zero", tenantBase.amount !== 0);

const nullAmountDb = db(`
  SELECT amount IS NULL
  FROM saas_commercial_price_components
  WHERE plan_version_id = '${DEV_MONTHLY_VERSION_ID}'
    AND component_type = 'tenant-base'
`);
assert("tenant-base amount NULL in database", nullAmountDb === "t", nullAmountDb);

// Payment method types and deterministic ordering
const paymentMethods = await request("GET", `/api/saas-commercial/catalog/${DEV_PLAN_CODE}/payment-methods?version=1`);
assert("payment methods endpoint → 200", paymentMethods.status === 200, `status=${paymentMethods.status}`);
const methods = paymentMethods.json?.paymentMethods ?? [];
const methodTypes = methods.map((m) => m.methodType);
assert(
  "payment method types card/bank-transfer/enterprise-invoice",
  methodTypes.includes("card")
    && methodTypes.includes("bank-transfer")
    && methodTypes.includes("enterprise-invoice"),
  methodTypes.join(","),
);
const displayOrders = methods.map((m) => m.displayOrder);
assert(
  "payment methods ordered by displayOrder",
  displayOrders.join(",") === [...displayOrders].sort((a, b) => a - b).join(","),
  displayOrders.join(","),
);
assert("card capability recurring", methods.find((m) => m.methodType === "card")?.recurringCapable === true);
assert(
  "bank-transfer manual verification required",
  methods.find((m) => m.methodType === "bank-transfer")?.manualVerificationRequired === true,
);

// Grace-period policy contract (DB-level policy surface)
const gracePolicy = db(`
  SELECT grace_period_days, restricted_behavior_token, suspension_eligibility_policy, operator_explanation_tr
  FROM saas_commercial_grace_period_policies
  WHERE plan_version_id = '${DEV_MONTHLY_VERSION_ID}'
`);
const [graceDays, restrictedToken, suspensionPolicy, explanation] = gracePolicy.split("|");
assert("grace period days contract", graceDays === "14", graceDays);
assert("restricted behavior token contract", restrictedToken === "read-only-restricted", restrictedToken);
assert("suspension eligibility policy contract", suspensionPolicy === "grace-then-suspend", suspensionPolicy);
assert("operator explanation present", explanation?.length > 0, explanation ?? "");

// Deterministic catalog ordering via API
const catalogA = await request("GET", "/api/saas-commercial/catalog");
const catalogB = await request("GET", "/api/saas-commercial/catalog");
assert("catalog list → 200", catalogA.status === 200 && catalogB.status === 200);
const codesA = (catalogA.json?.plans ?? []).map((p) => p.planCode);
const codesB = (catalogB.json?.plans ?? []).map((p) => p.planCode);
assert("catalog ordering stable across requests", codesA.join("|") === codesB.join("|"), `${codesA.join("|")} vs ${codesB.join("|")}`);
assert(
  "catalog ordered by normalized plan code",
  codesA.join(",") === [...codesA].sort((a, b) => a.localeCompare(b)).join(","),
  codesA.join(","),
);
assert("dev pilot plan included in catalog", codesA.includes(DEV_PLAN_CODE));

console.log("Preview closure fixtures:");
console.log(previewClosureFixtures() || "(none)");
removeClosureFixtures();
const remaining = Number(db(`SELECT COUNT(*) FROM saas_commercial_plans p WHERE ${closurePlanPredicate("p")}`));
assert("closure fixtures remaining = 0", remaining === 0, `count=${remaining}`);
assertDevCatalogPreserved();

console.log("SAAS-COMMERCIAL-1 domain verification passed.");
