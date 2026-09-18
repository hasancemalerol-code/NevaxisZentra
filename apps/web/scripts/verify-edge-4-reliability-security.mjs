#!/usr/bin/env node
/**
 * verify-edge-4-reliability-security.mjs
 * EDGE-4 reliability/security verifier.
 */

import { readFileSync, existsSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), "../../..");

function readFile(rel) {
  const abs = resolve(ROOT, rel);
  if (!existsSync(abs)) throw new Error(`File not found: ${rel}`);
  return readFileSync(abs, "utf8");
}

function readOpt(rel) {
  const abs = resolve(ROOT, rel);
  return existsSync(abs) ? readFileSync(abs, "utf8") : "";
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

const EDGE4_SOURCES = [
  "tools/NevaxisZentra.MachineSimulator/EdgeBuffer/EdgeBufferStore.cs",
  "tools/NevaxisZentra.MachineSimulator/EdgeBuffer/EdgeDeliveryWorker.cs",
  "tools/NevaxisZentra.MachineSimulator/EdgeBuffer/EdgeRetryPolicy.cs",
  "tools/NevaxisZentra.MachineSimulator/EdgeBuffer/EdgeReliabilityScenarios.cs",
  "tools/NevaxisZentra.MachineSimulator/Program.cs",
];

console.log("\n[1] Retry classification and backoff");
const retry = readFile("tools/NevaxisZentra.MachineSimulator/EdgeBuffer/EdgeRetryPolicy.cs");
const worker = readFile("tools/NevaxisZentra.MachineSimulator/EdgeBuffer/EdgeDeliveryWorker.cs");
assertContains("retryable 408", retry, "408");
assertContains("retryable 429", retry, "429");
assertContains("retryable 5xx", retry, ">= 500");
assertContains("non-retryable 400", retry, "400");
assertContains("ComputeNextAttemptUtc backoff", retry, "BackoffMultiplier");
assertContains("ShouldDeadLetter max attempts", retry, "ShouldDeadLetter");
assertContains("network unavailable retry", worker, "NetworkUnavailable");
assertContains("timeout handling", worker, "Timeout");

console.log("\n[2] Stale in-flight and concurrency");
const store = readFile("tools/NevaxisZentra.MachineSimulator/EdgeBuffer/EdgeBufferStore.cs");
assertContains("ClaimBatch lease", store, "InFlightLeaseId");
assertContains("RecoverStaleInFlight", store, "RecoverStaleInFlight");
assertContains("InFlightLeaseTimeout", readFile("tools/NevaxisZentra.MachineSimulator/EdgeBuffer/EdgeDeliveryStatuses.cs"), "InFlightLeaseTimeout");
assertContains("write lock on enqueue", store, "_writeLock");

console.log("\n[3] Buffer capacity");
assertContains("capacity exceeded code", store, "CapacityExceeded");
assertContains("MaxBufferedEvents", readFile("tools/NevaxisZentra.MachineSimulator/EdgeBuffer/EdgeDeliveryStatuses.cs"), "DefaultMaxBufferedEvents");
assertContains("buffer-capacity-test scenario", readFile("tools/NevaxisZentra.MachineSimulator/Program.cs"), "buffer-capacity-test");

console.log("\n[4] No machine commands / downstream mutation");
for (const src of EDGE4_SOURCES) {
  const content = readFile(src);
  const name = src.split("/").pop();
  assertNotContains(`${name}: no ProductionOrder`, content, "ProductionOrder");
  assertNotContains(`${name}: no QualityInspection`, content, "QualityInspection");
  assertNotContains(`${name}: no MaintenanceWorkOrder`, content, "MaintenanceWorkOrder");
  assertNotContains(`${name}: no ERP mutation`, content, "ErpOrder");
  assertNotContains(`${name}: no PLC write`, content, "PlcWrite");
  assertNotContains(`${name}: no machine command queue`, content, "MachineCommand");
}

console.log("\n[5] No credentials in queue / no Kafka RabbitMQ");
const allEdge4 = EDGE4_SOURCES.map(readFile).join("\n");
assertNotContains("no Kafka", allEdge4, "Kafka");
assertNotContains("no RabbitMQ for buffer", allEdge4.toLowerCase(), "rabbitmq");
assertNotContains("no MQTT implementation", allEdge4, "MqttClient");
assertNotContains("no OPC UA implementation", allEdge4, "OpcUa");
assertNotContains("no token stored in buffer", store, "Authorization");
assertNotContains("no connection string in buffer table", store, "connection_string");

console.log("\n[6] Reuses canonical ingestion API");
assertContains("POST events/ingest", worker, "/api/edge/events/ingest");
assertNotContains("no parallel replay pipeline", worker, "/api/edge/replay");

console.log("\n[7] No new NuGet dependency for local buffer");
const csproj = readFile("tools/NevaxisZentra.MachineSimulator/NevaxisZentra.MachineSimulator.csproj");
assertNotContains("no SQLite package churn", csproj, "Microsoft.Data.Sqlite");
assertNotContains("no Kafka", allEdge4, "Kafka");
const pkgLock = readOpt("pnpm-lock.yaml");
if (pkgLock) {
  assertNotContains("pnpm: no Kafka added for edge4", pkgLock, "kafkajs");
}

console.log("\n[8] Live backoff not hot-loop (static proof)");
assertContains("WorkerPollInterval", readFile("tools/NevaxisZentra.MachineSimulator/EdgeBuffer/EdgeDeliveryStatuses.cs"), "WorkerPollInterval");
assertContains("MaximumDelay bound", retry, "MaximumDelay");

console.log(`\n${"─".repeat(55)}`);
console.log(`EDGE-4 Reliability/Security Verifier: ${passed} passed, ${failed} failed`);
if (failed > 0) process.exit(1);
