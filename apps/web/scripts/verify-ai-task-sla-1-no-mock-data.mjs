#!/usr/bin/env node
import { existsSync, readdirSync, readFileSync, statSync } from "node:fs";
import { dirname, join, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), "../../..");
const FEATURE_DIR = resolve(ROOT, "apps/web/src/features/ai-task-center");

const FORBIDDEN_PATTERNS = [
  { label: "Math.random", pattern: /\bMath\.random\s*\(/ },
  { label: "fake task rows", pattern: /\bfake\s+task/i },
  { label: "fake users", pattern: /\bfake\s+user/i },
  { label: "preview-only task rows", pattern: /preview-only\s+task/i },
  { label: "random escalation", pattern: /random\s+escalation/i },
  { label: "hard-coded assignment examples", pattern: /hard-?coded\s+assignment/i },
  { label: "AI_TASK_PREVIEW_DATA runtime usage", pattern: /\bAI_TASK_PREVIEW_DATA\b/ },
  { label: "getAiTaskPreviewById runtime usage", pattern: /\bgetAiTaskPreviewById\b/ },
];

function fail(message) {
  console.error(`AI-TASK-SLA-1 no-mock-data verification failed: ${message}`);
  process.exit(1);
}

function assert(label, ok, detail = "") {
  if (!ok) fail(`${label}${detail ? `: ${detail}` : ""}`);
  console.log(`✓ ${label}`);
}

function walkFiles(dir) {
  const entries = [];
  for (const name of readdirSync(dir)) {
    const fullPath = join(dir, name);
    const stats = statSync(fullPath);
    if (stats.isDirectory()) {
      entries.push(...walkFiles(fullPath));
      continue;
    }
    if (/\.(ts|tsx|js|jsx|mjs)$/.test(name)) {
      entries.push(fullPath);
    }
  }
  return entries;
}

function isFixtureFile(filePath) {
  return filePath.replace(/\\/g, "/").includes("/features/ai-task-center/data/");
}

console.log("Running AI-TASK-SLA-1 no-mock-data verifier.");

const files = walkFiles(FEATURE_DIR);
assert("ai-task-center feature files discovered", files.length > 0, `count=${files.length}`);

for (const filePath of files) {
  if (isFixtureFile(filePath)) {
    continue;
  }
  const relative = filePath.replace(/\\/g, "/").replace(`${ROOT.replace(/\\/g, "/")}/`, "");
  const source = readFileSync(filePath, "utf8").replaceAll("\r\n", "\n");
  for (const forbidden of FORBIDDEN_PATTERNS) {
    if (forbidden.pattern.test(source)) {
      fail(`${relative} contains forbidden pattern: ${forbidden.label}`);
    }
  }
  if (source.includes("ai-task-preview-data") || source.includes("AI_TASK_PREVIEW_DATA") || source.includes("getAiTaskPreviewById")) {
    fail(`${relative} imports or references preview fixture data`);
  }
}

const apiClient = readFileSync(resolve(FEATURE_DIR, "server/ai-task-api-client.ts"), "utf8");
const queries = readFileSync(resolve(FEATURE_DIR, "server/ai-task-queries.ts"), "utf8");
const composition = readFileSync(resolve(FEATURE_DIR, "application/ai-task-center-composition.ts"), "utf8");

assert("api client uses live /api/ai-tasks", apiClient.includes("/api/ai-tasks"));
assert("queries use permission-backed reads", queries.includes('denyUnlessGranted("ai-task.read")'));
assert("composition maps server-backed task fields", composition.includes("slaState"));
assert("composition maps escalation fields", composition.includes("escalationLevel"));
assert("composition maps assignment fields", composition.includes("assignedMembershipId"));

const previewDataPath = resolve(FEATURE_DIR, "data/ai-task-preview-data.ts");
if (existsSync(previewDataPath)) {
  const previewData = readFileSync(previewDataPath, "utf8");
  assert("preview fixture isolated to data directory", previewData.includes("Isolated test fixture data"));
}

console.log(`Scanned ${files.filter((file) => !isFixtureFile(file)).length} production files under apps/web/src/features/ai-task-center`);
console.log("AI-TASK-SLA-1 no-mock-data verification passed.");
