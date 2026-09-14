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

  const beforePage = await fetch(`${WEB}/golden-demo/uretim-kurtarma`, { redirect: "follow" });
  const beforeHtml = await beforePage.text();
  if (!beforeHtml.includes("golden-demo-p2b-digital-factory-team")) {
    throw new Error("P2B digital factory team panel not rendered");
  }
  if (!beforeHtml.includes("golden-demo-p2b-orchestrator")) throw new Error("orchestrator panel missing");
  if (!beforeHtml.includes("golden-demo-p2b-approval-sheet")) throw new Error("approval sheet missing");

  const teamBefore = await api("GET", "/api/golden-demo-p2b/digital-factory-team");
  if (!teamBefore.ok) throw new Error("digital-factory-team failed");
  if (teamBefore.json.approvalSheet.isApproved) throw new Error("expected pending approval after reset");

  const approve = await api("POST", `/api/golden-demo-p1/events/${EVENT_ID}/approve`, { comment: "Approved for demo" }, REVIEWER);
  if (!approve.ok) throw new Error(`approve failed ${approve.status}`);

  const teamAfter = await api("GET", "/api/golden-demo-p2b/digital-factory-team");
  if (!teamAfter.ok) throw new Error("post-approve team failed");
  if (!teamAfter.json.approvalSheet.isApproved) throw new Error("approval must persist");
  if (!teamAfter.json.approvalSheet.approvedBySubjectId) throw new Error("approvedBy missing");

  const afterPage = await fetch(`${WEB}/golden-demo/uretim-kurtarma`, { redirect: "follow" });
  const afterHtml = await afterPage.text();
  if (!afterHtml.includes("golden-demo-p2b-approval-record") && !afterHtml.includes("Approval Record")) {
    throw new Error("approved state not rendered after refresh");
  }

  console.log("GOLDEN_DEMO_P2B_LIVE_CLOSURE = PASS");
}

main().catch((error) => {
  console.error(`GOLDEN_DEMO_P2B_LIVE_CLOSURE = FAIL: ${error.message}`);
  process.exit(1);
});
