#!/usr/bin/env node
import { readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), "../../..");

function fail(message) {
  console.error(`COMMERCIAL-SUPPLIER-COMPARISON-UI-1 permission verifier failed: ${message}`);
  process.exit(1);
}

function assert(label, ok, detail = "") {
  if (!ok) fail(`${label}${detail ? `: ${detail}` : ""}`);
  console.log(`✓ ${label}`);
}

function read(rel) {
  return readFileSync(resolve(ROOT, rel), "utf8").replaceAll("\r\n", "\n");
}

console.log("Running COMMERCIAL-SUPPLIER-COMPARISON-UI-1 permission verifier.");

const page = read("apps/web/src/app/(app)/ticari-zeka/tedarikci-karsilastirmasi/page.tsx");
const actions = read("apps/web/src/features/commercial-intelligence/server/commercial-intelligence-actions.ts");
const navigation = read("apps/web/src/lib/navigation/primary-navigation.ts");
const subscription = read("apps/web/src/features/saas-subscription/domain/saas-subscription-labels.ts");

assert("page requires commercial-intelligence.read", page.includes('forbidUnlessGranted("commercial-intelligence.read")'));
assert("refresh action requires read", actions.includes('refreshCommercialSupplierComparisonWorkspaceAction') && actions.includes('denyUnlessGranted("commercial-intelligence.read")'));
assert("evaluation uses manage permission via shared action", actions.includes('evaluateCommercialIntelligenceComparisonAction'));
assert("navigation permission key", navigation.includes('permissionKey: "commercial-intelligence.read"') && navigation.includes("/ticari-zeka/tedarikci-karsilastirmasi"));
assert("navigation subscription boundary", navigation.includes('subscriptionModulePath: "/ticari-zeka"') && navigation.includes("/ticari-zeka/tedarikci-karsilastirmasi"));
assert("supplier route revalidated", actions.includes('"/ticari-zeka/tedarikci-karsilastirmasi"'));
assert("readonly note in view", read("apps/web/src/features/commercial-intelligence/components/commercial-supplier-comparison-workspace-view.tsx").includes("COMMERCIAL_INTELLIGENCE_READONLY_NOTE"));

console.log("COMMERCIAL-SUPPLIER-COMPARISON-UI-1 permission verification passed.");
