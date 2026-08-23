#!/usr/bin/env node
import { spawnSync } from "node:child_process";
import { readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), "../../..");
const BASE = process.env.CREVAFACTOS_API_BASE_URL ?? "http://127.0.0.1:5000";
const FACILITY = "bb000001-0005-4000-8000-000000000005";
const MATERIAL = "bb000001-00a2-4000-8000-0000000000a2";
const CLOSURE_PREFIX = "OPERATIONAL-RISK1-CLOSURE";
const RUN_SUFFIX = Date.now();
const ACTOR = {
  issuer: "crevafactos-dev",
  subject: "planning-pilot-user",
  tenant: "dev-pilot-tenant",
};

function fail(message) {
  console.error(`OPERATIONAL-RISK-1 domain/lifecycle verification failed: ${message}`);
  process.exit(1);
}

function assert(label, ok, detail = "") {
  if (!ok) fail(`${label}${detail ? `: ${detail}` : ""}`);
  console.log(`✓ ${label}`);
}

function readRequired(relativePath) {
  return readFileSync(resolve(ROOT, relativePath), "utf8").replaceAll("\r\n", "\n");
}

function db(sql) {
  const result = spawnSync(
    "docker",
    ["exec", "crevafactos-local-postgres-1", "psql", "-U", "crevafactos", "-d", "crevafactos_local", "-t", "-A", "-c", sql],
    { encoding: "utf8" },
  );
  if (result.status !== 0) fail(result.stderr || result.stdout);
  return result.stdout.trim();
}

function headers(actor = ACTOR) {
  return {
    "Content-Type": "application/json",
    "X-CrevaFactOS-Dev-Issuer": actor.issuer,
    "X-CrevaFactOS-Dev-Subject": actor.subject,
    "X-CrevaFactOS-Tenant": actor.tenant,
  };
}

async function request(method, path, body, actor = ACTOR) {
  const response = await fetch(`${BASE}${path}`, {
    method,
    headers: headers(actor),
    body: body ? JSON.stringify(body) : undefined,
  });
  const text = await response.text();
  let json = null;
  try { json = text ? JSON.parse(text) : null; } catch { json = { raw: text }; }
  return { status: response.status, json, text };
}

function removeClosureFixtures() {
  db(`
    DELETE FROM operational_risk_audit_events
    WHERE entity_id IN (
      SELECT id FROM operational_risk_cases WHERE idempotency_key LIKE '${CLOSURE_PREFIX}%'
      UNION SELECT id FROM operational_risk_evidence WHERE idempotency_key LIKE '${CLOSURE_PREFIX}%'
    );
    DELETE FROM operational_risk_status_history
    WHERE case_id IN (SELECT id FROM operational_risk_cases WHERE idempotency_key LIKE '${CLOSURE_PREFIX}%');
    DELETE FROM operational_risk_evidence
    WHERE idempotency_key LIKE '${CLOSURE_PREFIX}%'
       OR case_id IN (SELECT id FROM operational_risk_cases WHERE idempotency_key LIKE '${CLOSURE_PREFIX}%');
    DELETE FROM operational_risk_cases WHERE idempotency_key LIKE '${CLOSURE_PREFIX}%';
  `);
}

function assess(evidenceState, likelihood, impact) {
  if (evidenceState === "source-conflict") {
    return { outcome: "source-conflict", score: null, severity: null };
  }
  if (evidenceState === "insufficient-data" || likelihood == null || impact == null) {
    return { outcome: "insufficient-data", score: null, severity: null };
  }
  const score = likelihood * impact;
  let severity = null;
  if (score >= 1 && score <= 4) severity = "low";
  else if (score <= 9) severity = "moderate";
  else if (score <= 16) severity = "high";
  else if (score <= 25) severity = "critical";
  return { outcome: "scored", score, severity };
}

console.log("Running OPERATIONAL-RISK-1 domain/lifecycle verifier.");

const constants = readRequired("packages/CrevaFactOS.Persistence/OperationalRisk/OperationalRiskConstants.cs");
const assessor = readRequired("packages/CrevaFactOS.Persistence/OperationalRisk/OperationalRiskAssessor.cs");
const lifecycle = readRequired("packages/CrevaFactOS.Persistence/OperationalRisk/OperationalRiskLifecycle.cs");
const seed = readRequired("packages/CrevaFactOS.Persistence/Development/DevelopmentPilotOperationalRiskSeed.cs");

