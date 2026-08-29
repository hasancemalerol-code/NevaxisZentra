#!/usr/bin/env node
import { readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), "../../..");

function fail(message) {
  console.error(`INVENTORY-RISK-FOUNDATION-1 web contract verification failed: ${message}`);
  process.exit(1);
}

function assert(label, ok, detail = "") {
  if (!ok) fail(`${label}${detail ? `: ${detail}` : ""}`);
  console.log(`✓ ${label}`);
}

function forbid(label, source, forbidden) {
  if (source.includes(forbidden)) fail(`${label} must not contain "${forbidden}"`);
}

function read(rel) {
  return readFileSync(resolve(ROOT, rel), "utf8").replaceAll("\r\n", "\n");
}

console.log("Running INVENTORY-RISK-FOUNDATION-1 web contract verifier.");

const page = read("apps/web/src/app/(app)/stok-ve-depo/malzeme-riskleri/page.tsx");
const view = read("apps/web/src/features/material-availability-risk/components/material-availability-risk-workspace-view.tsx");
const reader = read("apps/web/src/features/material-availability-risk/server/material-availability-risk-reader.ts");
const apiClient = read("apps/web/src/features/material-availability-risk/server/material-availability-risk-api-client.ts");
const contracts = read("apps/web/src/features/material-availability-risk/server/material-availability-risk-api-contracts.ts");
const actions = read("apps/web/src/features/material-availability-risk/server/material-availability-risk-actions.ts");
const labels = read("apps/web/src/features/material-availability-risk/presentation/material-availability-risk-labels.ts");
const domain = read("apps/web/src/features/material-availability-risk/domain/material-availability-risk.ts");
const nav = read("apps/web/src/lib/navigation/primary-navigation.ts");
const endpoints = read("apps/api/Material/MaterialAvailabilityRiskEndpointRouteBuilderExtensions.cs");
const featureBundle = [page, view, actions, labels].join("\n");

assert("dedicated malzeme-riskleri page exists", page.includes("MaterialAvailabilityRiskWorkspaceView"));
assert("page requires material.read", page.includes('forbidUnlessGranted("material.read")'));
assert("page route path", page.includes("Malzeme Riskleri"));
assert("navigation route present", nav.includes('href: "/stok-ve-depo/malzeme-riskleri"') && nav.includes("Malzeme Riskleri"));

assert("workspace testid", view.includes('data-testid="material-availability-risk-workspace"'));
assert("readonly note testid", view.includes('data-testid="material-availability-risk-readonly-note"'));
assert("kpi grid testid", view.includes('data-testid="material-availability-risk-kpi-grid"'));
assert("risk table testid", view.includes('data-testid="material-availability-risk-table"'));
assert("detail panel testid", view.includes('data-testid="material-availability-risk-detail-panel"'));
assert("detail ready testid", view.includes('data-testid="material-availability-risk-detail-ready"'));
assert("drivers testid", view.includes('data-testid="material-availability-risk-drivers"'));
assert("evidence testid", view.includes('data-testid="material-availability-risk-evidence"'));
assert("history testid", view.includes('data-testid="material-availability-risk-history"'));
assert("evaluate button testid", view.includes('data-testid="material-availability-risk-evaluate-button"'));
assert("material link testid", view.includes('data-testid="material-availability-risk-material-link"'));
assert("automation disclaimer testid", view.includes('data-testid="material-availability-risk-automation-disclaimer"'));
assert("assessment url param", view.includes('searchParams.get("assessment")') || page.includes("params.assessment"));
assert("row testid pattern", view.includes('data-testid={`material-availability-risk-row-${row.id}`}'));

assert("turkish page title", page.includes("Malzeme Riskleri"));
assert("turkish status labels", labels.includes("Yeterli") && labels.includes("Eksik") && labels.includes("Yetersiz veri"));
assert("turkish kpi labels", view.includes("Eksik") && view.includes("Yeterli") && view.includes("Yetersiz veri"));
assert("turkish evaluate label", view.includes("Yeniden değerlendir"));
assert("turkish material link", view.includes("Malzeme kaydını aç"));
assert("turkish disclaimer constant", labels.includes("otomatik stok, satın alma veya üretim işlemi yapmaz"));
assert("disclaimer rendered in view", view.includes("MATERIAL_AVAILABILITY_RISK_DISCLAIMER"));

forbid("feature bundle no ai task routes", featureBundle, "/ai-gorevleri/");
forbid("feature bundle no task draft action", featureBundle, "görev taslağı");
forbid("feature bundle no purchase order action", featureBundle, "satın alma emri");
forbid("feature bundle no create task button", featureBundle, "create-task-button");
forbid("feature bundle no linked task link", featureBundle, "linked-task");
assert("no task draft action strings", true);
assert("no purchase action strings", true);

assert("reader workspace loader", reader.includes("readMaterialAvailabilityRiskWorkspace"));
assert("reader uses workspace api", reader.includes("getMaterialAvailabilityRiskWorkspace"));
assert("reader detail loader", reader.includes("getMaterialAvailabilityRiskDetail"));
assert("reader history loader", reader.includes("getMaterialAvailabilityRiskHistory"));

assert("api client workspace path", apiClient.includes("/api/materials/availability-risks/workspace"));
assert("api client detail path", apiClient.includes("/api/materials/availability-risks/"));
assert("api client evaluate path", apiClient.includes("/api/materials/availability-risks/evaluate"));
assert("contracts workspace payload parser", contracts.includes("parseMaterialAvailabilityRiskWorkspacePayload"));
assert("contracts detail parser", contracts.includes("parseMaterialAvailabilityRiskDetail"));
assert("contracts evaluate parser", contracts.includes("parseMaterialAvailabilityRiskEvaluateResponse"));
assert("contracts shortage fields", contracts.includes("shortageQuantity") && contracts.includes("sourceFreshness"));

assert("actions evaluate permission", actions.includes('denyUnlessGranted("material.manage")'));
assert("actions refresh permission", actions.includes('denyUnlessGranted("material.read")'));
assert("actions revalidate route", actions.includes("/stok-ve-depo/malzeme-riskleri"));

assert("domain workspace payload type", domain.includes("MaterialAvailabilityRiskWorkspacePayload"));
assert("domain detail type", domain.includes("MaterialAvailabilityRiskDetail"));
assert("domain status union", domain.includes('"shortage"') && domain.includes('"insufficient-data"'));

assert("api workspace endpoint", endpoints.includes('MapGet("/workspace"'));
assert("api evaluate endpoint", endpoints.includes('MapPost("/evaluate"'));
assert("api detail endpoint", endpoints.includes('MapGet("/{assessmentId:guid}"'));
assert("api history endpoint", endpoints.includes("/history"));

console.log("INVENTORY-RISK-FOUNDATION-1 web contract verification passed.");
