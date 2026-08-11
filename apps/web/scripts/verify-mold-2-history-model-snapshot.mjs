#!/usr/bin/env node
import { readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), "../../..");
const SNAPSHOT = resolve(
  ROOT,
  "packages/CrevaFactOS.Persistence.Migrations/Migrations/CrevaFactOSDbContextModelSnapshot.cs",
);
const CONFIG = resolve(ROOT, "packages/CrevaFactOS.Persistence/Configurations/MoldEntityConfigurations.cs");

function fail(message) {
  console.error(`MOLD-2 model snapshot verifier failed: ${message}`);
  process.exit(1);
}

function assert(label, ok, detail = "") {
  if (!ok) fail(`${label}${detail ? `: ${detail}` : ""}`);
  console.log(`✓ ${label}`);
}

function requireAll(source, label, tokens) {
  for (const token of tokens) {
    assert(`${label}: ${token}`, source.includes(token));
  }
}

console.log("Running MOLD-2 model snapshot verifier.");

const snapshot = readFileSync(SNAPSHOT, "utf8");
const config = readFileSync(CONFIG, "utf8");

const entities = [
  "CrevaFactOS.Persistence.Mold.MoldMaintenanceRecord",
  "CrevaFactOS.Persistence.Mold.MoldRevisionRecord",
  "CrevaFactOS.Persistence.Mold.MoldTrialRecord",
];

for (const entity of entities) {
  assert(`snapshot entity ${entity}`, snapshot.includes(`modelBuilder.Entity("${entity}"`));
}

requireAll(snapshot, "maintenance table mapping", ['ToTable("mold_maintenance_records"']);
requireAll(snapshot, "revision table mapping", ['ToTable("mold_revision_records"']);
requireAll(snapshot, "trial table mapping", ['ToTable("mold_trial_records"']);

requireAll(snapshot, "maintenance properties", [
  "HasColumnName(\"maintenance_type\")",
  "HasColumnName(\"status\")",
  "HasColumnName(\"opened_at_utc\")",
  "HasColumnName(\"completed_at_utc\")",
  "HasColumnName(\"idempotency_key\")",
  "HasColumnName(\"idempotency_payload_fingerprint\")",
  "HasColumnName(\"version\")",
  "IsConcurrencyToken()",
]);

requireAll(snapshot, "revision properties", [
  "HasColumnName(\"revision_number\")",
  "HasColumnName(\"revision_number_normalized\")",
  "HasColumnName(\"revision_type\")",
  "HasColumnName(\"revision_date\")",
]);

requireAll(snapshot, "trial properties", [
  "HasColumnName(\"trial_number\")",
  "HasColumnName(\"trial_number_normalized\")",
  "HasColumnName(\"trial_date\")",
  "HasColumnName(\"result\")",
  "HasColumnName(\"shot_count\")",
  "HasColumnName(\"good_part_count\")",
  "HasColumnName(\"scrap_part_count\")",
]);

requireAll(snapshot, "maintenance indexes", [
  'HasIndex("TenantId", "MoldId", "IdempotencyKey")',
  'HasIndex("TenantId", "FacilityId", "MoldId", "OpenedAtUtc")',
]);

requireAll(snapshot, "revision unique indexes", [
  'HasIndex("TenantId", "MoldId", "RevisionNumberNormalized")',
]);

requireAll(snapshot, "trial unique indexes", [
  'HasIndex("TenantId", "MoldId", "TrialNumberNormalized")',
]);

requireAll(snapshot, "maintenance foreign keys", [
  'Entity("CrevaFactOS.Persistence.Mold.MoldMaintenanceRecord", b =>',
  'HasForeignKey("MoldId")',
  'HasForeignKey("TenantId")',
  'HasForeignKey("TenantId", "FacilityId")',
  "DeleteBehavior.Restrict",
]);

requireAll(config, "configuration alignment", [
  'ToTable("mold_maintenance_records")',
  'ToTable("mold_revision_records")',
  'ToTable("mold_trial_records")',
  "RevisionNumberNormalized",
  "TrialNumberNormalized",
  "IdempotencyPayloadFingerprint",
]);

console.log("MOLD-2 model snapshot verifier passed.");
