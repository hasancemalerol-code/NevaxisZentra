#!/usr/bin/env node
import { readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), "../../..");

function fail(message) {
  console.error(`COMMERCIAL-PROFITABILITY-UI-4 contract verifier failed: ${message}`);
  process.exit(1);
}

function assert(label, ok, detail = "") {
  if (!ok) fail(`${label}${detail ? `: ${detail}` : ""}`);
  console.log(`✓ ${label}`);
}

function read(rel) {
  return readFileSync(resolve(ROOT, rel), "utf8").replaceAll("\r\n", "\n");
}

console.log("Running COMMERCIAL-PROFITABILITY-UI-4 contract verifier.");

const page = read("apps/web/src/app/(app)/ticari-zeka/musteri-karliligi/page.tsx");
const catchAll = read("apps/web/src/app/(app)/[...sections]/page.tsx");
const productPage = read("apps/web/src/app/(app)/ticari-zeka/urun-karliligi/page.tsx");
const view = read("apps/web/src/features/commercial-intelligence/components/commercial-customer-profitability-workspace-view.tsx");
const sections = read("apps/web/src/features/commercial-intelligence/components/commercial-customer-profitability-workspace-sections.tsx");
const queries = read("apps/web/src/features/commercial-intelligence/server/commercial-customer-profitability-queries.ts");
const apiClient = read("apps/web/src/features/commercial-intelligence/server/commercial-profitability-api-client.ts");
const actions = read("apps/web/src/features/commercial-intelligence/server/commercial-intelligence-actions.ts");
const presentation = read("apps/web/src/features/commercial-intelligence/components/commercial-profitability-presentation.ts");
const nav = read("apps/web/src/lib/navigation/primary-navigation.ts");

assert("dedicated musteri-karliligi page exists", page.includes("CommercialCustomerProfitabilityWorkspaceView"));
assert("catch-all excludes musteri-karliligi", catchAll.includes('"/ticari-zeka/musteri-karliligi"'));
assert("catch-all still excludes urun-karliligi", catchAll.includes('"/ticari-zeka/urun-karliligi"'));
assert("placeholder replaced", !page.includes("Yer tutucu") && !page.includes("sonraki paketlerde"));
assert("navigation route preserved", nav.includes('href: "/ticari-zeka/musteri-karliligi"') && nav.includes('label: "Müşteri Kârlılığı"'));
assert("product workspace intact", productPage.includes("CommercialProfitabilityWorkspaceView"));

assert("workspace testid", view.includes('data-testid="commercial-customer-profitability-workspace"'));
assert("kpi grid testid", view.includes('data-testid="commercial-customer-profitability-kpi-grid"'));
assert("table testid", sections.includes('data-testid="commercial-customer-profitability-table"'));
assert("empty testid", view.includes('data-testid="commercial-customer-profitability-empty"'));
assert("insufficient testid", view.includes('data-testid="commercial-customer-profitability-insufficient"'));
assert("loading testid", view.includes('data-testid="commercial-customer-profitability-loading"'));
assert("detail loading testid", view.includes('data-testid="commercial-customer-profitability-detail-loading"'));
assert("error testid", view.includes('data-testid="commercial-customer-profitability-error"'));
assert("no selection testid", view.includes('data-testid="commercial-customer-profitability-no-selection"'));

assert("fixed customer comparison type", queries.includes("PROFITABILITY_CUSTOMER_CONTRIBUTION_TYPE"));
assert(
  "customer-contribution-position constant",
  queries.includes("PROFITABILITY_CUSTOMER_CONTRIBUTION_TYPE") || queries.includes('"customer-contribution-position"'),
);
assert("reuses profitability api client", queries.includes("listCommercialProfitabilityComparisons"));
assert("no duplicate api client", !queries.includes("fetch(") && queries.includes("./commercial-profitability-api-client"));
assert("reuses detail boundary", queries.includes("readCommercialProfitabilityComparisonDetail"));
assert("customer filters", queries.includes("customerReference") && queries.includes("evidenceStatus"));
assert("pagination support", queries.includes("pageSize") && view.includes("commercial-customer-profitability-pagination"));

assert("refresh action", actions.includes("refreshCommercialCustomerProfitabilityWorkspaceAction"));
assert("detail action", actions.includes("loadCommercialCustomerProfitabilityComparisonDetailAction"));
assert("route revalidated", actions.includes('"/ticari-zeka/musteri-karliligi"'));
assert("read permission only", page.includes('forbidUnlessGranted("commercial-intelligence.read")'));

assert("hesaplanamadi handling", view.includes("formatProfitabilityMarginDeltaDisplay") || sections.includes("formatProfitabilityMarginDeltaDisplay"));
assert("insufficient not zero hint", presentation.includes("Sıfır olarak gösterilmez"));
assert("no comparison type filter in customer view", !view.includes("temporal-contribution-trend") && !view.includes("cost-component-mix-change"));
assert("detail panel reuse", view.includes("CommercialProfitabilityComparisonDetailPanel"));
assert("server detail prefetch", page.includes("readCommercialCustomerProfitabilityComparisonDetail"));
assert("initial detail props", view.includes("initialComparisonDetail") && view.includes("initialDetailStatus"));
assert("selection triggers detail load", view.includes("loadComparisonDetail(comparisonId)"));

console.log("COMMERCIAL-PROFITABILITY-UI-4 contract verification passed.");
