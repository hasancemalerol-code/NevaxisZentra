#!/usr/bin/env node
import { readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), "../../..");

function fail(message) {
  console.error(`COMMERCIAL-PROFITABILITY-UI-5 contract verifier failed: ${message}`);
  process.exit(1);
}

function assert(label, ok, detail = "") {
  if (!ok) fail(`${label}${detail ? `: ${detail}` : ""}`);
  console.log(`✓ ${label}`);
}

function read(rel) {
  return readFileSync(resolve(ROOT, rel), "utf8").replaceAll("\r\n", "\n");
}

console.log("Running COMMERCIAL-PROFITABILITY-UI-5 contract verifier.");

const page = read("apps/web/src/app/(app)/ticari-zeka/malzeme-ve-maliyet-karliligi/page.tsx");
const catchAll = read("apps/web/src/app/(app)/[...sections]/page.tsx");
const productPage = read("apps/web/src/app/(app)/ticari-zeka/urun-karliligi/page.tsx");
const customerPage = read("apps/web/src/app/(app)/ticari-zeka/musteri-karliligi/page.tsx");
const view = read("apps/web/src/features/commercial-intelligence/components/commercial-material-cost-profitability-workspace-view.tsx");
const sections = read("apps/web/src/features/commercial-intelligence/components/commercial-material-cost-profitability-workspace-sections.tsx");
const queries = read("apps/web/src/features/commercial-intelligence/server/commercial-material-cost-profitability-queries.ts");
const filter = read("apps/web/src/features/commercial-intelligence/application/commercial-material-cost-profitability-filter.ts");
const actions = read("apps/web/src/features/commercial-intelligence/server/commercial-intelligence-actions.ts");
const presentation = read("apps/web/src/features/commercial-intelligence/components/commercial-profitability-presentation.ts");
const nav = read("apps/web/src/lib/navigation/primary-navigation.ts");

assert("dedicated malzeme-ve-maliyet-karliligi page exists", page.includes("CommercialMaterialCostProfitabilityWorkspaceView"));
assert("catch-all excludes malzeme-ve-maliyet-karliligi", catchAll.includes('"/ticari-zeka/malzeme-ve-maliyet-karliligi"'));
assert("catch-all still excludes urun-karliligi", catchAll.includes('"/ticari-zeka/urun-karliligi"'));
assert("catch-all still excludes musteri-karliligi", catchAll.includes('"/ticari-zeka/musteri-karliligi"'));
assert("navigation route preserved", nav.includes('href: "/ticari-zeka/malzeme-ve-maliyet-karliligi"') && nav.includes('label: "Malzeme ve Maliyet Kârlılığı"'));
assert("product workspace intact", productPage.includes("CommercialProfitabilityWorkspaceView"));
assert("customer workspace intact", customerPage.includes("CommercialCustomerProfitabilityWorkspaceView"));

assert("workspace testid", view.includes('data-testid="commercial-material-cost-profitability-workspace"'));
assert("kpi grid testid", view.includes('data-testid="commercial-material-cost-profitability-kpi-grid"'));
assert("material table testid", sections.includes('data-testid="commercial-material-cost-profitability-material-table"'));
assert("cost table testid", sections.includes('data-testid="commercial-material-cost-profitability-cost-table"'));
assert("lens rail testid", view.includes('data-testid="commercial-material-cost-profitability-lens-rail"'));
assert("material lens testid", view.includes("commercial-material-cost-profitability-lens-${lens.id}") || view.includes('commercial-material-cost-profitability-lens-material'));
assert("cost lens testid", view.includes("commercial-material-cost-profitability-lens-${lens.id}") || view.includes('commercial-material-cost-profitability-lens-cost-driver'));
assert("empty testid", view.includes('data-testid="commercial-material-cost-profitability-empty"'));
assert("insufficient testid", view.includes('data-testid="commercial-material-cost-profitability-insufficient"'));
assert("loading testid", view.includes('data-testid="commercial-material-cost-profitability-loading"'));
assert("detail loading testid", view.includes('data-testid="commercial-material-cost-profitability-detail-loading"'));
assert("error testid", view.includes('data-testid="commercial-material-cost-profitability-error"'));
assert("no selection testid", view.includes('data-testid="commercial-material-cost-profitability-no-selection"'));

assert("material comparison type lock", queries.includes("PROFITABILITY_MATERIAL_CONTRIBUTION_TYPE"));
assert("cost comparison type lock", queries.includes("PROFITABILITY_COST_COMPONENT_MIX_TYPE"));
assert("material-contribution-position constant", queries.includes('"material-contribution-position"') || queries.includes("PROFITABILITY_MATERIAL_CONTRIBUTION_TYPE"));
assert("cost-component-mix-change constant", queries.includes('"cost-component-mix-change"') || queries.includes("PROFITABILITY_COST_COMPONENT_MIX_TYPE"));
assert("reuses profitability api client", queries.includes("listCommercialProfitabilityComparisons"));
assert("no duplicate api client", !queries.includes("fetch(") && queries.includes("./commercial-profitability-api-client"));
assert("reuses detail boundary", queries.includes("readCommercialProfitabilityComparisonDetail"));
assert("material filters", filter.includes("materialId") && filter.includes("evidenceStatus"));
assert("pagination support", filter.includes("page") && view.includes("commercial-material-cost-profitability-pagination"));
assert("lens filter support", filter.includes("lens"));

assert("refresh action", actions.includes("refreshCommercialMaterialCostProfitabilityWorkspaceAction"));
assert("detail action", actions.includes("loadCommercialMaterialCostProfitabilityComparisonDetailAction"));
assert("route revalidated", actions.includes('"/ticari-zeka/malzeme-ve-maliyet-karliligi"'));
assert("read permission only", page.includes('forbidUnlessGranted("commercial-intelligence.read")'));

assert("hesaplanamadi handling", view.includes("formatProfitabilityMarginDeltaDisplay") || sections.includes("formatProfitabilityMarginDeltaDisplay"));
assert("insufficient not zero hint", presentation.includes("Sıfır olarak gösterilmez"));
assert("no temporal type in view", !view.includes("temporal-contribution-trend"));
assert("no customer type in view", !view.includes("customer-contribution-position"));
assert("detail panel reuse", view.includes("CommercialProfitabilityComparisonDetailPanel"));
assert("server detail prefetch", page.includes("readCommercialMaterialCostProfitabilityComparisonDetail"));
assert("initial detail props", view.includes("initialComparisonDetail") && view.includes("initialDetailStatus"));
assert("selection triggers detail load", view.includes("loadComparisonDetail(comparisonId)"));
assert("comparison url param", view.includes('searchParams.get("comparison")'));

console.log("COMMERCIAL-PROFITABILITY-UI-5 contract verification passed.");
