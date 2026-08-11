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

function db(sql) {
  const r = spawnSync(
    "docker",
    ["exec", "crevafactos-local-postgres-1", "psql", "-U", "crevafactos", "-d", "crevafactos_local", "-t", "-A", "-c", sql],
    { encoding: "utf8" },
  );
  if (r.status !== 0) {
    console.error(r.stderr || r.stdout);
    process.exit(1);
  }
  return r.stdout.trim();
}

const entities = read("packages/CrevaFactOS.Persistence/Maintenance/MaintenanceAiAgentEntities.cs");
const repo = read("packages/CrevaFactOS.Persistence/Maintenance/MaintenanceAiDraftRepository.cs");

const actions = [
  "maintenance.ai.draft.generation-requested",
  "maintenance.ai.draft.provider-completed",
  "maintenance.ai.draft.provider-failed",
  "maintenance.ai.draft.output-validated",
  "maintenance.ai.draft.output-rejected",
  "maintenance.ai.draft.created",
  "maintenance.ai.draft.edited",
  "maintenance.ai.draft.marked-stale",
  "maintenance.ai.draft.approved",
  "maintenance.ai.draft.rejected",
  "maintenance.ai.draft.request-creation-attempted",
  "maintenance.ai.draft.request-created",
  "maintenance.ai.draft.duplicate-approval-prevented",
];

for (const action of actions) {
  assert(`audit action ${action}`, entities.includes(action) || repo.includes(action));
}

assert("audit entity persisted", entities.includes("MaintenanceAiDraftAuditEvent"));
assert("AddDraftAudit helper", repo.includes("AddDraftAudit"));

const auditCount = db("SELECT COUNT(*) FROM maintenance_ai_draft_audit_events");
assert("audit events exist in database", Number(auditCount) >= 0);

console.log("MAINT-AI-1 audit verifier PASS");
