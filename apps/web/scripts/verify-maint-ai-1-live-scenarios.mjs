#!/usr/bin/env node
import { spawnSync } from "node:child_process";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const BASE = process.env.NEVAXIS_FACTOS_API_BASE_URL ?? "http://localhost:5000";
const TENANT = process.env.NEVAXIS_FACTOS_TENANT ?? "dev-pilot-tenant";
const ISSUER = "crevafactos-dev";
const OPERATOR = process.env.NEVAXIS_FACTOS_SUBJECT ?? "production-pilot-operator";
const REVIEWER = "planning-pilot-reviewer";
const FACILITY = process.env.NEVAXIS_FACTOS_FACILITY_ID ?? "bb000001-0005-4000-8000-000000000005";
const ASSET = "ASSET-CNC-001";

function fail(msg) {
  console.error(`MAINT-AI-1 live scenarios failed: ${msg}`);
  process.exit(1);
}

function assert(label, ok, detail = "") {
  if (!ok) fail(`${label}${detail ? `: ${detail}` : ""}`);
  console.log(`✓ ${label}`);
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

function db(sql) {
  const r = spawnSync(
    "docker",
    ["exec", "NevaxisFactOS-local-postgres-1", "psql", "-U", "crevafactos", "-d", "crevafactos_local", "-t", "-A", "-c", sql],
    { encoding: "utf8" },
  );
  if (r.status !== 0) fail(r.stderr || r.stdout);
  return r.stdout.trim();
}

async function json(method, path, body, subject = OPERATOR) {
  const res = await fetch(`${BASE}${path}`, {
    method,
    headers: headers(subject),
    body: body ? JSON.stringify(body) : undefined,
  });
  const text = await res.text();
  let data = null;
  try { data = text ? JSON.parse(text) : null; } catch { data = text; }
  return { status: res.status, data };
}

console.log("Running MAINT-AI-1 live scenario checks.");
try {
  const ready = await fetch(`${BASE}/health/ready`);
  assert("API ready", ready.status === 200, `got ${ready.status}`);

  await json("POST", `/api/maintenance/condition/risk/evaluate?facilityId=${FACILITY}`);

  const gen = await json("POST", `/api/maintenance/assets/${ASSET}/ai-drafts/generate?facilityId=${FACILITY}`);
  assert("successful draft generation", gen.status === 200, JSON.stringify(gen.data));

  const detail = await json("GET", `/api/maintenance/assets/${ASSET}/ai-drafts/current?facilityId=${FACILITY}`);
  assert("read current draft", detail.status === 200);
  assert("structured output present", Boolean(detail.data?.validatedOutput?.summary));
  assert("provider disclosure", Boolean(detail.data?.providerIdentifier));
  assert("input fingerprint present", Boolean(detail.data?.inputFingerprint));
  assert("rule-set version present", Boolean(detail.data?.ruleSetVersion));
  assert("development provider flagged", detail.data?.isDevelopmentProvider === true);

  const readOnly = await json("GET", `/api/maintenance/assets/${ASSET}/ai-drafts?facilityId=${FACILITY}&limit=5`, undefined, REVIEWER);
  assert("read-only draft list", readOnly.status === 200, `got ${readOnly.status}`);

  const edit = await json("PUT", `/api/maintenance/ai-drafts/${gen.data.draftId}?facilityId=${FACILITY}`, {
    expectedVersion: gen.data.version,
    title: "Operatör düzeltmesi — teknik inceleme",
    description: "İnsan tarafından düzenlenmiş açıklama.",
  });
  assert("human edit", edit.status === 200, JSON.stringify(edit.data));

  const afterEdit = await json("GET", `/api/maintenance/ai-drafts/${gen.data.draftId}?facilityId=${FACILITY}`);
  assert("original AI output preserved", Boolean(afterEdit.data?.aiOriginalOutput));
  assert("human edit recorded", afterEdit.data?.humanEditedTitle?.includes("Operatör"));
  assert("version incremented", afterEdit.data?.version > gen.data.version);

  const rejectDraftGen = await json("POST", `/api/maintenance/assets/ASSET-PRESS-001/ai-drafts/generate?facilityId=${FACILITY}`);
  const reject = await json("POST", `/api/maintenance/ai-drafts/${rejectDraftGen.data.draftId}/reject?facilityId=${FACILITY}`, {
    expectedVersion: rejectDraftGen.data.version,
    rejectionReason: "Test red nedeni — yetersiz kanıt.",
  });
  assert("rejection scenario", reject.status === 200);
  const rejectedDetail = await json("GET", `/api/maintenance/ai-drafts/${rejectDraftGen.data.draftId}?facilityId=${FACILITY}`);
  assert("rejected status", rejectedDetail.data?.status === "rejected");
  assert("no request on rejected draft", !rejectedDetail.data?.createdMaintenanceRequestId);

  const providerFailPrep = spawnSync(
    "docker",
    ["exec", "NevaxisFactOS-local-postgres-1", "psql", "-U", "crevafactos", "-d", "crevafactos_local", "-c",
      `UPDATE maintenance_asset_machine_mappings SET asset_name = 'AI-PROVIDER-FAIL probe' WHERE asset_reference = 'ASSET-PRESS-001' AND facility_id = '${FACILITY}'::uuid`],
    { encoding: "utf8" },
  );
  assert("provider-fail test prep", providerFailPrep.status === 0);
  const providerFail = await json("POST", `/api/maintenance/assets/ASSET-PRESS-001/ai-drafts/generate?facilityId=${FACILITY}`);
  spawnSync(
    "docker",
    ["exec", "NevaxisFactOS-local-postgres-1", "psql", "-U", "crevafactos", "-d", "crevafactos_local", "-c",
      `UPDATE maintenance_asset_machine_mappings SET asset_name = 'Pres Makinesi 001' WHERE asset_reference = 'ASSET-PRESS-001' AND facility_id = '${FACILITY}'::uuid`],
    { encoding: "utf8" },
  );
  assert("provider failure scenario", providerFail.status === 422 || providerFail.status === 400, `got ${providerFail.status}`);

  const invalidPrep = spawnSync(
    "docker",
    ["exec", "NevaxisFactOS-local-postgres-1", "psql", "-U", "crevafactos", "-d", "crevafactos_local", "-c",
      `UPDATE maintenance_ai_action_drafts SET status = 'stale' WHERE asset_reference = '${ASSET}' AND status IN ('draft','under-review'); UPDATE maintenance_asset_machine_mappings SET asset_name = 'INVALID-AI-OUTPUT probe' WHERE asset_reference = '${ASSET}' AND facility_id = '${FACILITY}'::uuid`],
    { encoding: "utf8" },
  );
  assert("invalid-output test prep", invalidPrep.status === 0);
  await json("POST", `/api/maintenance/condition/risk/evaluate?facilityId=${FACILITY}`);

  const invalidOut = await json("POST", `/api/maintenance/assets/${ASSET}/ai-drafts/generate?facilityId=${FACILITY}`);
  spawnSync(
    "docker",
    ["exec", "NevaxisFactOS-local-postgres-1", "psql", "-U", "crevafactos", "-d", "crevafactos_local", "-c",
      `UPDATE maintenance_asset_machine_mappings SET asset_name = 'CNC Tezgâh 001' WHERE asset_reference = '${ASSET}' AND facility_id = '${FACILITY}'::uuid`],
    { encoding: "utf8" },
  );
  assert("invalid structured response rejected", invalidOut.status === 422 || invalidOut.status === 400, `got ${invalidOut.status}`);

  const staleGen = await json("POST", `/api/maintenance/assets/${ASSET}/ai-drafts/generate?facilityId=${FACILITY}`);
  assert("stale-test draft generated", staleGen.status === 200);
  spawnSync(
    "docker",
    ["exec", "NevaxisFactOS-local-postgres-1", "psql", "-U", "crevafactos", "-d", "crevafactos_local", "-c",
      `UPDATE maintenance_ai_action_drafts SET input_fingerprint = 'forced-stale-fingerprint' WHERE id = '${staleGen.data.draftId}'::uuid`],
    { encoding: "utf8" },
  );
  const staleApprove = await json("POST", `/api/maintenance/ai-drafts/${staleGen.data.draftId}/approve?facilityId=${FACILITY}`, { expectedVersion: staleGen.data.version });
  assert("stale draft approval blocked", staleApprove.status === 409, `got ${staleApprove.status}`);

  const draftCountBeforeRestart = db("SELECT COUNT(*) FROM maintenance_ai_action_drafts");
  const auditCountBeforeRestart = db("SELECT COUNT(*) FROM maintenance_ai_draft_audit_events");

  assert("drafts persisted before restart check", Number(draftCountBeforeRestart) > 0);
  assert("audit persisted before restart check", Number(auditCountBeforeRestart) > 0);

  console.log("MAINT-AI-1 live scenarios PASS");
} catch (error) {
  fail(error instanceof Error ? error.message : String(error));
}
