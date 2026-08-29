#!/usr/bin/env node
import { readFileSync } from "node:fs";
import { randomUUID } from "node:crypto";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";
import { spawnSync } from "node:child_process";

const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), "../../..");
const BASE = process.env.CREVAFACTOS_API_BASE_URL ?? "http://127.0.0.1:5000";
const FACILITY = "bb000001-0005-4000-8000-000000000005";
const UNAUTH_FACILITY = "bb000001-0005-4000-8000-000000000099";
const SHORTAGE_ASSESSMENT = "bb000001-00b3-4000-8000-0000000000b3";
const INSUFFICIENT_ASSESSMENT = "bb000001-00b4-4000-8000-0000000000b4";
const MATERIAL_PP = "bb000001-00a2-4000-8000-0000000000a2";
const MATERIAL_ABS = "bb000001-00a4-4000-8000-0000000000a4";
const PROFILE_PP = "bb000001-00ae-4000-8000-0000000000ae";
const PROFILE_PP_EXCEEDS_REGRIND = "bb000001-00b0-4000-8000-0000000000b0";
const PROFILE_ABS_PARTIAL = "bb000001-00af-4000-8000-0000000000af";
const SUFFICIENT_ASSESSMENT = "bb000001-00b6-4000-8000-0000000000b6";
const RUN_SUFFIX = randomUUID();
const IDEM_KEY = `inventory-risk-foundation-1:${RUN_SUFFIX}`;
const MANAGER = { issuer: "crevafactos-dev", subject: "production-pilot-operator", tenant: "dev-pilot-tenant" };
const READONLY = { issuer: "crevafactos-dev", subject: "planning-pilot-reviewer", tenant: "dev-pilot-tenant" };

function fail(message) {
  console.error(`INVENTORY-RISK-FOUNDATION-1 security/idempotency verification failed: ${message}`);
  process.exit(1);
}

function assert(label, ok, detail = "") {
  if (!ok) fail(`${label}${detail ? `: ${detail}` : ""}`);
  console.log(`✓ ${label}`);
}

function readRequired(relativePath) {
  return readFileSync(resolve(ROOT, relativePath), "utf8").replaceAll("\r\n", "\n");
}

function actorHeaders(actor) {
  return {
    "Content-Type": "application/json",
    "X-CrevaFactOS-Dev-Issuer": actor.issuer,
    "X-CrevaFactOS-Dev-Subject": actor.subject,
    "X-CrevaFactOS-Tenant": actor.tenant,
  };
}

