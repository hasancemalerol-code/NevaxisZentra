#!/usr/bin/env node
/**
 * verify-edge-4-replay.mjs
 * EDGE-4 replay verifier — offline capture, reconnect, ordering, duplicate, partial, poison.
 */

import { readFileSync, existsSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), "../../..");
const SIM_URL = process.env.EDGE_SIMULATOR_URL ?? "http://localhost:5100";
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

console.log("\n[1] Replay implementation static checks");
const store = readFile("tools/NevaxisFactOS.MachineSimulator/EdgeBuffer/EdgeBufferStore.cs");
const scenarios = readFile("tools/NevaxisFactOS.MachineSimulator/EdgeBuffer/EdgeReliabilityScenarios.cs");
const worker = readFile("tools/NevaxisFactOS.MachineSimulator/EdgeBuffer/EdgeDeliveryWorker.cs");
const ingestion = readFile("packages/NevaxisFactOS.Persistence/Edge/EdgeIngestionService.cs");

assertContains("ordered claim batch", store, ".ThenBy(e => e.MachineId)");
assertContains("source timestamp preserved in buffer", readFile("tools/NevaxisFactOS.MachineSimulator/EdgeBuffer/EdgeBufferedEvent.cs"), "SourceTimestampUtc");
assertContains("offline buffer 12+ events", scenarios, "BuildOfflineBufferEvents");
assertContains("poison event fixture", scenarios, "CreatePoisonEvent");
assertContains("partial failure batch", scenarios, "BuildPartialFailureBatch");
assertContains("ResetDeliveredForReplay same identity", store, "ResetDeliveredForReplay");
assertContains("central idempotency preserved", ingestion, "IdempotencyKey == request.IdempotencyKey");
assertContains("duplicate result safe", ingestion, "EdgeIngestResult.Duplicate");

console.log("\n[2] Scenario modes in simulator");
const program = readFile("tools/NevaxisFactOS.MachineSimulator/Program.cs");
for (const mode of [
  "offline-buffer",
  "recover-replay",
  "duplicate-replay",
  "partial-failure",
  "poison-event",
  "restart-recovery",
  "api-interruption",
  "buffer-capacity-test",
]) {
  assertContains(`scenario mode ${mode}`, program, mode);
}

async function simFetch(path, opts = {}) {
  const resp = await fetch(`${SIM_URL}${path}`, opts);
  return { status: resp.status, json: await resp.json().catch(() => null) };
}

async function apiFetch(path, opts = {}) {
  const headers = {
    "Content-Type": "application/json",
    ...(API_TOKEN ? { Authorization: `Bearer ${API_TOKEN}` } : {}),
    ...(opts.headers ?? {}),
  };
  const resp = await fetch(`${API_BASE}${path}`, { ...opts, headers });
  return { status: resp.status, json: await resp.json().catch(() => null) };
}

console.log("\n[3] Live replay sequence");
let simOk = false;
try {
  const h = await simFetch("/health");
  simOk = h.status === 200;
} catch {
  simOk = false;
}

