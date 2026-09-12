#!/usr/bin/env node
import { spawnSync } from "node:child_process";
import { readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), "../../..");
const BASE = process.env.NEVAXIS_FACTOS_API_BASE_URL ?? "http://localhost:5000";
const TENANT = "bb000001-0001-4000-8000-000000000001";
const FACILITY = "bb000001-0005-4000-8000-000000000005";
const MACHINE = "bb000001-0096-4000-8000-000000000096";
const GATEWAY = "bb000001-0094-4000-8000-000000000094";
const PILOT_WINDOW = "bb000001-00c0-4000-8000-0000000000c0";
const NOW_ISO = new Date().toISOString();
const CLOSURE_QUALITY_ORDER = "ORD-PILOT-PLAN-003";
const CLOSURE_PREFIX = "POEE2-CLOSURE";
let closureQualityOrderSnapshot = null;
const CLOSURE_RUN = Date.now();
const ACTOR = {
  issuer: "crevafactos-dev",
  subject: "production-pilot-operator",
  tenant: "dev-pilot-tenant",
};

const LOSS_CATEGORIES = [
  "availability-loss",
  "performance-loss",
  "quality-loss",
  "insufficient-data",
  "source-conflict",
];

function fail(message) {
  console.error(`PROCESS-OEE-2 loss intelligence verification failed: ${message}`);
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
    { encoding: "utf8", maxBuffer: 20 * 1024 * 1024 },
  );
  if (result.status !== 0) {
    fail(result.stderr?.trim() || result.stdout?.trim() || `psql failed with status ${result.status}`);
  }
  return result.stdout.trim();
}

