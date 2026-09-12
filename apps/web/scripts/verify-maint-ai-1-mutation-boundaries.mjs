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
const endpoints = read("apps/api/Maintenance/MaintenanceAiEndpointRouteBuilderExtensions.cs");
const ui = read("apps/web/src/features/maintenance/components/maintenance-ai-assistant-section.tsx");
const sidebar = read("apps/web/src/components/app-shell/sidebar.tsx");

assert("no auto work order create", !repo.includes("MaintenanceWorkOrders.Add"));
assert("no technician assignment", !repo.includes("Technician") && !repo.includes("AssignTechnician"));
assert("no schedule mutation", !repo.includes("MaintenanceSchedule") && !repo.includes("ScheduleDowntime"));
assert("no machine command", !repo.includes("MachineCommand") && !repo.includes("WriteTag"));
assert("no edge mutation", !repo.includes("MachineAlarmObservations.Add") && !repo.includes("MachineDowntimeEpisodes.Add"));
assert("no ERP mutation", !repo.includes("Erp") && !repo.includes("Writeback"));
assert("no production mutation", !repo.includes("ProductionOrders.Add"));
assert("no quality mutation", !repo.includes("QualityNonconformance") && !repo.includes("quality_"));
assert("maintenance request via existing boundary pattern", repo.includes("MaintenanceRequests.Add"));
assert("source maintenance-ai-agent", repo.includes("maintenance-ai-agent"));
assert("human approver recorded", repo.includes("ApprovedBySubjectId"));
assert("no AI Task Center sidebar", !sidebar.includes("AI Task Center") && !sidebar.includes("ai-gorevleri"));
assert("AI section on durum-zekasi only", read("apps/web/src/features/maintenance/components/maintenance-condition-view.tsx").includes("MaintenanceAiAssistantSection"));
assert("no automatic approve UI", !ui.includes("autoApprove") && ui.includes("Onayla ve Bakım Talebi Oluştur"));

console.log("MAINT-AI-1 mutation-boundary verifier PASS");
