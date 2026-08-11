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

const page = read("apps/web/src/app/(app)/planlama/page.tsx");
const layout = read("apps/web/src/app/(app)/planlama/layout.tsx");
const runtimeConfig = read("apps/web/src/features/production-planning/server/production-plan-runtime-config.ts");
const reader = read("apps/web/src/features/production-planning/server/production-plan-reader.ts");

assert("planlama layout uses force-dynamic", layout.includes('export const dynamic = "force-dynamic"'));
assert("planlama route still loads workspace at runtime", page.includes("readProductionPlanningWorkspace"));
assert("production guard preserved", runtimeConfig.includes("Preview and development API runtime modes are forbidden in production"));
assert("runtime mode env controlled", runtimeConfig.includes("CREVAFACTOS_PRODUCTION_PLANNING_RUNTIME_MODE"));
assert("reader resolves request context for API modes", reader.includes("resolveProductionPlanRequestContext"));
assert("no guard removal", !runtimeConfig.includes("// Preview and development") && runtimeConfig.includes("mode !== \"api\""));
assert("no mock production data in page", !page.includes("PREVIEW_DATA") && !page.includes("preview-data"));

console.log("WEB-BUILD-FIX-2 planlama production-runtime verifier PASS");
