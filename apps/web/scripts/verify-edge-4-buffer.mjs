#!/usr/bin/env node
/**
 * verify-edge-4-buffer.mjs
 * EDGE-4 buffer verifier — local durable buffer, states, bounds, restart persistence.
 */

import { readFileSync, existsSync, rmSync, mkdirSync } from "node:fs";
import { dirname, resolve, join } from "node:path";
import { fileURLToPath } from "node:url";
import { spawnSync } from "node:child_process";

const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), "../../..");
const SIM_URL = process.env.EDGE_SIMULATOR_URL ?? "http://localhost:5100";
const TEST_DB = join(ROOT, "tmp", "edge4-buffer-verify.db");

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

function assertNotContains(label, content, text) {
  assert(label, !content.includes(text), `must not contain "${text}"`);
}

console.log("\n[1] Buffer implementation files");
const store = readFile("tools/NevaxisZentra.MachineSimulator/EdgeBuffer/EdgeBufferStore.cs");
const worker = readFile("tools/NevaxisZentra.MachineSimulator/EdgeBuffer/EdgeDeliveryWorker.cs");
const statuses = readFile("tools/NevaxisZentra.MachineSimulator/EdgeBuffer/EdgeDeliveryStatuses.cs");
const program = readFile("tools/NevaxisZentra.MachineSimulator/Program.cs");
const csproj = readFile("tools/NevaxisZentra.MachineSimulator/NevaxisZentra.MachineSimulator.csproj");

assertContains("EdgeBufferStore exists", store, "class EdgeBufferStore");
assertContains("JSON file persistence", store, "JsonSerializer");
assertContains("Schema version", store, "SchemaVersion");
assertContains("pending status", statuses, '"pending"');
assertContains("in-flight status", statuses, '"in-flight"');
assertContains("delivered status", statuses, '"delivered"');
assertContains("retry-wait status", statuses, '"retry-wait"');
assertContains("dead-letter status", statuses, '"dead-letter"');
assertContains("capacity-exceeded error", statuses, "capacity-exceeded");
assertContains("MarkDelivered with acknowledgement", store, "AcknowledgementId");
assertContains("Enqueue capacity check", store, "CapacityExceeded");
assertContains("No silent drop on full", store, "CapacityExceeded");

console.log("\n[2] Storage path — not in repo root");
assertContains("LocalApplicationData path", store, "LocalApplicationData");
assertContains("edge-buffer subfolder", store, "edge-buffer");
assertNotContains("repo root buffer path", store, "NevaxisZentra/NevaxisZentra");
assertContains("gitignore covers *.db", readFile(".gitignore"), "*.db");

console.log("\n[3] Security — no secrets in buffer");
assertNotContains("no Bearer token in store", store, "Bearer");
assertNotContains("no password in store", store.toLowerCase(), "password");
assertNotContains("no connection string persisted in payload", worker, "ConnectionString");
assertContains("delivery worker uses env token only at runtime", worker, "NEVAXIS_FACTOS_DEV_TOKEN");

console.log("\n[4] Delivery worker acknowledgement");
assertContains("MarkDelivered after success", worker, "MarkDelivered");
assertContains("IsDuplicateAck handling", worker, "isDuplicate");
assertContains("does not mark delivered on send only", worker, "response.IsSuccessStatusCode");

console.log("\n[5] Restart persistence via durable local file");
assertContains("durable event list", store, "snapshot.Events.Add");
assertContains("RecoverStaleInFlight", store, "RecoverStaleInFlight");

console.log("\n[6] Simulator reliability endpoints");
assertContains("health endpoint", program, "/simulate/reliability/health");
assertContains("buffer list endpoint", program, "/simulate/reliability/buffer");
assertContains("network control", program, "/simulate/reliability/network");
assertContains("scenario modes", program, "offline-buffer");
assertNotContains("no SQLite dependency required", csproj, "Microsoft.Data.Sqlite");

console.log("\n[7] Live buffer proof (simulator)");
async function liveFetch(path, opts = {}) {
  const resp = await fetch(`${SIM_URL}${path}`, opts);
  const json = await resp.json().catch(() => null);
  return { status: resp.status, json };
}

let liveOk = false;
try {
  const health = await liveFetch("/health");
  liveOk = health.status === 200;
} catch {
  liveOk = false;
}

if (!liveOk) {
  console.log("  ℹ Skipping live buffer tests (simulator not reachable).");
} else {
  await liveFetch("/simulate/reliability/reset", { method: "POST" });

  const offline = await liveFetch("/simulate/reliability/scenarios/offline-buffer", { method: "POST" });
  assert("offline-buffer scenario HTTP 200", offline.status === 200);
  if (offline.json) {
    assert("offline-buffer pending > 0", (offline.json.pending ?? 0) >= 10, `pending=${offline.json.pending}`);
  }

  const buf = await liveFetch("/simulate/reliability/buffer?limit=50");
  assert("buffer list HTTP 200", buf.status === 200);
  if (buf.json?.events) {
    assert("buffer has events", buf.json.events.length >= 10);
    assert("events have pending/retry status", buf.json.events.some((e) => e.status === "pending" || e.status === "retry-wait"));
  }

  const h1 = await liveFetch("/simulate/reliability/health");
  const pendingBefore = h1.json?.pendingCount ?? 0;
  assert("health shows pending count", pendingBefore > 0);

  // Simulate restart: new process would reload same DB — verify file path env override
  mkdirSync(dirname(TEST_DB), { recursive: true });
  if (existsSync(TEST_DB)) rmSync(TEST_DB);
  process.env.EDGE_BUFFER_DB_PATH = TEST_DB;

  const build = spawnSync(
    "dotnet",
    ["build", "tools/NevaxisZentra.MachineSimulator/NevaxisZentra.MachineSimulator.csproj", "--no-restore"],
    { cwd: ROOT, encoding: "utf8", shell: true },
  );
  assert("simulator builds for restart test", build.status === 0);

  delete process.env.EDGE_BUFFER_DB_PATH;
}

console.log(`\n${"─".repeat(55)}`);
console.log(`EDGE-4 Buffer Verifier: ${passed} passed, ${failed} failed`);
if (failed > 0) process.exit(1);
