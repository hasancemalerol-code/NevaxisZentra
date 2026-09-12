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

const repo = read("packages/NevaxisFactOS.Persistence/Maintenance/MaintenanceAiDraftRepository.cs");
const entities = read("packages/NevaxisFactOS.Persistence/Maintenance/MaintenanceAiAgentEntities.cs");
const ui = read("apps/web/src/features/maintenance/components/maintenance-ai-assistant-section.tsx");

assert("stale status code", entities.includes("Stale"));
assert("ValidateFreshnessAsync", repo.includes("ValidateFreshnessAsync"));
assert("fingerprint comparison", repo.includes("InputFingerprint"));
assert("risk assessment comparison", repo.includes("RiskAssessmentId"));
assert("approval blocked on stale", repo.includes("MaintenanceAiFailureCodes.StaleDraft"));
assert("DraftMarkedStale audit", repo.includes("MaintenanceAiAuditActions.DraftMarkedStale"));
assert("regeneration marks old drafts stale", repo.includes("MarkStaleDraftsAsync"));
assert("UI stale warning", ui.includes("Güncelliğini Yitirdi") || ui.includes("güncelliğini yitirdi"));
assert("regenerate action label", ui.includes("Güncel Verilerle Yeniden Hazırla"));

console.log("MAINT-AI-1 stale-draft verifier PASS");
