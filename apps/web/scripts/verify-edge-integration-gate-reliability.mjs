#!/usr/bin/env node
/**
 * verify-edge-integration-gate-reliability.mjs
 * EDGE Integration Gate — reliability aggregation.
 */

import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";
import { spawnSync } from "node:child_process";

const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), "../../..");
const SCRIPTS = resolve(ROOT, "apps/web/scripts");

let passed = 0;
let failed = 0;

function assert(label, condition, detail = "") {
  if (condition) {
    console.log(`  ✓ ${label}`);
    passed++;
  } else {
    console.error(`  ✗ ${label}${detail ? `: ${detail}` : ""}`);
    failed++;
  }
}

function runPackageVerifier(name) {
  console.log(`\n>>> ${name}`);
  const result = spawnSync("node", [resolve(SCRIPTS, name)], {
    cwd: ROOT,
    encoding: "utf8",
    shell: true,
    env: process.env,
  });
  if (result.stdout) process.stdout.write(result.stdout);
  if (result.stderr) process.stderr.write(result.stderr);
  assert(`${name} PASS`, result.status === 0, `exit=${result.status ?? "null"}`);
}

console.log("\n[1] EDGE-1 foundation + reliability");
runPackageVerifier("verify-edge-1-core.mjs");
runPackageVerifier("verify-edge-1-reliability.mjs");

console.log("\n[2] EDGE-2 telemetry + cycle integrity");
runPackageVerifier("verify-edge-2-telemetry.mjs");
runPackageVerifier("verify-edge-2-cycle.mjs");

console.log("\n[3] EDGE-3 operational observations");
runPackageVerifier("verify-edge-3-downtime-setup.mjs");
runPackageVerifier("verify-edge-3-alarms.mjs");

console.log("\n[4] EDGE-4 store-and-forward");
runPackageVerifier("verify-edge-4-buffer.mjs");
runPackageVerifier("verify-edge-4-replay.mjs");

console.log(`\n${"─".repeat(55)}`);
console.log(`EDGE Integration Gate Reliability: ${passed} passed, ${failed} failed`);
if (failed > 0) process.exit(1);
