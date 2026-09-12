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

const entities = read("packages/NevaxisFactOS.Persistence/Maintenance/MaintenanceAiAgentEntities.cs");
const labels = read("apps/web/src/features/maintenance/presentation/maintenance-ai-agent-labels.ts");
const repo = read("packages/NevaxisFactOS.Persistence/Maintenance/MaintenanceAiDraftRepository.cs");

const statuses = ["draft", "under-review", "approved", "rejected", "stale", "executed", "execution-failed"];
for (const s of statuses) {
  assert(`status code ${s}`, entities.includes(s));
}

assert("Turkish Taslak", labels.includes("Taslak"));
assert("Turkish İncelemede", labels.includes("İncelemede"));
assert("Turkish Onaylandı", labels.includes("Onaylandı"));
assert("Turkish Reddedildi", labels.includes("Reddedildi"));
assert("Turkish Güncelliğini Yitirdi", labels.includes("Güncelliğini Yitirdi"));
assert("Turkish Uygulandı", labels.includes("Uygulandı"));
assert("Turkish Uygulama Başarısız", labels.includes("Uygulama Başarısız"));
assert("terminal rejected immutable", repo.includes("Draft was rejected") || repo.includes("AlreadyRejected"));
assert("terminal executed idempotent", repo.includes("CreatedMaintenanceRequestId.HasValue"));
assert("edit prohibited on executed", repo.includes("Draft cannot be edited in current status"));
assert("version increment on edit", repo.includes("draft.Version += 1"));
assert("original AI output preserved", repo.includes("AiOriginalOutputJson"));

console.log("MAINT-AI-1 state-machine verifier PASS");
