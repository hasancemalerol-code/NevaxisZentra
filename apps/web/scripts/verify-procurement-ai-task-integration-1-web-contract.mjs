#!/usr/bin/env node
import { readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), "../../..");

function fail(message) {
  console.error(`PROCUREMENT-AI-TASK-INTEGRATION-1 web contract verification failed: ${message}`);
  process.exit(1);
}

function assert(label, ok, detail = "") {
  if (!ok) fail(`${label}${detail ? `: ${detail}` : ""}`);
  console.log(`✓ ${label}`);
}

function read(rel) {
  return readFileSync(resolve(ROOT, rel), "utf8").replaceAll("\r\n", "\n");
}

console.log("Running PROCUREMENT-AI-TASK-INTEGRATION-1 web contract verifier.");

const page = read("apps/web/src/app/(app)/satin-alma/tedarik-riskleri/page.tsx");
const view = read("apps/web/src/features/procurement-delivery-risk/components/procurement-delivery-risk-workspace-view.tsx");
const reader = read("apps/web/src/features/procurement-delivery-risk/server/procurement-delivery-risk-reader.ts");
const apiClient = read("apps/web/src/features/procurement-delivery-risk/server/procurement-delivery-risk-api-client.ts");
const contracts = read("apps/web/src/features/procurement-delivery-risk/server/procurement-delivery-risk-api-contracts.ts");
const actions = read("apps/web/src/features/procurement-delivery-risk/server/procurement-delivery-risk-actions.ts");
const labels = read("apps/web/src/features/procurement-delivery-risk/presentation/procurement-delivery-risk-labels.ts");
const domain = read("apps/web/src/features/procurement-delivery-risk/domain/procurement-delivery-risk.ts");
const sourceSection = read("apps/web/src/features/ai-task-center/components/ai-task-source-reference-section.tsx");
const endpoints = read("apps/api/Procurement/ProcurementDeliveryRiskFollowUpEndpointRouteBuilderExtensions.cs");

assert("dedicated tedarik-riskleri page exists", page.includes("ProcurementDeliveryRiskWorkspaceView"));
assert("page requires erp.read", page.includes('forbidUnlessGranted("erp.read")'));

assert("workspace testid", view.includes('data-testid="procurement-delivery-risk-workspace"'));
assert("kpi grid testid", view.includes('data-testid="procurement-delivery-risk-kpi-grid"'));
assert("assessment table testid", view.includes('data-testid="procurement-delivery-risk-table"'));
assert("detail testid", view.includes('data-testid="procurement-delivery-risk-detail"'));
assert("create task button testid", view.includes('data-testid="procurement-delivery-risk-create-task-button"'));
assert("linked task link testid", view.includes('data-testid="procurement-delivery-risk-linked-task-link"'));
assert("follow-up audit testid", view.includes('data-testid="procurement-delivery-risk-follow-up-audit"'));
assert("automation disclaimer testid", view.includes('data-testid="procurement-delivery-risk-automation-disclaimer"'));
assert("assessment url param", view.includes('searchParams.get("assessment")'));

assert("human review task label", view.includes("Tedarik inceleme görev taslağı oluştur"));
assert("data review task label", view.includes("Veri inceleme görev taslağı oluştur"));
assert("linked task navigation", view.includes("/ai-gorevleri/"));
assert("acknowledge action", view.includes("procurement-delivery-risk-acknowledge-button"));
assert("acknowledge label", view.includes("Görüldü olarak işaretle"));
assert("automation disclaimer copy", view.includes("Otomatik satın alma, tedarikçi iletişimi, stok veya üretim kararı uygulanmaz"));

assert("reader workspace loader", reader.includes("getProcurementDeliveryRiskWorkspace"));
assert("reader assessment detail", reader.includes("getProcurementDeliveryRiskDetail"));

assert("api client workspace path", apiClient.includes("/api/procurement/delivery-risks/workspace"));
assert("api client task draft path", apiClient.includes("/task-draft"));
assert("api client follow-up path", apiClient.includes("/follow-up"));
assert("contracts workspace payload parser", contracts.includes("parseProcurementDeliveryRiskWorkspacePayload"));
assert("contracts follow-up fields", contracts.includes("followUpStatus") && contracts.includes("linkedAiTaskId"));

assert("actions task draft permission", actions.includes('denyUnlessGranted("ai-task.create")'));
assert("actions follow-up permission", actions.includes('denyUnlessGranted("erp.manage")'));
assert("labels follow-up statuses", labels.includes("requires-review"));

assert("domain follow-up audit type", domain.includes("ProcurementDeliveryRiskFollowUpAuditEvent"));
assert("domain workspace payload type", domain.includes("ProcurementDeliveryRiskWorkspacePayload"));
assert("domain canCreateTask capability", domain.includes("canCreateTask"));

assert("source section procurement link", sourceSection.includes('sourceModule === "procurement"'));
assert("source section assessment deep link", sourceSection.includes("/satin-alma/tedarik-riskleri?assessment="));

assert("api task draft endpoint", endpoints.includes("/{assessmentId:guid}/task-draft"));
assert("api follow-up endpoint", endpoints.includes("/{assessmentId:guid}/follow-up"));
assert("api source module procurement", endpoints.includes('ProcurementDeliveryRiskFollowUpConstants.SourceModule'));
assert("api idempotency key format", endpoints.includes("BuildTaskDraftIdempotencyKey"));

console.log("PROCUREMENT-AI-TASK-INTEGRATION-1 web contract verification passed.");
