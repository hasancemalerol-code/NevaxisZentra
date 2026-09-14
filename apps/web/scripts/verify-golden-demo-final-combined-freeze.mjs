#!/usr/bin/env node

/**
 * Final combined sales-ready freeze verifier.
 * Validates production + customs Golden Demos independently with 3 repeat cycles.
 * Requires localhost API + Web already running.
 */

const API_BASE = process.env.NEVAXIS_FACTOS_API_BASE_URL ?? "http://127.0.0.1:5000";
const WEB = process.env.NEVAXIS_FACTOS_WEB_BASE_URL ?? "http://localhost:3001";
const FACILITY_ID = "bb000001-0005-4000-8000-000000000005";
const PRODUCTION_EVENT_ID = "bb000001-0010-4000-8000-000000000001";
const CUSTOMS_OUTCOME_ID = "cc000001-0010-4000-8000-000000000001";
const ISSUER = "crevafactos-dev";
const OPERATOR = "production-pilot-operator";
const REVIEWER = "planning-pilot-reviewer";
const UNAUTHORIZED = "unauthorized-demo-subject";
const TENANT = "dev-pilot-tenant";
const REPEAT_CYCLES = Number(process.env.GOLDEN_DEMO_COMBINED_REPEAT_CYCLES ?? "3");
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
    "X-NevaxisFactOS-Tenant": TENANT,
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

async function resetProduction() {
  const reset = await api("POST", `/api/golden-demo-p2/events/${PRODUCTION_EVENT_ID}/reset`);
  record("production reset", reset.ok && reset.json.resetStatus === "active", JSON.stringify(reset.json));
}

async function resetCustoms() {
  const reset = await api(
    "POST",
    `/api/golden-demo-customs-file/outcomes/${CUSTOMS_OUTCOME_ID}/reset`,
    null,
    OPERATOR,
  );
  record("customs reset", reset.ok, `${reset.status}`);
}

async function assertProductionStartState() {
  const status = await api("GET", "/api/golden-demo-p2/presentation-status");
  record("production start state active", status.ok && status.json.eventStatus === "active", status.json?.eventStatus);

  const workspace = await api("GET", "/api/golden-demo-p1/workspace");
  record("production evidence loads", workspace.ok && workspace.json.recovery?.productionRecoveryGoodParts > 0);
}

async function assertCustomsStartState() {
  const workspace = await api("GET", "/api/golden-demo-customs-file/workspace");
  record("customs workspace loads", workspace.ok);
  record("customs worker reused", workspace.json.workerCode === "DW-EXP-001", workspace.json.workerCode);
  record(
    "customs no stale package after reset",
    !workspace.json.package,
    workspace.json.package ? "package leaked" : "",
  );
  record(
    "customs no stale verification after reset",
    !workspace.json.verification || workspace.json.verification.result !== "verified-ready",
    workspace.json.verification?.result,
  );
}

async function completeProductionFlow() {
  const approve = await api(
    "POST",
    `/api/golden-demo-p1/events/${PRODUCTION_EVENT_ID}/approve`,
    { comment: "combined-freeze" },
    REVIEWER,
  );
  record("production approval", approve.ok, `${approve.status}`);

  const execute = await api("POST", `/api/golden-demo-p1/events/${PRODUCTION_EVENT_ID}/execute`, {
    idempotencyKey: crypto.randomUUID(),
  });
  record("production intervention", execute.ok, `${execute.status}`);

  const complete = await api("POST", `/api/golden-demo-p1/events/${PRODUCTION_EVENT_ID}/complete-intervention`, {
    idempotencyKey: crypto.randomUUID(),
  });
  record("production validation run", complete.ok && complete.json?.status === "validation-run", complete.json?.status);

  const verify = await api(
    "POST",
    `/api/golden-demo-p1/events/${PRODUCTION_EVENT_ID}/verify`,
    { idempotencyKey: crypto.randomUUID() },
    REVIEWER,
  );
  record("production verification", verify.ok, `${verify.status}`);

  const outcome = await api("GET", "/api/golden-demo-p3/outcome");
  record("production outcome verified", outcome.ok && outcome.json.outcomeStatus === "verified");
  record(
    "PRODUCTION RECOVERED hero",
    outcome.ok && outcome.json.hero?.showSuccessHero === true && outcome.json.hero?.headline === "PRODUCTION RECOVERED",
  );
  record(
    "+X GOOD PARTS / SHIFT runtime",
    outcome.ok && outcome.json.hero?.recoveredGoodPartsPerShift > 0,
  );
}