function dbCount(sql) {
  const result = spawnSync(
    "docker",
    ["exec", "crevafactos-local-postgres-1", "psql", "-U", "crevafactos", "-d", "crevafactos_local", "-t", "-A", "-c", sql],
    { encoding: "utf8" },
  );
  if (result.status !== 0) fail(result.stderr || result.stdout);
  const value = Number(result.stdout.trim());
  if (!Number.isFinite(value)) fail(`Expected numeric db count, got: ${result.stdout.trim()}`);
  return value;
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

console.log("Running INVENTORY-RISK-FOUNDATION-1 security/idempotency verifier.");

const endpoints = readRequired("apps/api/Material/MaterialAvailabilityRiskEndpointRouteBuilderExtensions.cs");
const repository = readRequired("packages/CrevaFactOS.Persistence/Material/MaterialRequirementRepository.cs");
const riskRepository = readRequired("packages/CrevaFactOS.Persistence/Material/MaterialAvailabilityRiskRepository.cs");
const constants = readRequired("packages/CrevaFactOS.Persistence/Material/MaterialAvailabilityRiskConstants.cs");
const calculator = readRequired("packages/CrevaFactOS.Persistence/Material/MaterialAvailabilityRiskCalculator.cs");

assert("workspace endpoint registered", endpoints.includes('MapGet("/workspace"'));
assert("evaluate endpoint registered", endpoints.includes('MapPost("/evaluate"'));
assert("assessment detail endpoint registered", endpoints.includes('MapGet("/{assessmentId:guid}"'));
assert("read access resolver", endpoints.includes("ResolveReadAccessAsync"));
assert("write access resolver", endpoints.includes("ResolveWriteAccessAsync"));
assert("manage permission check", endpoints.includes("MaterialPermissionKeys.Manage"));
assert("erp read permission check", endpoints.includes("HasErpReadAccessAsync"));
assert("repository evaluate availability", repository.includes("EvaluateAvailabilityRiskAsync"));
assert("repository idempotency key handling", repository.includes("IdempotencyKey"));
assert("repository assessment fingerprint replay", repository.includes("InputFingerprint"));
assert("risk repository workspace", riskRepository.includes("GetWorkspaceAsync"));
assert("rule set version constant", constants.includes("INVENTORY-RISK-V1"));
assert("no auto action disclaimer", constants.includes("otomatik stok, satın alma veya üretim işlemi yapmaz"));
assert("calculator no fabricated shortage", calculator.includes("MaterialAvailabilityRiskStatuses.InsufficientData"));
assert("calculator does not mutate erp", !calculator.includes("SaveChangesAsync"));

const ready = await fetch(`${BASE}/health/ready`);
assert("API ready", ready.status === 200, `status=${ready.status}`);

const workspace = await liveRequest("GET", `/api/materials/availability-risks/workspace?facilityId=${FACILITY}`);
assert("authorized workspace succeeds", workspace.status === 200);
assert("workspace capabilities authoritative", workspace.json?.capabilities?.canRead === true);
assert("workspace assessments array", Array.isArray(workspace.json?.assessments));
assert("pilot shortage assessment present", workspace.json?.assessments?.some((row) => row.id === SHORTAGE_ASSESSMENT));
assert("pilot insufficient assessment present", workspace.json?.assessments?.some((row) => row.id === INSUFFICIENT_ASSESSMENT));
assert("automation disclaimer in workspace", typeof workspace.json?.automationDisclaimer === "string" && workspace.json.automationDisclaimer.includes("otomatik stok"));

const unauthWorkspace = await liveRequest(
  "GET",
  `/api/materials/availability-risks/workspace?facilityId=${FACILITY}`,
  null,
  {},
);
assert("unauthenticated workspace rejected", unauthWorkspace.status === 401);

const wrongFacility = await liveRequest("GET", `/api/materials/availability-risks/workspace?facilityId=${UNAUTH_FACILITY}`);
assert("wrong facility workspace rejected", wrongFacility.status === 403);

const shortageDetail = await liveRequest("GET", `/api/materials/availability-risks/${SHORTAGE_ASSESSMENT}?facilityId=${FACILITY}`);
assert("shortage assessment detail succeeds", shortageDetail.status === 200);
assert("shortage status", shortageDetail.json?.status === "shortage");
assert("shortage has real erp quantities", shortageDetail.json?.erpAvailableQuantity === 400 && shortageDetail.json?.erpReservedQuantity === 50);

const insufficientDetail = await liveRequest("GET", `/api/materials/availability-risks/${INSUFFICIENT_ASSESSMENT}?facilityId=${FACILITY}`);
assert("insufficient assessment detail succeeds", insufficientDetail.status === 200);
assert("insufficient-data status", insufficientDetail.json?.status === "insufficient-data");

const readonlyEvaluate = await liveRequest(
  "POST",
  `/api/materials/availability-risks/evaluate?facilityId=${FACILITY}`,
  {
    materialId: MATERIAL_PP,
    requirementProfileId: PROFILE_PP,
    idempotencyKey: `${IDEM_KEY}-readonly`,
  },
  actorHeaders(READONLY),
);
assert("read-only cannot evaluate", readonlyEvaluate.status === 403);

const erpBalanceCountBefore = dbCount("SELECT COUNT(*) FROM erp_inventory_balances");
const aiTaskCountBefore = dbCount("SELECT COUNT(*) FROM ai_tasks");

spawnSync(
  "docker",
  [
    "exec",
    "crevafactos-local-postgres-1",
    "psql",
    "-U",
    "crevafactos",
    "-d",
    "crevafactos_local",
    "-c",
    `DELETE FROM material_requirement_assessments WHERE assessment_type = 'availability-risk' AND material_id = '${MATERIAL_PP}' AND id NOT IN ('${SHORTAGE_ASSESSMENT}', '${SUFFICIENT_ASSESSMENT}');`,
  ],
  { encoding: "utf8" },
);

const firstEvaluate = await liveRequest(
  "POST",
  `/api/materials/availability-risks/evaluate?facilityId=${FACILITY}`,
  {
    materialId: MATERIAL_PP,
    requirementProfileId: PROFILE_PP_EXCEEDS_REGRIND,
    idempotencyKey: IDEM_KEY,
  },
);
assert("manager can evaluate availability risk", firstEvaluate.status === 201 || firstEvaluate.status === 200, firstEvaluate.text?.slice(0, 200));
assert("evaluation id returned", Boolean(firstEvaluate.json?.id));

const secondEvaluate = await liveRequest(
  "POST",
  `/api/materials/availability-risks/evaluate?facilityId=${FACILITY}`,
  {
    materialId: MATERIAL_PP,
    requirementProfileId: PROFILE_PP_EXCEEDS_REGRIND,
    idempotencyKey: IDEM_KEY,
  },
);
assert("idempotent replay succeeds", secondEvaluate.status === 201 || secondEvaluate.status === 200);
assert("same assessment id on replay", secondEvaluate.json?.id === firstEvaluate.json?.id);
assert("idempotent replay flag", secondEvaluate.json?.idempotentReplay === true);

const idemConflict = await liveRequest(
  "POST",
  `/api/materials/availability-risks/evaluate?facilityId=${FACILITY}`,
  {
    materialId: MATERIAL_PP,
    requirementProfileId: PROFILE_PP,
    idempotencyKey: IDEM_KEY,
  },
);
assert("incompatible idempotency-key reuse conflicts", idemConflict.status === 409, `status=${idemConflict.status}`);

const erpBalanceCountAfter = dbCount("SELECT COUNT(*) FROM erp_inventory_balances");
const aiTaskCountAfter = dbCount("SELECT COUNT(*) FROM ai_tasks");
assert("evaluate does not write erp_inventory_balances", erpBalanceCountAfter === erpBalanceCountBefore);
assert("evaluate does not write ai_tasks", aiTaskCountAfter === aiTaskCountBefore);

const evaluatedDetail = await liveRequest("GET", `/api/materials/availability-risks/${firstEvaluate.json.id}?facilityId=${FACILITY}`);
assert("evaluated assessment readable", evaluatedDetail.status === 200);
assert("evaluated assessment rule set", evaluatedDetail.json?.ruleSetVersion === "INVENTORY-RISK-V1");

console.log("INVENTORY-RISK-FOUNDATION-1 security/idempotency verification passed.");
