#!/usr/bin/env node
import { readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), "../../..");

function fail(message) {
  console.error(`OPERATIONAL-RISK-UI-1 data-honesty verifier failed: ${message}`);
  process.exit(1);
}

function assert(label, ok, detail = "") {
  if (!ok) fail(`${label}${detail ? `: ${detail}` : ""}`);
  console.log(`✓ ${label}`);
}

function read(rel) {
  return readFileSync(resolve(ROOT, rel), "utf8").replaceAll("\r\n", "\n");
}

function forbidText(source, forbidden, label) {
  if (source.includes(forbidden)) fail(`${label} must not contain "${forbidden}"`);
}

console.log("Running OPERATIONAL-RISK-UI-1 data-honesty verifier.");

const view = read("apps/web/src/features/operational-risk/components/operational-risk-workspace-view.tsx");
const queries = read("apps/web/src/features/operational-risk/server/operational-risk-queries.ts");
const labels = read("apps/web/src/features/operational-risk/domain/operational-risk-labels.ts");
const presentation = read("apps/web/src/features/operational-risk/components/operational-risk-presentation.ts");
const bundle = `${view}\n${queries}\n${labels}\n${presentation}`;

assert("KPI reconciliation helper", presentation.includes("validateOperationalRiskKpiReconciliation"));
assert("distribution reconciliation helper", presentation.includes("reconcileOperationalRiskDistribution"));
assert("donut arc builder", presentation.includes("buildOperationalRiskDonutArcs"));
assert("unavailable adapters explicit", labels.includes("OPERATIONAL_RISK_ACTIVE_SOURCE_MODULES"));
assert("source coverage unavailable state", queries.includes('state: "unavailable"'));
assert("insufficient data semantics", labels.includes('"insufficient-data": "Veri yetersiz"'));
assert("source conflict semantics", labels.includes('"source-conflict": "Kaynak uyuşmazlığı"'));
assert("development banner Turkish", labels.includes("gerçek müşteri operasyonel kanıtı değildir"));
assert("simulated evidence label", labels.includes('simulated: "Simülasyon"'));

forbidText(bundle, "guaranteed savings", "bundle");
forbidText(bundle, "guaranteed loss", "bundle");
forbidText(bundle, "prevented loss", "bundle");
forbidText(bundle, "AI ", "bundle");
forbidText(bundle, "remediation", "bundle");
forbidText(bundle, "PLC write", "bundle");
forbidText(bundle, "machine command", "bundle");
forbidText(view, "bb000001", "view");
assert("no raw uuid primary cells", !/<td>\{[^}]*\.id\}<\/td>/.test(view));

assert("priority list limited to three", queries.includes(".slice(0, 3)"));
assert("six KPI order", labels.includes('"open-cases"') && labels.includes('"source-conflict"'));
assert("approved profile execution gate", queries.includes('profile.status === "approved"'));
assert("case title localization helper", presentation.includes("localizeOperationalRiskCaseTitle"));
assert("raw English title guard", presentation.includes("isRawEnglishOperationalRiskPrimaryTitle"));
assert("case title mapped in queries", queries.includes("localizeOperationalRiskCaseTitle(item.title"));
assert("profile rules mapped honestly", queries.includes("profile.rules"));
assert("visible token localization helper", presentation.includes("localizeOperationalRiskVisibleToken"));
assert("evidence summary localization", queries.includes("localizeOperationalRiskEvidenceSummary"));
assert("technical audit token collection", queries.includes("collectOperationalRiskTechnicalAuditTokens"));
assert("unknown technical reason fallback", presentation.includes("Tanımlanmamış teknik neden"));
assert("hasRuns detection summary gate", queries.includes("hasRuns:"));
assert("raw visible code inventory", presentation.includes("KNOWN_RAW_VISIBLE_CODES"));
assert("technical details restrained in view", view.includes("oprisk-technical-details"));

console.log("OPERATIONAL-RISK-UI-1 data-honesty verification passed.");
