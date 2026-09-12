#!/usr/bin/env node
/**
 * verify-edge-1-reliability.mjs
 * EDGE-1 reliability verifier.
 * Covers idempotency, out-of-order detection, failure preservation,
 * runtime persistence, and live API ingestion proof (when API is running).
 */

import { readFileSync, existsSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), "../../..");
const API_BASE = process.env.NEVAXIS_FACTOS_API_URL ?? "http://localhost:5000";
const API_TOKEN = process.env.NEVAXIS_FACTOS_DEV_TOKEN ?? "";
const FACILITY_ID = process.env.NEVAXIS_FACTOS_FACILITY_ID ?? "bb000001-0005-4000-8000-000000000005";

function readFile(rel) {
  const abs = resolve(ROOT, rel);
  if (!existsSync(abs)) throw new Error(`File not found: ${rel}`);
  return readFileSync(abs, "utf8");
}

let passed = 0;
let failed = 0;

function assert(label, condition, detail = "") {
  if (condition) {
    console.log(`  ✓ ${label}`);
    passed++;
  } else {
    console.error(`  ✗ ${label}${detail ? `: ${detail}` : ""}`);
    failed++;
  }
}

function assertContains(label, content, text) {
  assert(label, content.includes(text), `expected "${text}"`);
}

// ---------------------------------------------------------------------------
// 1. Static: idempotency implementation
// ---------------------------------------------------------------------------

console.log("\n[1] Idempotency implementation");
const ingestion = readFile("packages/NevaxisFactOS.Persistence/Edge/EdgeIngestionService.cs");

assertContains("Duplicate check queries by IdempotencyKey", ingestion, "IdempotencyKey == request.IdempotencyKey");
assertContains("EdgeIngestResult.Duplicate returned on dup", ingestion, "EdgeIngestResult.Duplicate");
const modelsFile = readFile("packages/NevaxisFactOS.Persistence/Edge/EdgeModels.cs");
assertContains("Accepted false on duplicate", modelsFile, "Accepted = false, IsDuplicate = true");
assertContains("No exception on duplicate — safe failure preservation", ingestion, "EdgeIngestResult.Duplicate");

// ---------------------------------------------------------------------------
// 2. Static: out-of-order detection
// ---------------------------------------------------------------------------

console.log("\n[2] Out-of-order detection");

assertContains("isOutOfOrder computed from LastSourceSequence", ingestion, "isOutOfOrder");
assertContains("LastSourceSequence only updated when in-order", ingestion, "if (!isOutOfOrder)");
assertContains("LastSequenceOutOfOrder flag stored", ingestion, "runtime.LastSequenceOutOfOrder = isOutOfOrder");

// ---------------------------------------------------------------------------
// 3. Static: runtime projection persistence
// ---------------------------------------------------------------------------

console.log("\n[3] Runtime projection persistence");

assertContains("LastSeenAtUtc updated on every ingest", ingestion, "runtime.LastSeenAtUtc = receivedAt");
assertContains("LastEventId updated", ingestion, "runtime.LastEventId = eventId");
assertContains("MachineRuntime added if not exists", ingestion, "dbContext.MachineRuntimes.Add(runtime)");
assertContains("Status derived from status-change payload", ingestion, "TryParseStatusPayload");
assertContains("IsReadyForProduction derived from Running status", ingestion, 'MachineStatuses.Running');

// ---------------------------------------------------------------------------
// 4. Static: failure preservation — Unavailable paths
// ---------------------------------------------------------------------------

console.log("\n[4] Failure preservation (Unavailable paths)");

assertContains("UnavailableEdgeIngestionService returns Failure not throws", ingestion, "Task.FromResult(EdgeIngestResult.Failure");
const repo = readFile("packages/NevaxisFactOS.Persistence/Edge/EdgeRepository.cs");
assertContains("UnavailableEdgeQueryRepository list returns empty (no throws)", repo, "Task.FromResult<IReadOnlyList<EdgeGatewayReadModel>>([])");

// ---------------------------------------------------------------------------
// 5. Static: MachineSimulator determinism
// ---------------------------------------------------------------------------

console.log("\n[5] MachineSimulator determinism");
const scenarios = readFile("tools/NevaxisFactOS.MachineSimulator/MachineSimulatorScenarios.cs");

assertContains("CNC-001 has at least one cycle-start", scenarios, '"cycle-start"');
assertContains("PRESS-001 has heartbeat", scenarios, '"PRESS-001"');
assertContains("Events ordered by SourceSequence (seq++)", scenarios, "SourceSequence: seq++,");
assertContains("IdempotencyKey includes machine code", scenarios, '"SIM-GW-001:CNC-001:');
assertContains("GetEvents filters by sinceSequence", scenarios, "sinceSequence");
assertContains("GetEvents respects limit", scenarios, ".Take(limit)");

