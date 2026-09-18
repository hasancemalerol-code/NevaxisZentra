#!/usr/bin/env node
import { existsSync, readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";
import { spawnSync } from "node:child_process";

const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), "../../..");

function read(rel) {
  const abs = resolve(ROOT, rel);
  if (!existsSync(abs)) throw new Error(`Missing ${rel}`);
  return readFileSync(abs, "utf8");
}

function assert(label, ok, detail = "") {
  if (!ok) {
    console.error(`✗ ${label}${detail ? `: ${detail}` : ""}`);
    process.exit(1);
  }
  console.log(`✓ ${label}`);
}

const repo = read("packages/NevaxisZentra.Persistence/Maintenance/MaintenanceConditionRepository.cs");
const endpoints = read("apps/api/Maintenance/MaintenanceConditionEndpointRouteBuilderExtensions.cs");
const migration = read("packages/NevaxisZentra.Persistence.Migrations/Migrations/20260811120000_AddMaintenanceConditionIntelligenceFoundation.cs");
const edgeRepo = read("packages/NevaxisZentra.Persistence/Edge/EdgeRepository.cs");

assert("maintenance read access", endpoints.includes("ResolveReadAccessAsync"));
assert("edge read-only consumption", repo.includes("MachineTelemetrySamples"));
assert("no edge write in condition repo", !repo.includes("IEdgeIngestionService"));
assert("facility filter", repo.includes("FacilityId == context.FacilityId"));
assert("RLS enabled", migration.includes("ENABLE ROW LEVEL SECURITY"));
assert("FORCE RLS", migration.includes("FORCE ROW LEVEL SECURITY"));
assert("no auto work order", !repo.toLowerCase().includes("createmaintenanceworkorder"));
assert("no auto request", !repo.toLowerCase().includes("createmaintenancerequest"));
assert("no machine command", !endpoints.toLowerCase().includes("machine command"));
assert("no production mutation in condition layer", !repo.includes("ProductionOrder"));

for (const forbidden of ["remaining useful life", "failure probability", "OpenAI", "LLM"]) {
  assert(`forbidden term absent: ${forbidden}`, !repo.toLowerCase().includes(forbidden));
}

console.log("Running maintenance integration boundary regression");
const maintBoundary = spawnSync("node", [resolve(ROOT, "apps/web/scripts/verify-maintenance-integration-boundaries.mjs")], {
  cwd: ROOT,
  encoding: "utf8",
  shell: true,
});
if (maintBoundary.stdout) process.stdout.write(maintBoundary.stdout);
if (maintBoundary.stderr) process.stderr.write(maintBoundary.stderr);
assert("maintenance integration boundaries", maintBoundary.status === 0);

console.log("Running edge integration gate boundaries regression");
const edgeBoundary = spawnSync("node", [resolve(ROOT, "apps/web/scripts/verify-edge-integration-gate-boundaries.mjs")], {
  cwd: ROOT,
  encoding: "utf8",
  shell: true,
});
if (edgeBoundary.stdout) process.stdout.write(edgeBoundary.stdout);
if (edgeBoundary.stderr) process.stderr.write(edgeBoundary.stderr);
assert("edge integration gate boundaries", edgeBoundary.status === 0);

assert("edge repository remains read-focused", edgeRepo.includes("ListTelemetryHistoryAsync"));
console.log("MAINT-PRED-1 security/boundaries verifier PASS");
