#!/usr/bin/env node
import { spawnSync } from "node:child_process";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), "../../..");
const BASELINE = "0ca1bf061ba0d2e385cc711d2804ed68f2a65c1a";

const PROTECTED_PATHS = [
  "apps/web/src/app/(app)/dashboard",
  "apps/web/src/components/dashboard",
  "apps/web/src/features/dashboard",
  "apps/web/src/app/command-center.css",
  "apps/web/src/app/dashboard.css",
  "apps/web/src/app/dashboard-premium.css",
  "apps/web/src/app/dashboard-layout-fix.css",
];

function fail(message) {
  console.error(`AI-TASK-SLA-1 dashboard protected-paths verification failed: ${message}`);
  process.exit(1);
}

function assert(label, ok, detail = "") {
  if (!ok) fail(`${label}${detail ? `: ${detail}` : ""}`);
  console.log(`✓ ${label}`);
}

console.log("Running AI-TASK-SLA-1 dashboard protected-paths verifier.");

const diff = spawnSync(
  "git",
  ["diff", BASELINE, "--", ...PROTECTED_PATHS],
  { cwd: ROOT, encoding: "utf8" },
);

if (diff.status !== 0) {
  fail(diff.stderr || diff.stdout || `git diff exited with status ${diff.status}`);
}

const output = (diff.stdout ?? "").trim();
assert("dashboard protected paths unchanged from baseline", output.length === 0, output ? `diff:\n${output}` : "");

const nameOnly = spawnSync(
  "git",
  ["diff", "--name-only", BASELINE, "--", ...PROTECTED_PATHS],
  { cwd: ROOT, encoding: "utf8" },
);

if (nameOnly.status !== 0) {
  fail(nameOnly.stderr || nameOnly.stdout || `git diff --name-only exited with status ${nameOnly.status}`);
}

const changedFiles = (nameOnly.stdout ?? "")
  .split(/\r?\n/)
  .map((line) => line.trim())
  .filter(Boolean);

assert("no protected dashboard files changed", changedFiles.length === 0, changedFiles.join(", "));

console.log(`Baseline commit: ${BASELINE}`);
console.log("AI-TASK-SLA-1 dashboard protected-paths verification passed.");