async function completeCustomsFlow() {
  await api("POST", `/api/golden-demo-customs-file/outcomes/${CUSTOMS_OUTCOME_ID}/assign`, {}, OPERATOR);
  let workspace = await api(
    "POST",
    `/api/golden-demo-customs-file/outcomes/${CUSTOMS_OUTCOME_ID}/evaluate`,
    null,
    OPERATOR,
  );
  record("customs evaluate", workspace.ok, `${workspace.status}`);

  const mismatchCheck = workspace.json.consistencyChecks.find((c) => c.checkCode === "net-weight-consistency");
  record("customs mismatch computed", mismatchCheck?.status === "fail", mismatchCheck?.status);
  record("customs blocker created", workspace.json.blockers.length > 0);
  record("customs corrective work created", workspace.json.correctiveWorks.length > 0);

  const workId = workspace.json.correctiveWorks[0].id;
  const afterComplete = await api(
    "POST",
    `/api/golden-demo-customs-file/outcomes/${CUSTOMS_OUTCOME_ID}/corrective-work/${workId}/complete`,
    { idempotencyKey: crypto.randomUUID() },
    OPERATOR,
  );
  record("customs corrective complete", afterComplete.ok);
  record(
    "customs blocker not cleared by task alone",
    !afterComplete.json.blockers.some((b) => b.status === "resolved"),
  );

  const packingVersions = afterComplete.json.documents.filter((d) => d.documentType === "packing-list");
  record("customs packing list versioned", packingVersions.length >= 2, `${packingVersions.length}`);

  workspace = await api("POST", `/api/golden-demo-customs-file/outcomes/${CUSTOMS_OUTCOME_ID}/recheck`, null, OPERATOR);
  record("customs evidence recheck", workspace.ok);
  record("customs blocker resolved after recheck", !workspace.json.blockers.some((b) => b.status === "open"));

  workspace = await api(
    "POST",
    `/api/golden-demo-customs-file/outcomes/${CUSTOMS_OUTCOME_ID}/approve`,
    {},
    REVIEWER,
  );
  record("customs approval", workspace.ok && workspace.json.approval?.isApproved === true);

  workspace = await api(
    "POST",
    `/api/golden-demo-customs-file/outcomes/${CUSTOMS_OUTCOME_ID}/assemble`,
    null,
    OPERATOR,
  );
  record("customs package assembled", workspace.ok && workspace.json.package != null);
  record(
    "customs manifest references current docs",
    workspace.json.package?.documentReferenceIds?.length > 0,
  );

  workspace = await api(
    "POST",
    `/api/golden-demo-customs-file/outcomes/${CUSTOMS_OUTCOME_ID}/verify`,
    { idempotencyKey: crypto.randomUUID() },
    REVIEWER,
  );
  record("customs verification", workspace.ok);
  record("customs VERIFIED READY", workspace.json.verification?.result === "verified-ready");
  record("customs success hero", workspace.json.hero?.showSuccessHero === true);
  record("customs broker handoff ready", workspace.json.hero?.brokerHandoffStatus === "READY");
}

