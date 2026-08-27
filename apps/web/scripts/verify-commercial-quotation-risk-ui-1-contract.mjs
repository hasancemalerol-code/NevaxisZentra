#!/usr/bin/env node
import { readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), "../../..");

function fail(message) {
  console.error(`COMMERCIAL-QUOTATION-RISK-UI-1 contract verifier failed: ${message}`);
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

console.log("Running COMMERCIAL-QUOTATION-RISK-UI-1 contract verifier.");

const page = read("apps/web/src/app/(app)/ticari-zeka/teklif-riskleri/page.tsx");
const catchAll = read("apps/web/src/app/(app)/[...sections]/page.tsx");
const view = read("apps/web/src/features/commercial-intelligence/components/commercial-quotation-risk-workspace-view.tsx");
const sections = read("apps/web/src/features/commercial-intelligence/components/commercial-quotation-risk-workspace-sections.tsx");
const presentation = read("apps/web/src/features/commercial-intelligence/components/commercial-quotation-risk-presentation.ts");
const labels = read("apps/web/src/features/commercial-intelligence/domain/commercial-quotation-risk-labels.ts");
const formatters = read("apps/web/src/features/commercial-intelligence/domain/commercial-intelligence-formatters.ts");
const queries = read("apps/web/src/features/commercial-intelligence/server/commercial-quotation-risk-queries.ts");
const apiClient = read("apps/web/src/features/commercial-intelligence/server/commercial-quotation-risk-api-client.ts");
const nav = read("apps/web/src/lib/navigation/primary-navigation.ts");

assert("dedicated teklif-riskleri page exists", page.includes("CommercialQuotationRiskWorkspaceView"));
assert("catch-all excludes teklif-riskleri", catchAll.includes('"/ticari-zeka/teklif-riskleri"'));
assert("placeholder replaced", !page.includes("Yer tutucu") && !page.includes("sonraki paketlerde"));
assert("navigation route preserved", nav.includes('href: "/ticari-zeka/teklif-riskleri"') && nav.includes('label: "Teklif Riskleri"'));

for (const tab of ["Genel Bakış", "Değerlendirmeler", "Risk Eğilimleri", "Müşteri Karşılaştırması", "Risk Profili"]) {
  assert(`tab label ${tab}`, labels.includes(tab));
}
assert("five tab ids", labels.includes('"overview"') && labels.includes('"profile"'));

assert("workspace testid", view.includes('data-testid="commercial-quotation-risk-workspace"'));
assert("overview testid", view.includes('data-testid="commercial-quotation-risk-overview"'));
assert("assessments testid", view.includes('data-testid="commercial-quotation-risk-assessments"'));
assert("trends testid", view.includes('data-testid="commercial-quotation-risk-trends"'));
assert("customers testid", view.includes('data-testid="commercial-quotation-risk-customers"'));
assert("profile testid", view.includes('data-testid="commercial-quotation-risk-profile"'));
assert("development banner testid", view.includes('data-testid="commercial-quotation-risk-development-banner"'));

assert("queries use quotation-risks api", apiClient.includes("/api/commercial-intelligence/quotation-risks"));
assert("queries load assessments", queries.includes("listCommercialQuotationRiskAssessments"));
assert("queries load comparisons", queries.includes("listCommercialQuotationRiskComparisons"));
assert("queries map comparison detail", queries.includes("mapComparisonDetail"));
assert("real mapping flag", queries.includes("realQuotationMappingComplete"));
assert("segment mapping honesty", queries.includes("customerSegmentMappingComplete: false"));

assert("kpi builder", presentation.includes("buildQuotationRiskKpiCards"));
assert("risk distribution builder", presentation.includes("buildQuotationRiskDistribution"));
assert("temporal chart builder", presentation.includes("buildQuotationRiskTemporalChartData"));
assert("customer rows builder", presentation.includes("buildQuotationRiskCustomerPositionRows"));
assert("tie label", presentation.includes("Eşit risk konumu"));

assert("incidence fraction formatter", formatters.includes("formatIncidenceFraction"));
assert("incidence delta formatter", formatters.includes("formatIncidenceDeltaPoints"));
assert("nullable count formatter", formatters.includes("formatNullableCount"));
assert("variance percent once", formatters.includes("multiply by 100 exactly once"));
assert("view uses incidence delta label", view.includes("Risk sıklığı değişimi") || sections.includes("Risk sıklığı değişimi"));

assert("denominator reconciliation", sections.includes("Payda doğrulaması") && sections.includes("formatCount(comparableCount)} + ${formatCount(excludedCount)}"));
assert("insufficient trend message", view.includes("Karşılaştırma için yeterli veri yok"));
assert("source conflict message", view.includes("Kaynak çakışması"));
assert("hesaplanamadi null handling", view.includes("Hesaplanamadı") || formatters.includes("Hesaplanamadı"));

assert("development banner copy", labels.includes("Gerçek teklif veri bağlantısı henüz tamamlanmadı"));
assert("view shows development banner", view.includes("COMMERCIAL_QUOTATION_RISK_DEVELOPMENT_BANNER"));

forbidText(view, "en iyi müşteri", "view");
forbidText(view, "tercih edilen", "view");
forbidText(view, "önerilen", "view");
forbidText(view, "onayla", "view");
forbidText(view, "reddet", "view");
forbidText(view, "kârlılık", "view");
forbidText(view, "garantili", "view");
forbidText(sections, "segment", "sections");
forbidText(view, "openai", "view");
forbidText(view, "llm", "view");

assert("higher observed risk wording", labels.includes("Daha yüksek gözlenen risk konumu"));
assert("assessment drawer", sections.includes("QuotationRiskAssessmentDetailDrawerBody"));
assert("driver ordering preserved", sections.includes("sortOrder") && sections.includes("isLeadingDriver"));

console.log("COMMERCIAL-QUOTATION-RISK-UI-1 contract verification passed.");
