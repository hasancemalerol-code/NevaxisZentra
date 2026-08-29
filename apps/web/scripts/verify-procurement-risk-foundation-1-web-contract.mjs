#!/usr/bin/env node
import { readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), "../../..");

function fail(message) {
  console.error(`PROCUREMENT-RISK-FOUNDATION-1 web contract verification failed: ${message}`);
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

console.log("Running PROCUREMENT-RISK-FOUNDATION-1 web contract verifier.");

const page = read("apps/web/src/app/(app)/satin-alma/tedarik-riskleri/page.tsx");
const view = read("apps/web/src/features/procurement-delivery-risk/components/procurement-delivery-risk-workspace-view.tsx");
const reader = read("apps/web/src/features/procurement-delivery-risk/server/procurement-delivery-risk-reader.ts");
const apiClient = read("apps/web/src/features/procurement-delivery-risk/server/procurement-delivery-risk-api-client.ts");
const contracts = read("apps/web/src/features/procurement-delivery-risk/server/procurement-delivery-risk-api-contracts.ts");
const actions = read("apps/web/src/features/procurement-delivery-risk/server/procurement-delivery-risk-actions.ts");
const labels = read("apps/web/src/features/procurement-delivery-risk/presentation/procurement-delivery-risk-labels.ts");
const nav = read("apps/web/src/lib/navigation/primary-navigation.ts");
const endpoints = read("apps/api/Procurement/ProcurementDeliveryRiskEndpointRouteBuilderExtensions.cs");
const featureBundle = [page, view, actions].join("\n");

assert("dedicated tedarik-riskleri page exists", page.includes("ProcurementDeliveryRiskWorkspaceView"));
assert("page requires erp.read", page.includes('forbidUnlessGranted("erp.read")'));
assert("page route path", page.includes("Tedarik Riskleri"));
assert("navigation route present", nav.includes('href: "/satin-alma/tedarik-riskleri"') && nav.includes("Tedarik Riskleri"));

assert("workspace testid", view.includes('data-testid="procurement-delivery-risk-workspace"'));
assert("readonly note testid", view.includes('data-testid="procurement-delivery-risk-readonly-note"'));
assert("kpi grid testid", view.includes('data-testid="procurement-delivery-risk-kpi-grid"'));
assert("risk table testid", view.includes('data-testid="procurement-delivery-risk-table"'));
assert("detail panel testid", view.includes('data-testid="procurement-delivery-risk-detail"'));
assert("history testid", view.includes('data-testid="procurement-delivery-risk-history"'));
assert("evaluate button testid", view.includes('data-testid="procurement-delivery-risk-evaluate"'));
assert("assessment url param", view.includes('searchParams.get("assessment")') || page.includes("params.assessment"));
assert("row testid pattern", view.includes('data-testid={`procurement-delivery-risk-row-${row.id}`}'));

assert("turkish page title", page.includes("Tedarik Riskleri"));
assert("turkish status labels", labels.includes("Gecikmiş") && labels.includes("Teslimat riski") && labels.includes("Yetersiz veri"));
assert("turkish kpi labels", view.includes("Gecikmiş") && view.includes("Plan dahilinde") && view.includes("Yetersiz veri"));
assert("turkish evaluate label", view.includes("Değerlendir"));
assert("turkish disclaimer constant", labels.includes("otomatik satın alma, tedarikçi iletişimi, stok veya üretim işlemi yapmaz"));
assert("disclaimer rendered in view", view.includes("PROCUREMENT_DELIVERY_RISK_DISCLAIMER"));

forbid("feature bundle no ai task routes", featureBundle, "/ai-gorevleri/");
forbid("feature bundle no task draft action", featureBundle, "görev taslağı");
forbid("feature bundle no purchase order create", featureBundle, "satın alma emri oluştur");
forbid("feature bundle no supplier contact action", featureBundle, "tedarikçi mesaj");

assert("reader workspace loader", reader.includes("readProcurementDeliveryRiskWorkspace"));
assert("reader uses workspace api", reader.includes("getProcurementDeliveryRiskWorkspace"));
assert("reader detail loader", reader.includes("getProcurementDeliveryRiskDetail"));
assert("reader history loader", reader.includes("getProcurementDeliveryRiskHistory"));

assert("api client workspace route", apiClient.includes("/api/procurement/delivery-risks/workspace"));
assert("api client evaluate route", apiClient.includes("/api/procurement/delivery-risks/evaluate"));
assert("contracts parse workspace", contracts.includes("parseProcurementDeliveryRiskWorkspacePayload"));
assert("contracts parse detail", contracts.includes("parseProcurementDeliveryRiskDetail"));
assert("evaluate action requires erp.manage", actions.includes('denyUnlessGranted("erp.manage")'));
assert("refresh action requires erp.read", actions.includes('denyUnlessGranted("erp.read")'));

assert("api workspace route", endpoints.includes("/api/procurement/delivery-risks"));
assert("api evaluate route", endpoints.includes('MapPost("/evaluate"'));
assert("api history route", endpoints.includes("/history"));

console.log("PROCUREMENT-RISK-FOUNDATION-1 web contract verification passed.");
