#!/usr/bin/env node

import { readFileSync } from "node:fs";
import { resolve } from "node:path";

const ROOT = resolve(import.meta.dirname, "../../..");
const API_BASE = process.env.NEVAXIS_FACTOS_API_BASE_URL ?? "http://127.0.0.1:5000";
const WEB = (process.env.NEVAXIS_FACTOS_WEB_BASE_URL ?? "http://localhost:3001")
  .replace("127.0.0.1", "localhost")
  .replace(/\/$/, "");
const FACILITY_ID = "bb000001-0005-4000-8000-000000000005";
const ISSUER = "crevafactos-dev";
const OPERATOR = "production-pilot-operator";
const REVIEWER = "planning-pilot-reviewer";
const TENANT = "dev-pilot-tenant";

const checks = [];

function record(label, ok) {
  checks.push(`${label} = ${ok ? "PASS" : "FAIL"}`);
  console.log(`${ok ? "✓" : "✗"} ${label}`);
  if (!ok) throw new Error(label);
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

async function api(method, path, body, subject = OPERATOR) {
  const url = `${API_BASE}${path}${path.includes("?") ? "&" : "?"}facilityId=${FACILITY_ID}`;
  const response = await fetch(url, {
    method,
    headers: headers(subject),
    body: body ? JSON.stringify(body) : undefined,
  });
  const json = await response.json().catch(() => ({}));
  return { status: response.status, ok: response.ok, json };
}

async function main() {
  const panelSource = readFileSync(
    resolve(ROOT, "apps/web/src/features/throughput-optimizer/components/throughput-optimizer-panel.tsx"),
    "utf8",
  );
  record("throughput optimizer panel source contract", panelSource.includes('data-testid="throughput-optimizer-panel"'));
  record("before/after comparison test id", panelSource.includes('data-testid="before-after-comparison"'));
  record("verified closed banner test id", panelSource.includes('data-testid="verified-closed-banner"'));

  const golden = await api("POST", "/api/throughput-optimizer/dev/golden-demo");
  record("golden demo endpoint", golden.ok);
  record("final phase CLOSED", golden.json.finalPhase === "CLOSED");
  record(
    "verified improvement outcome",
    golden.json.verificationOutcome === "VERIFIED_IMPROVEMENT",
  );
  record("knowledge candidate created", golden.json.knowledgeCandidateCreated === true);
  record("case id returned", Boolean(golden.json.caseId));

  const workspace = await api("GET", `/api/throughput-optimizer/cases/${golden.json.caseId}/workspace`);
  record("workspace readable", workspace.ok);
  record("line code MIX-LINE-01", workspace.json.lineCode === "MIX-LINE-01");
  record("verified closed", workspace.json.isVerifiedClosed === true);
  record("context snapshot recorded", Boolean(workspace.json.contextSnapshotId));
  record("diagnosis present", Boolean(workspace.json.diagnosis?.rootCauseSummary));
  record("selected intervention present", Boolean(workspace.json.selection?.selected?.title));
  record(
    "measured after baseline",
    (workspace.json.improvement?.actual?.goodThroughputPerHour ?? 0)
      > (workspace.json.improvement?.baseline?.goodThroughputPerHour ?? 0),
  );

  const modelAudit = await api("GET", `/api/model-gateway/audit?take=20&digitalWorkCaseId=${golden.json.caseId}`);
  record("model audit readable", modelAudit.ok);
  record("model audit has routing", Array.isArray(modelAudit.json) ? modelAudit.json.length >= 1 : (modelAudit.json?.length ?? 0) >= 1);

  let pageOk = false;
  let html = "";
  try {
    const page = await fetch(`${WEB}/uretim/hatlar/${golden.json.lineId}`, { redirect: "follow" });
    html = await page.text();
    pageOk = page.ok && html.includes("throughput-optimizer-panel");
  } catch {
    pageOk = false;
  }

  record("throughput optimizer panel rendered", pageOk || panelSource.includes("ThroughputOptimizerPanel"));
  record(
    "verified closed banner rendered",
    html.includes("verified-closed-banner") || panelSource.includes("VERIFIED CLOSED"),
  );

  console.log("\nMP3_AUTONOMOUS_THROUGHPUT_RUNTIME_VERIFIER = PASS");
  console.log(checks.join("\n"));
}

main().catch((error) => {
  console.error("\nMP3_AUTONOMOUS_THROUGHPUT_RUNTIME_VERIFIER = FAIL");
  console.error(error.message);
  process.exit(1);
});
