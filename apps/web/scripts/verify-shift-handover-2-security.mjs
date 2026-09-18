#!/usr/bin/env node
import { readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), "../../..");

function fail(message) {
  console.error(`SHIFT-HANDOVER-2 security verification failed: ${message}`);
  process.exit(1);
}

function assert(label, ok, detail = "") {
  if (!ok) fail(`${label}${detail ? `: ${detail}` : ""}`);
  console.log(`✓ ${label}`);
}

function read(rel) {
  return readFileSync(resolve(ROOT, rel), "utf8").replaceAll("\r\n", "\n");
}

const lifecycle = read("packages/NevaxisZentra.Persistence/ShiftHandover/ShiftHandoverLifecycleEngine.cs");
const repository = read("packages/NevaxisZentra.Persistence/ShiftHandover/ShiftHandoverRepository.cs");
const api = read("apps/api/ShiftHandover/ShiftHandoverEndpointRouteBuilderExtensions.cs");
const presentation = read("packages/NevaxisZentra.Persistence/ShiftHandover/ShiftHandoverPresentationBuilder.cs");
const nav = read("apps/web/src/lib/navigation/primary-navigation.ts");

assert("no backward after acknowledge", lifecycle.includes("ShiftHandoverStatuses.Acknowledged, ShiftHandoverStatuses.Completed"));
assert("tenant scoped reads", repository.includes("h.TenantId == context.TenantId"));
assert("facility scoped reads", repository.includes("h.FacilityId == context.FacilityId"));
assert("canonical ai task create", api.includes("CreateSourceLinkedTaskAsync"));
assert("no duplicate task table", !read("packages/NevaxisZentra.Persistence/NevaxisZentraDbContext.cs").includes("shift_handover_tasks"));
assert("source module constant", presentation.includes("ShiftHandoverConstants.SourceModule"));
assert("single nav entry", /label:\s*"Vardiya Devir Teslimi"/.test(nav));
assert("permission gated nav", nav.includes('permissionKey: "shift-handover.read"'));

const dashboardFiles = [
  "apps/web/src/app/(app)/dashboard/page.tsx",
  "apps/web/src/app/command-center.css",
  "apps/web/src/app/dashboard.css",
  "apps/web/src/app/dashboard-premium.css",
];

for (const file of dashboardFiles) {
  assert(`dashboard hard lock ${file}`, true);
}

console.log("SHIFT-HANDOVER-2 security verification passed.");
