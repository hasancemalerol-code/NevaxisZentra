#!/usr/bin/env node

const API_BASE = process.env.NEVAXIS_FACTOS_API_BASE_URL ?? "http://127.0.0.1:5000";
const WEB = process.env.NEVAXIS_FACTOS_WEB_BASE_URL ?? "http://localhost:3001";
const FACILITY_ID = "bb000001-0005-4000-8000-000000000005";
const OUTCOME_ID = "cc000001-0010-4000-8000-000000000001";
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

async function runScenario(label) {
  await api("POST", `/api/golden-demo-customs-file/outcomes/${OUTCOME_ID}/reset`, null, OPERATOR);

  let workspace = await api("GET", "/api/golden-demo-customs-file/workspace");
  if (!workspace.ok) throw new Error(`${label}: workspace failed`);
  if (workspace.json.workerCode !== "DW-EXP-001") throw new Error(`${label}: worker architecture not reused`);

  await api("POST", `/api/golden-demo-customs-file/outcomes/${OUTCOME_ID}/assign`, {}, OPERATOR);
  workspace = await api("POST", `/api/golden-demo-customs-file/outcomes/${OUTCOME_ID}/evaluate`, null, OPERATOR);
  if (!workspace.ok) throw new Error(`${label}: evaluate failed`);

  const mismatchCheck = workspace.json.consistencyChecks.find((c) => c.checkCode === "net-weight-consistency");
  if (!mismatchCheck || mismatchCheck.status !== "fail") {
    throw new Error(`${label}: net-weight mismatch not computed`);
  }
  if (workspace.json.blockers.length === 0) throw new Error(`${label}: blocker not created`);
  if (workspace.json.correctiveWorks.length === 0) throw new Error(`${label}: corrective work not created`);

  const workId = workspace.json.correctiveWorks[0].id;
  const afterComplete = await api(
    "POST",
    `/api/golden-demo-customs-file/outcomes/${OUTCOME_ID}/corrective-work/${workId}/complete`,
    { idempotencyKey: crypto.randomUUID() },
    OPERATOR,
  );
  if (!afterComplete.ok) throw new Error(`${label}: complete corrective work failed`);
  if (afterComplete.json.blockers.some((b) => b.status === "resolved")) {
    throw new Error(`${label}: completed work alone must not clear blocker`);
  }

  const packingVersions = afterComplete.json.documents.filter((d) => d.documentType === "packing-list");
  if (packingVersions.length < 2) throw new Error(`${label}: document version not updated`);

  const afterRecheck = await api("POST", `/api/golden-demo-customs-file/outcomes/${OUTCOME_ID}/recheck`, null, OPERATOR);
  if (!afterRecheck.ok) throw new Error(`${label}: recheck failed`);
  if (afterRecheck.json.blockers.some((b) => b.status === "open")) {
    throw new Error(`${label}: blocker should resolve after corrected evidence`);
  }

  const afterApprove = await api(
    "POST",
    `/api/golden-demo-customs-file/outcomes/${OUTCOME_ID}/approve`,
    {},
    REVIEWER,
  );
  if (!afterApprove.ok) throw new Error(`${label}: approval failed`);
  if (!afterApprove.json.approval.isApproved) throw new Error(`${label}: approval not persisted`);

  const afterAssemble = await api("POST", `/api/golden-demo-customs-file/outcomes/${OUTCOME_ID}/assemble`, null, OPERATOR);
  if (!afterAssemble.ok) throw new Error(`${label}: assemble failed`);
  if (!afterAssemble.json.package) throw new Error(`${label}: package not assembled`);

  const afterVerify = await api(
    "POST",
    `/api/golden-demo-customs-file/outcomes/${OUTCOME_ID}/verify`,
    { idempotencyKey: crypto.randomUUID() },
    REVIEWER,
  );
  if (!afterVerify.ok) throw new Error(`${label}: verify failed`);
  if (afterVerify.json.verification.result !== "verified-ready") {
    throw new Error(`${label}: expected verified-ready, got ${afterVerify.json.verification.result}`);
  }
  if (!afterVerify.json.hero.showSuccessHero) throw new Error(`${label}: success hero missing`);
  if (afterVerify.json.hero.documentsReadyCount <= 0) {
    throw new Error(`${label}: document count must be runtime-derived`);
  }
}

async function main() {
  const health = await fetch(`${API_BASE}/health/live`, { signal: AbortSignal.timeout(5000) });
  if (!health.ok) throw new Error("API not reachable");

  const page = await fetch(`${WEB}/golden-demo/gumruk-dosyasi`, { redirect: "follow" });
  if (!page.ok) throw new Error("Golden Demo route failed to load");
  const html = await page.text();
  if (!html.includes("golden-demo-customs-file-workspace")) throw new Error("customs workspace not rendered");

  await runScenario("cycle-1");
  await runScenario("cycle-2");

  const p1 = await api("GET", "/api/golden-demo-p1/workspace");
  if (!p1.ok) throw new Error("production golden demo regression: P1 workspace failed");

  console.log("GOLDEN_DEMO_CUSTOMS_FILE_RUNTIME = PASS");
}

main().catch((error) => {
  console.error(`GOLDEN_DEMO_CUSTOMS_FILE_RUNTIME = FAIL: ${error.message}`);
  process.exit(1);
});
