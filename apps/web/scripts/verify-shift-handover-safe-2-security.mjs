#!/usr/bin/env node
import { readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";
import { spawnSync } from "node:child_process";

const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), "../../..");

function fail(message) {
  console.error(`SHIFT-HANDOVER-SAFE-2 security verification failed: ${message}`);
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
const subscription = read("packages/NevaxisZentra.Persistence/SaasSubscription/SaasSubscriptionModuleEntitlements.cs");

assert("no backward after acknowledge", lifecycle.includes("ShiftHandoverStatuses.Acknowledged, ShiftHandoverStatuses.Completed"));
assert("tenant scoped reads", repository.includes("h.TenantId == context.TenantId"));
assert("facility scoped reads", repository.includes("h.FacilityId == context.FacilityId"));
assert("canonical ai task create", api.includes("CreateSourceLinkedTaskAsync"));
assert("no duplicate task table", !read("packages/NevaxisZentra.Persistence/NevaxisZentraDbContext.cs").includes("shift_handover_tasks"));
assert("source module constant", presentation.includes("ShiftHandoverConstants.SourceModule"));
assert("single nav entry", (nav.match(/label:\s*"Vardiya Devir Teslimi"/g) ?? []).length === 1);
assert("permission gated nav", nav.includes('permissionKey: "shift-handover.read"'));
const navDiff = spawnSync(
  "git",
  ["diff", "e9a13bd96712bd8d07e1c5e5d2a79d475ec814ae", "--", "apps/web/src/lib/navigation/primary-navigation.ts"],
  { cwd: ROOT, encoding: "utf8" },
);
assert("did not restore operasyon yonetim merkezi", !navDiff.stdout.includes("Operasyon Yönetim Merkezi"));
assert("subscription mapping", subscription.includes('("/vardiya-devir", EntitlementCodes.ShiftWorkforce)'));

console.log("SHIFT-HANDOVER-SAFE-2 security verification passed.");
