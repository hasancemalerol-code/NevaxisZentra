#!/usr/bin/env node
import { spawnSync } from "node:child_process";
import { readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), "../../..");
const BASE = process.env.NEVAXIS_FACTOS_API_BASE_URL ?? "http://127.0.0.1:5000";
const TENANT = "bb000001-0001-4000-8000-000000000001";
const FACILITY = "bb000001-0005-4000-8000-000000000005";
const MACHINE = "bb000001-0096-4000-8000-000000000096";
const GATEWAY = "bb000001-0094-4000-8000-000000000094";
const PILOT_WINDOW = "bb000001-00c0-4000-8000-0000000000c0";
const SHIFT_V1 = "bb000001-0040-4000-8000-000000000040";
const SHIFT_V2 = "bb000001-0041-4000-8000-000000000041";
const NOW_ISO = new Date().toISOString();
const CLOSURE_PREFIX = "POEE3-CLOSURE";
const CLOSURE_RUN = Date.now();
const ACTOR = {
  issuer: "crevafactos-dev",
  subject: "production-pilot-operator",
  tenant: "dev-pilot-tenant",
};

const STABILITY_CATEGORIES = [
  "cycle-variability",
  "cycle-drift",
  "micro-stop",
  "downtime-concentration",
  "shift-comparison",
  "insufficient-data",
  "source-conflict",
];

const MIN_SAMPLES = 8;
const MICRO_STOP_BOUNDARY_MS = 60_000;

let closureShiftAssignmentSnapshot = null;

function hoursAgo(hours) {
  return new Date(Date.now() - hours * 3_600_000).toISOString().replace(/\.\d{3}Z$/, "Z");
}

function isoAt(baseIso, offsetHours) {
  return new Date(new Date(baseIso).getTime() + offsetHours * 3_600_000)
    .toISOString()
    .replace(/\.\d{3}Z$/, "Z");
}

const CLOSURE_STABLE_START = hoursAgo(8);
const CLOSURE_STABLE_END = hoursAgo(4);
const CLOSURE_DRIFT_START = hoursAgo(16);
const CLOSURE_DRIFT_END = hoursAgo(12);
const CLOSURE_DOWNTIME_START = hoursAgo(24);
const CLOSURE_DOWNTIME_END = hoursAgo(20);
const CLOSURE_SHIFT_V1_START = hoursAgo(32);
const CLOSURE_SHIFT_V1_END = hoursAgo(28);
const CLOSURE_SHIFT_V2_START = hoursAgo(28);
const CLOSURE_SHIFT_V2_END = hoursAgo(24);
const CLOSURE_INSUFFICIENT_START = hoursAgo(48);
const CLOSURE_INSUFFICIENT_END = hoursAgo(44);

