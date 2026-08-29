#!/usr/bin/env node
import { readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), "../../..");

function fail(message) {
  console.error(`INVENTORY-AI-TASK-INTEGRATION-1 web contract verification failed: ${message}`);
  process.exit(1);
}

function assert(label, ok, detail = "") {
  if (!ok) fail(`${label}${detail ? `: ${detail}` : ""}`);
  console.log(`✓ ${label}`);
}

function read(rel) {
  return readFileSync(resolve(ROOT, rel), "utf8").replaceAll("\r\n", "\n");
}

console.log("Running INVENTORY-AI-TASK-INTEGRATION-1 web contract verifier.");

const page = read("apps/web/src/app/(app)/stok-ve-depo/malzeme-riskleri/page.tsx");
const view = read("apps/web/src/features/material-availability-risk/components/material-availability-risk-workspace-view.tsx");
const reader = read("apps/web/src/features/material-availability-risk/server/material-availability-risk-reader.ts");
const apiClient = read("apps/web/src/features/material-availability-risk/server/material-availability-risk-api-client.ts");
const contracts = read("apps/web/src/features/material-availability-risk/server/material-availability-risk-api-contracts.ts");
const actions = read("apps/web/src/features/material-availability-risk/server/material-availability-risk-actions.ts");
const labels = read("apps/web/src/features/material-availability-risk/presentation/material-availability-risk-labels.ts");
const domain = read("apps/web/src/features/material-availability-risk/domain/material-availability-risk.ts");
const sourceSection = read("apps/web/src/features/ai-task-center/components/ai-task-source-reference-section.tsx");
const endpoints = read("apps/api/Material/MaterialAvailabilityRiskFollowUpEndpointRouteBuilderExtensions.cs");

assert("dedicated malzeme-riskleri page exists", page.includes("MaterialAvailabilityRiskWorkspaceView"));
assert("page requires material.read", page.includes('forbidUnlessGranted("material.read")'));

assert("workspace testid", view.includes('data-testid="material-availability-risk-workspace"'));
assert("kpi grid testid", view.includes('data-testid="material-availability-risk-kpi-grid"'));
assert("assessment table testid", view.includes('data-testid="material-availability-risk-table"'));
assert("detail ready testid", view.includes('data-testid="material-availability-risk-detail-ready"'));
assert("create task button testid", view.includes('data-testid="material-availability-risk-create-task-button"'));
assert("linked task link testid", view.includes('data-testid="material-availability-risk-linked-task-link"'));
assert("insufficient evidence testid", view.includes('data-testid="material-availability-risk-insufficient-evidence"'));
assert("assessment url param", view.includes('searchParams.get("assessment")'));

assert("human review task label", view.includes("Stok ve malzeme inceleme görev taslağı oluştur"));
assert("data review task label", view.includes("Veri inceleme görev taslağı oluştur"));
assert("linked task navigation", view.includes("/ai-gorevleri/"));
assert("acknowledge action", view.includes("material-availability-risk-acknowledge-button"));

assert("reader workspace loader", reader.includes("getMaterialAvailabilityRiskWorkspace"));
assert("reader assessment detail", reader.includes("getMaterialAvailabilityRiskDetail"));

assert("api client workspace path", apiClient.includes("/api/materials/availability-risks/workspace"));
assert("api client task draft path", apiClient.includes("/task-draft"));
assert("api client follow-up path", apiClient.includes("/follow-up"));
assert("contracts workspace payload parser", contracts.includes("parseMaterialAvailabilityRiskWorkspacePayload"));
assert("contracts follow-up fields", contracts.includes("followUpStatus") && contracts.includes("linkedAiTaskId"));

assert("actions task draft permission", actions.includes('denyUnlessGranted("ai-task.create")'));
assert("actions follow-up permission", actions.includes('denyUnlessGranted("material.manage")'));
assert("labels follow-up statuses", labels.includes("requires-review"));
assert("labels task disclaimer", labels.includes("Otomatik stok, satın alma veya üretim işlemi uygulanmaz"));

assert("domain follow-up status type", domain.includes("MaterialAvailabilityRiskFollowUpStatus"));
assert("domain workspace payload type", domain.includes("MaterialAvailabilityRiskWorkspacePayload"));

assert("source section material link", sourceSection.includes('sourceModule === "material"'));
assert("source section assessment deep link", sourceSection.includes("/stok-ve-depo/malzeme-riskleri?assessment="));

assert("api task draft endpoint", endpoints.includes("/{assessmentId:guid}/task-draft"));
assert("api follow-up endpoint", endpoints.includes("/{assessmentId:guid}/follow-up"));
assert("api source module material", endpoints.includes('MaterialAvailabilityRiskFollowUpConstants.SourceModule'));

console.log("INVENTORY-AI-TASK-INTEGRATION-1 web contract verification passed.");
