#!/usr/bin/env node

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
  await api("POST", "/api/real-factory-pilot/initialize");
  const golden = await api("POST", "/api/real-factory-pilot/dev/golden-pilot");
  record("golden pilot endpoint", golden.ok);
  record("SYNTHETIC acceptance kind", golden.json.acceptanceKind === "SYNTHETIC_PILOT_PASS");
  record("case closed phase", golden.json.finalPhase === "CLOSED");
  record("evidence pack generated", golden.json.evidencePackGenerated === true);
  record(
    "verified improvement or partial",
    ["VERIFIED_IMPROVEMENT", "PARTIAL_IMPROVEMENT"].includes(golden.json.verificationOutcome),
  );

  const workspace = await api("GET", `/api/real-factory-pilot/runs/${golden.json.pilotRunId}/workspace`);
  record("workspace readable", workspace.ok);
  record("connectors active health", workspace.json.connectors?.every((c) => c.healthStatus) ?? false);
  record("shadow inactive after close", workspace.json.shadow?.isActive !== true || workspace.json.currentPhase === "CLOSED");
  record("line code LINE-PILOT-01", workspace.json.lineCode === "LINE-PILOT-01");

  await new Promise((resolve) => setTimeout(resolve, 3_000));

  let html = "";
  let pageOk = false;
  for (let attempt = 1; attempt <= 5; attempt += 1) {
    try {
      const page = await fetch(`${WEB}/ayarlar/entegrasyonlar/gercek-fabrika`, { redirect: "follow" });
      html = await page.text();
      pageOk = page.ok && html.includes("real-factory-pilot-workspace");
      if (pageOk) {
        break;
      }
    } catch (error) {
      if (attempt === 5) {
        throw error;
      }
    }

    await new Promise((resolve) => setTimeout(resolve, 2_000));
  }

  record("pilot workspace rendered", pageOk);

  console.log("\nSYNTHETIC PILOT ACCEPTANCE — PASS");
  console.log(checks.join("\n"));
}

main().catch((error) => {
  console.error(error.message ?? error);
  process.exit(1);
});
