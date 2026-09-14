#!/usr/bin/env node

import { readFileSync } from "node:fs";
import { resolve } from "node:path";

const ROOT = resolve(import.meta.dirname, "../../..");

const requiredPaths = [
  "apps/web/src/features/golden-demo-p2b/components/golden-demo-p2b-digital-factory-panel.tsx",
  "apps/web/src/features/golden-demo-p2b/server/golden-demo-p2b-api-client.ts",
  "apps/api/GoldenDemoP2B/GoldenDemoP2BEndpointRouteBuilderExtensions.cs",
  "packages/NevaxisFactOS.Persistence/GoldenDemoP2B/GoldenDemoP2BEngines.cs",
  "tests/NevaxisFactOS.Persistence.Tests/GoldenDemoP2BFoundationTests.cs",
];

for (const relativePath of requiredPaths) {
  readFileSync(resolve(ROOT, relativePath), "utf8");
}

const API_BASE = process.env.NEVAXIS_FACTOS_API_BASE_URL ?? "http://127.0.0.1:5000";
const FACILITY_ID = "bb000001-0005-4000-8000-000000000005";
const ISSUER = "crevafactos-dev";
const OPERATOR = "production-pilot-operator";
const TENANT = "dev-pilot-tenant";

function headers() {
  return {
    Accept: "application/json",
    "Content-Type": "application/json",
    "X-crevafactos-dev-Issuer": ISSUER,
    "X-crevafactos-dev-Subject": OPERATOR,
    "X-NevaxisFactOS-Tenant": TENANT,
  };
}

async function api(method, path) {
  const url = `${API_BASE}${path}${path.includes("?") ? "&" : "?"}facilityId=${FACILITY_ID}`;
  const response = await fetch(url, { method, headers: headers(), signal: AbortSignal.timeout(30000) });
  const json = await response.json().catch(() => ({}));
  return { status: response.status, json, ok: response.ok };
}

async function main() {
  const health = await fetch(`${API_BASE}/health/live`, { signal: AbortSignal.timeout(5000) });
  if (!health.ok) throw new Error("API not healthy");

  const team = await api("GET", "/api/golden-demo-p2b/digital-factory-team");
  if (!team.ok) throw new Error(`digital-factory-team failed ${team.status} ${JSON.stringify(team.json)}`);
  if (team.json.machineCode !== "INJ-001") throw new Error("machine must be INJ-001");
  if (!Array.isArray(team.json.agents) || team.json.agents.length < 5) throw new Error("agents missing");
  if (!team.json.orchestrator?.inputsUsed?.length) throw new Error("orchestrator inputs missing");
  if (!Array.isArray(team.json.activityTimeline) || team.json.activityTimeline.length < 5) {
    throw new Error("activity timeline missing");
  }
  if (!team.json.approvalSheet?.recommendedIntervention) throw new Error("approval sheet missing");
  if (!Array.isArray(team.json.planActions) || team.json.planActions.length < 1) {
    throw new Error("plan traceability missing");
  }
  if (!Array.isArray(team.json.interventionTimeline) || team.json.interventionTimeline.length < 1) {
    throw new Error("intervention timeline missing");
  }

  const machineAgent = team.json.agents.find((agent) => agent.agentCode === "machine-performance");
  if (!machineAgent) throw new Error("machine-performance agent missing");
  if (!machineAgent.calculationResults?.cycleLossSeconds) throw new Error("cycle loss evidence missing");

  console.log("GOLDEN_DEMO_P2B_RUNTIME = PASS");
}

main().catch((error) => {
  console.error(`GOLDEN_DEMO_P2B_RUNTIME = FAIL: ${error.message}`);
  process.exit(1);
});
