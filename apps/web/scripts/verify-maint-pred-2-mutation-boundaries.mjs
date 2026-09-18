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

const repository = read("packages/NevaxisZentra.Persistence/Maintenance/MaintenancePredictiveRiskRepository.cs");
const endpoints = read("apps/api/Maintenance/MaintenanceConditionEndpointRouteBuilderExtensions.cs");
const security = read("apps/web/scripts/verify-maint-pred-1-security-boundaries.mjs");

assert("no auto maintenance request", !repository.includes("MaintenanceRequests.Add"));
assert("no auto work order create", !repository.includes("MaintenanceWorkOrders.Add"));
assert("read-only maintenance history", repository.includes("AsNoTracking()"));
assert("direct edge read only", repository.includes("MachineAlarmObservations") && repository.includes("AsNoTracking()"));
assert("facility scoped", repository.includes("context.FacilityId"));
assert("risk evaluate endpoint", endpoints.includes("/condition/risk/evaluate"));
assert("risk overview endpoint", endpoints.includes("/condition/risk/overview"));
assert("predictive risk asset endpoint", endpoints.includes("/predictive-risk"));
assert("GET endpoints use Read access", endpoints.includes("ResolveReadAccessAsync"));
assert("risk evaluate uses Manage write access", endpoints.includes("MaintenancePermissionKeys.Manage"));
assert("MAINT-PRED-1 security regression chain", security.includes("verify-maintenance-integration-boundaries.mjs"));
assert("evaluation authorization verifier present", existsSync(resolve(ROOT, "apps/web/scripts/verify-maint-pred-2-evaluation-authorization.mjs")));

console.log("MAINT-PRED-2 mutation-boundary verifier PASS");