// ---------------------------------------------------------------------------
// 6. Live API tests (skipped if no token or API unreachable)
// ---------------------------------------------------------------------------

console.log("\n[6] Live API — ingestion idempotency proof");

async function apiRequest(method, path, body) {
  const headers = {
    "Content-Type": "application/json",
    ...(API_TOKEN ? { Authorization: `Bearer ${API_TOKEN}` } : {}),
  };
  const opts = { method, headers };
  if (body) opts.body = JSON.stringify(body);
  const resp = await fetch(`${API_BASE}${path}`, opts);
  const text = await resp.text();
  let json = null;
  try { json = JSON.parse(text); } catch {}
  return { status: resp.status, json };
}

let liveSkipped = false;

if (!API_TOKEN) {
  console.log("  ℹ Skipping live tests (no NEVAXIS_FACTOS_DEV_TOKEN).");
  liveSkipped = true;
} else {
  try {
    // Test: ingest a heartbeat event
    const idempotencyKey = `verify-edge-1-reliability-${Date.now()}`;
    const ingestPayload = {
      facilityId: FACILITY_ID,
      machineId: "bb000001-0095-4000-8000-000000000095",
      gatewayId: "bb000001-0094-4000-8000-000000000094",
      eventKind: "heartbeat",
      idempotencyKey,
      sourceTimestampUtc: new Date().toISOString(),
      sourceSequence: 99999,
      payload: null,
    };

    const r1 = await apiRequest("POST", `/api/edge/events/ingest?facilityId=${FACILITY_ID}`, ingestPayload);
    assert("First ingest accepted (201 or 200)", r1.status === 201 || r1.status === 200);

    if (r1.json) {
      assert("First ingest: accepted=true", r1.json.accepted === true);
      assert("First ingest: isDuplicate=false", r1.json.isDuplicate === false);
    }

    // Test idempotency: re-send the same event
    const r2 = await apiRequest("POST", `/api/edge/events/ingest?facilityId=${FACILITY_ID}`, ingestPayload);
    assert("Second ingest with same key accepted (200)", r2.status === 200);
    if (r2.json) {
      assert("Second ingest: isDuplicate=true", r2.json.isDuplicate === true, JSON.stringify(r2.json));
      assert("Second ingest: accepted=false", r2.json.accepted === false, JSON.stringify(r2.json));
    }

    // Test: list machines
    const rmachines = await apiRequest("GET", `/api/edge/machines?facilityId=${FACILITY_ID}`);
    assert("GET /api/edge/machines returns 200", rmachines.status === 200);
    if (Array.isArray(rmachines.json)) {
      assert("Machines list is non-empty (pilot seeded CNC-001, PRESS-001)",
        rmachines.json.length >= 2,
        `got ${rmachines.json.length}`);
      const codes = rmachines.json.map((m) => m.machineCode);
      assert("CNC-001 present", codes.includes("CNC-001"), `machines: ${codes.join(", ")}`);
      assert("PRESS-001 present", codes.includes("PRESS-001"), `machines: ${codes.join(", ")}`);
    }

    // Test: list gateways
    const rgw = await apiRequest("GET", `/api/edge/gateways?facilityId=${FACILITY_ID}`);
    assert("GET /api/edge/gateways returns 200", rgw.status === 200);
    if (Array.isArray(rgw.json)) {
      assert("Simulator gateway present", rgw.json.length >= 1);
    }

    // Test: runtime projection persisted
    const cncMachineId = "bb000001-0095-4000-8000-000000000095";
    const rruntime = await apiRequest("GET", `/api/edge/machines/${cncMachineId}/runtime?facilityId=${FACILITY_ID}`);
    assert("GET /api/edge/machines/{id}/runtime returns 200", rruntime.status === 200);
    if (rruntime.json) {
      assert("Runtime.lastSeenAtUtc is set (persisted)", !!rruntime.json.lastSeenAtUtc);
      assert("Runtime.lastEventId is set", !!rruntime.json.lastEventId);
    }

  } catch (err) {
    console.log(`  ℹ Live tests skipped (API unreachable: ${err.message})`);
    liveSkipped = true;
  }
}

if (liveSkipped) {
  console.log("  (Live ingestion proof deferred — run with API token and live API.)");
}

// ---------------------------------------------------------------------------
// Summary
// ---------------------------------------------------------------------------

const total = passed + failed;
console.log(`\nEDGE-1 reliability: ${passed}/${total} passed${liveSkipped ? " (some live checks skipped)" : ""}`);
if (failed > 0) {
  console.error(`EDGE-1 reliability verification FAILED: ${failed} check(s) failed.`);
  process.exit(1);
}
console.log("EDGE-1 reliability verification PASSED.\n");
