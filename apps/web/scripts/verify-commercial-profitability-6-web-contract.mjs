#!/usr/bin/env node
import { readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), "../../..");
const ROUTE = "/ticari-zeka/karlilik-uyarilari";

function fail(message) {
  console.error(`COMMERCIAL-PROFITABILITY-6 web contract verification failed: ${message}`);
  process.exit(1);
}

function assert(label, ok, detail = "") {
  if (!ok) fail(`${label}${detail ? `: ${detail}` : ""}`);
  console.log(`✓ ${label}`);
}

function read(rel) {
  return readFileSync(resolve(ROOT, rel), "utf8").replaceAll("\r\n", "\n");
}

function forbid(source, forbidden, label) {
  if (source.includes(forbidden)) fail(`${label} must not contain "${forbidden}"`);
}

console.log("Running COMMERCIAL-PROFITABILITY-6 web contract verifier.");

const page = read("apps/web/src/app/(app)/ticari-zeka/karlilik-uyarilari/page.tsx");
const catchAll = read("apps/web/src/app/(app)/[...sections]/page.tsx");
const view = read("apps/web/src/features/commercial-intelligence/components/commercial-profitability-alert-workspace-view.tsx");
const queries = read("apps/web/src/features/commercial-intelligence/server/commercial-profitability-alert-queries.ts");
const apiClient = read("apps/web/src/features/commercial-intelligence/server/commercial-profitability-alert-api-client.ts");
const filter = read("apps/web/src/features/commercial-intelligence/application/commercial-profitability-alert-filter.ts");
const labels = read("apps/web/src/features/commercial-intelligence/domain/commercial-profitability-alert-labels.ts");
const types = read("apps/web/src/features/commercial-intelligence/types/commercial-profitability-alert-types.ts");
const actions = read("apps/web/src/features/commercial-intelligence/server/commercial-intelligence-actions.ts");
const nav = read("apps/web/src/lib/navigation/primary-navigation.ts");
const productPage = read("apps/web/src/app/(app)/ticari-zeka/urun-karliligi/page.tsx");

assert("dedicated karlilik-uyarilari page exists", page.includes("CommercialProfitabilityAlertWorkspaceView"));
assert("catch-all excludes karlilik-uyarilari", catchAll.includes('"/ticari-zeka/karlilik-uyarilari"'));
assert("product workspace intact", productPage.includes("CommercialProfitabilityWorkspaceView"));
assert("navigation route present", nav.includes('href: "/ticari-zeka/karlilik-uyarilari"') && nav.includes("Kârlılık Uyarıları"));

assert("workspace testid", view.includes('data-testid="commercial-profitability-alert-workspace"'));
assert("kpi grid testid", view.includes('data-testid="commercial-profitability-alert-kpi-grid"'));
assert("filter rail testid", view.includes('data-testid="commercial-profitability-alert-filter-rail"'));
assert("alert table testid", view.includes('data-testid="commercial-profitability-alert-table"'));
assert("detail panel testid", view.includes('data-testid="commercial-profitability-alert-detail-panel"'));
assert("detail ready testid", view.includes('data-testid="commercial-profitability-alert-detail-ready"'));
assert("empty testid", view.includes('data-testid="commercial-profitability-alert-empty"'));
assert("loading testid", view.includes('data-testid="commercial-profitability-alert-loading"'));
assert("error testid", view.includes('data-testid="commercial-profitability-alert-error"'));
assert("no selection testid", view.includes('data-testid="commercial-profitability-alert-no-selection"'));
assert("audit timeline testid", view.includes('data-testid="commercial-profitability-alert-audit-timeline"'));
assert("task create button testid", view.includes('data-testid="commercial-profitability-alert-create-task-button"'));

assert("alert url param", view.includes('searchParams.get("alert")'));
assert("selection triggers detail load", view.includes("loadAlertDetail") || view.includes("loadCommercialProfitabilityAlertDetailAction"));
assert("comparison detail reuse", view.includes("CommercialProfitabilityComparisonDetailPanel"));
assert("task draft button label", view.includes("Görev taslağı oluştur"));
assert("linked task navigation", view.includes("/ai-gorevleri/") || view.includes("linkedAiTaskId"));

assert("filter alert param", filter.includes("alert") && filter.includes("buildCommercialProfitabilityAlertFilterParams"));
assert("category filter", filter.includes("category") && filter.includes("severity"));
assert("lifecycle filter", filter.includes("lifecycleStatus"));

assert("types workspace payload", types.includes("CommercialProfitabilityAlertWorkspacePayload"));
assert("types alert detail", types.includes("CommercialProfitabilityAlertDetail"));

assert("labels automation disclaimer", labels.includes("Otomatik ticari işlem yapılmaz"));
assert("labels product category", labels.includes("product-contribution-deterioration"));
assert("labels data review category", labels.includes("data-review"));
assert("labels dismissed lifecycle", labels.includes("dismissed"));

assert("queries read permission", queries.includes('denyUnlessGranted("commercial-intelligence.read")'));
assert("queries workspace endpoint", queries.includes("readCommercialProfitabilityAlertWorkspace"));
assert("queries list endpoint", queries.includes("listCommercialProfitabilityAlerts"));
assert("queries detail endpoint", queries.includes("readCommercialProfitabilityAlertDetail"));
assert("no duplicate fetch client", !queries.includes("fetch(") && queries.includes("./commercial-profitability-alert-api-client"));

assert("api client workspace path", apiClient.includes("/profitability/alerts/workspace"));
assert("api client list path", apiClient.includes("/profitability/alerts"));
assert("api client materialize path", apiClient.includes("/materialize"));
assert("api client transition path", apiClient.includes("/transitions"));
assert("api client task draft path", apiClient.includes("/task-draft"));

assert("route constant", actions.includes('PROFITABILITY_ALERT_ROUTE_PATH = "/ticari-zeka/karlilik-uyarilari"'));
assert("refresh action", actions.includes("refreshCommercialProfitabilityAlertWorkspaceAction"));
assert("detail action", actions.includes("loadCommercialProfitabilityAlertDetailAction"));
assert("materialize action", actions.includes("materializeCommercialProfitabilityAlertsAction"));
assert("transition action", actions.includes("transitionCommercialProfitabilityAlertAction"));
assert("task draft action", actions.includes("createCommercialProfitabilityAlertTaskDraftAction"));
assert("route revalidated", actions.includes('"/ticari-zeka/karlilik-uyarilari"'));
assert("read permission on page", page.includes('forbidUnlessGranted("commercial-intelligence.read")'));

forbid(view, "henüz kullanıma hazır değil", "alert workspace view");
forbid(view, "yalnızca arayüz önizlemesidir", "alert workspace view");
forbid(page, "AI_TASK_PREVIEW_DATA", "alert page");

console.log("COMMERCIAL-PROFITABILITY-6 web contract verification passed.");
