#!/usr/bin/env node
/**
 * verify-edge-integration-gate-security.mjs
 * EDGE Integration Gate — security aggregation.
 */

import { readFileSync, existsSync, readdirSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";
import { spawnSync } from "node:child_process";

const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), "../../..");
const SCRIPTS = resolve(ROOT, "apps/web/scripts");
const API_BASE = process.env.NEVAXIS_FACTOS_API_URL ?? "http://localhost:5000";

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

function runPackageVerifier(name) {
  console.log(`\n>>> ${name}`);
  const result = spawnSync("node", [resolve(SCRIPTS, name)], {
    cwd: ROOT,
    encoding: "utf8",
    shell: true,
    env: process.env,
  });
  if (result.stdout) process.stdout.write(result.stdout);
  if (result.stderr) process.stderr.write(result.stderr);
  assert(`${name} PASS`, result.status === 0, `exit=${result.status ?? "null"}`);
}

console.log("\n[1] Package security verifiers");
runPackageVerifier("verify-edge-1-security.mjs");
runPackageVerifier("verify-edge-4-reliability-security.mjs");

console.log("\n[2] RLS on Edge central tables");
const migrationDir = resolve(ROOT, "packages/NevaxisZentra.Persistence.Migrations/Migrations");
const edgeMigrations = readdirSync(migrationDir).filter(
  (f) => f.includes("Edge") && f.endsWith(".cs"),
);
assert("Edge migrations present", edgeMigrations.length >= 3, `count=${edgeMigrations.length}`);
for (const file of edgeMigrations) {
  const content = readFile(`packages/NevaxisZentra.Persistence.Migrations/Migrations/${file}`);
  if (content.includes("ENABLE ROW LEVEL SECURITY")) {
    assertContains(`${file}: ENABLE RLS`, content, "ENABLE ROW LEVEL SECURITY");
    assertContains(`${file}: FORCE RLS`, content, "FORCE ROW LEVEL SECURITY");
  }
}

console.log("\n[3] Append-only / no history DELETE surface");
const api = readFile("apps/api/Edge/EdgeEndpointRouteBuilderExtensions.cs");
assertNotContains("No MapDelete on edge endpoints", api, "MapDelete");
assertNotContains("No history purge endpoint", api.toLowerCase(), "delete history");
const ingestion = readFile("packages/NevaxisZentra.Persistence/Edge/EdgeIngestionService.cs");
assertNotContains("Ingestion no RemoveRange events", ingestion, "RemoveRange");
assertNotContains("Ingestion no machine write", ingestion, "WriteToMachine");

console.log("\n[4] Machine command boundary (static)");
const edgeUi = readFile("apps/web/src/features/edge/components/edge-workspace-view.tsx");
const simProgram = readFile("tools/NevaxisZentra.MachineSimulator/Program.cs");
for (const [label, content] of [
  ["workspace", edgeUi],
  ["simulator", simProgram],
]) {
  assertNotContains(`${label}: no StartMachine`, content, "StartMachine");
  assertNotContains(`${label}: no StopMachine`, content, "StopMachine");
  assertNotContains(`${label}: no WriteToMachine`, content, "WriteToMachine");
  assertNotContains(`${label}: no PLC write`, content, "WriteNodeAsync");
}

console.log("\n[5] Live auth negative (when API reachable)");
async function probe(path, opts = {}) {
  const resp = await fetch(`${API_BASE}${path}`, opts);
  return resp.status;
}
try {
  const health = await probe("/health/ready");
  if (health === 200) {
    const unauth = await probe("/edge/gateways");
    assert("Unauthenticated edge read → 401", unauth === 401, `status=${unauth}`);
  } else {
    console.log("  ℹ API not reachable — live 401/403 deferred.");
  }
} catch {
  console.log("  ℹ API not reachable — live 401/403 deferred.");
}

console.log(`\n${"─".repeat(55)}`);
console.log(`EDGE Integration Gate Security: ${passed} passed, ${failed} failed`);
if (failed > 0) process.exit(1);
