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

const page = read("apps/web/src/app/(app)/siparisler/page.tsx");
const workspace = read("apps/web/src/features/orders/components/order-draft-filtered-workspace.tsx");
const hook = read("apps/web/src/features/orders/application/use-order-draft-filter-state.ts");

assert("route imports Suspense", page.includes('import { Suspense } from "react"'));
assert("OrderDraftFilteredWorkspace wrapped in Suspense", page.includes("<Suspense fallback={<OrderDraftFilteredWorkspaceFallback />}"));
assert("fallback component defined", page.includes("OrderDraftFilteredWorkspaceFallback"));
assert("fallback does not use useSearchParams", !page.includes("useSearchParams"));
assert("useSearchParams in filter hook", hook.includes("useSearchParams"));
assert("workspace uses filter hook", workspace.includes("useOrderDraftFilterState"));
assert("no force-dynamic bypass", !page.includes('export const dynamic = "force-dynamic"'));
assert("fallback accessible", page.includes('aria-live="polite"'));

console.log("WEB-BUILD-FIX-1 Suspense boundary verifier PASS");
