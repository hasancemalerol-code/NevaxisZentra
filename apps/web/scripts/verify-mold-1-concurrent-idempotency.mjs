#!/usr/bin/env node
import { spawnSync } from "node:child_process";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), "../../..");

function fail(message) {
  console.error(`MOLD-1 concurrent idempotency verifier failed: ${message}`);
  process.exit(1);
}

function run(script) {
  const result = spawnSync("node", [resolve(ROOT, script)], {
    cwd: ROOT,
    encoding: "utf8",
    stdio: "inherit",
  });
  if (result.status !== 0) {
    fail(`${script} exited with ${result.status ?? "unknown"}`);
  }
}

console.log("Running MOLD-1 concurrent idempotency narrow verifier.");

run("apps/web/scripts/verify-mold-1-create-idempotency.mjs");
run("apps/web/scripts/verify-mold-1-transition-idempotency.mjs");
run("apps/web/scripts/verify-mold-1-counter-idempotency.mjs");

console.log("MOLD-1 concurrent idempotency verification passed.");
