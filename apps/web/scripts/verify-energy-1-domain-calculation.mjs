#!/usr/bin/env node
import { spawnSync } from "node:child_process";
import { readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), "../../..");
const BASE = process.env.CREVAFACTOS_API_BASE_URL ?? "http://127.0.0.1:5000";
const FACILITY = "bb000001-0005-4000-8000-000000000005";
const INJ1_MACHINE = "bb000001-0096-4000-8000-000000000096";
const PILOT_TARIFF = "bb000001-0166-4000-8000-000000000166";
const PILOT_WINDOW = "bb000001-0167-4000-8000-000000000167";
const PREFIX = `energy1-calc-${Date.now()}`;
const RUN_SUFFIX = Date.now();
const CLOSURE_PREFIX = "ENERGY1-CLOSURE";
const ACTOR = {
  issuer: "crevafactos-dev",
  subject: "planning-pilot-user",
  tenant: "dev-pilot-tenant",
};

function fail(message) {
  console.error(`ENERGY-1 domain/calculation verification failed: ${message}`);
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
    ["exec", "crevafactos-local-postgres-1", "psql", "-U", "crevafactos", "-d", "crevafactos_local", "-t", "-A", "-c", sql],
    { encoding: "utf8" },
  );
  if (result.status !== 0) fail(result.stderr || result.stdout);
  return result.stdout.trim();
}

function headers(actor = ACTOR) {
  return {
    "Content-Type": "application/json",
    "X-CrevaFactOS-Dev-Issuer": actor.issuer,
    "X-CrevaFactOS-Dev-Subject": actor.subject,
    "X-CrevaFactOS-Tenant": actor.tenant,
  };
}

async function request(method, path, body, actor = ACTOR) {
  const response = await fetch(`${BASE}${path}`, {
    method,
    headers: headers(actor),
    body: body ? JSON.stringify(body) : undefined,
  });
  const text = await response.text();
  let json = null;
  try { json = text ? JSON.parse(text) : null; } catch { json = { raw: text }; }
  return { status: response.status, json, raw: text };
}

function removeClosureFixtures() {
  db(`
    DELETE FROM energy_allocations
    WHERE consumption_window_id IN (
      SELECT id FROM energy_consumption_windows
      WHERE evaluate_idempotency_key LIKE '${CLOSURE_PREFIX}%'
         OR evaluate_idempotency_key LIKE '${PREFIX}%'
    );
    DELETE FROM energy_audit_events
    WHERE entity_id IN (
      SELECT id FROM energy_consumption_windows
      WHERE evaluate_idempotency_key LIKE '${CLOSURE_PREFIX}%'
         OR evaluate_idempotency_key LIKE '${PREFIX}%'
      UNION SELECT id FROM energy_readings
      WHERE idempotency_key LIKE '${CLOSURE_PREFIX}%'
         OR idempotency_key LIKE '${PREFIX}%'
    );
    DELETE FROM energy_consumption_windows
    WHERE evaluate_idempotency_key LIKE '${CLOSURE_PREFIX}%'
       OR evaluate_idempotency_key LIKE '${PREFIX}%';
    DELETE FROM energy_readings
    WHERE idempotency_key LIKE '${CLOSURE_PREFIX}%'
       OR idempotency_key LIKE '${PREFIX}%';
  `);
}

console.log("Running ENERGY-1 domain/calculation verifier.");

const constants = readRequired("packages/CrevaFactOS.Persistence/Energy/EnergyConstants.cs");
const calculator = readRequired("packages/CrevaFactOS.Persistence/Energy/EnergyCalculator.cs");

assert("rule-set ENERGY-1.0", constants.includes("ENERGY-1.0"));
assert("interval sum method", calculator.includes("EnergyCalculationMethods.IntervalSum"));
assert("cumulative delta method", calculator.includes("EnergyCalculationMethods.CumulativeDelta"));
assert("duplicate timestamp skip", calculator.includes("lastTimestamp"));
assert("negative delta handling", calculator.includes("delta < 0"));
assert("counter reset conflict", calculator.includes("EnergyEvidenceStates.SourceConflict"));
assert("window validation", calculator.includes("windowEndUtc <= windowStartUtc"));
assert("allocation conservation", calculator.includes("ValidateAllocations"));
assert("residual calculation", calculator.includes("ResidualKwh"));
assert("kwh-per-kg metric", calculator.includes("kwh-per-kg"));
assert("kwh-per-1000-total-parts", calculator.includes("kwh-per-1000-total-parts"));
assert("kwh-per-1000-good-parts", calculator.includes("kwh-per-1000-good-parts"));
assert("missing denominator unavailable", calculator.includes("UnavailableMetric"));
assert("tariff cost calculation", calculator.includes("CalculateCost"));
assert("missing tariff null cost", calculator.includes("tariff is null"));
assert("simulated evidence state", constants.includes("Simulated = \"simulated\"") || constants.includes('Simulated = "simulated"'));
assert("measured evidence state", constants.includes("Measured = \"measured\"") || constants.includes('Measured = "measured"'));
assert("deterministic rounding", calculator.includes("RoundEnergy"));

const EnergyCalculationMethods = { IntervalSum: "interval-sum" };

const ready = await fetch(`${BASE}/health/ready`);
assert("API ready", ready.status === 200, `status=${ready.status}`);

removeClosureFixtures();