function headers(actor = ACTOR) {
  return {
    "Content-Type": "application/json",
    "X-crevafactos-dev-Issuer": actor.issuer,
    "X-crevafactos-dev-Subject": actor.subject,
    "X-NevaxisFactOS-Tenant": actor.tenant,
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
  try {
    json = text ? JSON.parse(text) : null;
  } catch {
    json = { raw: text };
  }
  return { status: response.status, json, raw: text };
}

function insertInBatches(tableSqlPrefix, rows, batchSize = 40) {
  for (let offset = 0; offset < rows.length; offset += batchSize) {
    const batch = rows.slice(offset, offset + batchSize);
    db(`${tableSqlPrefix} VALUES ${batch.join(",")};`);
  }
}

function seedClosureFixtures() {
  const qualitySnapshot = db(`
    SELECT good_quantity, scrap_quantity
    FROM production_orders
    WHERE reference_code = '${CLOSURE_QUALITY_ORDER}';
  `);
  const [goodQuantity, scrapQuantity] = qualitySnapshot.split("|");
  closureQualityOrderSnapshot = {
    goodQuantity: Number(goodQuantity),
    scrapQuantity: Number(scrapQuantity),
  };

  db(`
    DELETE FROM machine_observed_cycles
    WHERE idempotency_key LIKE '${CLOSURE_PREFIX}%';
    DELETE FROM machine_downtime_episodes
    WHERE start_idempotency_key LIKE '${CLOSURE_PREFIX}%';
  `);

  // Availability window 2026-08-12T06:00-10:00: overlapping intervals + one open-ended event.
  db(`
    INSERT INTO machine_downtime_episodes (
      id, tenant_id, facility_id, machine_id, gateway_id,
      started_at_source_utc, ended_at_source_utc,
      started_at_received_utc, ended_at_received_utc,
      duration_ms, quality, start_idempotency_key, end_idempotency_key,
      is_closed, is_micro_stop, is_out_of_order, created_at_utc, updated_at_utc
    ) VALUES
    (
      'e2a00001-0001-4000-8000-000000000001', '${TENANT}', '${FACILITY}', '${MACHINE}', '${GATEWAY}',
      '2026-08-12T06:30:00Z', '2026-08-12T07:30:00Z',
      '${NOW_ISO}', '${NOW_ISO}',
      3600000, 'good', '${CLOSURE_PREFIX}-DT-OVERLAP-A', '${CLOSURE_PREFIX}-DT-OVERLAP-A-END',
      TRUE, FALSE, FALSE, '${NOW_ISO}', '${NOW_ISO}'
    ),
    (
      'e2a00001-0002-4000-8000-000000000002', '${TENANT}', '${FACILITY}', '${MACHINE}', '${GATEWAY}',
      '2026-08-12T07:00:00Z', '2026-08-12T08:00:00Z',
      '${NOW_ISO}', '${NOW_ISO}',
      3600000, 'good', '${CLOSURE_PREFIX}-DT-OVERLAP-B', '${CLOSURE_PREFIX}-DT-OVERLAP-B-END',
      TRUE, FALSE, FALSE, '${NOW_ISO}', '${NOW_ISO}'
    ),
    (
      'e2a00001-0003-4000-8000-000000000003', '${TENANT}', '${FACILITY}', '${MACHINE}', '${GATEWAY}',
      '2026-08-12T09:00:00Z', NULL,
      '${NOW_ISO}', NULL,
      NULL, 'good', '${CLOSURE_PREFIX}-DT-OPEN-ENDED', NULL,
      FALSE, FALSE, FALSE, '${NOW_ISO}', '${NOW_ISO}'
    );
  `);

  // Performance window 2026-08-12T11:00-15:00: observed cycles (slow vs 45s ideal on 14400s operating).
  const perfCycleCount = 80;
  const perfWindowSeconds = 14_400;
  const perfSpacingSeconds = Math.floor(perfWindowSeconds / perfCycleCount);
  const perfCycles = [];
  for (let index = 0; index < perfCycleCount; index += 1) {
    const totalSeconds = index * perfSpacingSeconds;
    const hour = 11 + Math.floor(totalSeconds / 3600);
    const minute = Math.floor((totalSeconds % 3600) / 60);
    const second = totalSeconds % 60;
    const cycleId = `e2a00002-${String(index + 1).padStart(4, "0")}-4000-8000-000000000001`;
    perfCycles.push(`
      (
        '${cycleId}', '${TENANT}', '${FACILITY}', '${MACHINE}', '${GATEWAY}',
        '${CLOSURE_PREFIX}-CYCLE-${index}', 72000, NULL, 'good',
        '2026-08-12T${String(hour).padStart(2, "0")}:${String(minute).padStart(2, "0")}:${String(second).padStart(2, "0")}Z',
        '${NOW_ISO}', ${index + 1}, '${CLOSURE_PREFIX}-CYCLE-${index}', FALSE, FALSE, '${NOW_ISO}'
      )`);
  }
  insertInBatches(`
    INSERT INTO machine_observed_cycles (
      id, tenant_id, facility_id, machine_id, gateway_id,
      cycle_identity, cycle_duration_ms, observed_counter, quality,
      source_timestamp_utc, received_at_utc, source_sequence, idempotency_key,
      is_duplicate, is_out_of_order, created_at_utc
    )
  `, perfCycles);

  // Mixed window 2026-08-13T06:00-14:00: downtime + cycles.
  db(`
    INSERT INTO machine_downtime_episodes (
      id, tenant_id, facility_id, machine_id, gateway_id,
      started_at_source_utc, ended_at_source_utc,
      started_at_received_utc, ended_at_received_utc,
      duration_ms, quality, start_idempotency_key, end_idempotency_key,
      is_closed, is_micro_stop, is_out_of_order, created_at_utc, updated_at_utc
    ) VALUES
    (
      'e2a00003-0001-4000-8000-000000000001', '${TENANT}', '${FACILITY}', '${MACHINE}', '${GATEWAY}',
      '2026-08-13T07:00:00Z', '2026-08-13T09:00:00Z',
      '${NOW_ISO}', '${NOW_ISO}',
      7200000, 'good', '${CLOSURE_PREFIX}-DT-MIXED', '${CLOSURE_PREFIX}-DT-MIXED-END',
      TRUE, FALSE, FALSE, '${NOW_ISO}', '${NOW_ISO}'
    );
  `);

  const mixedCycleCount = 120;
  const mixedWindowSeconds = 28_800;
  const mixedSpacingSeconds = Math.floor(mixedWindowSeconds / mixedCycleCount);
  const mixedCycles = [];
  for (let index = 0; index < mixedCycleCount; index += 1) {
    const totalSeconds = index * mixedSpacingSeconds;
    const hour = 6 + Math.floor(totalSeconds / 3600);
    const minute = Math.floor((totalSeconds % 3600) / 60);
    const second = totalSeconds % 60;
    const cycleId = `e2a00004-${String(index + 1).padStart(4, "0")}-4000-8000-000000000001`;
    mixedCycles.push(`
      (
        '${cycleId}', '${TENANT}', '${FACILITY}', '${MACHINE}', '${GATEWAY}',
        '${CLOSURE_PREFIX}-MIXED-CYCLE-${index}', 54000, NULL, 'good',
        '2026-08-13T${String(hour).padStart(2, "0")}:${String(minute).padStart(2, "0")}:${String(second).padStart(2, "0")}Z',
        '${NOW_ISO}', ${1000 + index}, '${CLOSURE_PREFIX}-MIXED-CYCLE-${index}', FALSE, FALSE, '${NOW_ISO}'
      )`);
  }
  insertInBatches(`
    INSERT INTO machine_observed_cycles (
      id, tenant_id, facility_id, machine_id, gateway_id,
      cycle_identity, cycle_duration_ms, observed_counter, quality,
      source_timestamp_utc, received_at_utc, source_sequence, idempotency_key,
      is_duplicate, is_out_of_order, created_at_utc
    )
  `, mixedCycles);

  db(`
    UPDATE production_orders
    SET good_quantity = 950,
        scrap_quantity = 50,
        updated_at_utc = '${NOW_ISO}'
    WHERE reference_code = '${CLOSURE_QUALITY_ORDER}';
  `);
}

function cleanupClosureFixtures() {
  if (closureQualityOrderSnapshot) {
    db(`
      UPDATE production_orders
      SET good_quantity = ${closureQualityOrderSnapshot.goodQuantity},
          scrap_quantity = ${closureQualityOrderSnapshot.scrapQuantity},
          updated_at_utc = '${NOW_ISO}'
      WHERE reference_code = '${CLOSURE_QUALITY_ORDER}';
    `);
    closureQualityOrderSnapshot = null;
  }
  db(`
    DELETE FROM machine_observed_cycles
    WHERE idempotency_key LIKE '${CLOSURE_PREFIX}%';
    DELETE FROM machine_downtime_episodes
    WHERE start_idempotency_key LIKE '${CLOSURE_PREFIX}%';
  `);
  db(`
    DELETE FROM process_oee_shift_comparison_rows
    WHERE batch_id IN (
      SELECT id FROM process_oee_shift_comparison_batches
      WHERE idempotency_key LIKE '${CLOSURE_PREFIX}%'
    );
    DELETE FROM process_oee_shift_comparison_batches
    WHERE idempotency_key LIKE '${CLOSURE_PREFIX}%';
    DELETE FROM process_oee_downtime_breakdown_rows
    WHERE assessment_id IN (
      SELECT id FROM process_oee_assessments
      WHERE idempotency_key LIKE '${CLOSURE_PREFIX}%'
    );
    DELETE FROM process_oee_stability_drivers
    WHERE assessment_id IN (
      SELECT id FROM process_oee_assessments
      WHERE idempotency_key LIKE '${CLOSURE_PREFIX}%'
    );
    DELETE FROM process_oee_stability_assessments
    WHERE assessment_id IN (
      SELECT id FROM process_oee_assessments
      WHERE idempotency_key LIKE '${CLOSURE_PREFIX}%'
    );
  `);
  db(`
    DELETE FROM process_oee_loss_drivers
    WHERE assessment_id IN (
      SELECT id FROM process_oee_assessments
      WHERE idempotency_key LIKE '${CLOSURE_PREFIX}%'
         OR evaluation_window_id IN (
           SELECT id FROM process_oee_evaluation_windows
           WHERE create_idempotency_key LIKE '${CLOSURE_PREFIX}%'
         )
    )
  `);
  db(`
    DELETE FROM process_oee_assessments
    WHERE idempotency_key LIKE '${CLOSURE_PREFIX}%'
       OR evaluation_window_id IN (
         SELECT id FROM process_oee_evaluation_windows
         WHERE create_idempotency_key LIKE '${CLOSURE_PREFIX}%'
       )
  `);
  db(`
    DELETE FROM process_oee_evaluation_windows
    WHERE create_idempotency_key LIKE '${CLOSURE_PREFIX}%'
  `);
  db(`
    DELETE FROM process_oee_evaluation_windows w
    WHERE w.source_reference = 'bounded-evaluation'
      AND NOT EXISTS (
        SELECT 1 FROM process_oee_assessments a WHERE a.evaluation_window_id = w.id
      )
      AND w.id NOT IN (
        'bb000001-00c0-4000-8000-0000000000c0',
        'bb000001-00c1-4000-8000-0000000000c1',
        'bb000001-00c2-4000-8000-0000000000c2'
      )
  `);
}

function closureWindowCount() {
  return Number(db(`
    SELECT COUNT(*) FROM process_oee_evaluation_windows
    WHERE create_idempotency_key LIKE '${CLOSURE_PREFIX}%'
  `));
}

function closureAssessmentCount() {
  return Number(db(`
    SELECT COUNT(*) FROM process_oee_assessments
    WHERE idempotency_key LIKE '${CLOSURE_PREFIX}%'
  `));
}

function lossDriverCountForAssessment(assessmentId) {
  return Number(db(`
    SELECT COUNT(*) FROM process_oee_loss_drivers
    WHERE assessment_id = '${assessmentId}'
  `));
}

function duplicateReasonCodeCount(assessmentIds) {
  const idList = assessmentIds.map((id) => `'${id}'`).join(", ");
  return Number(db(`
    SELECT COUNT(*) FROM (
      SELECT assessment_id, reason_code
      FROM process_oee_loss_drivers
      WHERE assessment_id IN (${idList})
      GROUP BY assessment_id, reason_code
      HAVING COUNT(*) > 1
    ) duplicates
  `));
}

function readAssessmentId(responseJson) {
  return responseJson?.entityId ?? responseJson?.id ?? null;
}

function completeBoundedBody(windowStartUtc, windowEndUtc, idempotencyKey, overrides = {}) {
  return {
    machineAssetId: MACHINE,
    productionOrderReference: "PILOT-PO-PP-001",
    moldReference: "PILOT-MOLD-PP-4CAV",
    windowStartUtc,
    windowEndUtc,
    plannedProductionSeconds: 14_400,
    idealCycleTimeSeconds: 45,
    effectiveCavityCount: 4,
    idempotencyKey,
    ...overrides,
  };
}

function assertLossEnvelope(label, lossesResponse) {
  assert(`${label} GET losses → 200`, lossesResponse.status === 200, `status=${lossesResponse.status}`);
  const body = lossesResponse.json;
  assert(`${label} rule set`, body?.ruleSetVersion === "PROCESS-OEE-LOSS-V1", `ruleSetVersion=${body?.ruleSetVersion}`);
  assert(
    `${label} leading loss method`,
    body?.leadingLossMethod === "normalized-oee-impact-points-v1",
    `leadingLossMethod=${body?.leadingLossMethod}`,
  );
  assert(`${label} drivers array`, Array.isArray(body?.drivers), "drivers missing");
  assert(`${label} drivers non-empty`, body.drivers.length > 0, "no drivers returned");
  for (const driver of body.drivers) {
    assert(
      `${label} driver category valid (${driver.reasonCode})`,
      LOSS_CATEGORIES.includes(driver.category),
      `category=${driver.category}`,
    );
    assert(`${label} driver reasonCode (${driver.reasonCode})`, Boolean(driver.reasonCode));
    assert(`${label} driver explanationCode (${driver.reasonCode})`, Boolean(driver.explanationCode));
  }
  return body;
}

function assertCategoryPresent(label, body, category) {
  assert(
    `${label} includes ${category}`,
    body.drivers.some((driver) => driver.category === category),
    `categories=${body.drivers.map((d) => d.category).join(",")}`,
  );
}

function assertNoFabricatedZeroLosses(label, body) {
  const zeroAvailability = body.drivers.filter(
    (driver) => driver.category === "availability-loss" && driver.affectedDurationSeconds === 0,
  );
  assert(`${label} no fabricated zero availability duration`, zeroAvailability.length === 0);

  const zeroPerformance = body.drivers.filter(
    (driver) => driver.category === "performance-loss" && driver.affectedDurationSeconds === 0,
  );
  assert(`${label} no fabricated zero performance duration`, zeroPerformance.length === 0);

  const zeroQuality = body.drivers.filter(
    (driver) => driver.category === "quality-loss" && driver.affectedQuantity === 0,
  );
  assert(`${label} no fabricated zero quality quantity`, zeroQuality.length === 0);
}

async function boundedEvaluate(windowStartIso, windowEndIso, idempotencyKey, overrides = {}) {
  const response = await request(
    "POST",
    `/api/process-oee/evaluations/bounded?facilityId=${FACILITY}`,
    completeBoundedBody(windowStartIso, windowEndIso, idempotencyKey, overrides),
  );
  assert(
    `bounded evaluation accepted (${idempotencyKey})`,
    response.status === 201 || response.status === 200,
    `status=${response.status}`,
  );
  const assessmentId = readAssessmentId(response.json);
  assert(`assessment id returned (${idempotencyKey})`, Boolean(assessmentId));
  return assessmentId;
}

console.log("Running PROCESS-OEE-2 loss intelligence verifier.");

const calculator = readRequired("packages/NevaxisFactOS.Persistence/ProcessOee/ProcessOeeLossCalculator.cs");
const lossConstants = readRequired("packages/NevaxisFactOS.Persistence/ProcessOee/ProcessOeeLossConstants.cs");
const migration = readRequired("packages/NevaxisFactOS.Persistence.Migrations/Migrations/20260812185022_AddProcessOeeLossDrivers.cs");
const entityConfig = readRequired("packages/NevaxisFactOS.Persistence/Configurations/ProcessOeeEntityConfigurations.cs");

assert("calculator uses loss rule set constant", calculator.includes("ProcessOeeLossConstants.RuleSetVersion"));
assert("calculator uses leading loss method constant", calculator.includes("ProcessOeeLossConstants.LeadingLossMethod"));
assert("rule-set PROCESS-OEE-LOSS-V1", lossConstants.includes("PROCESS-OEE-LOSS-V1"));
assert("normalized-oee-impact-points-v1", lossConstants.includes("normalized-oee-impact-points-v1"));
assert("ApplyLeadingLoss ranking", calculator.includes("ApplyLeadingLoss"));
assert("availability loss analysis", calculator.includes("AnalyzeAvailability"));
assert("performance loss analysis", calculator.includes("AnalyzePerformance"));
assert("quality loss analysis", calculator.includes("AnalyzeQuality"));
assert("missing downtime does not imply zero", calculator.includes("Missing downtime evidence does not imply zero downtime"));
assert(
  "unique loss driver index in migration",
  migration.includes("ix_process_oee_loss_drivers_tenant_assessment_reason") && migration.includes("unique: true"),
);
assert(
  "unique index tenant+assessment+reason_code",
  migration.includes('"tenant_id", "assessment_id", "reason_code"'),
);
assert(
  "entity unique index configured",
  entityConfig.includes("ix_process_oee_loss_drivers_tenant_assessment_reason") && entityConfig.includes(".IsUnique()"),
);

const ready = await fetch(`${BASE}/health/ready`);
assert("API ready", ready.status === 200, `status=${ready.status}`);

cleanupClosureFixtures();
seedClosureFixtures();

const availabilityAssessmentId = await boundedEvaluate(
  "2026-08-12T06:00:00.000Z",
  "2026-08-12T10:00:00.000Z",
  `${CLOSURE_PREFIX}-AVAIL-${CLOSURE_RUN}`,
);
const availabilityLosses = await request(
  "GET",
  `/api/process-oee/assessments/${availabilityAssessmentId}/losses?facilityId=${FACILITY}`,
);
const availabilityBody = assertLossEnvelope("availability scenario", availabilityLosses);
assertCategoryPresent("availability scenario", availabilityBody, "availability-loss");
const availabilityDriver = availabilityBody.drivers.find((driver) => driver.reasonCode === "availability-loss-downtime-duration");
assert("availability scenario downtime driver present", Boolean(availabilityDriver));
assert(
  "availability overlapping downtime merged (9000s not 10800s)",
  availabilityDriver?.affectedDurationSeconds === 9000,
  `affectedDurationSeconds=${availabilityDriver?.affectedDurationSeconds}`,
);
assert(
  "availability open-ended event clipped within window",
  availabilityDriver?.dataLimitations?.some((item) => String(item).includes("clipped")) ?? false,
  `limitations=${JSON.stringify(availabilityDriver?.dataLimitations)}`,
);
const availabilityAssessment = await request(
  "GET",
  `/api/process-oee/assessments/${availabilityAssessmentId}?facilityId=${FACILITY}`,
);
assert("availability assessment readable", availabilityAssessment.status === 200);
assert(
  "availability loss reconciles with operating time",
  availabilityDriver?.affectedDurationSeconds
    === (availabilityAssessment.json?.plannedProductionSeconds ?? 0) - (availabilityAssessment.json?.operatingSeconds ?? 0),
  `loss=${availabilityDriver?.affectedDurationSeconds} planned=${availabilityAssessment.json?.plannedProductionSeconds} operating=${availabilityAssessment.json?.operatingSeconds}`,
);

db(`
  UPDATE machine_downtime_episodes
  SET ended_at_source_utc = '2026-08-12T10:00:00Z',
      ended_at_received_utc = '${NOW_ISO}',
      duration_ms = 3600000,
      end_idempotency_key = '${CLOSURE_PREFIX}-DT-OPEN-ENDED-END',
      is_closed = TRUE,
      updated_at_utc = '${NOW_ISO}'
  WHERE start_idempotency_key = '${CLOSURE_PREFIX}-DT-OPEN-ENDED';
`);

const performanceAssessmentId = await boundedEvaluate(
  "2026-08-12T11:00:00.000Z",
  "2026-08-12T15:00:00.000Z",
  `${CLOSURE_PREFIX}-PERF-${CLOSURE_RUN}`,
);
const performanceLosses = await request(
  "GET",
  `/api/process-oee/assessments/${performanceAssessmentId}/losses?facilityId=${FACILITY}`,
);
const performanceBody = assertLossEnvelope("performance scenario", performanceLosses);
assertCategoryPresent("performance scenario", performanceBody, "performance-loss");

const qualityAssessmentId = await boundedEvaluate(
  "2026-08-12T16:00:00.000Z",
  "2026-08-12T20:00:00.000Z",
  `${CLOSURE_PREFIX}-QUAL-${CLOSURE_RUN}`,
  { productionOrderReference: CLOSURE_QUALITY_ORDER },
);
const qualityLosses = await request(
  "GET",
  `/api/process-oee/assessments/${qualityAssessmentId}/losses?facilityId=${FACILITY}`,
);
const qualityBody = assertLossEnvelope("quality scenario", qualityLosses);
assertCategoryPresent("quality scenario", qualityBody, "quality-loss");

const mixedAssessmentId = await boundedEvaluate(
  "2026-08-13T06:00:00.000Z",
  "2026-08-13T14:00:00.000Z",
  `${CLOSURE_PREFIX}-MIXED-${CLOSURE_RUN}`,
  { plannedProductionSeconds: 28_800, productionOrderReference: CLOSURE_QUALITY_ORDER },
);
const mixedLosses = await request(
  "GET",
  `/api/process-oee/assessments/${mixedAssessmentId}/losses?facilityId=${FACILITY}`,
);
const mixedBody = assertLossEnvelope("mixed scenario", mixedLosses);
assertCategoryPresent("mixed scenario", mixedBody, "availability-loss");
assertCategoryPresent("mixed scenario", mixedBody, "performance-loss");
assertCategoryPresent("mixed scenario", mixedBody, "quality-loss");
assert("mixed scenario leading loss returned", mixedBody.leadingLoss != null);
assert(
  "mixed scenario leading loss flagged on driver",
  mixedBody.drivers.some((driver) => driver.isLeadingLoss),
);
assert(
  "mixed scenario leading loss category matches driver",
  mixedBody.drivers.some(
    (driver) => driver.isLeadingLoss && driver.category === mixedBody.leadingLoss.category,
  ),
  `leading=${mixedBody.leadingLoss?.category}`,
);
assert(
  "mixed scenario normalized impact present",
  mixedBody.leadingLoss.normalizedImpactPoints != null
    || mixedBody.drivers.some((driver) => driver.isLeadingLoss && driver.normalizedImpactPoints != null),
);

const insufficientAssessmentId = await boundedEvaluate(
  "2026-08-13T15:00:00.000Z",
  "2026-08-13T19:00:00.000Z",
  `${CLOSURE_PREFIX}-INSUFFICIENT-${CLOSURE_RUN}`,
  {
    plannedProductionSeconds: undefined,
    productionOrderReference: undefined,
    moldReference: undefined,
    idealCycleTimeSeconds: undefined,
    effectiveCavityCount: undefined,
  },
);
const insufficientLosses = await request(
  "GET",
  `/api/process-oee/assessments/${insufficientAssessmentId}/losses?facilityId=${FACILITY}`,
);
const insufficientBody = assertLossEnvelope("insufficient scenario", insufficientLosses);
assertCategoryPresent("insufficient scenario", insufficientBody, "insufficient-data");
assertNoFabricatedZeroLosses("insufficient scenario", insufficientBody);

const staleAssessmentId = await boundedEvaluate(
  "2025-01-06T06:00:00.000Z",
  "2025-01-06T14:00:00.000Z",
  `${CLOSURE_PREFIX}-STALE-${CLOSURE_RUN}`,
  { plannedProductionSeconds: 28_800 },
);
const staleLosses = await request(
  "GET",
  `/api/process-oee/assessments/${staleAssessmentId}/losses?facilityId=${FACILITY}`,
);
const staleBody = assertLossEnvelope("stale scenario", staleLosses);
const staleSignals = staleBody.drivers.some((driver) => driver.category === "insufficient-data")
  || staleBody.drivers.some((driver) => driver.freshnessState === "stale")
  || staleBody.drivers.some((driver) => driver.reasonCode === "insufficient-data-stale-evidence")
  || staleBody.assessmentStatus === "stale-data";
assert(
  "stale scenario insufficient-data or stale freshness",
  staleSignals,
  `status=${staleBody.assessmentStatus} drivers=${staleBody.drivers.map((d) => `${d.category}/${d.freshnessState}/${d.reasonCode}`).join(",")}`,
);
assertNoFabricatedZeroLosses("stale scenario", staleBody);

const pilotReplay = await request(
  "POST",
  `/api/process-oee/evaluation-windows/${PILOT_WINDOW}/evaluations?facilityId=${FACILITY}`,
  { idempotencyKey: `${CLOSURE_PREFIX}-PILOT-${CLOSURE_RUN}` },
);
assert(
  "pilot window re-evaluation accepted",
  pilotReplay.status === 201 || pilotReplay.status === 200,
  `status=${pilotReplay.status}`,
);
const pilotAssessmentId = readAssessmentId(pilotReplay.json);
assert("pilot assessment id returned", Boolean(pilotAssessmentId));

const pilotLosses = await request(
  "GET",
  `/api/process-oee/assessments/${pilotAssessmentId}/losses?facilityId=${FACILITY}`,
);
const pilotBody = assertLossEnvelope("pilot window replay", pilotLosses);
assert(
  "pilot loss drivers persisted",
  lossDriverCountForAssessment(pilotAssessmentId) > 0,
  `count=${lossDriverCountForAssessment(pilotAssessmentId)}`,
);

const duplicateWindowA = await request(
  "POST",
  `/api/process-oee/evaluation-windows?facilityId=${FACILITY}`,
  {
    machineAssetId: MACHINE,
    productionOrderReference: "PILOT-PO-PP-001",
    moldReference: "PILOT-MOLD-PP-4CAV",
    windowStartUtc: "2026-08-14T06:00:00.000Z",
    windowEndUtc: "2026-08-14T10:00:00.000Z",
    plannedProductionSeconds: 14_400,
    idealCycleTimeSeconds: 45,
    effectiveCavityCount: 4,
    sourceReference: `${CLOSURE_PREFIX}-DUP-A`,
    idempotencyKey: `${CLOSURE_PREFIX}-WINDOW-A-${CLOSURE_RUN}`,
  },
);
assert(
  "duplicate protection window A created",
  duplicateWindowA.status === 201 || duplicateWindowA.status === 200,
  `status=${duplicateWindowA.status}`,
);
const duplicateWindowAId = duplicateWindowA.json?.id;
assert("duplicate protection window A id", Boolean(duplicateWindowAId));

const duplicateWindowB = await request(
  "POST",
  `/api/process-oee/evaluation-windows?facilityId=${FACILITY}`,
  {
    machineAssetId: MACHINE,
    productionOrderReference: "PILOT-PO-PP-001",
    moldReference: "PILOT-MOLD-PP-4CAV",
    windowStartUtc: "2026-08-14T11:00:00.000Z",
    windowEndUtc: "2026-08-14T15:00:00.000Z",
    plannedProductionSeconds: 14_400,
    idealCycleTimeSeconds: 45,
    effectiveCavityCount: 4,
    sourceReference: `${CLOSURE_PREFIX}-DUP-B`,
    idempotencyKey: `${CLOSURE_PREFIX}-WINDOW-B-${CLOSURE_RUN}`,
  },
);
assert(
  "duplicate protection window B created",
  duplicateWindowB.status === 201 || duplicateWindowB.status === 200,
  `status=${duplicateWindowB.status}`,
);
const duplicateWindowBId = duplicateWindowB.json?.id;
assert("duplicate protection window B id", Boolean(duplicateWindowBId));

const duplicateEvalA = await request(
  "POST",
  `/api/process-oee/evaluation-windows/${duplicateWindowAId}/evaluations?facilityId=${FACILITY}`,
  { idempotencyKey: `${CLOSURE_PREFIX}-EVAL-A-${CLOSURE_RUN}` },
);
assert(
  "duplicate protection evaluation A accepted",
  duplicateEvalA.status === 201 || duplicateEvalA.status === 200,
  `status=${duplicateEvalA.status}`,
);
const duplicateAssessmentAId = readAssessmentId(duplicateEvalA.json);
assert("duplicate protection assessment A id", Boolean(duplicateAssessmentAId));

const driversBeforeSecondEval = lossDriverCountForAssessment(duplicateAssessmentAId);

const duplicateEvalB = await request(
  "POST",
  `/api/process-oee/evaluation-windows/${duplicateWindowBId}/evaluations?facilityId=${FACILITY}`,
  { idempotencyKey: `${CLOSURE_PREFIX}-EVAL-B-${CLOSURE_RUN}` },
);
assert(
  "duplicate protection evaluation B accepted",
  duplicateEvalB.status === 201 || duplicateEvalB.status === 200,
  `status=${duplicateEvalB.status}`,
);
const duplicateAssessmentBId = readAssessmentId(duplicateEvalB.json);
assert("duplicate protection assessment B id", Boolean(duplicateAssessmentBId));
assert(
  "duplicate protection creates distinct assessments",
  duplicateAssessmentAId !== duplicateAssessmentBId,
  `a=${duplicateAssessmentAId} b=${duplicateAssessmentBId}`,
);

assert(
  "duplicate protection assessment A drivers persisted",
  driversBeforeSecondEval > 0,
  `count=${driversBeforeSecondEval}`,
);
assert(
  "duplicate protection assessment B drivers persisted",
  lossDriverCountForAssessment(duplicateAssessmentBId) > 0,
  `count=${lossDriverCountForAssessment(duplicateAssessmentBId)}`,
);
assert(
  "duplicate protection first assessment drivers unchanged",
  lossDriverCountForAssessment(duplicateAssessmentAId) === driversBeforeSecondEval,
  `before=${driversBeforeSecondEval} after=${lossDriverCountForAssessment(duplicateAssessmentAId)}`,
);
assert(
  "duplicate protection no duplicate reason_code within assessments",
  duplicateReasonCodeCount([duplicateAssessmentAId, duplicateAssessmentBId]) === 0,
);

const closureWindowsBeforeCleanup = closureWindowCount();
const closureAssessmentsBeforeCleanup = closureAssessmentCount();
assert("closure fixtures created", closureWindowsBeforeCleanup >= 2 && closureAssessmentsBeforeCleanup >= 1);

cleanupClosureFixtures();

assert("closure windows remaining zero", closureWindowCount() === 0);
assert("closure assessments remaining zero", closureAssessmentCount() === 0);
assert(
  "closure loss drivers remaining zero",
  Number(db(`
    SELECT COUNT(*) FROM process_oee_loss_drivers d
    LEFT JOIN process_oee_assessments a ON a.id = d.assessment_id
    WHERE a.id IS NULL
       OR a.idempotency_key LIKE '${CLOSURE_PREFIX}%'
  `)) === 0,
);

const pilotCount = Number(db(`
  SELECT COUNT(*) FROM process_oee_evaluation_windows
  WHERE id IN (
    'bb000001-00c0-4000-8000-0000000000c0',
    'bb000001-00c1-4000-8000-0000000000c1',
    'bb000001-00c2-4000-8000-0000000000c2'
  )
`));
assert("pilot windows preserved", pilotCount === 3, `count=${pilotCount}`);

console.log("PROCESS-OEE-2 loss intelligence verification passed.");
