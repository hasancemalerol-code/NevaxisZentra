#!/usr/bin/env node

import { readFileSync } from "node:fs";
import { resolve } from "node:path";

const ROOT = resolve(import.meta.dirname, "../../..");
const migration = readFileSync(
  resolve(ROOT, "packages/NevaxisZentra.Persistence.Migrations/Migrations/20260914093200_AddGoldenDemoP2ResetAndPresentation.cs"),
  "utf8",
);

for (const permission of [
  "000000000f7b",
  "golden-demo-p2.read",
  "000000000f7c",
  "golden-demo-p2.reset",
]) {
  if (!migration.includes(permission)) {
    throw new Error(`permission ${permission} missing`);
  }
}

console.log("GOLDEN_DEMO_P2_MIGRATION_VERIFIER = PASS");
