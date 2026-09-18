#!/usr/bin/env node
import { existsSync, readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

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

const provider = read("packages/NevaxisZentra.Persistence/Maintenance/DeterministicMaintenanceAiProvider.cs");
const models = read("packages/NevaxisZentra.Persistence/Maintenance/MaintenanceAiAgentModels.cs");

assert("prompt injection marker detection", provider.includes("ContainsPromptInjectionMarker"));
assert("IGNORE PREVIOUS synthetic marker", provider.includes("IGNORE PREVIOUS"));
assert("SYSTEM PROMPT synthetic marker", provider.includes("SYSTEM PROMPT"));
assert("injection handled as evidence", provider.includes("yalnızca kanıt olarak değerlendirildi"));
assert("bounded input snapshot DTO", models.includes("MaintenanceAiInputSnapshot"));
assert("no unbounded history in snapshot builder", read("packages/NevaxisZentra.Persistence/Maintenance/MaintenanceAiAgentServices.cs").includes(".Take("));

console.log("MAINT-AI-1 prompt-injection verifier PASS");
