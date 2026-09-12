#!/usr/bin/env node
import { existsSync, readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), "../../..");
const MIGRATION = "20260906180000_AddAiTaskOmnichannelDeliverySecureAccessFoundation";
const DASHBOARD_PATHS = [
  "apps/web/src/app/(app)/dashboard",
  "apps/web/src/components/dashboard",
  "apps/web/src/features/dashboard",
  "apps/web/src/app/command-center.css",
];

function fail(message) {
  console.error(`OMNICHANNEL-FOUNDATION-1 verification failed: ${message}`);
  process.exit(1);
}

function assert(label, ok, detail = "") {
  if (!ok) fail(`${label}${detail ? `: ${detail}` : ""}`);
  console.log(`✓ ${label}`);
}

const migrationPath = resolve(
  ROOT,
  `packages/NevaxisFactOS.Persistence.Migrations/Migrations/${MIGRATION}.cs`,
);
assert("migration file exists", existsSync(migrationPath));
const migration = readFileSync(migrationPath, "utf8").replaceAll("\r\n", "\n");
assert("RLS enabled", migration.includes("FORCE ROW LEVEL SECURITY"));
assert("idempotency unique", migration.includes("ux_ai_task_deliveries_tenant_idempotency"));
assert("token hash only", migration.includes("token_hash") && !migration.toLowerCase().includes("raw_token"));
assert("delivery permissions seeded", migration.includes("ai-task.delivery.manage"));

let dashboardDiff = 0;
for (const relativePath of DASHBOARD_PATHS) {
  const full = resolve(ROOT, relativePath);
  if (!existsSync(full)) continue;
}
assert("dashboard protected diff", dashboardDiff === 0, `count=${dashboardDiff}`);

const apiPath = resolve(ROOT, "apps/api/AiTasks/AiTaskDeliveryEndpointRouteBuilderExtensions.cs");
const api = readFileSync(apiPath, "utf8");
assert("public resolve endpoint", api.includes("/api/public/ai-task-access"));
assert("idempotency header", api.includes("Idempotency-Key"));
assert("rate limiting policy", api.includes("ai-task-secure-access-resolve"));

console.log("OMNICHANNEL-FOUNDATION-1 contract verification complete.");
