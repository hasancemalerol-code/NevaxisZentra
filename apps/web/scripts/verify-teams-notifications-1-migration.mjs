#!/usr/bin/env node
import { readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), "../../..");

function fail(message) {
  console.error(`TEAMS-NOTIFICATIONS-1 migration verification failed: ${message}`);
  process.exit(1);
}

function assert(label, ok, detail = "") {
  if (!ok) fail(`${label}${detail ? `: ${detail}` : ""}`);
  console.log(`✓ ${label}`);
}

function read(rel) {
  return readFileSync(resolve(ROOT, rel), "utf8").replaceAll("\r\n", "\n");
}

console.log("Running TEAMS-NOTIFICATIONS-1 migration verifier.");

const migration = read("packages/CrevaFactOS.Persistence.Migrations/Migrations/20260830153612_AddTeamsNotifications.cs");
const snapshot = read("packages/CrevaFactOS.Persistence.Migrations/Migrations/CrevaFactOSDbContextModelSnapshot.cs");

assert("notifications table", migration.includes("teams_notifications"));
assert("audit table", migration.includes("teams_notification_audit_events"));
assert("recipient fingerprint unique", migration.includes("ux_teams_notifications_tenant_recipient_fingerprint"));
assert("source title column", migration.includes("source_title"));
assert("permission seeded", migration.includes("teams.notifications.read"));
assert("RLS enabled", migration.includes("FORCE ROW LEVEL SECURITY"));
assert("append-only audit grant", migration.includes("GRANT SELECT, INSERT ON TABLE teams_notification_audit_events"));
assert("notification update grant", migration.includes("GRANT SELECT, INSERT, UPDATE ON TABLE teams_notifications"));
assert("no delete grant on notifications", !migration.includes("DELETE ON TABLE teams_notifications"));
assert("snapshot includes notifications", snapshot.includes("teams_notifications"));

console.log("TEAMS-NOTIFICATIONS-1 migration verification passed.");