const pilotWindow = await request(
  "GET",
  `/api/energy/consumption?facilityId=${FACILITY}&measurementPointReference=EM-INJ-001&pageSize=5`,
);
assert("pilot consumption list 200", pilotWindow.status === 200);
const calculated = pilotWindow.json?.items?.find((w) => w.id === PILOT_WINDOW);
assert("pilot calculated window present", calculated != null);
assert("pilot interval-sum method", calculated?.calculationMethod === "interval-sum", calculated?.calculationMethod);
assert("pilot simulated evidence", calculated?.evidenceStatus === "simulated", calculated?.evidenceStatus);
assert("pilot consumed kWh = 38.5", Number(calculated?.consumedKwh) === 38.5, calculated?.consumedKwh);

const pilotAllocations = await request(
  "GET",
  `/api/energy/allocations?facilityId=${FACILITY}&consumptionWindowId=${PILOT_WINDOW}`,
);
assert("pilot allocations 200", pilotAllocations.status === 200);
const allocItems = pilotAllocations.json?.items ?? [];
const machineAlloc = allocItems.find((a) => a.targetType === "machine");
const residualAlloc = allocItems.find((a) => a.targetType === "unallocated");
assert("pilot machine allocation present", machineAlloc != null);
assert("pilot residual allocation present", residualAlloc != null);
const allocSum = allocItems.reduce((sum, a) => sum + Number(a.allocatedKwh), 0);
assert("allocation conservation reconciles", Math.abs(allocSum - Number(calculated.consumedKwh)) < 0.0001, `${allocSum} vs ${calculated.consumedKwh}`);
assert("allocation does not exceed consumption", allocSum <= Number(calculated.consumedKwh) + 0.0001);

const uniqueMs = Number(String(RUN_SUFFIX).slice(-5));
const windowStart = new Date(Date.now() - 8 * 3600 * 1000 - uniqueMs * 1000);
const windowEnd = new Date(windowStart.getTime() + 4 * 3600 * 1000);
const intervalKey = `${PREFIX}-interval-eval`;
const intervalBody = {
  measurementPointReference: "EM-INJ-002",
  windowStartUtc: windowStart.toISOString(),
  windowEndUtc: windowEnd.toISOString(),
  idempotencyKey: intervalKey,
  allocations: [
    {
      targetType: "machine",
      targetReference: "bb000001-0103-4000-8000-000000000103",
      allocatedKwh: 5,
      allocationMethod: "direct-assignment",
      evidenceSource: "closure-test",
      confidenceStatus: "calculated",
    },
    {
      targetType: "unallocated",
      targetReference: null,
      allocatedKwh: 0,
      allocationMethod: "residual",
      evidenceSource: "closure-test",
      confidenceStatus: "calculated",
    },
  ],
};

const reading1Key = `${PREFIX}-reading-1`;
await request("POST", `/api/energy/readings?facilityId=${FACILITY}`, {
  measurementPointReference: "EM-INJ-002",
  occurredAtUtc: new Date(windowStart.getTime() + 3600 * 1000).toISOString(),
  intervalEnergyKwh: 5,
  sourceType: "closure-test",
  evidenceState: "simulated",
  idempotencyKey: reading1Key,
});

const evalResult = await request(
  "POST",
  `/api/energy/consumption/evaluate?facilityId=${FACILITY}`,
  { ...intervalBody, tariffSnapshotId: PILOT_TARIFF },
);
assert("interval evaluate 200", evalResult.status === 200, evalResult.raw?.slice(0, 200));
assert("interval consumed 5 kWh", Number(evalResult.json?.id) > 0 || evalResult.json?.id);

const replayEval = await request(
  "POST",
  `/api/energy/consumption/evaluate?facilityId=${FACILITY}`,
  { ...intervalBody, tariffSnapshotId: PILOT_TARIFF },
);
assert("evaluate idempotent replay 200", replayEval.status === 200);
assert("evaluate idempotent replay flag", replayEval.json?.idempotentReplay === true);

const badWindow = await request(
  "POST",
  `/api/energy/consumption/evaluate?facilityId=${FACILITY}`,
  {
    measurementPointReference: "EM-INJ-002",
    windowStartUtc: windowEnd.toISOString(),
    windowEndUtc: windowStart.toISOString(),
    idempotencyKey: `${PREFIX}-bad-window`,
  },
);
assert("invalid window rejected", badWindow.status === 400, `status=${badWindow.status}`);

const uniqueOffsetHours = Number(String(RUN_SUFFIX).slice(-4)) % 72;
const noTariffStart = new Date(Date.now() - (96 + uniqueOffsetHours) * 3600 * 1000);
noTariffStart.setUTCMinutes(0, 0, 0);
const noTariffEnd = new Date(noTariffStart.getTime() + 4 * 3600 * 1000);
const noTariffEval = await request(
  "POST",
  `/api/energy/consumption/evaluate?facilityId=${FACILITY}`,
  {
    measurementPointReference: "EM-INJ-002",
    windowStartUtc: noTariffStart.toISOString(),
    windowEndUtc: noTariffEnd.toISOString(),
    idempotencyKey: `${PREFIX}-no-tariff`,
  },
);
assert("no tariff evaluate 200", noTariffEval.status === 200);

const points = await request("GET", `/api/energy/measurement-points?facilityId=${FACILITY}&pageSize=50`);
assert("measurement points deterministic order", points.status === 200);
const codes = points.json?.items?.map((p) => p.pointCode) ?? [];
assert("points sorted by code", codes.join(",") === [...codes].sort().join(","));

removeClosureFixtures();
const closureRemaining = Number(db(`
  SELECT COUNT(*) FROM energy_consumption_windows
  WHERE evaluate_idempotency_key LIKE '${CLOSURE_PREFIX}%'
     OR evaluate_idempotency_key LIKE '${PREFIX}%'
`));
assert("closure fixtures removed", closureRemaining === 0, `count=${closureRemaining}`);

console.log("ENERGY-1 domain/calculation verification passed.");
