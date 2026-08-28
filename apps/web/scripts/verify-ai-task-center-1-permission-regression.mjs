#!/usr/bin/env node
import { readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), "../../..");

function fail(message) {
  console.error(`AI-TASK-CENTER-1 permission regression verification failed: ${message}`);
  process.exit(1);
}

function assert(label, ok, detail = "") {
  if (!ok) fail(`${label}${detail ? `: ${detail}` : ""}`);
  console.log(`✓ ${label}`);
}

function read(relativePath) {
  return readFileSync(resolve(ROOT, relativePath), "utf8").replaceAll("\r\n", "\n");
}

console.log("Running AI-TASK-CENTER-1 permission regression verifier.");

const queries = read("apps/web/src/features/ai-task-center/server/ai-task-queries.ts");
const actions = read("apps/web/src/features/ai-task-center/server/ai-task-actions.ts");
const createPage = read("apps/web/src/app/(app)/ai-gorevleri/yeni/page.tsx");
const bootstrap = read("packages/CrevaFactOS.Persistence/Development/DevelopmentPilotBootstrapService.cs");
const migration = read("packages/CrevaFactOS.Persistence.Migrations/Migrations/20260828094900_AddAiTaskCenterFoundation.cs");

assert("workspace read permission", queries.includes('denyUnlessGranted("ai-task.read")'));
assert("detail read permission", queries.includes("readAiTaskDetail"));
assert("refresh read permission", actions.includes('refreshAiTaskWorkspaceAction') && actions.includes('denyUnlessGranted("ai-task.read")'));
assert("create permission gate", actions.includes('denyUnlessGranted("ai-task.create")'));
assert("review permission gate", actions.includes('denyUnlessGranted("ai-task.review")'));
assert("create page checks canCreate", createPage.includes("canCreate"));

assert("migration seeds read permission", migration.includes("ai-task.read"));
assert("migration seeds create permission", migration.includes("ai-task.create"));
assert("migration seeds review permission", migration.includes("ai-task.review"));
assert("bootstrap manager read", bootstrap.includes("EnsureAiTaskReadPermissionAssignmentAsync"));
assert("bootstrap manager create", bootstrap.includes("EnsureAiTaskCreatePermissionAssignmentAsync"));
assert("bootstrap manager review", bootstrap.includes("EnsureAiTaskReviewPermissionAssignmentAsync"));

console.log("AI-TASK-CENTER-1 permission regression verification passed.");
