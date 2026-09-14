#!/usr/bin/env node

const API_BASE = process.env.NEVAXIS_FACTOS_API_BASE_URL ?? "http://127.0.0.1:5000";
const WEB = process.env.NEVAXIS_FACTOS_WEB_BASE_URL ?? "http://localhost:3001";
const FACILITY_ID = "bb000001-0005-4000-8000-000000000005";
const EVENT_ID = "bb000001-0010-4000-8000-000000000001";
const ISSUER = "crevafactos-dev";
const OPERATOR = "production-pilot-operator";
const REVIEWER = "planning-pilot-reviewer";
const TENANT = "dev-pilot-tenant";

function headers(subject = OPERATOR) {
  return {
    Accept: "application/json",
    "Content-Type": "application/json",
    "X-crevafactos-dev-Issuer": ISSUER,
    "X-crevafactos-dev-Subject": subject,
    "X-NevaxisFactOS-Tenant": TENANT,
  };
}

async function api(method, path, body, subject) {
  const url = `${API_BASE}${path}${path.includes("?") ? "&" : "?"}facilityId=${FACILITY_ID}`;
  const response = await fetch(url, {
    method,
    headers: headers(subject),
    body: body ? JSON.stringify(body) : undefined,
    signal: AbortSignal.timeout(30000),
  });
  const json = await response.json().catch(() => ({}));
  return { status: response.status, json, ok: response.ok };
}

async function main() {
  const health = await fetch(`${API_BASE}/health/live`, { signal: AbortSignal.timeout(5000) });
  if (!health.ok) throw new Error("API not healthy");

  await api("POST", `/api/golden-demo-p2/events/${EVENT_ID}/reset`);

  const p2b = await api("GET", "/api/golden-demo-p2b/digital-factory-team");
  if (!p2b.ok) throw new Error("P2B regression failed");

  let approve = await api("POST", `/api/golden-demo-p1/events/${EVENT_ID}/approve`, { comment: "p3-closure" }, REVIEWER);
  if (!approve.ok) throw new Error(`approve failed ${approve.status}`);

  let execute = await api("POST", `/api/golden-demo-p1/events/${EVENT_ID}/execute`, { idempotencyKey: crypto.randomUUID() });
  if (!execute.ok) throw new Error(`execute failed ${execute.status}`);

  let complete = await api("POST", `/api/golden-demo-p1/events/${EVENT_ID}/complete-intervention`, {
    idempotencyKey: crypto.randomUUID(),
  });
  if (!complete.ok) throw new Error(`complete failed ${complete.status}`);

  const preVerify = await api("GET", "/api/golden-demo-p3/outcome");
  if (!preVerify.ok) throw new Error("pre-verify outcome failed");
  if (preVerify.json.hero.recoveredGoodPartsPerShift <= 0) throw new Error("recovered production must be calculated");
  if (!preVerify.json.metricComparisons.some((m) => m.metricCode === "cycle-time")) throw new Error("cycle comparison missing");

  let verify = await api("POST", `/api/golden-demo-p1/events/${EVENT_ID}/verify`, { idempotencyKey: crypto.randomUUID() }, REVIEWER);
  if (!verify.ok) throw new Error(`verify failed ${verify.status}`);
  if (verify.json.recoveredGoodPartsPerShift <= 0) throw new Error("verified recovery must be positive");

  const postVerify = await api("GET", "/api/golden-demo-p3/outcome");
  if (!postVerify.ok) throw new Error("post-verify outcome failed");
  if (postVerify.json.outcomeStatus !== "verified") throw new Error(`expected verified, got ${postVerify.json.outcomeStatus}`);
  if (postVerify.json.businessImpact.estimatedBusinessClassification !== "estimated") {
    throw new Error("business impact must remain estimated");
  }

  const page = await fetch(`${WEB}/golden-demo/uretim-kurtarma`, { redirect: "follow" });
  const html = await page.text();
  if (!html.includes("golden-demo-p3-outcome-hero")) throw new Error("P3 outcome hero not rendered");
  if (!html.includes("golden-demo-p3-recovered-good-parts")) throw new Error("recovered KPI not rendered");
  if (!html.includes("golden-demo-p2b-digital-factory-team")) throw new Error("P2B regression in UI");

  console.log("GOLDEN_DEMO_P3_LIVE_CLOSURE = PASS");
}

main().catch((error) => {
  console.error(`GOLDEN_DEMO_P3_LIVE_CLOSURE = FAIL: ${error.message}`);
  process.exit(1);
});
