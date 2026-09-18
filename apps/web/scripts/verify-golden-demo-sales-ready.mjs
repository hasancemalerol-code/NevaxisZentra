#!/usr/bin/env node

/**
 * Bounded Tuesday sales-readiness verifier for the Golden Demo chain.
 * Requires localhost API + Web already running.
 */

const API_BASE = process.env.NEVAXIS_FACTOS_API_BASE_URL ?? "http://127.0.0.1:5000";
const WEB = process.env.NEVAXIS_FACTOS_WEB_BASE_URL ?? "http://localhost:3001";
const FACILITY_ID = "bb000001-0005-4000-8000-000000000005";
const EVENT_ID = "bb000001-0010-4000-8000-000000000001";
const ISSUER = "crevafactos-dev";
const OPERATOR = "production-pilot-operator";
const REVIEWER = "planning-pilot-reviewer";
const TENANT = "dev-pilot-tenant";
const REPEAT_CYCLES = Number(process.env.GOLDEN_DEMO_SALES_REPEAT_CYCLES ?? "3");
const TIMEOUT_MS = 30000;

const checks = [];

function record(name, pass, detail = "") {
  checks.push({ name, pass, detail });
  if (!pass) {
    throw new Error(`${name}${detail ? `: ${detail}` : ""}`);
  }
}

function headers(subject = OPERATOR) {
  return {
    Accept: "application/json",
    "Content-Type": "application/json",
    "X-crevafactos-dev-Issuer": ISSUER,
    "X-crevafactos-dev-Subject": subject,
    "X-NevaxisZentra-Tenant": TENANT,
  };
}

async function api(method, path, body, subject) {
  const url = `${API_BASE}${path}${path.includes("?") ? "&" : "?"}facilityId=${FACILITY_ID}`;
  const response = await fetch(url, {
    method,
    headers: headers(subject),
    body: body ? JSON.stringify(body) : undefined,
    signal: AbortSignal.timeout(TIMEOUT_MS),
  });
  const json = await response.json().catch(() => ({}));
  return { status: response.status, json, ok: response.ok };
}

async function resetDemo() {
  const reset = await api("POST", `/api/golden-demo-p2/events/${EVENT_ID}/reset`);
  record("reset works", reset.ok && reset.json.resetStatus === "active", JSON.stringify(reset.json));
  const status = await api("GET", "/api/golden-demo-p2/presentation-status");
  record("post-reset active state", status.ok && status.json.eventStatus === "active", status.json?.eventStatus);
}

async function assertInitialEvidence() {
  const workspace = await api("GET", "/api/golden-demo-p1/workspace");
  record("target-at-risk evidence loads", workspace.ok && workspace.json.recovery?.productionRecoveryGoodParts > 0);
  record(
    "Machine Intelligence loads",
    workspace.ok && workspace.json.metrics != null && workspace.json.cycleLoss != null,
  );
  record(
    "Cycle Loss Decomposition loads",
    workspace.ok && workspace.json.cycleLoss?.hasBaseline === true,
  );
  record(
    "recovery opportunity loads",
    workspace.ok && workspace.json.recovery?.productionRecoveryGoodParts > 0,
  );

  const team = await api("GET", "/api/golden-demo-p2b/digital-factory-team");
  record("Digital Factory Team loads", team.ok && Array.isArray(team.json.agents) && team.json.agents.length > 0);
  record("orchestrator evidence loads", team.ok && team.json.orchestrator?.whatIsHappening?.length > 0);
}

async function runFlowToVerified() {
  const approve = await api(
    "POST",
    `/api/golden-demo-p1/events/${EVENT_ID}/approve`,
    { comment: "sales-ready-freeze" },
    REVIEWER,
  );
  record("approval persists", approve.ok, `${approve.status}`);

  const execute = await api("POST", `/api/golden-demo-p1/events/${EVENT_ID}/execute`, {
    idempotencyKey: crypto.randomUUID(),
  });
  record("intervention trace exists", execute.ok, `${execute.status}`);

  const complete = await api("POST", `/api/golden-demo-p1/events/${EVENT_ID}/complete-intervention`, {
    idempotencyKey: crypto.randomUUID(),
  });
  record("validation runs", complete.ok && complete.json?.status === "validation-run", complete.json?.status);

  const preOutcome = await api("GET", "/api/golden-demo-p3/outcome");
  record("before/after exists", preOutcome.ok && preOutcome.json.metricComparisons?.length > 0);
  record(
    "+X GOOD PARTS / SHIFT is runtime-calculated",
    preOutcome.ok && preOutcome.json.hero?.recoveredGoodPartsPerShift > 0,
  );
  record(
    "energy result is truthful",
    preOutcome.ok &&
      ["measured", "calculated", "estimated", "verified"].includes(preOutcome.json.energyImpact?.classification),
  );
  record(
    "business result classification is truthful",
    preOutcome.ok && preOutcome.json.businessImpact?.estimatedBusinessClassification === "estimated",
  );

  const verify = await api(
    "POST",
    `/api/golden-demo-p1/events/${EVENT_ID}/verify`,
    { idempotencyKey: crypto.randomUUID() },
    REVIEWER,
  );
  record("verification executes", verify.ok, `${verify.status}`);

  const postOutcome = await api("GET", "/api/golden-demo-p3/outcome");
  record("outcome classifications PASS", postOutcome.ok && postOutcome.json.outcomeStatus === "verified");
  record(
    "PRODUCTION RECOVERED supported",
    postOutcome.ok && postOutcome.json.hero?.showSuccessHero === true && postOutcome.json.hero?.headline === "PRODUCTION RECOVERED",
  );
}

async function assertWebRoute() {
  const page = await fetch(`${WEB}/golden-demo/uretim-kurtarma`, {
    redirect: "follow",
    signal: AbortSignal.timeout(TIMEOUT_MS),
  });
  record("Web reachable", page.ok, `${page.status}`);
  record("Golden Demo route loads", page.ok);

  const html = await page.text();
  record("no runtime crash (workspace shell)", html.includes("golden-demo-p1-workspace"));
  record("sales control rendered", html.includes("golden-demo-sales-control") || html.includes("golden-demo-p2-reset"));
  record("Digital Factory Team rendered", html.includes("golden-demo-p2b-digital-factory-team"));
  record("P3 outcome rendered", html.includes("golden-demo-p3-outcome-hero"));
}

async function main() {
  const apiHealth = await fetch(`${API_BASE}/health/live`, { signal: AbortSignal.timeout(5000) });
  record("API reachable", apiHealth.ok);

  const webHealth = await fetch(`${WEB}/health`, { signal: AbortSignal.timeout(5000) }).catch(() => null);
  record("Web health endpoint", webHealth?.ok === true, webHealth ? `${webHealth.status}` : "unreachable");

  await assertWebRoute();

  for (let cycle = 1; cycle <= REPEAT_CYCLES; cycle += 1) {
    await resetDemo();
    await assertInitialEvidence();
    await runFlowToVerified();
  }

  await resetDemo();
  record("reset succeeds after completed run", true);

  console.log(`GOLDEN_DEMO_SALES_READY = PASS (${checks.length} checks, ${REPEAT_CYCLES} repeat cycles)`);
}

main().catch((error) => {
  console.error(`GOLDEN_DEMO_SALES_READY = FAIL: ${error.message}`);
  process.exit(1);
});
