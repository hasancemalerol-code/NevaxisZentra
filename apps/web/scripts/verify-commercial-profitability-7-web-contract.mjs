#!/usr/bin/env node
import { readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), "../../..");
const ROUTE = "/ticari-zeka/fiyat-koruma-ve-onay";

function fail(message) {
  console.error(`COMMERCIAL-PROFITABILITY-7 web contract verification failed: ${message}`);
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

console.log("Running COMMERCIAL-PROFITABILITY-7 web contract verifier.");

const page = read("apps/web/src/app/(app)/ticari-zeka/fiyat-koruma-ve-onay/page.tsx");
const catchAll = read("apps/web/src/app/(app)/[...sections]/page.tsx");
const view = read("apps/web/src/features/commercial-intelligence/components/commercial-pricing-scenario-workspace-view.tsx");
const queries = read("apps/web/src/features/commercial-intelligence/server/commercial-pricing-scenario-queries.ts");
const apiClient = read("apps/web/src/features/commercial-intelligence/server/commercial-pricing-scenario-api-client.ts");
const filter = read("apps/web/src/features/commercial-intelligence/application/commercial-pricing-scenario-filter.ts");
const labels = read("apps/web/src/features/commercial-intelligence/domain/commercial-pricing-scenario-labels.ts");
const types = read("apps/web/src/features/commercial-intelligence/types/commercial-pricing-scenario-types.ts");
const actions = read("apps/web/src/features/commercial-intelligence/server/commercial-intelligence-actions.ts");
const nav = read("apps/web/src/lib/navigation/primary-navigation.ts");
const alertsPage = read("apps/web/src/app/(app)/ticari-zeka/karlilik-uyarilari/page.tsx");

assert("dedicated fiyat-koruma-ve-onay page exists", page.includes("CommercialPricingScenarioWorkspaceView"));
assert("catch-all excludes fiyat-koruma-ve-onay", catchAll.includes('"/ticari-zeka/fiyat-koruma-ve-onay"'));
assert("alerts workspace intact", alertsPage.includes("CommercialProfitabilityAlertWorkspaceView"));
assert("navigation route present", nav.includes(`href: "${ROUTE}"`) && nav.includes("Fiyat Koruma ve Onay"));

assert("workspace testid", view.includes('data-testid="commercial-pricing-scenario-workspace"'));
assert("kpi grid testid", view.includes('data-testid="commercial-pricing-scenario-kpi-grid"'));
assert("filter rail testid", view.includes('data-testid="commercial-pricing-scenario-filter-rail"'));
assert("scenario table testid", view.includes('data-testid="commercial-pricing-scenario-table"'));
assert("detail panel testid", view.includes('data-testid="commercial-pricing-scenario-detail-panel"'));
assert("detail ready testid", view.includes('data-testid="commercial-pricing-scenario-detail-ready"'));
assert("empty testid", view.includes('data-testid="commercial-pricing-scenario-empty"'));
assert("loading testid", view.includes('data-testid="commercial-pricing-scenario-loading"'));
assert("error testid", view.includes('data-testid="commercial-pricing-scenario-error"'));
assert("no selection testid", view.includes('data-testid="commercial-pricing-scenario-no-selection"'));
assert("audit timeline testid", view.includes('data-testid="commercial-pricing-scenario-audit-timeline"'));
assert("evaluation panel testid", view.includes('data-testid="commercial-pricing-scenario-evaluation-panel"'));
assert("create form testid", view.includes('data-testid="commercial-pricing-scenario-create-form"'));
assert("create button testid", view.includes('data-testid="commercial-pricing-scenario-create-button"'));
assert("evaluate button testid", view.includes('data-testid="commercial-pricing-scenario-evaluate-button"'));
assert("submit button testid", view.includes('data-testid="commercial-pricing-scenario-submit-button"'));
assert("approve button testid", view.includes('data-testid="commercial-pricing-scenario-approve-button"'));
assert("reject button testid", view.includes('data-testid="commercial-pricing-scenario-reject-button"'));
assert("task create button testid", view.includes('data-testid="commercial-pricing-scenario-create-task-button"'));
assert("automation disclaimer testid", view.includes('data-testid="commercial-pricing-scenario-automation-disclaimer"'));

assert("scenario url param", view.includes('searchParams.get("scenario")'));
assert("selection triggers detail load", view.includes("loadScenarioDetail"));
assert("server-side evaluation labels", view.includes("expectedContributionMarginRatioLabel"));
assert("server-side delta labels", view.includes("contributionMarginDeltaLabel"));
assert("task draft navigation", view.includes("/ai-gorevleri/"));
assert("linked task link testid", view.includes('data-testid="commercial-pricing-scenario-linked-task-link"'));

assert("filter scenario param", filter.includes("scenario") && filter.includes("buildCommercialPricingScenarioFilterParams"));
assert("lifecycle filter", filter.includes("lifecycleStatus"));
assert("guardrail filter", filter.includes("guardrailResult"));

assert("types workspace payload", types.includes("CommercialPricingScenarioWorkspacePayload"));
assert("types scenario detail", types.includes("CommercialPricingScenarioDetail"));
assert("types evaluation snapshot", types.includes("CommercialPricingScenarioEvaluationSnapshot"));

assert("labels automation disclaimer", labels.includes("Bu onay fiyatı otomatik olarak uygulamaz."));
assert("labels scope note", labels.includes("Senaryolar otomatik fiyat uygulamaz"));
assert("labels passes guardrail", labels.includes("passes"));
assert("labels needs-review guardrail", labels.includes("needs-review"));
assert("labels blocked guardrail", labels.includes("blocked"));
assert("labels insufficient-data guardrail", labels.includes("insufficient-data"));
assert("labels pilot within sales evidence", labels.includes("bb000001-0321-4000-8000-000000000321"));
assert("labels pilot below sales evidence", labels.includes("bb000001-0323-4000-8000-000000000323"));

assert("queries read permission", queries.includes('denyUnlessGranted("commercial-intelligence.read")'));
assert("queries workspace endpoint", queries.includes("readCommercialPricingScenarioWorkspace"));
assert("queries list endpoint", queries.includes("listCommercialPricingScenarios"));
assert("queries detail endpoint", queries.includes("readCommercialPricingScenarioDetail"));
assert("queries map evaluation labels", queries.includes("formatRatioAsPercent"));
assert("no duplicate fetch client", !queries.includes("fetch(") && queries.includes("./commercial-pricing-scenario-api-client"));

assert("api client workspace path", apiClient.includes("/pricing-scenarios/workspace"));
assert("api client list path", apiClient.includes('"/api/commercial-intelligence/pricing-scenarios"'));
assert("api client detail path", apiClient.includes("/pricing-scenarios/${scenarioId}"));
assert("api client create path", apiClient.includes('"/api/commercial-intelligence/pricing-scenarios"') && apiClient.includes('"POST"'));
assert("api client evaluate path", apiClient.includes("/evaluate"));
assert("api client transition path", apiClient.includes("/transitions"));
assert("api client task draft path", apiClient.includes("/task-draft"));

assert("route constant", actions.includes('PRICING_SCENARIO_ROUTE_PATH = "/ticari-zeka/fiyat-koruma-ve-onay"'));
assert("refresh action", actions.includes("refreshCommercialPricingScenarioWorkspaceAction"));
assert("detail action", actions.includes("loadCommercialPricingScenarioDetailAction"));
assert("create action", actions.includes("createCommercialPricingScenarioAction"));
assert("evaluate action", actions.includes("evaluateCommercialPricingScenarioAction"));
assert("transition action", actions.includes("transitionCommercialPricingScenarioAction"));
assert("task draft action", actions.includes("createCommercialPricingScenarioTaskDraftAction"));
assert("route revalidated", actions.includes('"/ticari-zeka/fiyat-koruma-ve-onay"'));
assert("read permission on page", page.includes('forbidUnlessGranted("commercial-intelligence.read")'));

forbid(view, "expectedContributionMarginRatio:", "pricing scenario workspace view raw margin field");
forbid(view, "contributionMarginDelta:", "pricing scenario workspace view raw delta field");
forbid(view, "ComputeContributionMarginRatio", "pricing scenario workspace view");
forbid(view, "ComputeUnitContribution", "pricing scenario workspace view");
forbid(view, "formatRatioAsPercent", "pricing scenario workspace view");
forbid(view, "totalIncludedUnitCost -", "pricing scenario workspace view margin math");
forbid(view, "proposedUnitPrice -", "pricing scenario workspace view margin math");
forbid(view, "/ proposedUnitPrice", "pricing scenario workspace view margin math");
forbid(view, "henüz kullanıma hazır değil", "pricing scenario workspace view");
forbid(view, "yalnızca arayüz önizlemesidir", "pricing scenario workspace view");
forbid(page, "AI_TASK_PREVIEW_DATA", "pricing scenario page");

console.log("COMMERCIAL-PROFITABILITY-7 web contract verification passed.");
