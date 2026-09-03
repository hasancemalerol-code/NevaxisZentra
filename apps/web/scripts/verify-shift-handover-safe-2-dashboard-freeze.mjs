#!/usr/bin/env node
import { createHash } from "node:crypto";
import { existsSync, readFileSync, readdirSync, statSync } from "node:fs";
import { dirname, join, resolve } from "node:path";
import { spawnSync } from "node:child_process";
import { fileURLToPath } from "node:url";

const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), "../../..");
const BASELINE = "e9a13bd96712bd8d07e1c5e5d2a79d475ec814ae";
const BASELINE_HASHES = resolve(ROOT, ".cursor-screenshots/shift-handover-safe-2-dashboard-baseline-hashes.json");

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
  console.error(`SHIFT-HANDOVER-SAFE-2 dashboard freeze verification failed: ${message}`);
  process.exit(1);
}

function assert(label, ok, detail = "") {
  if (!ok) fail(`${label}${detail ? `: ${detail}` : ""}`);
  console.log(`✓ ${label}`);
}

function hashFile(path) {
  return createHash("sha256").update(readFileSync(path)).digest("hex");
}

function walk(dir, out = []) {
  for (const entry of readdirSync(dir, { withFileTypes: true })) {
    const full = join(dir, entry.name);
    if (entry.isDirectory()) walk(full, out);
    else out.push(full);
  }
  return out;
}

function collectProtectedFiles() {
  const files = [];
  for (const rel of PROTECTED_PATHS) {
    const abs = resolve(ROOT, rel);
    if (!existsSync(abs)) continue;
    if (statSync(abs).isDirectory()) walk(abs, files);
    else files.push(abs);
  }
  return files;
}

const diff = spawnSync("git", ["diff", "--name-only", BASELINE, "--", ...PROTECTED_PATHS], { cwd: ROOT, encoding: "utf8" });
if (diff.status !== 0) fail(diff.stderr || diff.stdout || "git diff failed");
assert("protected dashboard git diff empty", diff.stdout.trim() === "", diff.stdout.trim());

const baseline = existsSync(BASELINE_HASHES)
  ? JSON.parse(readFileSync(BASELINE_HASHES, "utf8").replace(/^\uFEFF/, ""))
  : null;
assert("baseline hash file present", Boolean(baseline));

function normalizeRel(file) {
  return file.replace(ROOT + "\\", "").replace(ROOT + "/", "").replaceAll("\\", "/");
}

let changed = 0;
for (const file of collectProtectedFiles()) {
  const rel = normalizeRel(file);
  const current = hashFile(file);
  const baselineKey = Object.keys(baseline).find((key) => normalizeRel(resolve(ROOT, key.replaceAll("/", "\\"))) === rel || key.replaceAll("\\", "/") === rel);
  const expected = baselineKey ? baseline[baselineKey] : baseline[rel];
  if (expected && expected.toLowerCase() !== current.toLowerCase()) {
    changed += 1;
    console.error(`hash changed: ${rel} (expected ${expected}, got ${current})`);
  }
}
assert("protected dashboard hashes unchanged", changed === 0, `changed=${changed}`);

console.log("SHIFT-HANDOVER-SAFE-2 dashboard freeze verification passed.");
