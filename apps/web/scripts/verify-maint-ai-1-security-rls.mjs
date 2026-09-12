#!/usr/bin/env node
import { spawnSync } from "node:child_process";
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

function db(sql) {
  const r = spawnSync(
    "docker",
    ["exec", "NevaxisFactOS-local-postgres-1", "psql", "-U", "crevafactos", "-d", "crevafactos_local", "-t", "-A", "-c", sql],
    { encoding: "utf8" },
  );
  if (r.status !== 0) {
    console.error(r.stderr || r.stdout);
    process.exit(1);
  }
  return r.stdout.trim();
}

const migration = read("packages/NevaxisFactOS.Persistence.Migrations/Migrations/20260811190000_AddMaintenanceAiAgentFoundation.cs");

assert("RLS enabled on drafts", migration.includes("ENABLE ROW LEVEL SECURITY"));
assert("FORCE RLS on drafts", migration.includes("FORCE ROW LEVEL SECURITY"));
assert("tenant isolation policy drafts", migration.includes("maintenance_ai_action_drafts"));
assert("tenant isolation policy audit", migration.includes("maintenance_ai_draft_audit_events"));

const draftRls = db("SELECT relrowsecurity, relforcerowsecurity FROM pg_class WHERE relname = 'maintenance_ai_action_drafts'");
assert("drafts RLS active", draftRls.includes("t|t") || draftRls.split("|").every((v) => v === "t"));

const auditRls = db("SELECT relrowsecurity, relforcerowsecurity FROM pg_class WHERE relname = 'maintenance_ai_draft_audit_events'");
assert("audit RLS active", auditRls.includes("t|t") || auditRls.split("|").every((v) => v === "t"));

const draftPolicy = db("SELECT COUNT(*) FROM pg_policies WHERE tablename = 'maintenance_ai_action_drafts' AND policyname = 'tenant_isolation'");
assert("draft tenant policy exists", draftPolicy === "1");

const auditPolicy = db("SELECT COUNT(*) FROM pg_policies WHERE tablename = 'maintenance_ai_draft_audit_events' AND policyname = 'tenant_isolation'");
assert("audit tenant policy exists", auditPolicy === "1");

const migrationApplied = db("SELECT COUNT(*) FROM \"__EFMigrationsHistory\" WHERE \"MigrationId\" = '20260811190000_AddMaintenanceAiAgentFoundation'");
assert("migration recorded", migrationApplied === "1");

console.log("MAINT-AI-1 security/RLS verifier PASS");