function fail(message) {
  console.error(`PROCESS-OEE-3 stability/downtime verification failed: ${message}`);
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
    "X-NevaxisZentra-Tenant": actor.tenant,
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

function buildCycles(windowStartIso, cycleCount, durationMs, keyPrefix, idPrefix) {
  const start = new Date(windowStartIso);
  const windowSeconds = 14_400;
  const spacingSeconds = Math.floor(windowSeconds / cycleCount);
  const cycles = [];
  for (let index = 0; index < cycleCount; index += 1) {
    const totalSeconds = index * spacingSeconds;
    const timestamp = new Date(start.getTime() + totalSeconds * 1000).toISOString().replace(/\.\d{3}Z$/, "Z");
    const cycleId = `${idPrefix}-${String(index + 1).padStart(4, "0")}-4000-8000-000000000001`;
    cycles.push(`
      (
        '${cycleId}', '${TENANT}', '${FACILITY}', '${MACHINE}', '${GATEWAY}',
        '${keyPrefix}-${index}', ${durationMs}, NULL, 'good',
        '${timestamp}',
        '${NOW_ISO}', ${index + 1}, '${keyPrefix}-${index}', FALSE, FALSE, '${NOW_ISO}'
      )`);
  }
  return cycles;
}

function buildDriftingCycles(windowStartIso, cycleCount, keyPrefix, idPrefix) {
  const start = new Date(windowStartIso);
  const windowSeconds = 14_400;
  const spacingSeconds = Math.floor(windowSeconds / cycleCount);
  const cycles = [];
  for (let index = 0; index < cycleCount; index += 1) {
    const progress = cycleCount <= 1 ? 0 : index / (cycleCount - 1);
    const durationMs = Math.round(40_000 + progress * 45_000);
    const totalSeconds = index * spacingSeconds;
    const timestamp = new Date(start.getTime() + totalSeconds * 1000).toISOString().replace(/\.\d{3}Z$/, "Z");
    const cycleId = `${idPrefix}-${String(index + 1).padStart(4, "0")}-4000-8000-000000000001`;
    cycles.push(`
      (
        '${cycleId}', '${TENANT}', '${FACILITY}', '${MACHINE}', '${GATEWAY}',
        '${keyPrefix}-${index}', ${durationMs}, NULL, 'good',
        '${timestamp}',
        '${NOW_ISO}', ${1000 + index}, '${keyPrefix}-${index}', FALSE, FALSE, '${NOW_ISO}'
      )`);
  }
  return cycles;
}

function seedClosureShiftAssignments() {
  const template = db(`
    SELECT production_plan_handoff_id, source_production_plan_id, source_production_plan_version
    FROM production_orders
    WHERE tenant_id = '${TENANT}' AND facility_id = '${FACILITY}'
    ORDER BY created_at_utc DESC
    LIMIT 1;
  `);
  assert("shift comparison template order", Boolean(template));
  const [, planId, planVersion] = template.split("|");

  function ensureOrder(referenceCode, orderId, closureHandoffId, handoffSuffix) {
    const existing = db(`
      SELECT id FROM production_orders
      WHERE tenant_id = '${TENANT}' AND facility_id = '${FACILITY}'
        AND reference_code = '${referenceCode}';
    `);
    if (existing) {
      return existing;
    }

    db(`
      INSERT INTO production_plan_handoffs (
        id, tenant_id, facility_id, production_plan_id, production_plan_version,
        reference_code, status, idempotency_key, prepared_at_utc, prepared_by_subject_id,
        consumed_at_utc, production_reference, correlation_id, version, payload_json,
        created_at_utc, updated_at_utc
      ) VALUES (
        '${closureHandoffId}', '${TENANT}', '${FACILITY}', '${planId}', ${planVersion},
        '${referenceCode}-handoff', 'consumed', '${CLOSURE_PREFIX}-HANDOFF-${handoffSuffix}',
        '${NOW_ISO}', 'production-pilot-operator', '${NOW_ISO}', '${referenceCode}',
        'e3a00003-000${handoffSuffix}-4000-8000-000000000099', 1, '{}',
        '${NOW_ISO}', '${NOW_ISO}'
      );
    `);

    db(`
      INSERT INTO production_orders (
        id, tenant_id, facility_id, production_plan_handoff_id, source_production_plan_id,
        source_production_plan_version, reference_code, title, status,
        planned_start_at_utc, planned_end_at_utc, actual_start_at_utc, actual_end_at_utc,
        machine_reference, mold_reference, created_by_subject_id, last_modified_by_subject_id,
        version, created_at_utc, updated_at_utc, history_json, good_quantity, scrap_quantity,
        total_downtime_seconds, total_setup_seconds, manual_cycle_count, total_manual_cycle_duration_ms
      ) VALUES (
        '${orderId}', '${TENANT}', '${FACILITY}', '${closureHandoffId}', '${planId}',
        ${planVersion}, '${referenceCode}', '${referenceCode} closure', 'in-progress',
        '2026-08-15T06:00:00Z', '2026-08-15T22:00:00Z', '2026-08-15T06:00:00Z', NULL,
        'PILOT-PRESS-01', 'PILOT-MOLD-PP-4CAV', 'production-pilot-operator', 'production-pilot-operator',
        1, '${NOW_ISO}', '${NOW_ISO}', '[]', 0, 0, 0, 0, 0, 0
      );
    `);
    return orderId;
  }

  const order1 = ensureOrder(
    "PILOT-PO-PP-001",
    "e3a00004-0001-4000-8000-000000000001",
    "e3a00003-0001-4000-8000-000000000001",
    "1",
  );
  const order2 = ensureOrder(
    "PILOT-PO-PP-002",
    "e3a00004-0002-4000-8000-000000000002",
    "e3a00003-0002-4000-8000-000000000001",
    "2",
  );
  assert("shift comparison order PILOT-PO-PP-001", Boolean(order1));
  assert("shift comparison order PILOT-PO-PP-002", Boolean(order2));

  closureShiftAssignmentSnapshot = db(`
    SELECT id
    FROM production_order_shift_assignments
    WHERE tenant_id = '${TENANT}'
      AND facility_id = '${FACILITY}'
      AND production_order_id IN ('${order1}', '${order2}')
      AND released_at_utc IS NULL;
  `);

  if (closureShiftAssignmentSnapshot) {
    const activeIds = closureShiftAssignmentSnapshot.split("\n").filter(Boolean).map((id) => `'${id}'`).join(", ");
    db(`
      UPDATE production_order_shift_assignments
      SET released_at_utc = '${NOW_ISO}',
          released_by_subject_id = 'production-pilot-operator',
          release_reason = '${CLOSURE_PREFIX}-TEMP-RELEASE'
      WHERE id IN (${activeIds});
    `);
  }

  db(`
    INSERT INTO production_order_shift_assignments (
      id, tenant_id, facility_id, production_order_id, production_shift_id,
      shift_code, shift_date, shift_start_at_utc, shift_end_at_utc,
      assigned_at_utc, assigned_by_subject_id, released_at_utc,
      released_by_subject_id, release_reason, correlation_id, version
    ) VALUES
    (
      'e3a00005-0001-4000-8000-000000000001', '${TENANT}', '${FACILITY}', '${order1}', '${SHIFT_V1}',
      'V1', '2026-08-15', '${CLOSURE_SHIFT_V1_START}', '${CLOSURE_SHIFT_V1_END}',
      '${NOW_ISO}', 'production-pilot-operator', NULL,
      NULL, NULL, 'e3a00005-0001-4000-8000-000000000099', 1
    ),
    (
      'e3a00005-0002-4000-8000-000000000002', '${TENANT}', '${FACILITY}', '${order2}', '${SHIFT_V2}',
      'V2', '2026-08-15', '${CLOSURE_SHIFT_V2_START}', '${CLOSURE_SHIFT_V2_END}',
      '${NOW_ISO}', 'production-pilot-operator', NULL,
      NULL, NULL, 'e3a00005-0002-4000-8000-000000000099', 1
    );
  `);
}

function restoreClosureShiftAssignments() {
  db(`
    DELETE FROM production_order_shift_assignments
    WHERE id IN (
      'e3a00005-0001-4000-8000-000000000001',
      'e3a00005-0002-4000-8000-000000000002'
    );
  `);

  if (closureShiftAssignmentSnapshot) {
    const activeIds = closureShiftAssignmentSnapshot.split("\n").filter(Boolean).map((id) => `'${id}'`).join(", ");
    db(`
      UPDATE production_order_shift_assignments
      SET released_at_utc = NULL,
          released_by_subject_id = NULL,
          release_reason = NULL
      WHERE id IN (${activeIds});
    `);
  }

  closureShiftAssignmentSnapshot = null;
}

function seedClosureFixtures() {
  db(`
    DELETE FROM machine_observed_cycles
    WHERE idempotency_key LIKE '${CLOSURE_PREFIX}%';
    DELETE FROM machine_downtime_episodes
    WHERE start_idempotency_key LIKE '${CLOSURE_PREFIX}%';
  `);

  const stableCycles = buildCycles(
    "2026-08-10T06:00:00Z",
    40,
    45_000,
    `${CLOSURE_PREFIX}-STABLE-CYCLE`,
    "e3a00001",
  );
  insertInBatches(`
    INSERT INTO machine_observed_cycles (
      id, tenant_id, facility_id, machine_id, gateway_id,
      cycle_identity, cycle_duration_ms, observed_counter, quality,
      source_timestamp_utc, received_at_utc, source_sequence, idempotency_key,
      is_duplicate, is_out_of_order, created_at_utc
    )
  `, stableCycles);

  const driftingCycles = buildDriftingCycles(
    "2026-08-10T11:00:00Z",
    40,
    `${CLOSURE_PREFIX}-DRIFT-CYCLE`,
    "e3a00002",
  );
  insertInBatches(`
    INSERT INTO machine_observed_cycles (
      id, tenant_id, facility_id, machine_id, gateway_id,
      cycle_identity, cycle_duration_ms, observed_counter, quality,
      source_timestamp_utc, received_at_utc, source_sequence, idempotency_key,
      is_duplicate, is_out_of_order, created_at_utc
    )
  `, driftingCycles);

  db(`
    INSERT INTO machine_downtime_episodes (
      id, tenant_id, facility_id, machine_id, gateway_id,
      started_at_source_utc, ended_at_source_utc,
      started_at_received_utc, ended_at_received_utc,
      duration_ms, quality, reason_code, is_micro_stop,
      start_idempotency_key, end_idempotency_key,
      is_closed, is_out_of_order, created_at_utc, updated_at_utc
    ) VALUES
    (
      'e3a00003-0001-4000-8000-000000000001', '${TENANT}', '${FACILITY}', '${MACHINE}', '${GATEWAY}',
      '2026-08-16T07:00:00Z', '2026-08-16T07:00:30Z',
      '${NOW_ISO}', '${NOW_ISO}',
      30000, 'good', 'micro-jam', TRUE,
      '${CLOSURE_PREFIX}-MICRO-A', '${CLOSURE_PREFIX}-MICRO-A-END',
      TRUE, FALSE, '${NOW_ISO}', '${NOW_ISO}'
    ),
    (
      'e3a00003-0002-4000-8000-000000000002', '${TENANT}', '${FACILITY}', '${MACHINE}', '${GATEWAY}',
      '2026-08-16T08:00:00Z', '2026-08-16T08:00:45Z',
      '${NOW_ISO}', '${NOW_ISO}',
      45000, 'good', NULL, TRUE,
      '${CLOSURE_PREFIX}-MICRO-B', '${CLOSURE_PREFIX}-MICRO-B-END',
      TRUE, FALSE, '${NOW_ISO}', '${NOW_ISO}'
    ),
    (
      'e3a00003-0003-4000-8000-000000000003', '${TENANT}', '${FACILITY}', '${MACHINE}', '${GATEWAY}',
      '2026-08-16T09:00:00Z', '2026-08-16T11:00:00Z',
      '${NOW_ISO}', '${NOW_ISO}',
      7200000, 'good', 'tool-change', FALSE,
      '${CLOSURE_PREFIX}-DT-TOOL', '${CLOSURE_PREFIX}-DT-TOOL-END',
      TRUE, FALSE, '${NOW_ISO}', '${NOW_ISO}'
    ),
    (
      'e3a00003-0004-4000-8000-000000000004', '${TENANT}', '${FACILITY}', '${MACHINE}', '${GATEWAY}',
      '2026-08-16T10:30:00Z', '2026-08-16T11:30:00Z',
      '${NOW_ISO}', '${NOW_ISO}',
      3600000, 'good', 'tool-change', FALSE,
      '${CLOSURE_PREFIX}-DT-OVERLAP-A', '${CLOSURE_PREFIX}-DT-OVERLAP-A-END',
      TRUE, FALSE, '${NOW_ISO}', '${NOW_ISO}'
    ),
    (
      'e3a00003-0005-4000-8000-000000000005', '${TENANT}', '${FACILITY}', '${MACHINE}', '${GATEWAY}',
      '2026-08-16T11:00:00Z', '2026-08-16T12:00:00Z',
      '${NOW_ISO}', '${NOW_ISO}',
      3600000, 'good', 'material-shortage', FALSE,
      '${CLOSURE_PREFIX}-DT-OVERLAP-B', '${CLOSURE_PREFIX}-DT-OVERLAP-B-END',
      TRUE, FALSE, '${NOW_ISO}', '${NOW_ISO}'
    ),
    (
      'e3a00003-0006-4000-8000-000000000006', '${TENANT}', '${FACILITY}', '${MACHINE}', '${GATEWAY}',
      '2026-08-16T12:30:00Z', '2026-08-16T13:30:00Z',
      '${NOW_ISO}', '${NOW_ISO}',
      3600000, 'good', NULL, FALSE,
      '${CLOSURE_PREFIX}-DT-UNKNOWN', '${CLOSURE_PREFIX}-DT-UNKNOWN-END',
      TRUE, FALSE, '${NOW_ISO}', '${NOW_ISO}'
    ),
    (
      'e3a00003-0007-4000-8000-000000000007', '${TENANT}', '${FACILITY}', '${MACHINE}', '${GATEWAY}',
      '2026-08-16T13:00:00Z', NULL,
      '${NOW_ISO}', NULL,
      NULL, 'good', 'utility-failure', FALSE,
      '${CLOSURE_PREFIX}-DT-OPEN', NULL,
      FALSE, FALSE, '${NOW_ISO}', '${NOW_ISO}'
    ),
    (
      'e3a00003-0008-4000-8000-000000000008', '${TENANT}', '${FACILITY}', '${MACHINE}', '${GATEWAY}',
      '2026-08-16T05:00:00Z', '2026-08-16T05:30:00Z',
      '${NOW_ISO}', '${NOW_ISO}',
      1800000, 'good', 'outside-window', FALSE,
      '${CLOSURE_PREFIX}-DT-OUTSIDE', '${CLOSURE_PREFIX}-DT-OUTSIDE-END',
      TRUE, FALSE, '${NOW_ISO}', '${NOW_ISO}'
    );
  `);

  const paretoCycles = buildCycles(
    "2026-08-16T06:00:00Z",
    40,
    45_000,
    `${CLOSURE_PREFIX}-PARETO-CYCLE`,
    "e3a00004",
  );
  insertInBatches(`
    INSERT INTO machine_observed_cycles (
      id, tenant_id, facility_id, machine_id, gateway_id,
      cycle_identity, cycle_duration_ms, observed_counter, quality,
      source_timestamp_utc, received_at_utc, source_sequence, idempotency_key,
      is_duplicate, is_out_of_order, created_at_utc
    )
  `, paretoCycles);

  const shiftACycles = buildCycles(
    CLOSURE_SHIFT_V1_START,
    30,
    45_000,
    `${CLOSURE_PREFIX}-SHIFT-A-CYCLE`,
    "e3a00005",
  );
  insertInBatches(`
    INSERT INTO machine_observed_cycles (
      id, tenant_id, facility_id, machine_id, gateway_id,
      cycle_identity, cycle_duration_ms, observed_counter, quality,
      source_timestamp_utc, received_at_utc, source_sequence, idempotency_key,
      is_duplicate, is_out_of_order, created_at_utc
    )
  `, shiftACycles);

  const shiftBCycles = buildCycles(
    CLOSURE_SHIFT_V2_START,
    30,
    72_000,
    `${CLOSURE_PREFIX}-SHIFT-B-CYCLE`,
    "e3a00006",
  );
  insertInBatches(`
    INSERT INTO machine_observed_cycles (
      id, tenant_id, facility_id, machine_id, gateway_id,
      cycle_identity, cycle_duration_ms, observed_counter, quality,
      source_timestamp_utc, received_at_utc, source_sequence, idempotency_key,
      is_duplicate, is_out_of_order, created_at_utc
    )
  `, shiftBCycles);

  const insufficientCycles = buildCycles(
    "2026-08-17T06:00:00Z",
    3,
    45_000,
    `${CLOSURE_PREFIX}-INSUFF-CYCLE`,
    "e3a00007",
  );
  insertInBatches(`
    INSERT INTO machine_observed_cycles (
      id, tenant_id, facility_id, machine_id, gateway_id,
      cycle_identity, cycle_duration_ms, observed_counter, quality,
      source_timestamp_utc, received_at_utc, source_sequence, idempotency_key,
      is_duplicate, is_out_of_order, created_at_utc
    )
  `, insufficientCycles);

  seedClosureShiftAssignments();
}

function tableExists(tableName) {
  return db(`
    SELECT EXISTS (
      SELECT 1 FROM information_schema.tables
      WHERE table_schema = 'public' AND table_name = '${tableName}'
    );
  `) === "t";
}

function closureAssessmentIdsSql() {
  return `
    SELECT id FROM process_oee_assessments
    WHERE idempotency_key LIKE '${CLOSURE_PREFIX}%'
       OR evaluation_window_id IN (
         SELECT id FROM process_oee_evaluation_windows
         WHERE create_idempotency_key LIKE '${CLOSURE_PREFIX}%'
       )
  `;
}

function deleteIfTableExists(tableName, sql) {
  if (tableExists(tableName)) {
    db(sql);
  }
}

function cleanupClosureFixtures() {
  restoreClosureShiftAssignments();

  db(`
    DELETE FROM machine_observed_cycles
    WHERE idempotency_key LIKE '${CLOSURE_PREFIX}%';
    DELETE FROM machine_downtime_episodes
    WHERE start_idempotency_key LIKE '${CLOSURE_PREFIX}%';
  `);

  deleteIfTableExists("process_oee_shift_comparison_rows", `
    DELETE FROM process_oee_shift_comparison_rows
    WHERE batch_id IN (
      SELECT id FROM process_oee_shift_comparison_batches
      WHERE idempotency_key LIKE '${CLOSURE_PREFIX}%'
    );
  `);
  deleteIfTableExists("process_oee_shift_comparison_batches", `
    DELETE FROM process_oee_shift_comparison_batches
    WHERE idempotency_key LIKE '${CLOSURE_PREFIX}%';
  `);
  deleteIfTableExists("process_oee_downtime_breakdown_rows", `
    DELETE FROM process_oee_downtime_breakdown_rows
    WHERE assessment_id IN (${closureAssessmentIdsSql()});
  `);
  deleteIfTableExists("process_oee_stability_drivers", `
    DELETE FROM process_oee_stability_drivers
    WHERE assessment_id IN (${closureAssessmentIdsSql()});
  `);
  deleteIfTableExists("process_oee_stability_assessments", `
    DELETE FROM process_oee_stability_assessments
    WHERE assessment_id IN (${closureAssessmentIdsSql()});
  `);
  db(`
    DELETE FROM process_oee_loss_drivers
    WHERE assessment_id IN (${closureAssessmentIdsSql()});
  `);
  db(`
    DELETE FROM process_oee_assessments
    WHERE idempotency_key LIKE '${CLOSURE_PREFIX}%'
       OR evaluation_window_id IN (
         SELECT id FROM process_oee_evaluation_windows
         WHERE create_idempotency_key LIKE '${CLOSURE_PREFIX}%'
       );
  `);
  db(`
    DELETE FROM process_oee_evaluation_windows
    WHERE create_idempotency_key LIKE '${CLOSURE_PREFIX}%';
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
      );
  `);
  db(`
    DELETE FROM production_order_shift_assignments
    WHERE id IN (
      'e3a00005-0001-4000-8000-000000000001',
      'e3a00005-0002-4000-8000-000000000002'
    );
    DELETE FROM production_orders
    WHERE id IN (
      'e3a00004-0001-4000-8000-000000000001',
      'e3a00004-0002-4000-8000-000000000002'
    );
    DELETE FROM production_plan_handoffs
    WHERE id IN (
      'e3a00003-0001-4000-8000-000000000001',
      'e3a00003-0002-4000-8000-000000000001'
    );
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

function stabilityDriverCountForAssessment(assessmentId) {
  if (!tableExists("process_oee_stability_drivers")) {
    return 0;
  }
  return Number(db(`
    SELECT COUNT(*) FROM process_oee_stability_drivers
    WHERE assessment_id = '${assessmentId}'
  `));
}

function duplicateStabilityReasonCodeCount(assessmentIds) {
  if (!tableExists("process_oee_stability_drivers")) {
    return 0;
  }
  const idList = assessmentIds.map((id) => `'${id}'`).join(", ");
  return Number(db(`
    SELECT COUNT(*) FROM (
      SELECT assessment_id, reason_code
      FROM process_oee_stability_drivers
      WHERE assessment_id IN (${idList})
      GROUP BY assessment_id, reason_code
      HAVING COUNT(*) > 1
    ) duplicates
  `));
}

function shiftComparisonBatchCount() {
  if (!tableExists("process_oee_shift_comparison_batches")) {
    return 0;
  }
  return Number(db(`
    SELECT COUNT(*) FROM process_oee_shift_comparison_batches
    WHERE idempotency_key LIKE '${CLOSURE_PREFIX}%'
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

function assertStabilityEnvelope(label, stabilityResponse) {
  assert(`${label} GET stability → 200`, stabilityResponse.status === 200, `status=${stabilityResponse.status}`);
  const body = stabilityResponse.json;
  assert(`${label} rule set`, body?.ruleSetVersion === "PROCESS-OEE-STABILITY-V1", `ruleSetVersion=${body?.ruleSetVersion}`);
  assert(`${label} result state`, Boolean(body?.resultState));
  assert(`${label} drivers array`, Array.isArray(body?.drivers), "drivers missing");
  assert(`${label} downtime breakdown array`, Array.isArray(body?.downtimeBreakdown), "downtimeBreakdown missing");
  for (const driver of body.drivers) {
    assert(
      `${label} driver category valid (${driver.reasonCode})`,
      STABILITY_CATEGORIES.includes(driver.category),
      `category=${driver.category}`,
    );
    assert(`${label} driver reasonCode (${driver.reasonCode})`, Boolean(driver.reasonCode));
    assert(`${label} driver explanationCode (${driver.reasonCode})`, Boolean(driver.explanationCode));
  }
  return body;
}

function assertNoFabricatedNormalStability(label, body) {
  if (body.resultState !== "insufficient-data") {
    return;
  }
  assert(
    `${label} insufficient-data sample count honest`,
    body.sampleCount < MIN_SAMPLES || body.drivers.some((d) => d.reasonCode === "insufficient-data-cycle-samples"),
    `sampleCount=${body.sampleCount}`,
  );
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

console.log("Running PROCESS-OEE-3 stability/downtime verifier.");

const calculator = readRequired("packages/NevaxisZentra.Persistence/ProcessOee/ProcessOeeStabilityCalculator.cs");
const stabilityConstants = readRequired("packages/NevaxisZentra.Persistence/ProcessOee/ProcessOeeStabilityConstants.cs");
const migration = readRequired("packages/NevaxisZentra.Persistence.Migrations/Migrations/20260812200000_AddProcessOeeStabilityIntelligence.cs");
const entityConfig = readRequired("packages/NevaxisZentra.Persistence/Configurations/ProcessOeeEntityConfigurations.cs");

assert("calculator uses stability rule set constant", calculator.includes("ProcessOeeStabilityConstants.RuleSetVersion"));
assert("rule-set PROCESS-OEE-STABILITY-V1", stabilityConstants.includes("PROCESS-OEE-STABILITY-V1"));
assert("minimum sample threshold", stabilityConstants.includes(`MinimumSampleCount = ${MIN_SAMPLES}`));
assert("micro-stop boundary 60000ms", stabilityConstants.includes("MicroStopBoundaryMs"));
assert("median + MAD analysis", calculator.includes("MedianAbsoluteDeviation"));
assert("early/late drift analysis", calculator.includes("AnalyzeDrift"));
assert("micro-stop analysis", calculator.includes("AnalyzeMicroStops"));
assert("downtime pareto analysis", calculator.includes("AnalyzeDowntimePareto"));
assert("shift comparison", calculator.includes("CompareShifts"));
assert(
  "unique stability driver index in migration",
  migration.includes("ix_process_oee_stability_drivers_tenant_assessment_reason") && migration.includes("unique: true"),
);
assert(
  "entity stability driver append-only FK",
  entityConfig.includes("DeleteBehavior.Restrict"),
);

const ready = await fetch(`${BASE}/health/ready`);
assert("API ready", ready.status === 200, `status=${ready.status}`);

if (!tableExists("process_oee_stability_assessments")) {
  fail("PROCESS-OEE-3 migration not applied (process_oee_stability_assessments missing). Run database migration before this verifier.");
}

cleanupClosureFixtures();
seedClosureFixtures();

const stableAssessmentId = await boundedEvaluate(
  "2026-08-10T06:00:00.000Z",
  "2026-08-10T10:00:00.000Z",
  `${CLOSURE_PREFIX}-STABLE-${CLOSURE_RUN}`,
);
const stableStability = await request(
  "GET",
  `/api/process-oee/assessments/${stableAssessmentId}/stability?facilityId=${FACILITY}`,
);
const stableBody = assertStabilityEnvelope("scenario A stable", stableStability);
assert("scenario A stable result state", stableBody.resultState === "stable", `resultState=${stableBody.resultState}`);
assert(
  "scenario A sample count",
  stableBody.sampleCount >= MIN_SAMPLES,
  `sampleCount=${stableBody.sampleCount}`,
);
assert(
  "scenario A stable cycle driver",
  stableBody.drivers.some((d) => d.reasonCode === "cycle-stability-stable"),
);
assert(
  "scenario A no drift",
  stableBody.driftDirection === "none",
  `driftDirection=${stableBody.driftDirection}`,
);

const driftAssessmentId = await boundedEvaluate(
  "2026-08-10T11:00:00.000Z",
  "2026-08-10T17:00:00.000Z",
  `${CLOSURE_PREFIX}-DRIFT-${CLOSURE_RUN}`,
);
const driftStability = await request(
  "GET",
  `/api/process-oee/assessments/${driftAssessmentId}/stability?facilityId=${FACILITY}`,
);
const driftBody = assertStabilityEnvelope("scenario B variable/drift", driftStability);
assert(
  "scenario B drifting or variable state",
  driftBody.resultState === "drifting" || driftBody.resultState === "variable",
  `resultState=${driftBody.resultState}`,
);
assert(
  "scenario B increasing drift evidence",
  driftBody.driftDirection === "increasing"
    || driftBody.drivers.some((d) => d.reasonCode === "cycle-drift-increasing" || d.reasonCode === "cycle-variability-high"),
  `driftDirection=${driftBody.driftDirection}`,
);
assert(
  "scenario B no predictive-failure wording",
  !JSON.stringify(driftBody).toLowerCase().includes("predictive"),
);

db(`
  UPDATE machine_downtime_episodes
  SET ended_at_source_utc = '2026-08-16T14:00:00Z',
      ended_at_received_utc = '${NOW_ISO}',
      duration_ms = 3600000,
      end_idempotency_key = '${CLOSURE_PREFIX}-DT-OPEN-END',
      is_closed = TRUE,
      updated_at_utc = '${NOW_ISO}'
  WHERE start_idempotency_key = '${CLOSURE_PREFIX}-DT-OPEN';
`);

const paretoAssessmentId = await boundedEvaluate(
  "2026-08-16T06:00:00.000Z",
  "2026-08-16T14:00:00.000Z",
  `${CLOSURE_PREFIX}-PARETO-${CLOSURE_RUN}`,
  { plannedProductionSeconds: 28_800 },
);
const paretoStability = await request(
  "GET",
  `/api/process-oee/assessments/${paretoAssessmentId}/stability?facilityId=${FACILITY}`,
);
const paretoBody = assertStabilityEnvelope("scenario C micro-stop/pareto", paretoStability);
assert(
  "scenario C micro-stop driver",
  paretoBody.drivers.some((d) => d.reasonCode === "micro-stop-summary"),
);
assert(
  "scenario C micro-stop count",
  paretoBody.microStopCount >= 2,
  `microStopCount=${paretoBody.microStopCount}`,
);
assert(
  "scenario C micro-stop boundary referenced",
  paretoBody.drivers.some((d) =>
    d.reasonCode === "micro-stop-summary"
    && (d.dataLimitations?.some((item) => String(item).includes(String(MICRO_STOP_BOUNDARY_MS))) ?? false)),
);
assert(
  "scenario C downtime breakdown non-empty",
  paretoBody.downtimeBreakdown.length > 0,
);
assert(
  "scenario C unknown reason visible",
  paretoBody.downtimeBreakdown.some((row) => row.reasonCode === "unknown"),
);
assert(
  "scenario C classified downtime",
  paretoBody.classifiedDowntimeSeconds > 0,
  `classified=${paretoBody.classifiedDowntimeSeconds}`,
);
assert(
  "scenario C unclassified downtime",
  paretoBody.unclassifiedDowntimeSeconds > 0,
  `unclassified=${paretoBody.unclassifiedDowntimeSeconds}`,
);

const paretoEndpoint = await request(
  "GET",
  `/api/process-oee/assessments/${paretoAssessmentId}/downtime-pareto?facilityId=${FACILITY}`,
);
assert("scenario C downtime-pareto → 200", paretoEndpoint.status === 200, `status=${paretoEndpoint.status}`);
const paretoRows = paretoEndpoint.json?.rows ?? [];
assert("scenario C pareto rows", paretoRows.length > 0);
for (let index = 1; index < paretoRows.length; index += 1) {
  const prev = paretoRows[index - 1];
  const current = paretoRows[index];
  const ordered =
    current.durationSeconds < prev.durationSeconds
    || (current.durationSeconds === prev.durationSeconds
      && current.occurrenceCount <= prev.occurrenceCount
      && current.reasonCode >= prev.reasonCode);
  assert(
    `scenario C pareto order rank ${index + 1}`,
    ordered,
    `prev=${prev.reasonCode}/${prev.durationSeconds} current=${current.reasonCode}/${current.durationSeconds}`,
  );
}
assert(
  "scenario C outside-window episode excluded from pareto total",
  !paretoRows.some((row) => row.reasonCode === "outside-window"),
  `reasons=${paretoRows.map((row) => row.reasonCode).join(",")}`,
);

const shiftAssessmentAId = await boundedEvaluate(
  CLOSURE_SHIFT_V1_START.replace("Z", ".000Z"),
  CLOSURE_SHIFT_V1_END.replace("Z", ".000Z"),
  `${CLOSURE_PREFIX}-SHIFT-A-${CLOSURE_RUN}`,
  { productionOrderReference: "PILOT-PO-PP-001" },
);
const shiftAssessmentBId = await boundedEvaluate(
  CLOSURE_SHIFT_V2_START.replace("Z", ".000Z"),
  CLOSURE_SHIFT_V2_END.replace("Z", ".000Z"),
  `${CLOSURE_PREFIX}-SHIFT-B-${CLOSURE_RUN}`,
  { productionOrderReference: "PILOT-PO-PP-002" },
);
const shiftPeriodStart = CLOSURE_SHIFT_V1_START.replace("Z", ".000Z");
const shiftPeriodEnd = CLOSURE_SHIFT_V2_END.replace("Z", ".000Z");
const shiftComparisonGet = await request(
  "GET",
  `/api/process-oee/shift-comparison?facilityId=${FACILITY}&machineAssetId=${MACHINE}&periodStartUtc=${encodeURIComponent(shiftPeriodStart)}&periodEndUtc=${encodeURIComponent(shiftPeriodEnd)}`,
);
assert("scenario D shift comparison → 200", shiftComparisonGet.status === 200, `status=${shiftComparisonGet.status}`);
assert(
  "scenario D rule set",
  shiftComparisonGet.json?.ruleSetVersion === "PROCESS-OEE-STABILITY-V1",
);
assert(
  "scenario D comparable rows",
  (shiftComparisonGet.json?.rows?.length ?? 0) >= 2,
  `rows=${shiftComparisonGet.json?.rows?.length}`,
);
assert(
  "scenario D shift labels present",
  shiftComparisonGet.json?.rows?.every((row) => Boolean(row.shiftLabel)),
);
assert(
  "scenario D no operator attribution",
  !JSON.stringify(shiftComparisonGet.json ?? {}).toLowerCase().includes("operator"),
);
const shiftComparisonKey = `${CLOSURE_PREFIX}-SHIFT-COMPARE-${CLOSURE_RUN}`;
const shiftComparisonPost = await request(
  "POST",
  `/api/process-oee/shift-comparison/evaluations?facilityId=${FACILITY}`,
  {
    machineAssetId: MACHINE,
    periodStartUtc: shiftPeriodStart,
    periodEndUtc: shiftPeriodEnd,
    idempotencyKey: shiftComparisonKey,
  },
);
assert(
  "scenario D shift comparison evaluation accepted",
  shiftComparisonPost.status === 201 || shiftComparisonPost.status === 200,
  `status=${shiftComparisonPost.status}`,
);
const shiftBatchId = readAssessmentId(shiftComparisonPost.json);
assert("scenario D shift comparison batch id", Boolean(shiftBatchId));
const shiftComparisonReplay = await request(
  "POST",
  `/api/process-oee/shift-comparison/evaluations?facilityId=${FACILITY}`,
  {
    machineAssetId: MACHINE,
    periodStartUtc: shiftPeriodStart,
    periodEndUtc: shiftPeriodEnd,
    idempotencyKey: shiftComparisonKey,
  },
);
assert(
  "scenario D shift comparison idempotent replay",
  shiftComparisonReplay.status === 201 || shiftComparisonReplay.status === 200,
  `status=${shiftComparisonReplay.status}`,
);
assert(
  "scenario D shift comparison idempotent entity",
  (readAssessmentId(shiftComparisonReplay.json) ?? "") === shiftBatchId,
  `first=${shiftBatchId} replay=${readAssessmentId(shiftComparisonReplay.json)}`,
);
assert(
  "scenario D shift comparison single batch",
  shiftComparisonBatchCount() === 1,
  `count=${shiftComparisonBatchCount()}`,
);
if (shiftComparisonGet.json?.isComparable) {
  assert("scenario D comparison summary", shiftComparisonGet.json?.summary != null);
}

const insufficientAssessmentId = await boundedEvaluate(
  "2026-08-17T06:00:00.000Z",
  "2026-08-17T10:00:00.000Z",
  `${CLOSURE_PREFIX}-INSUFFICIENT-${CLOSURE_RUN}`,
);
const insufficientStability = await request(
  "GET",
  `/api/process-oee/assessments/${insufficientAssessmentId}/stability?facilityId=${FACILITY}`,
);
const insufficientBody = assertStabilityEnvelope("scenario E insufficient", insufficientStability);
assert(
  "scenario E insufficient-data state",
  insufficientBody.resultState === "insufficient-data",
  `resultState=${insufficientBody.resultState}`,
);
assert(
  "scenario E insufficient sample driver",
  insufficientBody.drivers.some((d) => d.reasonCode === "insufficient-data-cycle-samples"),
);
assertNoFabricatedNormalStability("scenario E insufficient", insufficientBody);

const staleAssessmentId = await boundedEvaluate(
  "2025-01-06T06:00:00.000Z",
  "2025-01-06T14:00:00.000Z",
  `${CLOSURE_PREFIX}-STALE-${CLOSURE_RUN}`,
  { plannedProductionSeconds: 28_800 },
);
const staleStability = await request(
  "GET",
  `/api/process-oee/assessments/${staleAssessmentId}/stability?facilityId=${FACILITY}`,
);
const staleBody = assertStabilityEnvelope("scenario E stale", staleStability);
const staleSignals = staleBody.resultState === "insufficient-data"
  || staleBody.drivers.some((d) => d.reasonCode === "insufficient-data-stale-cycles")
  || staleBody.drivers.some((d) => d.freshnessState === "stale");
assert(
  "scenario E stale or insufficient evidence",
  staleSignals,
  `resultState=${staleBody.resultState}`,
);
assertNoFabricatedNormalStability("scenario E stale", staleBody);

const overview = await request("GET", `/api/process-oee/stability-overview?facilityId=${FACILITY}`);
assert("stability overview → 200", overview.status === 200, `status=${overview.status}`);
assert(
  "stability overview rule set",
  overview.json?.ruleSetVersion === "PROCESS-OEE-STABILITY-V1",
);

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
const driversBeforeReplay = stabilityDriverCountForAssessment(pilotAssessmentId);
const pilotStability = await request(
  "GET",
  `/api/process-oee/assessments/${pilotAssessmentId}/stability?facilityId=${FACILITY}`,
);
if (pilotStability.status === 200) {
  assert(
    "pilot stability drivers persisted",
    stabilityDriverCountForAssessment(pilotAssessmentId) > 0,
    `count=${stabilityDriverCountForAssessment(pilotAssessmentId)}`,
  );
  const pilotReplayAgain = await request(
    "POST",
    `/api/process-oee/evaluation-windows/${PILOT_WINDOW}/evaluations?facilityId=${FACILITY}`,
    { idempotencyKey: `${CLOSURE_PREFIX}-PILOT-REPLAY-${CLOSURE_RUN}` },
  );
  const pilotAssessmentReplayId = readAssessmentId(pilotReplayAgain.json);
  if (pilotAssessmentReplayId && pilotAssessmentReplayId !== pilotAssessmentId) {
    assert(
      "stability idempotency first assessment drivers unchanged",
      stabilityDriverCountForAssessment(pilotAssessmentId) === driversBeforeReplay,
      `before=${driversBeforeReplay} after=${stabilityDriverCountForAssessment(pilotAssessmentId)}`,
    );
    assert(
      "stability idempotency no duplicate reason_code",
      duplicateStabilityReasonCodeCount([pilotAssessmentId, pilotAssessmentReplayId]) === 0,
    );
  }
}

const closureWindowsBeforeCleanup = closureWindowCount();
const closureAssessmentsBeforeCleanup = closureAssessmentCount();
assert("closure fixtures created", closureAssessmentsBeforeCleanup >= 4);

cleanupClosureFixtures();

assert("closure windows remaining zero", closureWindowCount() === 0);
assert("closure assessments remaining zero", closureAssessmentCount() === 0);
assert("closure shift comparison batches remaining zero", shiftComparisonBatchCount() === 0);
assert(
  "closure stability drivers remaining zero",
  Number(db(`
    SELECT COUNT(*) FROM process_oee_stability_drivers d
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

console.log("PROCESS-OEE-3 stability/downtime verification passed.");
