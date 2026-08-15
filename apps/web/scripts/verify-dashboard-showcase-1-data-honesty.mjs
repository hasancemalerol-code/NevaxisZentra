#!/usr/bin/env node
import { readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), "../../..");

const REFERENCE_NUMBERS = [
  "42.680",
  "42680",
  "50.800",
  "50800",
  "78,5",
  "78.5",
  "2,35",
  "2.35",
  "22,8",
  "22.8",
  "96,4",
  "96.4",
  "20.450",
  "20450",
  "12.810",
  "12810",
];

function fail(message) {
  console.error(`DASHBOARD-SHOWCASE-1 data-honesty verification failed: ${message}`);
  process.exit(1);
}

function assert(label, ok, detail = "") {
  if (!ok) fail(`${label}${detail ? `: ${detail}` : ""}`);
  console.log(`✓ ${label}`);
}

function readRequired(relativePath) {
  return readFileSync(resolve(ROOT, relativePath), "utf8").replaceAll("\r\n", "\n");
}

console.log("Running DASHBOARD-SHOWCASE-1 data-honesty verifier.");

const queries = readRequired("apps/web/src/features/dashboard-showcase/server/dashboard-showcase-queries.ts");
const mapper = readRequired("apps/web/src/features/dashboard-showcase/server/dashboard-showcase-mapper.ts");
const view = readRequired("apps/web/src/features/dashboard-showcase/components/factory-showcase-view.tsx");
const labels = readRequired("apps/web/src/features/dashboard-showcase/domain/dashboard-showcase-labels.ts");
const actions = readRequired("apps/web/src/features/dashboard-showcase/server/dashboard-showcase-actions.ts");

assert("reader uses production reporting overview", queries.includes("getProductionReportingOverview"));
assert("reader uses production orders", queries.includes("listProductionOrderPerformance"));
assert("reader uses production shifts", queries.includes("listProductionShiftReporting"));
assert("reader uses scrap analytics", queries.includes("getProductionScrapAnalytics"));
assert("reader uses cycle analytics", queries.includes("getProductionCycleAnalytics"));
assert("reader uses process OEE workspace", queries.includes("readProcessOeeWorkspace"));
assert("reader uses process OEE assessments", queries.includes("readProcessOeeAssessments"));
assert("reader uses quality reporting overview", queries.includes("readQualityReportingOverview"));
assert("mapper missing values render dash", labels.includes('return "—"'));
assert("pending state in mapper/cards", mapper.includes("pending:") && view.includes("Veri bekleniyor"));
assert("safe distribution labels", mapper.includes("DISTRIBUTION_GROUP_LABELS"));
assert("no customer/order labels in mapper", !mapper.includes("referenceCode") && !mapper.includes("customer"));
assert("refresh action delegates to reader", actions.includes("readDashboardShowcaseWorkspace"));
assert("development pilot banner guard", queries.includes("developmentPilotBanner"));
assert("presentation mode selection", queries.includes("isLiveShowcaseSourceComplete") && queries.includes("buildDevelopmentShowcaseViewModel"));
assert("development showcase dataset module", readRequired("apps/web/src/features/dashboard-showcase/domain/dashboard-showcase-development-dataset.ts").includes("presentationMode: \"development-showcase\""));
assert("retain last valid on client refresh", view.includes("if (result.status === \"success\")"));

const devDataset = readRequired("apps/web/src/features/dashboard-showcase/domain/dashboard-showcase-development-dataset.ts");
for (const number of REFERENCE_NUMBERS) {
  for (const source of [queries, mapper, view, labels, actions, devDataset]) {
    if (source.includes(number)) {
      fail(`reference mock number "${number}" found in implementation sources`);
    }
  }
}

assert("no mock-data import", !queries.includes("mock-data") && !mapper.includes("mock-data"));
assert("no dashboardPreviewData import", !queries.includes("dashboardPreviewData"));

console.log("DASHBOARD-SHOWCASE-1 data-honesty verification passed.");
