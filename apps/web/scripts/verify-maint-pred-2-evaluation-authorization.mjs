#!/usr/bin/env node
import { existsSync, readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";
import { spawnSync } from "node:child_process";

const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), "../../..");
const BASE = process.env.NEVAXIS_FACTOS_API_BASE_URL ?? "http://localhost:5000";
const TENANT = process.env.NEVAXIS_FACTOS_TENANT ?? "dev-pilot-tenant";
const ISSUER = "crevafactos-dev";
const OPERATOR = process.env.NEVAXIS_FACTOS_SUBJECT ?? "production-pilot-operator";
const REVIEWER = "planning-pilot-reviewer";
const NEGATIVE = "quality-negative-test-user";
const OTHER_TENANT = "dev-other-tenant";
const FACILITY = process.env.NEVAXIS_FACTOS_FACILITY_ID ?? "bb000001-0005-4000-8000-000000000005";
const UNAUTH_FACILITY = "bb000001-0005-4000-8000-000000000099";
const EVAL_PATH = `/api/maintenance/condition/risk/evaluate?facilityId=${FACILITY}`;
const OVERVIEW_PATH = `/api/maintenance/condition/risk/overview?facilityId=${FACILITY}`;

function read(rel) {
  const abs = resolve(ROOT, rel);
  if (!existsSync(abs)) throw new Error(`Missing ${rel}`);
  return readFileSync(abs, "utf8");
}

function fail(msg) {
  console.error(`MAINT-PRED-2 evaluation authorization verifier failed: ${msg}`);
  process.exit(1);
}

function assert(label, ok, detail = "") {
  if (!ok) fail(`${label}${detail ? `: ${detail}` : ""}`);
  console.log(`✓ ${label}`);
}

function headers(subject = OPERATOR, extra = {}) {
  return {
    Accept: "application/json",
    "Content-Type": "application/json",
    "X-crevafactos-dev-Issuer": ISSUER,
    "X-crevafactos-dev-Subject": subject,
    "X-NevaxisFactOS-Tenant": TENANT,
    ...extra,
  };
}

async function statusFor(method, path, subject = OPERATOR, extra = {}) {
  const res = await fetch(`${BASE}${path}`, { method, headers: headers(subject, extra) });
  return res.status;
}

function dbCount(sql) {
  const r = spawnSync(
    "docker",
    ["exec", "NevaxisFactOS-local-postgres-1", "psql", "-U", "crevafactos", "-d", "crevafactos_local", "-t", "-A", "-c", sql],
    { encoding: "utf8" },
  );
  if (r.status !== 0) fail(r.stderr || r.stdout);
  return r.stdout.trim();
}

console.log("Running MAINT-PRED-2 evaluation authorization static checks.");
const endpoints = read("apps/api/Maintenance/MaintenanceConditionEndpointRouteBuilderExtensions.cs");
const helpers = read("apps/api/Maintenance/MaintenanceEndpointHelpers.cs");

const evaluateMethod = endpoints.match(
  /private static async Task<IResult> EvaluatePredictiveRiskAsync[\s\S]*?\n    \}/,
);
assert("risk evaluate method present", Boolean(evaluateMethod));
assert("risk evaluate uses Manage write access", evaluateMethod[0].includes("MaintenancePermissionKeys.Manage"));
assert("risk evaluate does not use Read access", !evaluateMethod[0].includes("ResolveReadAccessAsync"));
assert("risk evaluate uses Write access helper", evaluateMethod[0].includes("ResolveWriteAccessAsync"));
assert("GET overview retains Read access", endpoints.match(/GetPredictiveRiskOverviewAsync[\s\S]*?ResolveReadAccessAsync/));
assert("GET asset assessment retains Read access", endpoints.match(/GetAssetPredictiveRiskAsync[\s\S]*?ResolveReadAccessAsync/));
assert("GET history retains Read access", endpoints.match(/ListAssetPredictiveRiskHistoryAsync[\s\S]*?ResolveReadAccessAsync/));
assert("condition evaluate unchanged Read access", endpoints.match(/EvaluateConditionAsync[\s\S]*?ResolveReadAccessAsync/));
assert("write access helper exists", helpers.includes("ResolveWriteAccessAsync"));
assert("Manage permission key exists", helpers.includes("MaintenancePermissionKeys.Manage") || endpoints.includes("MaintenancePermissionKeys.Manage"));

console.log("Running MAINT-PRED-2 evaluation authorization live checks.");
try {
  const ready = await fetch(`${BASE}/health/ready`);
  if (ready.status !== 200) fail(`/health/ready returned ${ready.status}`);

  const beforeAssess = dbCount("SELECT COUNT(*) FROM maintenance_predictive_risk_assessments");
  const beforeRec = dbCount("SELECT COUNT(*) FROM maintenance_predictive_recommendations");

  const authorized = await statusFor("POST", EVAL_PATH, OPERATOR);
  assert("authorized manage actor POST evaluate → 200", authorized === 200, `got ${authorized}`);

  const readOnly = await statusFor("POST", EVAL_PATH, REVIEWER);
  assert("read-only actor POST evaluate → 403", readOnly === 403, `got ${readOnly}`);

  const noPerm = await statusFor("POST", EVAL_PATH, NEGATIVE);
  assert("actor without Maintenance permission POST evaluate → 403", noPerm === 403, `got ${noPerm}`);

  const missingAuth = await fetch(`${BASE}${EVAL_PATH}`, { method: "POST" }).then((r) => r.status);
  assert("missing authentication POST evaluate → 401", missingAuth === 401, `got ${missingAuth}`);

  const unauthFacility = await statusFor("POST", `/api/maintenance/condition/risk/evaluate?facilityId=${UNAUTH_FACILITY}`, OPERATOR);
  assert("unauthorized facility POST evaluate → 403", unauthFacility === 403, `got ${unauthFacility}`);

  const crossTenant = await statusFor("POST", EVAL_PATH, OPERATOR, { "X-NevaxisFactOS-Tenant": OTHER_TENANT });
  assert("cross-tenant POST evaluate → 403", crossTenant === 403, `got ${crossTenant}`);

  const readGet = await statusFor("GET", OVERVIEW_PATH, REVIEWER);
  assert("read-only actor GET overview → 200", readGet === 200, `got ${readGet}`);

  const afterDeniedAssess = dbCount("SELECT COUNT(*) FROM maintenance_predictive_risk_assessments");
  const afterDeniedRec = dbCount("SELECT COUNT(*) FROM maintenance_predictive_recommendations");
  assert("denied POST did not increase assessments", afterDeniedAssess === beforeAssess, `${beforeAssess} → ${afterDeniedAssess}`);
  assert("denied POST did not increase recommendations", afterDeniedRec === beforeRec, `${beforeRec} → ${afterDeniedRec}`);
} catch (error) {
  fail(error instanceof Error ? error.message : String(error));
}

console.log("MAINT-PRED-2 evaluation authorization verifier PASS");
