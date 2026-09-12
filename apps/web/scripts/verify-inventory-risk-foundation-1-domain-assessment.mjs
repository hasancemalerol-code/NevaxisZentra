#!/usr/bin/env node
import { readFileSync } from "node:fs";
import { randomUUID } from "node:crypto";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), "../../..");
const BASE = process.env.NEVAXIS_FACTOS_API_BASE_URL ?? "http://127.0.0.1:5000";
const FACILITY = "bb000001-0005-4000-8000-000000000005";
const MATERIAL_PP = "bb000001-00a2-4000-8000-0000000000a2";
const MATERIAL_ABS = "bb000001-00a4-4000-8000-0000000000a4";
const PROFILE_PP = "bb000001-00ae-4000-8000-0000000000ae";
const PROFILE_ABS_PARTIAL = "bb000001-00af-4000-8000-0000000000af";
const RUN_SUFFIX = randomUUID();
const PREFIX = `INV-RISK-DOMAIN-${RUN_SUFFIX}`;
const MANAGER = { issuer: "crevafactos-dev", subject: "production-pilot-operator", tenant: "dev-pilot-tenant" };

function fail(message) {
  console.error(`INVENTORY-RISK-FOUNDATION-1 domain assessment verification failed: ${message}`);
  process.exit(1);
}

function assert(label, ok, detail = "") {
  if (!ok) fail(`${label}${detail ? `: ${detail}` : ""}`);
  console.log(`✓ ${label}`);
}

function readRequired(relativePath) {
  return readFileSync(resolve(ROOT, relativePath), "utf8").replaceAll("\r\n", "\n");
}

function headers() {
  return {
    "Content-Type": "application/json",
    "X-crevafactos-dev-Issuer": MANAGER.issuer,
    "X-crevafactos-dev-Subject": MANAGER.subject,
    "X-NevaxisFactOS-Tenant": MANAGER.tenant,
  };
}

async function request(method, path, body) {
  const response = await fetch(`${BASE}${path}`, {
    method,
    headers: headers(),
    body: body ? JSON.stringify(body) : undefined,
  });
  const text = await response.text();
  let json = null;
  try { json = text ? JSON.parse(text) : null; } catch { json = { raw: text }; }
  return { status: response.status, json, text };
}

function isNullishQuantity(value) {
  return value === null || value === undefined;
}

console.log("Running INVENTORY-RISK-FOUNDATION-1 domain assessment verifier.");

const calculator = readRequired("packages/NevaxisFactOS.Persistence/Material/MaterialAvailabilityRiskCalculator.cs");
const constants = readRequired("packages/NevaxisFactOS.Persistence/Material/MaterialAvailabilityRiskConstants.cs");
const seed = readRequired("packages/NevaxisFactOS.Persistence/Development/DevelopmentPilotMaterialAvailabilityRiskSeed.cs");
const erpSeed = readRequired("packages/NevaxisFactOS.Persistence/Development/DevelopmentPilotErpInventorySeed.cs");

assert("calculator assess method", calculator.includes("AssessAvailabilityRisk"));
assert("calculator shortage branch", calculator.includes("MaterialAvailabilityRiskStatuses.Shortage"));
assert("calculator insufficient branch", calculator.includes("MaterialAvailabilityRiskStatuses.InsufficientData"));
assert("calculator converts grams to erp unit", calculator.includes("ConvertGramsToUnit"));
assert("rule set version", constants.includes("INVENTORY-RISK-V1"));
assert("pilot shortage seed", seed.includes("MaterialPilotAvailabilityRiskShortageAssessmentId"));
assert("pilot insufficient seed", seed.includes("MaterialPilotAvailabilityRiskInsufficientAssessmentId"));
assert("pilot erp balance seed", erpSeed.includes("QuantityAvailable = 400m") && erpSeed.includes("QuantityReserved = 50m"));

const ready = await fetch(`${BASE}/health/ready`);
assert("API ready", ready.status === 200, `status=${ready.status}`);

const ppEvaluate = await request(
  "POST",
  `/api/materials/availability-risks/evaluate?facilityId=${FACILITY}`,
  {
    materialId: MATERIAL_PP,
    requirementProfileId: PROFILE_PP,
    idempotencyKey: `${PREFIX}-pp-shortage`,
  },
);
assert("PP evaluate succeeds", ppEvaluate.status === 201 || ppEvaluate.status === 200, ppEvaluate.text?.slice(0, 200));

const ppDetail = await request(
  "GET",
  `/api/materials/availability-risks/${ppEvaluate.json.id}?facilityId=${FACILITY}`,
);
assert("PP detail HTTP 200", ppDetail.status === 200);
assert("PP shortage status", ppDetail.json?.status === "shortage");
assert("PP rule set version", ppDetail.json?.ruleSetVersion === "INVENTORY-RISK-V1");
assert("PP required quantity present", (ppDetail.json?.requiredQuantityGrams ?? 0) > 0);
assert("PP erp available quantity", ppDetail.json?.erpAvailableQuantity === 400);
assert("PP erp reserved quantity", ppDetail.json?.erpReservedQuantity === 50);
assert("PP erp unit kg", ppDetail.json?.erpUnit === "kg");
assert("PP available after reservation", ppDetail.json?.availableAfterReservationQuantity === 350);
assert("PP shortage quantity positive", (ppDetail.json?.shortageQuantity ?? 0) > 0);
assert("PP source freshness present", typeof ppDetail.json?.sourceFreshness === "string" && ppDetail.json.sourceFreshness.length > 0);
assert("PP shortage driver present", Array.isArray(ppDetail.json?.drivers) && ppDetail.json.drivers.some((d) => d.code === "shortage-detected"));
assert("PP erp evidence present", Array.isArray(ppDetail.json?.evidence) && ppDetail.json.evidence.some((e) => e.field === "erpAvailableQuantity" && !e.isMissing));

const absEvaluate = await request(
  "POST",
  `/api/materials/availability-risks/evaluate?facilityId=${FACILITY}`,
  {
    materialId: MATERIAL_ABS,
    requirementProfileId: PROFILE_ABS_PARTIAL,
    idempotencyKey: `${PREFIX}-abs-insufficient`,
  },
);
assert("ABS evaluate succeeds", absEvaluate.status === 201 || absEvaluate.status === 200, absEvaluate.text?.slice(0, 200));

const absDetail = await request(
  "GET",
  `/api/materials/availability-risks/${absEvaluate.json.id}?facilityId=${FACILITY}`,
);
assert("ABS detail HTTP 200", absDetail.status === 200);
assert("ABS insufficient-data status", absDetail.json?.status === "insufficient-data");
assert("ABS rule set version", absDetail.json?.ruleSetVersion === "INVENTORY-RISK-V1");
assert("ABS no fabricated erp available", isNullishQuantity(absDetail.json?.erpAvailableQuantity));
assert("ABS no fabricated erp reserved", isNullishQuantity(absDetail.json?.erpReservedQuantity));
assert("ABS no fabricated shortage", isNullishQuantity(absDetail.json?.shortageQuantity));
assert("ABS no fabricated available after reservation", isNullishQuantity(absDetail.json?.availableAfterReservationQuantity));
assert("ABS requirement incomplete driver", Array.isArray(absDetail.json?.drivers) && absDetail.json.drivers.some((d) => d.code === "requirement-incomplete"));
assert("ABS no shortage driver", !absDetail.json?.drivers?.some((d) => d.code === "shortage-detected"));

console.log("INVENTORY-RISK-FOUNDATION-1 domain assessment verification passed.");