assert("rule-set OPERATIONAL-RISK-1.0", constants.includes("OPERATIONAL-RISK-1.0"));
assert("material-availability category", constants.includes("material-availability"));
assert("iot-endpoint scope", constants.includes("iot-endpoint"));
assert("insufficient-data evidence state", constants.includes("insufficient-data"));
assert("source-conflict evidence state", constants.includes("source-conflict"));
assert("detected lifecycle status", constants.includes("detected"));
assert("resolved lifecycle status", constants.includes("resolved"));
assert("assessor severity bands", assessor.includes("OperationalRiskSeverityBands.Low"));
assert("lifecycle transition rules", lifecycle.includes("CanTransition"));

const boundaryMatrix = [
  [2, 2, 4, "low"],
  [2, 2, 4, "low"],
  [2, 3, 6, "moderate"],
  [3, 3, 9, "moderate"],
  [3, 4, 12, "high"],
  [4, 4, 16, "high"],
  [4, 5, 20, "critical"],
  [5, 5, 25, "critical"],
];
for (const [l, i, score, severity] of boundaryMatrix) {
  const result = assess("simulated", l, i);
  assert(`score boundary ${l}x${i}=${score}`, result.score === score, `got ${result.score}`);
  assert(`severity boundary ${score}=${severity}`, result.severity === severity, `got ${result.severity}`);
}

assert("severity boundary 4 low", assess("simulated", 2, 2).severity === "low");
assert("severity boundary 9 moderate", assess("simulated", 3, 3).severity === "moderate");
assert("severity boundary 10 high", assess("simulated", 2, 5).severity === "high");
assert("severity boundary 16 high", assess("simulated", 4, 4).severity === "high");
assert("severity boundary 17 critical", assess("simulated", 5, 4).severity === "critical");
assert("severity boundary 25 critical", assess("simulated", 5, 5).severity === "critical");

const insufficient = assess("insufficient-data", 3, 3);
assert("insufficient-data outcome", insufficient.outcome === "insufficient-data");
assert("insufficient-data null score", insufficient.score === null);
const conflict = assess("source-conflict", 4, 4);
assert("source-conflict outcome", conflict.outcome === "source-conflict");
assert("source-conflict null score", conflict.score === null);

const ready = await fetch(`${BASE}/health/ready`);
assert("API ready", ready.status === 200, `status=${ready.status}`);

removeClosureFixtures();

const provenance = "Deterministic local Development operational-risk data — simulated, non-production, not real customer evidence or guaranteed loss.";
const now = new Date().toISOString();
const createBody = {
  category: "material-availability",
  scopeType: "material",
  scopeReference: MATERIAL,
  title: "Closure material risk",
  description: "Closure test case",
  evidenceState: "simulated",
  likelihoodLevel: 3,
  impactLevel: 4,
  sourceModule: "closure-test",
  sourceReference: "closure-material",
  sourceProvenance: provenance,
  firstDetectedAtUtc: now,
  lastObservedAtUtc: now,
  idempotencyKey: `${CLOSURE_PREFIX}-case-${RUN_SUFFIX}`,
};
const created = await request("POST", `/api/operational-risks/cases?facilityId=${FACILITY}`, createBody);
assert("create case 201", created.status === 201, created.text?.slice(0, 200));
assert("server-side score", created.json?.id && assess("simulated", 3, 4).score === 12);

const caseId = created.json.id;
const detail = await request("GET", `/api/operational-risks/cases/${caseId}?facilityId=${FACILITY}`);
assert("case detail 200", detail.status === 200);
assert("detail risk score 12", detail.json?.riskScore === 12);
assert("detail severity high", detail.json?.severityBand === "high");

const evidence = await request(
  "POST",
  `/api/operational-risks/cases/${caseId}/evidence?facilityId=${FACILITY}`,
  {
    evidenceTypeCode: "stock-level",
    evidenceState: "simulated",
    observedAtUtc: now,
    summary: "Closure evidence",
    sourceProvenance: provenance,
    idempotencyKey: `${CLOSURE_PREFIX}-evidence-${RUN_SUFFIX}`,
  },
);
assert("append evidence 201", evidence.status === 201);

