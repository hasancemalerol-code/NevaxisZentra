#!/usr/bin/env node
import { readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), "../../..");

function fail(message) {
  console.error(`MAINTENANCE-AI-TASK-INTEGRATION-1 web contract verification failed: ${message}`);
  process.exit(1);
}

function assert(label, ok, detail = "") {
  if (!ok) fail(`${label}${detail ? `: ${detail}` : ""}`);
  console.log(`✓ ${label}`);
}

function read(rel) {
  return readFileSync(resolve(ROOT, rel), "utf8").replaceAll("\r\n", "\n");
}

console.log("Running MAINTENANCE-AI-TASK-INTEGRATION-1 web contract verifier.");

const page = read("apps/web/src/app/(app)/bakim/kestirimci-riskler/page.tsx");
const view = read("apps/web/src/features/maintenance/components/maintenance-predictive-risk-workspace-view.tsx");
const reader = read("apps/web/src/features/maintenance/server/maintenance-predictive-risk-reader.ts");
const apiClient = read("apps/web/src/features/maintenance/server/maintenance-predictive-risk-api-client.ts");
const contracts = read("apps/web/src/features/maintenance/server/maintenance-predictive-risk-api-contracts.ts");
const actions = read("apps/web/src/features/maintenance/server/maintenance-predictive-risk-actions.ts");
const labels = read("apps/web/src/features/maintenance/presentation/maintenance-predictive-risk-labels.ts");
const domain = read("apps/web/src/features/maintenance/domain/maintenance-predictive-risk.ts");
const nav = read("apps/web/src/features/maintenance/components/maintenance-workspace-nav.tsx");
const sourceSection = read("apps/web/src/features/ai-task-center/components/ai-task-source-reference-section.tsx");
const endpoints = read("apps/api/Maintenance/MaintenancePredictiveRiskEndpointRouteBuilderExtensions.cs");

assert("dedicated kestirimci-riskler page exists", page.includes("MaintenancePredictiveRiskWorkspaceView"));
assert("page requires maintenance.read", page.includes('forbidUnlessGranted("maintenance.read")'));
assert("navigation route present", nav.includes('href: "/bakim/kestirimci-riskler"') && nav.includes("Kestirimci Riskler"));

assert("workspace testid", view.includes('data-testid="maintenance-predictive-risk-workspace"'));
assert("kpi grid testid", view.includes('data-testid="maintenance-predictive-risk-kpi-grid"'));
assert("risk table testid", view.includes('data-testid="maintenance-predictive-risk-table"'));
assert("detail ready testid", view.includes('data-testid="maintenance-predictive-risk-detail-ready"'));
assert("create task button testid", view.includes('data-testid="maintenance-predictive-risk-create-task-button"'));
assert("linked task link testid", view.includes('data-testid="maintenance-predictive-risk-linked-task-link"'));
assert("automation disclaimer testid", view.includes('data-testid="maintenance-predictive-risk-automation-disclaimer"'));
assert("asset url param", view.includes('searchParams.get("asset")') || page.includes("params.asset"));

assert("maintenance task draft button label", view.includes("Bakım görev taslağı oluştur"));
assert("data review task label", view.includes("Veri inceleme görev taslağı oluştur"));
assert("linked task navigation", view.includes("/ai-gorevleri/"));
assert("human review action", view.includes("maintenance-predictive-risk-mark-reviewed-button"));

assert("reader workspace loader", reader.includes("getMaintenancePredictiveRiskWorkspace"));
assert("reader assessment by asset", reader.includes("getMaintenancePredictiveRiskAssessment"));
assert("reader assessment by id", reader.includes("getMaintenancePredictiveRiskAssessmentById"));

assert("api client workspace path", apiClient.includes("/api/maintenance/predictive-risk/workspace"));
assert("api client task draft path", apiClient.includes("/task-draft"));
assert("api client follow-up path", apiClient.includes("/follow-up"));
assert("contracts workspace payload parser", contracts.includes("parseMaintenancePredictiveRiskWorkspacePayload"));
assert("contracts follow-up fields", contracts.includes("followUpStatus") && contracts.includes("linkedAiTaskId"));

assert("actions task draft permission", actions.includes('denyUnlessGranted("ai-task.create")'));
assert("actions follow-up permission", actions.includes('denyUnlessGranted("maintenance.manage")'));
assert("labels follow-up statuses", labels.includes("task-draft-created"));
assert("labels task disclaimer", labels.includes("otomatik bakım emri"));

assert("domain follow-up status type", domain.includes("MaintenancePredictiveRiskFollowUpStatus"));
assert("domain workspace payload type", domain.includes("MaintenancePredictiveRiskWorkspacePayload"));

assert("source section maintenance link", sourceSection.includes('sourceModule === "maintenance"'));
assert("source section assessment deep link", sourceSection.includes("/bakim/kestirimci-riskler?assessment="));

assert("api task draft endpoint", endpoints.includes('MapPost("/predictive-risk/{assessmentId:guid}/task-draft"'));
assert("api workspace endpoint", endpoints.includes('MapGet("/predictive-risk/workspace"'));
assert("api source module maintenance", endpoints.includes('MaintenancePredictiveRiskConstants.SourceModule'));

console.log("MAINTENANCE-AI-TASK-INTEGRATION-1 web contract verification passed.");
