#!/usr/bin/env node
import { readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), "../../..");
const ROUTE = "/ticari-zeka/karlilik-yonetim-merkezi";

function fail(message) {
  console.error(`COMMERCIAL-PROFITABILITY-8 web contract verification failed: ${message}`);
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

console.log("Running COMMERCIAL-PROFITABILITY-8 web contract verifier.");

const page = read("apps/web/src/app/(app)/ticari-zeka/karlilik-yonetim-merkezi/page.tsx");
const catchAll = read("apps/web/src/app/(app)/[...sections]/page.tsx");
const view = read("apps/web/src/features/commercial-intelligence/components/commercial-profitability-command-center-view.tsx");
const queries = read("apps/web/src/features/commercial-intelligence/server/commercial-profitability-command-center-queries.ts");
const filter = read("apps/web/src/features/commercial-intelligence/application/commercial-profitability-command-center-filter.ts");
const labels = read("apps/web/src/features/commercial-intelligence/domain/commercial-profitability-command-center-labels.ts");
const types = read("apps/web/src/features/commercial-intelligence/types/commercial-profitability-command-center-types.ts");
const actions = read("apps/web/src/features/commercial-intelligence/server/commercial-intelligence-actions.ts");
const nav = read("apps/web/src/lib/navigation/primary-navigation.ts");
const alertsPage = read("apps/web/src/app/(app)/ticari-zeka/karlilik-uyarilari/page.tsx");
const pricingPage = read("apps/web/src/app/(app)/ticari-zeka/fiyat-koruma-ve-onay/page.tsx");

assert("dedicated karlilik-yonetim-merkezi page exists", page.includes("CommercialProfitabilityCommandCenterView"));
assert("catch-all excludes karlilik-yonetim-merkezi", catchAll.includes('"/ticari-zeka/karlilik-yonetim-merkezi"'));
assert("alerts workspace intact", alertsPage.includes("CommercialProfitabilityAlertWorkspaceView"));
assert("pricing workspace intact", pricingPage.includes("CommercialPricingScenarioWorkspaceView"));
assert("navigation route present", nav.includes(`href: "${ROUTE}"`) && nav.includes("Kârlılık Yönetim Merkezi"));

assert("workspace testid", view.includes('data-testid="commercial-profitability-command-center-workspace"'));
assert("kpi grid testid", view.includes('data-testid="commercial-profitability-command-center-kpi-grid"'));
assert("filter rail testid", view.includes('data-testid="commercial-profitability-command-center-filter-rail"'));
assert("queue table testid", view.includes('data-testid="commercial-profitability-command-center-queue-table"'));
assert("queue empty testid", view.includes('data-testid="commercial-profitability-command-center-queue-empty"'));
assert("lenses testid", view.includes('data-testid="commercial-profitability-command-center-lenses"'));
assert("pricing snapshot testid", view.includes('data-testid="commercial-profitability-command-center-pricing-snapshot"'));
assert("alert snapshot testid", view.includes('data-testid="commercial-profitability-command-center-alert-snapshot"'));
assert("data confidence testid", view.includes('data-testid="commercial-profitability-command-center-data-confidence"'));
assert("refresh button testid", view.includes('data-testid="commercial-profitability-command-center-refresh-button"'));
assert("readonly note testid", view.includes('data-testid="commercial-profitability-command-center-readonly-note"'));
assert("human decision badge testid", view.includes('data-testid="commercial-profitability-command-center-human-decision"'));
assert("linked task link testid", view.includes('data-testid="commercial-profitability-command-center-linked-task-link"'));
assert("window from testid", view.includes('data-testid="commercial-profitability-command-center-window-from"'));
assert("window to testid", view.includes('data-testid="commercial-profitability-command-center-window-to"'));

assert("url window filters", filter.includes("windowFrom") && filter.includes("windowTo"));
assert("types workspace payload", types.includes("CommercialProfitabilityCommandCenterWorkspacePayload"));
assert("types queue item", types.includes("CommercialProfitabilityCommandCenterQueueItem"));
assert("types scope state", types.includes("CommercialProfitabilityCommandCenterScopeState"));

assert("labels readonly scope", labels.includes("yalnızca okuma"));
assert("labels route path", labels.includes(ROUTE));

assert("queries read permission", queries.includes('denyUnlessGranted("commercial-intelligence.read")'));
assert("queries compose alert workspace", queries.includes("getCommercialProfitabilityAlertWorkspace"));
assert("queries compose scenario workspace", queries.includes("getCommercialPricingScenarioWorkspace"));
assert("queries compose comparisons", queries.includes("listCommercialProfitabilityComparisons"));
assert("queries priority sort", queries.includes("sortQueueItems"));
assert("queries partial scope", queries.includes("scopeFromPage"));
assert("queries no duplicate margin math", !queries.includes("ComputeContributionMarginRatio"));
assert("queries resolve context once", queries.includes("resolveCommercialIntelligenceRequestContext"));

assert("refresh action", actions.includes("refreshCommercialProfitabilityCommandCenterAction"));
assert("route constant", actions.includes('PROFITABILITY_COMMAND_CENTER_ROUTE_PATH = "/ticari-zeka/karlilik-yonetim-merkezi"'));
assert("route revalidated", actions.includes('revalidatePath(PROFITABILITY_COMMAND_CENTER_ROUTE_PATH)'));
assert("read permission on page", page.includes('forbidUnlessGranted("commercial-intelligence.read")'));

forbid(view, "transitionCommercial", "command center view write action");
forbid(view, "createCommercial", "command center view write action");
forbid(view, "materializeCommercial", "command center view write action");
forbid(view, "evaluateCommercial", "command center view write action");
forbid(view, "approve", "command center view approval action");
forbid(view, "formatRatioAsPercent", "command center view margin math");
forbid(page, "AI_TASK_PREVIEW_DATA", "command center page");

console.log("COMMERCIAL-PROFITABILITY-8 web contract verification passed.");
