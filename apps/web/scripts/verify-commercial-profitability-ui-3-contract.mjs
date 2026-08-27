#!/usr/bin/env node
import { readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), "../../..");

function fail(message) {
  console.error(`COMMERCIAL-PROFITABILITY-UI-3 contract verifier failed: ${message}`);
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

console.log("Running COMMERCIAL-PROFITABILITY-UI-3 contract verifier.");

const page = read("apps/web/src/app/(app)/ticari-zeka/urun-karliligi/page.tsx");
const catchAll = read("apps/web/src/app/(app)/[...sections]/page.tsx");
const view = read("apps/web/src/features/commercial-intelligence/components/commercial-profitability-workspace-view.tsx");
const sections = read("apps/web/src/features/commercial-intelligence/components/commercial-profitability-workspace-sections.tsx");
const presentation = read("apps/web/src/features/commercial-intelligence/components/commercial-profitability-presentation.ts");
const labels = read("apps/web/src/features/commercial-intelligence/domain/commercial-profitability-labels.ts");
const formatters = read("apps/web/src/features/commercial-intelligence/domain/commercial-intelligence-formatters.ts");
const queries = read("apps/web/src/features/commercial-intelligence/server/commercial-profitability-queries.ts");
const apiClient = read("apps/web/src/features/commercial-intelligence/server/commercial-profitability-api-client.ts");
const actions = read("apps/web/src/features/commercial-intelligence/server/commercial-intelligence-actions.ts");
const nav = read("apps/web/src/lib/navigation/primary-navigation.ts");

assert("dedicated urun-karliligi page exists", page.includes("CommercialProfitabilityWorkspaceView"));
assert("catch-all excludes urun-karliligi", catchAll.includes('"/ticari-zeka/urun-karliligi"'));
assert("placeholder replaced", !page.includes("Yer tutucu") && !page.includes("sonraki paketlerde"));
assert("navigation route preserved", nav.includes('href: "/ticari-zeka/urun-karliligi"') && nav.includes('label: "Ürün Kârlılığı"'));

for (const tab of ["Genel Bakış", "Kârlılık Eğilimleri", "Maliyet Bileşeni", "Konum Karşılaştırması"]) {
  assert(`tab label ${tab}`, labels.includes(tab));
}
assert("four tab ids", labels.includes('"overview"') && labels.includes('"positions"'));

assert("workspace testid", view.includes('data-testid="commercial-profitability-workspace"'));
assert("overview testid", view.includes('data-testid="commercial-profitability-overview"'));
assert("temporal testid", view.includes('data-testid="commercial-profitability-temporal"'));
assert("cost-mix testid", view.includes('data-testid="commercial-profitability-cost-mix"'));
assert("positions testid", view.includes('data-testid="commercial-profitability-positions"'));
assert("development banner testid", view.includes('data-testid="commercial-profitability-development-banner"'));
assert("loading testid", view.includes('data-testid="commercial-profitability-loading"'));
assert("empty testid", view.includes('data-testid="commercial-profitability-empty"'));
assert("insufficient testid", view.includes('data-testid="commercial-profitability-insufficient"'));
assert("error testid", view.includes('data-testid="commercial-profitability-error"'));

assert("queries use profitability api", apiClient.includes("/api/commercial-intelligence/profitability"));
assert("queries load comparisons", queries.includes("listCommercialProfitabilityComparisons"));
assert("queries map comparison detail", queries.includes("mapComparisonDetail"));
assert("real mapping flag", queries.includes("realProfitabilityMappingComplete"));

assert("kpi builder", presentation.includes("buildProfitabilityKpiCards"));
assert("temporal chart builder", presentation.includes("buildProfitabilityTemporalChartData"));
assert("position rows builder", presentation.includes("buildProfitabilityPositionPreviewRows"));

assert("ratio percent formatter", formatters.includes("formatRatioAsPercent"));
assert("multiply by 100 exactly once", formatters.includes("multiply by 100 exactly once"));
assert("hesaplanamadi null handling", view.includes("Hesaplanamadı") || formatters.includes("Hesaplanamadı"));

assert("denominator reconciliation", sections.includes("Payda doğrulaması") && sections.includes("formatCount(summary.comparableAssessmentCount)} + ${formatCount(summary.excludedAssessmentCount)}"));
assert(
  "insufficient trend message",
  view.includes("Karşılaştırma için yeterli veri yok") || sections.includes("Karşılaştırma için yeterli veri yok"),
);
assert(
  "source conflict message",
  view.includes("Kaynak çakışması") || sections.includes("Kaynak çakışması"),
);
assert("insufficient not zero hint", presentation.includes("Sıfır olarak gösterilmez"));

assert("development banner copy", labels.includes("Gerçek gelir ve maliyet veri bağlantısı henüz tamamlanmadı"));
assert("view shows development banner", view.includes("COMMERCIAL_PROFITABILITY_DEVELOPMENT_BANNER"));
assert("readonly note", view.includes("COMMERCIAL_PROFITABILITY_READONLY_NOTE"));

assert("refresh action", actions.includes("refreshCommercialProfitabilityWorkspaceAction"));
assert("comparison detail action", actions.includes("loadCommercialProfitabilityComparisonDetailAction"));
assert("comparison detail panel", sections.includes("CommercialProfitabilityComparisonDetailPanel"));
assert("driver ordering preserved", sections.includes("sortOrder") && sections.includes("isLeadingDriver"));

forbidText(view, "onayla", "view");
forbidText(view, "reddet", "view");
forbidText(view, "openai", "view");
forbidText(view, "llm", "view");

console.log("COMMERCIAL-PROFITABILITY-UI-3 contract verification passed.");
