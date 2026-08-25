#!/usr/bin/env node
import { readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), "../../..");

function fail(message) {
  console.error(`COMMERCIAL-SALES-PRICE-UI-1 permission verifier failed: ${message}`);
  process.exit(1);
}

function assert(label, ok, detail = "") {
  if (!ok) fail(`${label}${detail ? `: ${detail}` : ""}`);
  console.log(`✓ ${label}`);
}

function read(rel) {
  return readFileSync(resolve(ROOT, rel), "utf8").replaceAll("\r\n", "\n");
}

console.log("Running COMMERCIAL-SALES-PRICE-UI-1 permission verifier.");

const page = read("apps/web/src/app/(app)/ticari-zeka/satis-fiyat-analizi/page.tsx");
const actions = read("apps/web/src/features/commercial-intelligence/server/commercial-intelligence-actions.ts");
const navigation = read("apps/web/src/lib/navigation/primary-navigation.ts");
const subscription = read("apps/web/src/features/saas-subscription/domain/saas-subscription-labels.ts");

assert("page requires commercial-intelligence.read", page.includes('forbidUnlessGranted("commercial-intelligence.read")'));
assert("refresh action requires read", actions.includes("refreshCommercialSalesPriceWorkspaceAction") && actions.includes('denyUnlessGranted("commercial-intelligence.read")'));
assert("evaluation uses manage permission via shared resolver", actions.includes("evaluateCommercialSalesPriceComparisonAction"));
assert("navigation href", navigation.includes("/ticari-zeka/satis-fiyat-analizi"));
assert("navigation subscription boundary", navigation.includes('subscriptionModulePath: "/ticari-zeka"') && navigation.includes("/ticari-zeka/satis-fiyat-analizi"));
assert("subscription module mapping", subscription.includes('prefix: "/ticari-zeka"') && subscription.includes("module.commercial-intelligence"));
assert("sales route revalidated", actions.includes('"/ticari-zeka/satis-fiyat-analizi"'));
assert("readonly note in view", read("apps/web/src/features/commercial-intelligence/components/commercial-sales-price-workspace-view.tsx").includes("COMMERCIAL_INTELLIGENCE_READONLY_NOTE"));

console.log("COMMERCIAL-SALES-PRICE-UI-1 permission verification passed.");