async function assertNoCrossContamination(productionExpected, customsExpected) {
  const production = await api("GET", "/api/golden-demo-p3/outcome");
  const customs = await api("GET", "/api/golden-demo-customs-file/workspace");

  if (productionExpected === "verified") {
    record("production unaffected by customs reset", production.ok && production.json.outcomeStatus === "verified");
  } else if (productionExpected === "active") {
    const status = await api("GET", "/api/golden-demo-p2/presentation-status");
    record("production reset isolated", status.ok && status.json.eventStatus === "active");
  }

  if (customsExpected === "verified-ready") {
    record(
      "customs unaffected by production reset",
      customs.ok && customs.json.verification?.result === "verified-ready",
    );
  } else if (customsExpected === "baseline") {
    record("customs reset isolated", customs.ok && !customs.json.package);
  }
}

async function assertSecurityChecks() {
  const unauthorizedApprove = await api(
    "POST",
    `/api/golden-demo-customs-file/outcomes/${CUSTOMS_OUTCOME_ID}/approve`,
    {},
    UNAUTHORIZED,
  );
  record("customs unauthorized approval blocked", !unauthorizedApprove.ok, `${unauthorizedApprove.status}`);

  const missingTenant = await fetch(
    `${API_BASE}/api/golden-demo-customs-file/workspace?facilityId=${FACILITY_ID}`,
    {
      headers: {
        Accept: "application/json",
        "X-crevafactos-dev-Issuer": ISSUER,
        "X-crevafactos-dev-Subject": OPERATOR,
      },
      signal: AbortSignal.timeout(TIMEOUT_MS),
    },
  );
  record("tenant header required", missingTenant.status === 401 || missingTenant.status === 403, `${missingTenant.status}`);
}

async function assertWebRoutes() {
  const productionPage = await fetch(`${WEB}/golden-demo/uretim-kurtarma`, {
    redirect: "follow",
    signal: AbortSignal.timeout(TIMEOUT_MS),
  });
  record("production route loads", productionPage.ok, `${productionPage.status}`);
  const productionHtml = await productionPage.text();
  record("production workspace rendered", productionHtml.includes("golden-demo-p1-workspace"));

  const customsPage = await fetch(`${WEB}/golden-demo/gumruk-dosyasi`, {
    redirect: "follow",
    signal: AbortSignal.timeout(TIMEOUT_MS),
  });
  record("customs route loads", customsPage.ok, `${customsPage.status}`);
  const customsHtml = await customsPage.text();
  record("customs workspace rendered", customsHtml.includes("golden-demo-customs-file-workspace"));
}

async function runCombinedCycle(cycle) {
  record(`cycle ${cycle} start`, true);

  await resetProduction();
  await resetCustoms();
  await assertProductionStartState();
  await assertCustomsStartState();

  await completeProductionFlow();
  await assertCustomsStartState();
  record(`cycle ${cycle} production did not contaminate customs baseline`, true);

  await completeCustomsFlow();
  await assertNoCrossContamination("verified", "verified-ready");

  await resetProduction();
  await assertNoCrossContamination("active", "verified-ready");

  await resetCustoms();
  await assertNoCrossContamination("active", "baseline");
}

async function main() {
  const apiHealth = await fetch(`${API_BASE}/health/live`, { signal: AbortSignal.timeout(5000) });
  record("API health", apiHealth.ok);

  const webHealth = await fetch(`${WEB}/health`, { signal: AbortSignal.timeout(5000) }).catch(() => null);
  record("Web health", webHealth?.ok === true, webHealth ? `${webHealth.status}` : "unreachable");

  await assertWebRoutes();
  await assertSecurityChecks();

  for (let cycle = 1; cycle <= REPEAT_CYCLES; cycle += 1) {
    await runCombinedCycle(cycle);
  }

  console.log(
    `GOLDEN_DEMO_FINAL_COMBINED_FREEZE = PASS (${checks.length} checks, ${REPEAT_CYCLES} combined cycles)`,
  );
}

main().catch((error) => {
  console.error(`GOLDEN_DEMO_FINAL_COMBINED_FREEZE = FAIL: ${error.message}`);
  process.exit(1);
});
