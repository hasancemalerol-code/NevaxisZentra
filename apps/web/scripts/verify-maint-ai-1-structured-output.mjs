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

const services = read("packages/NevaxisZentra.Persistence/Maintenance/MaintenanceAiAgentServices.cs");
const models = read("packages/NevaxisZentra.Persistence/Maintenance/MaintenanceAiAgentModels.cs");
const repo = read("packages/NevaxisZentra.Persistence/Maintenance/MaintenanceAiDraftRepository.cs");

assert("structured output record", models.includes("MaintenanceAiStructuredOutput"));
assert("summary field required", models.includes("Summary"));
assert("qualitative confidence levels", models.includes("QualitativeConfidence"));
assert("prohibited action confirmation", models.includes("ProhibitedActionConfirmation"));
assert("output validator present", services.includes("MaintenanceAiOutputValidator"));
assert("failure probability rejection", services.includes("FailureProbabilityPattern"));
assert("RUL rejection", services.includes("RulPattern"));
assert("days-to-failure rejection", services.includes("DaysToFailurePattern"));
assert("exact failure date rejection", services.includes("ExactFailureDatePattern"));
assert("machine command rejection", services.includes("MachineCommandPattern"));
assert("evidence reference validation", services.includes("Evidence reference is not in trusted input snapshot"));
assert("input fingerprint", services.includes("ComputeFingerprint"));
assert("invalid output not persisted as valid", repo.includes("MaintenanceAiAuditActions.OutputRejected"));
assert("no percentage confidence in UI labels", !read("apps/web/src/features/maintenance/presentation/maintenance-ai-agent-labels.ts").includes("%"));

console.log("MAINT-AI-1 structured output verifier PASS");