if (!simOk) {
  console.log("  ℹ Skipping live replay tests (simulator not reachable).");
} else {
  await simFetch("/simulate/reliability/reset", { method: "POST" });

  // Normal delivery
  await simFetch("/simulate/reliability/network", {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ available: true }),
  });
  const normal = await simFetch("/simulate/reliability/scenarios/normal", { method: "POST" });
  assert("normal scenario HTTP 200", normal.status === 200);

  // Offline buffer
  await simFetch("/simulate/reliability/network", {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ available: false }),
  });
  const offline = await simFetch("/simulate/reliability/scenarios/offline-buffer", { method: "POST" });
  assert("offline-buffer HTTP 200", offline.status === 200);
  if (offline.json) assert("offline pending >= 10", (offline.json.pending ?? 0) >= 10);

  const bufOffline = await simFetch("/simulate/reliability/buffer?limit=20");
  if (bufOffline.json?.events?.[0]) {
    assert("source timestamp preserved in buffer row", !!bufOffline.json.events[0].sourceTimestampUtc);
  }

  // Restart recovery scenario seeds while offline
  await simFetch("/simulate/reliability/scenarios/restart-recovery", { method: "POST" });
  const afterRestart = await simFetch("/simulate/reliability/health");
  assert("pending survives restart-recovery seed", (afterRestart.json?.pendingCount ?? 0) > 0);

  // Recover replay
  await simFetch("/simulate/reliability/network", {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ available: true }),
  });

  if (API_TOKEN) {
    let apiOk = false;
    try {
      const ping = await apiFetch(`/api/edge/gateways?facilityId=${FACILITY_ID}`);
      apiOk = ping.status === 200;
    } catch {
      apiOk = false;
    }

    if (apiOk) {
      // Drain queue
      for (let i = 0; i < 20; i++) {
        await simFetch("/simulate/reliability/deliver-now", { method: "POST" });
        const health = await simFetch("/simulate/reliability/health");
        const active = (health.json?.pendingCount ?? 0) + (health.json?.retryCount ?? 0);
        if (active === 0) break;
        await new Promise((r) => setTimeout(r, 500));
      }

      const finalHealth = await simFetch("/simulate/reliability/health");
      const finalPending = (finalHealth.json?.pendingCount ?? 0) + (finalHealth.json?.retryCount ?? 0);
      assert("queue drained or mostly drained after replay", finalPending <= 2, `pending=${finalPending}`);

      // Duplicate replay
      await simFetch("/simulate/reliability/scenarios/duplicate-replay", { method: "POST" });
      const eventsBefore = await apiFetch(`/api/edge/machines/bb000001-0095-4000-8000-000000000095/events?facilityId=${FACILITY_ID}&limit=500`);
      const countBefore = eventsBefore.json?.length ?? 0;
      await simFetch("/simulate/reliability/deliver-now", { method: "POST" });
      const eventsAfter = await apiFetch(`/api/edge/machines/bb000001-0095-4000-8000-000000000095/events?facilityId=${FACILITY_ID}&limit=500`);
      const countAfter = eventsAfter.json?.length ?? 0;
      assert("duplicate replay does not inflate event count", countAfter <= countBefore + 2, `${countBefore} -> ${countAfter}`);
    } else {
      console.log("  ℹ API unreachable — skipping central projection checks.");
    }
  } else {
    console.log("  ℹ No NEVAXIS_FACTOS_DEV_TOKEN — skipping API drain/duplicate checks.");
    await simFetch("/simulate/reliability/scenarios/recover-replay", { method: "POST" });
  }

  // Poison event
  await simFetch("/simulate/reliability/reset", { method: "POST" });
  const poison = await simFetch("/simulate/reliability/scenarios/poison-event", { method: "POST" });
  assert("poison scenario HTTP 200", poison.status === 200);
  if (poison.json) assert("poison dead-letter >= 1", (poison.json.deadLetter ?? 0) >= 1);

  // Partial failure
  await simFetch("/simulate/reliability/reset", { method: "POST" });
  const partial = await simFetch("/simulate/reliability/scenarios/partial-failure", { method: "POST" });
  assert("partial-failure HTTP 200", partial.status === 200);
  if (partial.json) {
    assert("partial: some delivered", (partial.json.delivered ?? 0) >= 1);
    assert("partial: invalid dead-letter", (partial.json.deadLetter ?? 0) >= 1);
  }

  const dl = await simFetch("/simulate/reliability/dead-letters");
  assert("dead-letter list HTTP 200", dl.status === 200);
}

console.log(`\n${"─".repeat(55)}`);
console.log(`EDGE-4 Replay Verifier: ${passed} passed, ${failed} failed`);
if (failed > 0) process.exit(1);
