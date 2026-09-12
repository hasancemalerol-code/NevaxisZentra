#!/usr/bin/env node
import { readFileSync } from "node:fs";
import { spawnSync } from "node:child_process";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), "../../..");
const BASE = process.env.NEVAXIS_FACTOS_API_BASE_URL ?? "http://localhost:5000";
const FACILITY = "bb000001-0005-4000-8000-000000000005";
const TENANT = "dev-pilot-tenant";
const ACTOR = { issuer: "crevafactos-dev", subject: "production-pilot-operator", tenant: TENANT };

function fail(message) {
  console.error(`MOLD-2 history domain/idempotency verifier failed: ${message}`);
  process.exit(1);
}

function assert(label, ok, detail = "") {
  if (!ok) fail(`${label}${detail ? `: ${detail}` : ""}`);
  console.log(`✓ ${label}`);
}

function headers(actor = ACTOR) {
  return {
    "Content-Type": "application/json",
    "X-crevafactos-dev-Issuer": actor.issuer,
    "X-crevafactos-dev-Subject": actor.subject,
    "X-NevaxisFactOS-Tenant": actor.tenant,
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
  try { json = text ? JSON.parse(text) : null; } catch { json = text; }
  return { status: response.status, json };
}

async function createMold(code) {
  const idempotencyKey = crypto.randomUUID();
  const create = await request("POST", `/api/molds?facilityId=${FACILITY}`, {
    code,
    name: "MOLD-2 History Test",
    sourceType: "manual",
    ownershipType: "company-owned",
    locationType: "warehouse",
    idempotencyKey,
  });
  if (create.status !== 201) fail(`create mold ${create.status}`);
  return create.json.id;
}

console.log("Running MOLD-2 history domain/idempotency verifier.");

const ready = await fetch(`${BASE}/health/ready`);
if (ready.status !== 200) fail(`/health/ready returned ${ready.status}`);

const moldId = await createMold(`MOLD2-HIST-${Date.now()}`);
const maintKey = crypto.randomUUID();
const maintBody = {
  maintenanceType: "preventive",
  status: "planned",
  openedAtUtc: new Date().toISOString(),
  summary: "Planlı temizlik ve kontrol",
  sourceType: "manual",
  idempotencyKey: maintKey,
};

const createMaint = await request("POST", `/api/molds/${moldId}/maintenance-history?facilityId=${FACILITY}`, maintBody);
assert("maintenance create", createMaint.status === 201, String(createMaint.status));
const maintenanceId = createMaint.json.id;

const replayMaint = await request("POST", `/api/molds/${moldId}/maintenance-history?facilityId=${FACILITY}`, maintBody);
assert("maintenance exact idempotent replay", replayMaint.status === 201 && replayMaint.json.id === maintenanceId);

const conflictMaint = await request("POST", `/api/molds/${moldId}/maintenance-history?facilityId=${FACILITY}`, {
  ...maintBody,
  summary: "Farklı özet",
});
assert("maintenance conflicting idempotency", conflictMaint.status === 409);
assert(
  "history idempotency conflict code",
  (conflictMaint.json?.extensions?.failureCode ?? conflictMaint.json?.failureCode) === "mold.history-idempotency-conflict",
  JSON.stringify(conflictMaint.json),
);

const complete = await request(
  "POST",
  `/api/molds/${moldId}/maintenance-history/${maintenanceId}/complete?facilityId=${FACILITY}`,
  {
    expectedVersion: createMaint.json.version,
    completedAtUtc: new Date().toISOString(),
    actionsTaken: "Temizlik tamamlandı",
  },
);
assert("maintenance complete", complete.status === 200, String(complete.status));

const revKey = crypto.randomUUID();
const revBody = {
  revisionNumber: "R-01",
  revisionType: "cavity-change",
  revisionDate: "2026-08-01",
  summary: "Gözenek sayısı güncellendi",
  sourceType: "manual",
  idempotencyKey: revKey,
};
const appendRev = await request("POST", `/api/molds/${moldId}/revision-history?facilityId=${FACILITY}`, revBody);
assert("revision append", appendRev.status === 201, String(appendRev.status));

const trialKey = crypto.randomUUID();
const trialBody = {
  trialNumber: "T-01",
  trialDate: "2026-08-02",
  result: "successful",
  machineReference: "IM-120",
  shotCount: 50,
  goodPartCount: 48,
  scrapPartCount: 2,
  sourceType: "manual",
  idempotencyKey: trialKey,
};
const appendTrial = await request("POST", `/api/molds/${moldId}/trial-history?facilityId=${FACILITY}`, trialBody);
assert("trial append", appendTrial.status === 201, String(appendTrial.status));

const timeline = await request("GET", `/api/molds/${moldId}/timeline?facilityId=${FACILITY}`);
assert("timeline read", timeline.status === 200 && Array.isArray(timeline.json), String(timeline.status));
assert("timeline includes maintenance", timeline.json.some((e) => e.eventKind === "maintenance"));
assert("timeline includes revision", timeline.json.some((e) => e.eventKind === "revision"));
assert("timeline includes trial", timeline.json.some((e) => e.eventKind === "trial"));

const ids = timeline.json.map((e) => `${e.eventKind}:${e.id}`);
assert("timeline no duplicate events", ids.length === new Set(ids).size);

console.log("Running MOLD-2 critical behavior closure proof.");

const closureCode = `MOLD2-CLOSURE-${Date.now()}`;
const closureMoldId = await createMold(closureCode);

function db(sql) {
  const result = spawnSync(
    "docker",
    ["exec", "NevaxisFactOS-local-postgres-1", "psql", "-U", "crevafactos", "-d", "crevafactos_local", "-t", "-A", "-c", sql],
    { encoding: "utf8" },
  );
  if (result.status !== 0) fail(result.stderr || result.stdout);
  return result.stdout.trim();
}

const maintForUpdate = await request("POST", `/api/molds/${closureMoldId}/maintenance-history?facilityId=${FACILITY}`, {
  maintenanceType: "preventive",
  status: "planned",
  openedAtUtc: new Date().toISOString(),
  summary: "Closure update target",
  sourceType: "manual",
  idempotencyKey: crypto.randomUUID(),
});
assert("maintenance create for update", maintForUpdate.status === 201);
const updateMaintId = maintForUpdate.json.id;
const updateMaintVersion = maintForUpdate.json.version;

const updateOk = await request(
  "PUT",
  `/api/molds/${closureMoldId}/maintenance-history/${updateMaintId}?facilityId=${FACILITY}`,
  {
    expectedVersion: updateMaintVersion,
    maintenanceType: "preventive",
    status: "in-progress",
    openedAtUtc: new Date().toISOString(),
    summary: "Closure updated summary",
    sourceType: "manual",
  },
);
assert("maintenance update", updateOk.status === 200, String(updateOk.status));

const staleUpdate = await request(
  "PUT",
  `/api/molds/${closureMoldId}/maintenance-history/${updateMaintId}?facilityId=${FACILITY}`,
  {
    expectedVersion: updateMaintVersion,
    maintenanceType: "preventive",
    status: "in-progress",
    openedAtUtc: new Date().toISOString(),
    summary: "Stale version attempt",
    sourceType: "manual",
  },
);
assert("maintenance stale-version conflict", staleUpdate.status === 409, String(staleUpdate.status));

const maintForCancel = await request("POST", `/api/molds/${closureMoldId}/maintenance-history?facilityId=${FACILITY}`, {
  maintenanceType: "corrective",
  status: "planned",
  openedAtUtc: new Date().toISOString(),
  summary: "Closure cancel target",
  sourceType: "manual",
  idempotencyKey: crypto.randomUUID(),
});
assert("maintenance create for cancel", maintForCancel.status === 201);
const cancelMaintId = maintForCancel.json.id;

const cancelOk = await request(
  "POST",
  `/api/molds/${closureMoldId}/maintenance-history/${cancelMaintId}/cancel?facilityId=${FACILITY}`,
  { expectedVersion: maintForCancel.json.version, findings: "Closure iptal" },
);
assert("maintenance cancel", cancelOk.status === 200, String(cancelOk.status));

const closureRev1 = await request("POST", `/api/molds/${closureMoldId}/revision-history?facilityId=${FACILITY}`, {
  revisionNumber: "CR-01",
  revisionType: "design-change",
  revisionDate: "2026-06-01",
  summary: "Later revision",
  sourceType: "manual",
  idempotencyKey: crypto.randomUUID(),
});
assert("closure revision first append", closureRev1.status === 201);

const dupRevision = await request("POST", `/api/molds/${closureMoldId}/revision-history?facilityId=${FACILITY}`, {
  revisionNumber: "cr-01",
  revisionType: "other",
  revisionDate: "2026-07-01",
  summary: "Duplicate normalized revision",
  sourceType: "manual",
  idempotencyKey: crypto.randomUUID(),
});
assert("duplicate normalized revision rejected", dupRevision.status === 409, String(dupRevision.status));

const closureTrial1 = await request("POST", `/api/molds/${closureMoldId}/trial-history?facilityId=${FACILITY}`, {
  trialNumber: "CT-01",
  trialDate: "2026-06-02",
  result: "successful",
  sourceType: "manual",
  idempotencyKey: crypto.randomUUID(),
});
assert("closure trial first append", closureTrial1.status === 201);

const dupTrial = await request("POST", `/api/molds/${closureMoldId}/trial-history?facilityId=${FACILITY}`, {
  trialNumber: "ct-01",
  trialDate: "2026-06-03",
  result: "failed",
  sourceType: "manual",
  idempotencyKey: crypto.randomUUID(),
});
assert("duplicate normalized trial rejected", dupTrial.status === 409, String(dupTrial.status));

const negativeTrial = await request("POST", `/api/molds/${closureMoldId}/trial-history?facilityId=${FACILITY}`, {
  trialNumber: "CT-NEG",
  trialDate: "2026-06-04",
  result: "pending",
  scrapPartCount: -1,
  sourceType: "manual",
  idempotencyKey: crypto.randomUUID(),
});
assert("negative trial numeric rejected", negativeTrial.status === 400, String(negativeTrial.status));

const repoSource = readFileSync(
  resolve(ROOT, "packages/NevaxisFactOS.Persistence/Mold/MoldHistoryRepository.cs"),
  "utf8",
);
assert("bounded pagination clamp contract", repoSource.includes("Math.Clamp(filters.PageSize, 1, 100)"));

const oversizedPage = await request(
  "GET",
  `/api/molds/${closureMoldId}/maintenance-history?facilityId=${FACILITY}&pageSize=999`,
);
assert("oversized pageSize accepted", oversizedPage.status === 200, String(oversizedPage.status));

await request("POST", `/api/molds/${closureMoldId}/revision-history?facilityId=${FACILITY}`, {
  revisionNumber: "CR-02",
  revisionType: "other",
  revisionDate: "2026-01-01",
  summary: "Earlier revision",
  sourceType: "manual",
  idempotencyKey: crypto.randomUUID(),
});

const revisions = await request("GET", `/api/molds/${closureMoldId}/revision-history?facilityId=${FACILITY}`);
assert("revision list for ordering", revisions.status === 200 && revisions.json.length >= 2);
assert(
  "stable revision ordering by date desc",
  revisions.json[0].revisionNumber === "CR-01" && revisions.json[1].revisionNumber === "CR-02",
  JSON.stringify(revisions.json.map((r) => r.revisionNumber)),
);

const closureTimeline = await request("GET", `/api/molds/${closureMoldId}/timeline?facilityId=${FACILITY}`);
assert("timeline list for ordering", closureTimeline.status === 200 && closureTimeline.json.length >= 2);
for (let i = 1; i < closureTimeline.json.length; i += 1) {
  const prev = closureTimeline.json[i - 1];
  const curr = closureTimeline.json[i];
  const prevKey = `${prev.eventDateUtc}|${prev.createdAtUtc}|${prev.id}`;
  const currKey = `${curr.eventDateUtc}|${curr.createdAtUtc}|${curr.id}`;
  assert("stable timeline ordering", prevKey >= currKey, `${prevKey} vs ${currKey}`);
}

db(`DELETE FROM mold_maintenance_records WHERE mold_id = '${closureMoldId}'`);
db(`DELETE FROM mold_revision_records WHERE mold_id = '${closureMoldId}'`);
db(`DELETE FROM mold_trial_records WHERE mold_id = '${closureMoldId}'`);
db(`DELETE FROM mold_lifecycle_events WHERE mold_id = '${closureMoldId}'`);
db(`DELETE FROM mold_counter_snapshots WHERE mold_id = '${closureMoldId}'`);
db(`DELETE FROM molds WHERE id = '${closureMoldId}'`);
assert("closure fixtures removed", db(`SELECT COUNT(*) FROM molds WHERE id = '${closureMoldId}'`) === "0");

console.log("MOLD-2 history domain/idempotency verifier passed.");
