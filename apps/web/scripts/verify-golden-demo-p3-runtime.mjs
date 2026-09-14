#!/usr/bin/env node

import { readFileSync } from "node:fs";
import { resolve } from "node:path";

const ROOT = resolve(import.meta.dirname, "../../..");
const API_BASE = process.env.NEVAXIS_FACTOS_API_BASE_URL ?? "http://127.0.0.1:5000";
const FACILITY_ID = "bb000001-0005-4000-8000-000000000005";
const ISSUER = "crevafactos-dev";
const OPERATOR = "production-pilot-operator";
const TENANT = "dev-pilot-tenant";

for (const relativePath of [
  "packages/NevaxisFactOS.Persistence/GoldenDemoP3/GoldenDemoP3Engines.cs",
  "apps/api/GoldenDemoP3/GoldenDemoP3EndpointRouteBuilderExtensions.cs",
  "apps/web/src/features/golden-demo-p3/components/golden-demo-p3-outcome-panel.tsx",
  "tests/NevaxisFactOS.Persistence.Tests/GoldenDemoP3FoundationTests.cs",
]) {
  readFileSync(resolve(ROOT, relativePath), "utf8");
}

function headers() {
  return {
    Accept: "application/json",
    "Content-Type": "application/json",
    "X-crevafactos-dev-Issuer": ISSUER,
    "X-crevafactos-dev-Subject": OPERATOR,
    "X-NevaxisFactOS-Tenant": TENANT,
  };
}

async function api(method, path, body) {
  const url = `${API_BASE}${path}${path.includes("?") ? "&" : "?"}facilityId=${FACILITY_ID}`;
  const response = await fetch(url, {
    method,
    headers: headers(),
    body: body ? JSON.stringify(body) : undefined,
    signal: AbortSignal.timeout(30000),
  });
  const json = await response.json().catch(() => ({}));
  return { status: response.status, json, ok: response.ok };
}

async function main() {
  const health = await fetch(`${API_BASE}/health/live`, { signal: AbortSignal.timeout(5000) });
  if (!health.ok) throw new Error("API not healthy");

  const outcome = await api("GET", "/api/golden-demo-p3/outcome");
  if (!outcome.ok) throw new Error(`outcome failed ${outcome.status} ${JSON.stringify(outcome.json)}`);
  if (outcome.json.machineCode !== "INJ-001") throw new Error("machine must be INJ-001");
  if (!outcome.json.validationWindow) throw new Error("validation window missing");
  if (!outcome.json.hero) throw new Error("hero missing");
  if (!Array.isArray(outcome.json.metricComparisons)) throw new Error("metric comparisons missing");
  if (!outcome.json.businessImpact?.estimatedBusinessClassification) throw new Error("business classification missing");
  if (outcome.json.businessImpact.estimatedBusinessClassification === "verified") {
    throw new Error("financial values must not be marked verified");
  }

  console.log("GOLDEN_DEMO_P3_RUNTIME = PASS");
}

main().catch((error) => {
  console.error(`GOLDEN_DEMO_P3_RUNTIME = FAIL: ${error.message}`);
  process.exit(1);
});
