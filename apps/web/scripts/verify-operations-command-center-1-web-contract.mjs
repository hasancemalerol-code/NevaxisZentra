#!/usr/bin/env node
import { readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), "../../..");
const ROUTE = "/operasyon/yonetim-merkezi";

function fail(message) {
  console.error(`OPERATIONS-COMMAND-CENTER-1 web contract verification failed: ${message}`);
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

console.log("Running OPERATIONS-COMMAND-CENTER-1 web contract verifier.");

const page = read("apps/web/src/app/(app)/operasyon/yonetim-merkezi/page.tsx");
const catchAll = read("apps/web/src/app/(app)/[...sections]/page.tsx");
const view = read("apps/web/src/features/operations-command-center/components/operations-command-center-view.tsx");
const queries = read("apps/web/src/features/operations-command-center/server/operations-command-center-queries.ts");
const filter = read("apps/web/src/features/operations-command-center/application/operations-command-center-filter.ts");
const labels = read("apps/web/src/features/operations-command-center/domain/operations-command-center-labels.ts");
const types = read("apps/web/src/features/operations-command-center/types/operations-command-center-types.ts");
const actions = read("apps/web/src/features/operations-command-center/server/operations-command-center-actions.ts");
const nav = read("apps/web/src/lib/navigation/primary-navigation.ts");
const subscription = read("apps/web/src/features/saas-subscription/domain/saas-subscription-labels.ts");

assert("dedicated operasyon yonetim merkezi page exists", page.includes("OperationsCommandCenterView"));
assert("catch-all excludes operasyon yonetim merkezi", catchAll.includes('"/operasyon/yonetim-merkezi"'));
assert("navigation route present", nav.includes(`href: "${ROUTE}"`) && nav.includes("Operasyon Yönetim Merkezi"));
assert("subscription route mapping production module", subscription.includes('prefix: "/operasyon/yonetim-merkezi"') && subscription.includes("module.production"));

assert("workspace testid", view.includes('data-testid="operations-command-center-workspace"'));
assert("kpi grid testid", view.includes('data-testid="operations-command-center-kpi-grid"'));
assert("filter rail testid", view.includes('data-testid="operations-command-center-filter-rail"'));
assert("queue table testid", view.includes('data-testid="operations-command-center-queue-table"'));
assert("queue empty testid", view.includes('data-testid="operations-command-center-queue-empty"'));
assert("module health testid", view.includes('data-testid="operations-command-center-module-health"'));
assert("scope strip testid", view.includes('data-testid="operations-command-center-scope-strip"'));
assert("refresh button testid", view.includes('data-testid="operations-command-center-refresh-button"'));
assert("readonly note testid", view.includes('data-testid="operations-command-center-readonly-note"'));
assert("automation disclaimer testid", view.includes('data-testid="operations-command-center-automation-disclaimer"'));
assert("commercial deep link testid", view.includes('data-testid="operations-command-center-commercial-deep-link"'));
assert("linked task link testid", view.includes('data-testid="operations-command-center-linked-task-link"'));
assert("module filter testid", view.includes('data-testid="operations-command-center-module-filter"'));
assert("state filter testid", view.includes('data-testid="operations-command-center-state-filter"'));
assert("no access testid", view.includes('data-testid="operations-command-center-no-access"'));

assert("url window filters", filter.includes("windowFrom") && filter.includes("windowTo"));
assert("url module filter", filter.includes("module"));
assert("url state filter", filter.includes("state"));
assert("types workspace payload", types.includes("OperationsCommandCenterWorkspacePayload"));
assert("types queue item", types.includes("OperationsCommandCenterQueueItem"));
assert("types source availability", types.includes("OperationsCommandCenterSourceAvailability"));

assert("labels automation disclaimer", labels.includes("otomatik üretim, stok, satın alma, bakım veya kalite işlemi yapmaz"));
assert("labels route path", labels.includes(ROUTE));
assert("labels commercial link", labels.includes("Ticari Kârlılık Yönetim Merkezi"));

assert("queries readOperationsCommandCenterWorkspace", queries.includes("readOperationsCommandCenterWorkspace"));
assert("queries parallel fetch", queries.includes("Promise.all"));
assert("queries production workspace", queries.includes("getProductionFollowUpWorkspace"));
assert("queries material workspace", queries.includes("getMaterialAvailabilityRiskWorkspace"));
assert("queries procurement workspace", queries.includes("getProcurementDeliveryRiskWorkspace"));
assert("queries quality workspace", queries.includes("getQualityNcrFollowUpWorkspace"));
assert("queries maintenance workspace", queries.includes("getMaintenancePredictiveRiskWorkspace"));
assert("queries mold workspace", queries.includes("getMoldFollowUpWorkspace"));
assert("queries commercial alerts", queries.includes("listCommercialProfitabilityAlerts"));
assert("queries ai tasks", queries.includes("listAiTasks"));
assert("queries priority tiers documented", queries.includes("PRIORITY_TIER"));
assert("queries permission per module", queries.includes("evaluateModuleAccess"));
assert("queries subscription check", queries.includes("isModuleEntitled"));
assert("queries no detail prefetch", !queries.includes("getProductionFollowUpDetail"));
assert("queries no detail prefetch material", !queries.includes("getMaterialAvailabilityRiskDetail"));

assert("refresh action", actions.includes("refreshOperationsCommandCenterAction"));
assert("page any granted gate", page.includes("denyUnlessAnyGranted"));

forbid(view, "task-draft", "view must not expose task draft");
forbid(view, "follow-up", "view must not expose follow-up transition");
forbid(queries, "createTask", "queries must not create tasks");
forbid(queries, "transition", "queries must not transition follow-up");

console.log("OPERATIONS-COMMAND-CENTER-1 web contract verification passed.");
