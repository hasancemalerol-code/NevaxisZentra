#!/usr/bin/env node
import { spawnSync } from "node:child_process";
import { readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), "../../..");
const BASE = process.env.NEVAXIS_FACTOS_API_BASE_URL ?? "http://127.0.0.1:5000";
const FACILITY = "bb000001-0005-4000-8000-000000000005";
const MATERIAL = "bb000001-00a2-4000-8000-0000000000a2";
const PROFILE = "bb000001-0270-4000-8000-000000000270";
const UNAUTH_FACILITY = "bb000001-0005-4000-8000-000000000099";
const CLOSURE_PREFIX = "COMMSALE1-CLOSURE";
const SEC_PREFIX = "COMMSALE1-SEC";
const SOURCE_EVIDENCE_KIND = "manual-development-simulator";
const PRICE_BASIS_GROSS_NET = "net";
const RUN_SUFFIX = Date.now();
const MANAGER = {
  issuer: "crevafactos-dev",
  subject: "planning-pilot-user",
  tenant: "dev-pilot-tenant",
};
const READONLY = {
  issuer: "crevafactos-dev",
  subject: "planning-pilot-reviewer",
  tenant: "dev-pilot-tenant",
};

function fail(message) {
  console.error(`COMMERCIAL-SALES-PRICE-1 security/idempotency verification failed: ${message}`);
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

function db(sql) {
  const result = spawnSync(
    "docker",
    ["exec", "NevaxisZentra-local-postgres-1", "psql", "-U", "crevafactos", "-d", "crevafactos_local", "-t", "-A", "-c", sql],
    { encoding: "utf8" },
  );
  if (result.status !== 0) fail(result.stderr || result.stdout);
  return result.stdout.trim();
}

function actorHeaders(actor) {
  return {
    "Content-Type": "application/json",
    "X-crevafactos-dev-Issuer": actor.issuer,
    "X-crevafactos-dev-Subject": actor.subject,
    "X-NevaxisZentra-Tenant": actor.tenant,
  };
}

async function liveRequest(method, path, body, headers = actorHeaders(MANAGER)) {
  const response = await fetch(`${BASE}${path}`, {
    method,
    headers,
    body: body ? JSON.stringify(body) : undefined,
  });
  const text = await response.text();
  let json = null;
  try { json = text ? JSON.parse(text) : null; } catch { json = { raw: text }; }
  return { status: response.status, json, text };
}

function removeClosureFixtures() {
  db(`
    DELETE FROM commercial_sales_price_assessment_drivers
    WHERE assessment_id IN (
      SELECT id FROM commercial_sales_price_assessments
      WHERE idempotency_key LIKE '${CLOSURE_PREFIX}%'
         OR idempotency_key LIKE '${SEC_PREFIX}%'
    );
    DELETE FROM commercial_sales_price_assessments
    WHERE idempotency_key LIKE '${CLOSURE_PREFIX}%'
       OR idempotency_key LIKE '${SEC_PREFIX}%';
    DELETE FROM commercial_sales_price_profiles
    WHERE create_idempotency_key LIKE '${CLOSURE_PREFIX}%'
       OR create_idempotency_key LIKE '${SEC_PREFIX}%';
  `);
}

console.log("Running COMMERCIAL-SALES-PRICE-1 security/idempotency verifier.");

const endpoints = readRequired("apps/api/CommercialIntelligence/CommercialSalesPriceEndpointRouteBuilderExtensions.cs");
const helpers = readRequired("apps/api/CommercialIntelligence/CommercialIntelligenceEndpointHelpers.cs");
const migration = readRequired("packages/NevaxisZentra.Persistence.Migrations/Migrations/20260824195725_AddCommercialSalesPriceIntelligenceFoundation.cs");
const permissions = readRequired("packages/NevaxisZentra.Persistence/CommercialIntelligence/CommercialIntelligencePermissionKeys.cs");
const bootstrap = readRequired("packages/NevaxisZentra.Persistence/Development/DevelopmentPilotBootstrapService.cs");

assert("endpoints require auth", endpoints.includes("RequireAuthorization"));
assert("read access helper", helpers.includes("ResolveReadAccessAsync"));
assert("manage access helper", helpers.includes("ResolveManageAccessAsync"));
assert("commercial-intelligence.read key", permissions.includes("commercial-intelligence.read"));
assert("commercial-intelligence.manage key", permissions.includes("commercial-intelligence.manage"));
assert("RLS enabled", migration.includes("ENABLE ROW LEVEL SECURITY"));
assert("FORCE RLS", migration.includes("FORCE ROW LEVEL SECURITY"));
assert("assessment append-only grant", migration.includes("GRANT SELECT, INSERT ON TABLE commercial_sales_price_assessments"));
forbidText(migration, "GRANT DELETE ON TABLE commercial_sales_price_assessments", "assessments");
forbidText(migration, "permission_definitions", "sales migration permissions");
forbidText(endpoints, "MapDelete", "commercial sales price endpoints");
forbidText(endpoints, "ai-task", "commercial sales price endpoints");
forbidText(endpoints, "notification", "commercial sales price endpoints");
forbidText(endpoints, "remediation", "commercial sales price endpoints");
assert("bootstrap read seed", bootstrap.includes("EnsureCommercialIntelligenceReadPermissionAssignmentAsync"));
assert("bootstrap manage seed", bootstrap.includes("EnsureCommercialIntelligenceManagePermissionAssignmentAsync"));

const managerHeaders = actorHeaders(MANAGER);
const readonlyHeaders = actorHeaders(READONLY);
const crossTenantHeaders = actorHeaders({
  issuer: "crevafactos-dev",
  subject: MANAGER.subject,
  tenant: "unknown-tenant-key",
});

const ready = await fetch(`${BASE}/health/ready`);
assert("API ready", ready.status === 200, `status=${ready.status}`);

removeClosureFixtures();

const assessmentsBeforeDeny = Number(db("SELECT COUNT(*) FROM commercial_sales_price_assessments"));
const driversBeforeDeny = Number(db("SELECT COUNT(*) FROM commercial_sales_price_assessment_drivers"));

const noAuth = await fetch(`${BASE}/api/commercial-intelligence/sales-prices/profiles?facilityId=${FACILITY}`);
assert("missing auth 401", noAuth.status === 401, `status=${noAuth.status}`);

const readonlyWrite = await liveRequest(
  "POST",
  `/api/commercial-intelligence/sales-prices/assessments/evaluate?facilityId=${FACILITY}`,
  {
    profileId: PROFILE,
    materialId: MATERIAL,
    sourceEvidenceKind: SOURCE_EVIDENCE_KIND,
    comparisonDimension: "material",
    currentUnitPrice: 100,
    currencyCode: "TRY",
    normalizedUnit: "kg",
    windowStartUtc: "2026-04-01T00:00:00.000Z",
    windowEndUtc: "2026-08-25T00:00:00.000Z",
    priceBasisGrossNet: PRICE_BASIS_GROSS_NET,
    priceBasisTaxInclusion: "excluded",
    priceBasisDiscountInclusion: "excluded",
    priceBasisFreightInclusion: "excluded",
    sourceProvenance: "closure",
    idempotencyKey: `${CLOSURE_PREFIX}-readonly-${RUN_SUFFIX}`,
  },
  readonlyHeaders,
);
assert("readonly write 403", readonlyWrite.status === 403, `status=${readonlyWrite.status}`);

const wrongFacility = await liveRequest(
  "GET",
  `/api/commercial-intelligence/sales-prices/profiles?facilityId=${UNAUTH_FACILITY}`,
  null,
  managerHeaders,
);
assert("wrong facility 403", wrongFacility.status === 403, `status=${wrongFacility.status}`);

const crossTenant = await liveRequest(
  "GET",
  `/api/commercial-intelligence/sales-prices/profiles?facilityId=${FACILITY}`,
  null,
  crossTenantHeaders,
);
assert("cross tenant denied", crossTenant.status === 401 || crossTenant.status === 403, `status=${crossTenant.status}`);

const assessmentsAfterDeny = Number(db("SELECT COUNT(*) FROM commercial_sales_price_assessments"));
const driversAfterDeny = Number(db("SELECT COUNT(*) FROM commercial_sales_price_assessment_drivers"));
assert("denied requests created no assessments", assessmentsAfterDeny === assessmentsBeforeDeny, `${assessmentsBeforeDeny}→${assessmentsAfterDeny}`);
assert("denied requests created no drivers", driversAfterDeny === driversBeforeDeny, `${driversBeforeDeny}→${driversAfterDeny}`);

const provenance =
  "Deterministic local Development sales-price evidence — simulated, non-production, not real customer, invoice, quotation, or market evidence.";
const evaluateBody = {
  profileId: PROFILE,
  materialId: MATERIAL,
  sourceEvidenceKind: SOURCE_EVIDENCE_KIND,
  comparisonDimension: "material",
  currentUnitPrice: 101,
  currencyCode: "TRY",
  normalizedUnit: "kg",
  windowStartUtc: "2026-04-01T00:00:00.000Z",
  windowEndUtc: "2026-08-25T00:00:00.000Z",
  priceBasisGrossNet: PRICE_BASIS_GROSS_NET,
  priceBasisTaxInclusion: "excluded",
  priceBasisDiscountInclusion: "excluded",
  priceBasisFreightInclusion: "excluded",
  sourceProvenance: provenance,
  idempotencyKey: `${SEC_PREFIX}-evaluate-${RUN_SUFFIX}`,
};
const firstEvaluate = await liveRequest(
  "POST",
  `/api/commercial-intelligence/sales-prices/assessments/evaluate?facilityId=${FACILITY}`,
  evaluateBody,
  managerHeaders,
);
assert("authorized evaluate 201", firstEvaluate.status === 201, firstEvaluate.text?.slice(0, 200));
const replayEvaluate = await liveRequest(
  "POST",
  `/api/commercial-intelligence/sales-prices/assessments/evaluate?facilityId=${FACILITY}`,
  evaluateBody,
  managerHeaders,
);
assert("identical idempotent replay 200", replayEvaluate.status === 200);
assert("identical replay flag", replayEvaluate.json?.idempotentReplay === true);
assert("identical replay same id", replayEvaluate.json?.id === firstEvaluate.json?.id);

const conflictEvaluate = await liveRequest(
  "POST",
  `/api/commercial-intelligence/sales-prices/assessments/evaluate?facilityId=${FACILITY}`,
  { ...evaluateBody, currentUnitPrice: 120 },
  managerHeaders,
);
assert("different payload conflict 409", conflictEvaluate.status === 409, `status=${conflictEvaluate.status}`);
forbidText(conflictEvaluate.text ?? "", "Npgsql", "conflict response");
forbidText(conflictEvaluate.text ?? "", "password", "conflict response");

const staleProfile = await liveRequest(
  "PUT",
  `/api/commercial-intelligence/sales-prices/profiles/${PROFILE}?facilityId=${FACILITY}`,
  {
    expectedVersion: 999,
    displayName: "Stale profile",
    status: "approved",
    comparisonDimension: "material",
    referenceWindowDurationDays: 120,
    minimumComparableObservations: 6,
    maximumPositiveVarianceRatio: 0.1,
    allowedEvidenceStatuses: "simulated",
    allowedSourceEvidenceKinds: SOURCE_EVIDENCE_KIND,
    requiredTaxInclusion: "excluded",
    requiredDiscountInclusion: "excluded",
    requiredFreightInclusion: "excluded",
    requiredGrossNetBasis: PRICE_BASIS_GROSS_NET,
    sourceProvenance: provenance,
  },
  managerHeaders,
);
assert("stale expected version 409", staleProfile.status === 409, `status=${staleProfile.status}`);

removeClosureFixtures();
console.log("COMMERCIAL-SALES-PRICE-1 security/idempotency verification passed.");
