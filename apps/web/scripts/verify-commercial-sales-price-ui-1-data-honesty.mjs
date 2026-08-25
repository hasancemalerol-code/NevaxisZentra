#!/usr/bin/env node
import { readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), "../../..");

function fail(message) {
  console.error(`COMMERCIAL-SALES-PRICE-UI-1 data-honesty verifier failed: ${message}`);
  process.exit(1);
}

function assert(label, ok, detail = "") {
  if (!ok) fail(`${label}${detail ? `: ${detail}` : ""}`);
  console.log(`✓ ${label}`);
}

function read(rel) {
  return readFileSync(resolve(ROOT, rel), "utf8").replaceAll("\r\n", "\n");
}

function forbidText(source, forbidden, label) {
  if (source.toLowerCase().includes(forbidden.toLowerCase())) {
    fail(`${label} must not contain "${forbidden}"`);
  }
}

console.log("Running COMMERCIAL-SALES-PRICE-UI-1 data-honesty verifier.");

const presentation = read("apps/web/src/features/commercial-intelligence/components/commercial-intelligence-presentation.ts");
const view = read("apps/web/src/features/commercial-intelligence/components/commercial-sales-price-workspace-view.tsx");
const sections = read("apps/web/src/features/commercial-intelligence/components/commercial-sales-price-workspace-sections.tsx");
const customerRow = read("apps/web/src/features/commercial-intelligence/components/commercial-intelligence-customer-row.tsx");
const labels = read("apps/web/src/features/commercial-intelligence/domain/commercial-intelligence-labels.ts");
const queries = read("apps/web/src/features/commercial-intelligence/server/commercial-sales-price-queries.ts");
const apiClient = read("apps/web/src/features/commercial-intelligence/server/commercial-sales-price-api-client.ts");

assert("sales KPI builder", presentation.includes("buildCommercialSalesPriceKpiCards"));
assert("temporal selector reused", presentation.includes("selectLatestCompatibleTemporalSalesComparison"));
assert("customer selector reused", presentation.includes("selectLatestCompatibleCustomerSalesComparison"));
assert("customer filter helper", presentation.includes("filterCommercialSalesPriceCustomerComparisons"));
assert("spread reconciliation reused", presentation.includes("reconcileCommercialSalesPriceCustomerSpread"));
assert("single comparison evidence context", presentation.includes("Son dönemsel satış karşılaştırması"));
assert("comparable sales evidence KPI", presentation.includes("comparable-sales-evidence"));
assert("compared customers KPI", presentation.includes("compared-customers"));
assert("history filter builder", sections.includes("filterSalesPriceHistoryRows"));
assert("history type separation", sections.includes('resultTypeKey: "assessment"') && sections.includes('resultTypeKey: item.comparisonType'));
assert("drawer field groups", sections.includes('testId="windows-medians"') && sections.includes('testId="provenance"'));
assert("leading driver separation", sections.includes("isLeadingDriver") && sections.includes("commercial-sales-price-detail-supporting-drivers"));
assert("executive driver summary", sections.includes("SalesPriceExecutiveDriverSummary"));
assert("queries use sales api client", queries.includes("getCommercialSalesPriceComparison"));
assert("queries map customer results", queries.includes("mapCommercialSalesPriceCustomerResult"));
assert("api uses sales-prices namespace", apiClient.includes("/api/commercial-intelligence/sales-prices"));
assert("customer spread ratio mapping", queries.includes("customerSpreadRatio"));
assert("real transaction mapping flag", queries.includes("realTransactionMappingComplete"));
forbidText(view, "garantili tasarruf", "sales view");
forbidText(view, "otomatik satış", "sales view");
forbidText(view, "en iyi müşteri", "sales view");
forbidText(view, "tercih edilen müşteri", "sales view");
forbidText(sections, "garantili gelir", "sales sections");
forbidText(sections, "önerilen fiyat", "sales sections");
forbidText(sections, "kârlılık", "sales sections");
assert("baseline label not recommendation", customerRow.includes("Fiyat baz çizgisi"));
assert("no uuid primary labels in drawer", !sections.includes("summary.id") && !sections.includes("summary.profileId"));
assert("excluded evidence scope guard", presentation.includes("hasEvidenceScope"));
assert("excluded evidence zero uses formatCount", presentation.includes('hasEvidenceScope ? formatCount(excludedTotal)'));
assert("excluded evidence avoids truthy zero fallback", !presentation.includes("excludedTotal > 0 ? formatCount(excludedTotal)"));
assert("sales development banner constant", labels.includes("COMMERCIAL_SALES_PRICE_DEVELOPMENT_BANNER"));
assert("sales development banner copy", labels.includes("gerçek müşteri işlemi, satış faturası, teklif veya piyasa kanıtı değildir"));
assert("sales provenance localization", labels.includes("localizeCommercialSalesPriceProvenance"));
assert("queries localize provenance", queries.includes("localizeCommercialSalesPriceProvenance"));
assert("sales view uses sales banner", view.includes("COMMERCIAL_SALES_PRICE_DEVELOPMENT_BANNER"));
forbidText(view, "tedarik fiyatı", "sales view");
forbidText(sections, "tedarik fiyatı", "sales sections");
forbidText(view, "Deterministic local Development", "sales view");
forbidText(sections, "Deterministic local Development", "sales sections");

console.log("COMMERCIAL-SALES-PRICE-UI-1 data-honesty verification passed.");
