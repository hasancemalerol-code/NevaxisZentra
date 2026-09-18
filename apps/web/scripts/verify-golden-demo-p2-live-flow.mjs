#!/usr/bin/env node

import { resolve } from "node:path";

const API_BASE = process.env.NEVAXIS_FACTOS_API_BASE_URL ?? "http://127.0.0.1:5000";
const WEB = process.env.NEVAXIS_FACTOS_WEB_BASE_URL ?? "http://localhost:3001";
const FACILITY_ID = "bb000001-0005-4000-8000-000000000005";
const EVENT_ID = "bb000001-0010-4000-8000-000000000001";
const ISSUER = "crevafactos-dev";
const OPERATOR = "production-pilot-operator";
const TENANT = "dev-pilot-tenant";

function headers() {
  return {
    Accept: "application/json",
    "Content-Type": "application/json",
    "X-crevafactos-dev-Issuer": ISSUER,
    "X-crevafactos-dev-Subject": OPERATOR,
    "X-NevaxisZentra-Tenant": TENANT,
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

  const status = await api("GET", "/api/golden-demo-p2/presentation-status");
  if (!status.ok) throw new Error(`presentation-status failed ${status.status}`);
  if (status.json.machineCode !== "INJ-001") throw new Error("machine must be INJ-001");
  if (status.json.stepNumber < 1) throw new Error("invalid step number");

  const reset = await api("POST", `/api/golden-demo-p2/events/${EVENT_ID}/reset`);
  if (!reset.ok) throw new Error(`reset failed ${reset.status} ${JSON.stringify(reset.json)}`);
  if (reset.json.resetStatus !== "active") throw new Error("reset must return active status");

  const after = await api("GET", "/api/golden-demo-p2/presentation-status");
  if (!after.ok) throw new Error("post-reset status failed");
  if (after.json.eventStatus !== "active") throw new Error("event must be active after reset");
  if (after.json.canReset) throw new Error("canReset must be false after reset");

  const page = await fetch(`${WEB}/golden-demo/uretim-kurtarma`, { redirect: "follow" });
  const html = await page.text();
  if (!html.includes("golden-demo-p2-presentation") && !html.includes("golden-demo-p2-step")) {
    throw new Error("P2 presentation panel not rendered");
  }

  console.log("GOLDEN_DEMO_P2_LIVE_CLOSURE = PASS");
}

main().catch((error) => {
  console.error(`GOLDEN_DEMO_P2_LIVE_CLOSURE = FAIL: ${error.message}`);
  process.exit(1);
});
