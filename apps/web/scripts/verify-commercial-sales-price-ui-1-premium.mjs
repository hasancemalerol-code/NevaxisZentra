#!/usr/bin/env node
import { readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), "../../..");

function fail(message) {
  console.error(`COMMERCIAL-SALES-PRICE-UI-1 premium UI verifier failed: ${message}`);
  process.exit(1);
}

function assert(label, ok, detail = "") {
  if (!ok) fail(`${label}${detail ? `: ${detail}` : ""}`);
  console.log(`✓ ${label}`);
}

function read(rel) {
  return readFileSync(resolve(ROOT, rel), "utf8").replaceAll("\r\n", "\n");
}

console.log("Running COMMERCIAL-SALES-PRICE-UI-1 premium UI verifier.");

const page = read("apps/web/src/app/(app)/ticari-zeka/satis-fiyat-analizi/page.tsx");
const view = read("apps/web/src/features/commercial-intelligence/components/commercial-sales-price-workspace-view.tsx");
const sections = read("apps/web/src/features/commercial-intelligence/components/commercial-sales-price-workspace-sections.tsx");
const customerRow = read("apps/web/src/features/commercial-intelligence/components/commercial-intelligence-customer-row.tsx");
const presentation = read("apps/web/src/features/commercial-intelligence/components/commercial-intelligence-presentation.ts");
const labels = read("apps/web/src/features/commercial-intelligence/domain/commercial-intelligence-labels.ts");
const navigation = read("apps/web/src/lib/navigation/primary-navigation.ts");
const catchAll = read("apps/web/src/app/(app)/[...sections]/page.tsx");
const css = read("apps/web/src/features/commercial-intelligence/components/commercial-intelligence-workspace.css");

assert("dedicated sales price page", page.includes('forbidUnlessGranted("commercial-intelligence.read")'));
assert("route excluded from catch-all", catchAll.includes('"/ticari-zeka/satis-fiyat-analizi"'));
assert("navigation href", navigation.includes('href: "/ticari-zeka/satis-fiyat-analizi"'));
assert("navigation label", navigation.includes('label: "Satış Fiyat Analizi"'));
assert("executive eyebrow", view.includes("TİCARİ ZEKÂ"));
assert("executive title", view.includes("<h1>Satış Fiyat Analizi</h1>"));
assert("six KPI grid", view.includes('data-testid="commercial-sales-price-kpi-grid"'));
assert("four tabs", labels.includes("Genel Bakış") && labels.includes("Dönemsel Sapma") && labels.includes("Müşteri Karşılaştırması") && labels.includes("Değerlendirme Geçmişi"));
assert("overview temporal panel", view.includes('data-testid="commercial-sales-price-temporal-deviation"'));
assert("overview customer panel", view.includes('data-testid="commercial-sales-price-customer-position"'));
assert("temporal workspace", view.includes('data-testid="commercial-sales-price-temporal-workspace"'));
assert("customers workspace", view.includes('data-testid="commercial-sales-price-customers-workspace"'));
assert("history workspace", view.includes('data-testid="commercial-sales-price-history-workspace"'));
assert("history filter rail", sections.includes('data-testid="commercial-sales-price-history-filter-rail"'));
assert("history filter result type", sections.includes('data-testid="commercial-sales-price-history-filter-result-type"'));
assert("history filter outcome", sections.includes('data-testid="commercial-sales-price-history-filter-outcome"'));
assert("history filter material", sections.includes('data-testid="commercial-sales-price-history-filter-material"'));
assert("history filter customer", sections.includes('data-testid="commercial-sales-price-history-filter-customer"'));
assert("history filter evidence", sections.includes('data-testid="commercial-sales-price-history-filter-evidence"'));
assert("history filter date from", sections.includes('data-testid="commercial-sales-price-history-filter-date-from"'));
assert("history filter date to", sections.includes('data-testid="commercial-sales-price-history-filter-date-to"'));
assert("history filter clear", sections.includes('data-testid="commercial-sales-price-history-filter-clear"'));
assert("history empty state", view.includes('data-testid="commercial-sales-price-history-empty"'));
assert("executive driver summary", sections.includes('data-testid="commercial-sales-price-executive-driver-summary"'));
assert("leading driver block", sections.includes('data-testid="commercial-sales-price-leading-driver"'));
assert("supporting drivers block", sections.includes('data-testid="commercial-sales-price-supporting-drivers"'));
assert("detail drawer sections", sections.includes('testId="identity"') && sections.includes('testId="drivers"'));
assert("temporal evaluation controls", view.includes('data-testid="commercial-sales-price-temporal-evaluation-controls"'));
assert("customer evaluation controls", view.includes('data-testid="commercial-sales-price-customer-evaluation-controls"'));
assert("detail ready testid", sections.includes('data-testid="commercial-sales-price-detail-ready"'));
assert("readonly note", view.includes('data-testid="commercial-sales-price-readonly-note"'));
assert("provenance strip", view.includes('data-testid="commercial-sales-price-provenance-strip"'));
assert("sales development banner", view.includes("COMMERCIAL_SALES_PRICE_DEVELOPMENT_BANNER"));
assert("no purchase development banner in sales view", !view.includes("COMMERCIAL_INTELLIGENCE_DEVELOPMENT_BANNER"));
assert("sales development banner copy", labels.includes("gerçek müşteri işlemi, satış faturası, teklif veya piyasa kanıtı değildir"));
assert("provenance localization helper", labels.includes("localizeCommercialSalesPriceProvenance"));
assert("excluded evidence scope guard", presentation.includes("hasEvidenceScope"));
assert("mapping note", view.includes('data-testid="commercial-sales-price-mapping-note"'));
assert("shared customer row component", view.includes("CommercialIntelligenceCustomerRow"));
assert("60/40 overview grid", css.includes("commint-panel-grid--overview") && css.includes("grid-template-columns: minmax(0, 3fr) minmax(0, 2fr)"));
assert("filter rail css", css.includes(".commint-filter-rail"));
assert("driver summary css", css.includes(".commint-driver-summary__header"));
assert("spread panel", view.includes('data-testid="commercial-sales-price-spread-panel"'));
assert("baseline badge", css.includes("commint-badge--baseline"));
assert("comparable sales evidence KPI", presentation.includes('"Karşılaştırılabilir satış kanıtı"'));
assert("compared customers KPI", presentation.includes('"Karşılaştırılan müşteriler"'));
assert("spec subtitle", labels.includes("Satış fiyatı kanıtlarını, dönemsel sapmaları ve müşteri bazlı"));
assert("real transaction mapping note", labels.includes("Gerçek satış işlemi eşlemesi henüz tamamlanmadı"));
assert("temporal sales type constant", presentation.includes("temporal-sales-price-deviation"));
assert("customer position type constant", presentation.includes("customer-price-position"));
assert("no profit wording", !view.toLowerCase().includes("kârlılık") && !view.toLowerCase().includes("karlılık") && !sections.toLowerCase().includes("gelir artışı"));
assert("baseline wording", customerRow.includes("Fiyat baz çizgisi"));

console.log("COMMERCIAL-SALES-PRICE-UI-1 premium UI verification passed.");