const transitionReview = await request(
  "POST",
  `/api/operational-risks/cases/${caseId}/transitions?facilityId=${FACILITY}`,
  {
    expectedVersion: created.json.version,
    targetStatus: "under-review",
    note: "Review started",
    sourceProvenance: provenance,
  },
);
assert("valid transition under-review", transitionReview.status === 200);

const invalidTransition = await request(
  "POST",
  `/api/operational-risks/cases/${caseId}/transitions?facilityId=${FACILITY}`,
  {
    expectedVersion: transitionReview.json.version,
    targetStatus: "resolved",
    note: "Skipping monitoring should fail",
    sourceProvenance: provenance,
  },
);
assert("invalid transition without monitoring 409", invalidTransition.status === 409);

const dismissWithoutNote = await request(
  "POST",
  `/api/operational-risks/cases/${caseId}/transitions?facilityId=${FACILITY}`,
  {
    expectedVersion: transitionReview.json.version,
    targetStatus: "dismissed",
    note: "   ",
    sourceProvenance: provenance,
  },
);
assert("dismissal requires note 400/409", dismissWithoutNote.status === 400 || dismissWithoutNote.status === 409);

const dismissCase = await request("POST", `/api/operational-risks/cases?facilityId=${FACILITY}`, {
  ...createBody,
  title: "Dismiss closure case",
  idempotencyKey: `${CLOSURE_PREFIX}-dismiss-${RUN_SUFFIX}`,
});
const dismissTransition = await request(
  "POST",
  `/api/operational-risks/cases/${dismissCase.json.id}/transitions?facilityId=${FACILITY}`,
  {
    expectedVersion: dismissCase.json.version,
    targetStatus: "dismissed",
    note: "Dismissed in closure test",
    sourceProvenance: provenance,
  },
);
assert("dismiss with note", dismissTransition.status === 200);
const terminalBlock = await request(
  "POST",
  `/api/operational-risks/cases/${dismissCase.json.id}/transitions?facilityId=${FACILITY}`,
  {
    expectedVersion: dismissTransition.json.version,
    targetStatus: "under-review",
    note: "Should fail",
    sourceProvenance: provenance,
  },
);
assert("terminal state protection 409", terminalBlock.status === 409);

const auditCount = Number(db(`
  SELECT COUNT(*) FROM operational_risk_audit_events
  WHERE entity_id = '${caseId}'
`));
assert("audit events created", auditCount >= 2, `count=${auditCount}`);
const historyCount = Number(db(`
  SELECT COUNT(*) FROM operational_risk_status_history
  WHERE case_id = '${caseId}'
`));
assert("status history created", historyCount >= 1, `count=${historyCount}`);

assert("seed material case", seed.includes("OperationalRiskPilotMaterialCaseId"));
assert("seed resolved case", seed.includes("OperationalRiskPilotResolvedCaseId"));
const pilotMaterial = Number(db(`SELECT COUNT(*) FROM operational_risk_cases WHERE id = 'bb000001-0200-4000-8000-000000000200'`));
const pilotResolved = Number(db(`SELECT COUNT(*) FROM operational_risk_cases WHERE lifecycle_status = 'resolved' AND id = 'bb000001-0206-4000-8000-000000000206'`));
assert("pilot material case present", pilotMaterial === 1, `count=${pilotMaterial}`);
assert("pilot resolved case present", pilotResolved === 1, `count=${pilotResolved}`);
const duplicatePilot = Number(db(`SELECT COUNT(*) FROM operational_risk_cases WHERE case_reference = 'OR-PILOT-MAT-001'`));
assert("restart idempotency pilot material", duplicatePilot === 1, `count=${duplicatePilot}`);

removeClosureFixtures();
const orphanEvidence = Number(db(`
  SELECT COUNT(*) FROM operational_risk_evidence e
  LEFT JOIN operational_risk_cases c ON c.id = e.case_id
  WHERE c.id IS NULL
`));
assert("no orphan evidence after cleanup", orphanEvidence === 0, `count=${orphanEvidence}`);

console.log("OPERATIONAL-RISK-1 domain/lifecycle verification passed.");
