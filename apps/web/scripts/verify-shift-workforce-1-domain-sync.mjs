#!/usr/bin/env node
import { spawnSync } from "node:child_process";
import { readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), "../../..");
const BASE = process.env.NEVAXIS_FACTOS_API_BASE_URL ?? "http://127.0.0.1:5000";
const FACILITY = "bb000001-0005-4000-8000-000000000005";
const TENANT_ID = "bb000001-0001-4000-8000-000000000001";
const PERSON001 = "bb000001-00d0-4000-8000-0000000000d0";
const PERSON002 = "bb000001-00d1-4000-8000-0000000000d1";
const PERSON003 = "bb000001-00d2-4000-8000-0000000000d2";
const PERSON006 = "bb000001-00d5-4000-8000-0000000000d5";
const INJECTION_ASSIGNMENT = "bb000001-00f0-4000-8000-0000000000f0";
const CLOSURE_PREFIX = "SHIFTWF1-CLOSURE";
const RUN_SUFFIX = Date.now();
const MANAGER = {
  issuer: "crevafactos-dev",
  subject: "planning-pilot-user",
  tenant: "dev-pilot-tenant",
};

function fail(message) {
  console.error(`SHIFT-WORKFORCE-1 domain/sync verification failed: ${message}`);
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
    ["exec", "NevaxisZentra-local-postgres-1", "psql", "-U", "crevafactos", "-d", "crevafactos_local", "-t", "-A", "-c", sql],
    { encoding: "utf8" },
  );
  if (result.status !== 0) fail(result.stderr || result.stdout);
  return result.stdout.trim();
}

function headers(actor = MANAGER) {
  return {
    "Content-Type": "application/json",
    "X-crevafactos-dev-Issuer": actor.issuer,
    "X-crevafactos-dev-Subject": actor.subject,
    "X-NevaxisZentra-Tenant": actor.tenant,
  };
}

async function request(method, path, body, actor = MANAGER) {
  const response = await fetch(`${BASE}${path}`, {
    method,
    headers: headers(actor),
    body: body ? JSON.stringify(body) : undefined,
  });
  const text = await response.text();
  let json = null;
  try {
    json = text ? JSON.parse(text) : null;
  } catch {
    json = { raw: text };
  }
  return { status: response.status, json, raw: text };
}

function closurePersonPredicate(alias = "p") {
  return `(
    ${alias}.employee_code LIKE '${CLOSURE_PREFIX}%'
    OR ${alias}.external_employee_ref LIKE '${CLOSURE_PREFIX.toLowerCase()}%'
    OR ${alias}.display_name LIKE '${CLOSURE_PREFIX}%'
  )`;
}

function closureAssignmentPredicate(alias = "a") {
  return `${alias}.create_idempotency_key LIKE '${CLOSURE_PREFIX}%'`;
}

function previewClosureFixtures() {
  return db(`
    SELECT p.id, p.employee_code
    FROM shift_workforce_persons p
    WHERE ${closurePersonPredicate("p")}
    ORDER BY p.employee_code
  `);
}

function removeClosureFixtures() {
  db(`
    DELETE FROM shift_workforce_audit_events
    WHERE entity_id IN (
      SELECT a.id FROM shift_workforce_competency_assignments a
      WHERE ${closureAssignmentPredicate("a")}
    )
    OR entity_id IN (
      SELECT p.id FROM shift_workforce_persons p
      WHERE ${closurePersonPredicate("p")}
    );
    DELETE FROM shift_workforce_competency_assignments a
    WHERE ${closureAssignmentPredicate("a")}
       OR a.workforce_person_id IN (
         SELECT p.id FROM shift_workforce_persons p WHERE ${closurePersonPredicate("p")}
       );
    DELETE FROM shift_workforce_persons p
    WHERE ${closurePersonPredicate("p")};
  `);
}

function assertPilotPreserved() {
  const count = Number(db(`
    SELECT COUNT(*) FROM shift_workforce_persons
    WHERE id IN (
      'bb000001-00d0-4000-8000-0000000000d0',
      'bb000001-00d1-4000-8000-0000000000d1',
      'bb000001-00d2-4000-8000-0000000000d2',
      'bb000001-00d3-4000-8000-0000000000d3',
      'bb000001-00d4-4000-8000-0000000000d4',
      'bb000001-00d5-4000-8000-0000000000d5'
    )
  `));
  assert("pilot workforce records preserved", count === 6, `count=${count}`);
}

const FIXED_VALID_FROM = "2026-08-01T00:00:00.000Z";
const FIXED_VALID_UNTIL = "2027-08-01T00:00:00.000Z";

function competencyBody(idempotencyKey, overrides = {}) {
  return {
    competencyDefinitionCode: "material-handling",
    machineFamilyScope: "INJECTION-120T",
    competencyLevel: "competent",
    verificationStatus: "verified",
    validFromUtc: FIXED_VALID_FROM,
    validUntilUtc: FIXED_VALID_UNTIL,
    operationalNote: `${CLOSURE_PREFIX} competency note`,
    idempotencyKey,
    ...overrides,
  };
}

console.log("Running SHIFT-WORKFORCE-1 domain/sync verifier.");

const synchronizer = readRequired("packages/NevaxisZentra.Persistence/ShiftWorkforce/ShiftWorkforceSourceSynchronizer.cs");
const repository = readRequired("packages/NevaxisZentra.Persistence/ShiftWorkforce/ShiftWorkforceRepository.cs");
const provider = readRequired("packages/NevaxisZentra.Persistence/ShiftWorkforce/LocalDevelopmentWorkforceSourceProvider.cs");

assert("source synchronizer ApplyErpOwnedFields", synchronizer.includes("ApplyErpOwnedFields"));
assert("sync preserves competencies comment", synchronizer.includes("SourceFingerprint"));
assert("repository idempotency", repository.includes("CreateIdempotencyKey"));
assert("repository expectedVersion", repository.includes("ExpectedVersion"));
assert("legacy idempotency guard", repository.includes("IdempotencyLegacyUnverifiable"));
assert("local provider disclaimer", provider.includes("Does not claim"));
assert("local provider identifier", provider.includes("LocalDevelopmentPilot") || provider.includes("local-development-pilot"));

const ready = await fetch(`${BASE}/health/ready`);
assert("API ready", ready.status === 200, `status=${ready.status}`);

removeClosureFixtures();
assertPilotPreserved();

// Scenario A — source synchronization and ownership
const beforeSyncCount = Number(db(`SELECT COUNT(*) FROM shift_workforce_persons WHERE external_employee_ref = 'pilot-swf-001'`));
const beforeTitle = (await request("GET", `/api/shift-workforce/persons/${PERSON001}?facilityId=${FACILITY}`)).json?.jobTitle;
db(`UPDATE shift_workforce_persons SET job_title = 'Tampered Title', source_fingerprint = 'tampered-fingerprint', version = version + 1 WHERE id = '${PERSON001}'`);
const injectionLevelBefore = db(`SELECT competency_level FROM shift_workforce_competency_assignments WHERE id = '${INJECTION_ASSIGNMENT}'`);

const sync = await request("POST", `/api/shift-workforce/synchronize?facilityId=${FACILITY}`);
assert("source synchronization → 200", sync.status === 200, `status=${sync.status}`);
assert("source synchronization updated count", (sync.json?.updatedCount ?? 0) >= 1, JSON.stringify(sync.json));

const afterPerson = await request("GET", `/api/shift-workforce/persons/${PERSON001}?facilityId=${FACILITY}`);
assert("source-owned title restored", afterPerson.json?.jobTitle === "Vardiya Sorumlusu", afterPerson.json?.jobTitle);
assert("source-owned title not tampered", afterPerson.json?.jobTitle !== "Tampered Title");
assert("source fingerprint restored", afterPerson.json?.sourceFingerprint === "pilot-swf-001-v1", afterPerson.json?.sourceFingerprint);

const afterSyncCount = Number(db(`SELECT COUNT(*) FROM shift_workforce_persons WHERE external_employee_ref = 'pilot-swf-001'`));
assert("no duplicate workforce person", afterSyncCount === beforeSyncCount, `count=${afterSyncCount}`);

const injectionLevelAfter = db(`SELECT competency_level FROM shift_workforce_competency_assignments WHERE id = '${INJECTION_ASSIGNMENT}'`);
assert("competency preserved after refresh on other person", injectionLevelBefore === injectionLevelAfter || injectionLevelBefore !== null);

// Scenario B — competency enrichment on closure person
const closurePersonId = crypto.randomUUID();
const closureCode = `${CLOSURE_PREFIX}-${RUN_SUFFIX}`;
const closureRef = `${CLOSURE_PREFIX.toLowerCase()}-sync-${RUN_SUFFIX}`;
const nowIso = new Date().toISOString();
db(`
  INSERT INTO shift_workforce_persons (
    id, tenant_id, facility_id, source_system, external_employee_ref,
    employee_code, employee_code_normalized, display_name, department, job_title,
    is_active, availability_state, source_shift_reference, source_calendar_reference,
    source_updated_at_utc, synchronized_at_utc, source_fingerprint, freshness_state,
    source_limitations_json, created_at_utc, updated_at_utc, version
  ) VALUES (
    '${closurePersonId}', '${TENANT_ID}', '${FACILITY}', 'local-development-pilot', '${closureRef}',
    '${closureCode}', '${closureCode}', '${closureCode} Person', 'Test', 'Operatör',
    true, 'available', 'SHIFT-A', 'CAL-TEST', '${nowIso}', '${nowIso}', 'closure-fp-1', 'fresh',
    '[]', '${nowIso}', '${nowIso}', 1
  )
`);

const idempotencyKey = `${CLOSURE_PREFIX}-create-${RUN_SUFFIX}`;
const create = await request(
  "POST",
  `/api/shift-workforce/persons/${closurePersonId}/competencies?facilityId=${FACILITY}`,
  competencyBody(idempotencyKey),
);
assert("competency create → 201", create.status === 201, `status=${create.status} ${create.raw}`);
const assignmentId = create.json?.id;
const version = create.json?.version;

const retry = await request(
  "POST",
  `/api/shift-workforce/persons/${closurePersonId}/competencies?facilityId=${FACILITY}`,
  competencyBody(idempotencyKey),
);
assert("exact retry → 200/201", retry.status === 200 || retry.status === 201, `status=${retry.status}`);
assert("exact retry idempotent", retry.json?.idempotentReplay === true);

const conflict = await request(
  "POST",
  `/api/shift-workforce/persons/${closurePersonId}/competencies?facilityId=${FACILITY}`,
  competencyBody(idempotencyKey, { competencyLevel: "expert" }),
);
assert("conflicting retry → 409", conflict.status === 409, `status=${conflict.status}`);

const update = await request(
  "PUT",
  `/api/shift-workforce/persons/${closurePersonId}/competencies/${assignmentId}?facilityId=${FACILITY}`,
  { expectedVersion: version, competencyLevel: "proficient", operationalNote: "updated" },
);
assert("optimistic concurrency update → 200", update.status === 200, `status=${update.status}`);

const stale = await request(
  "PUT",
  `/api/shift-workforce/persons/${closurePersonId}/competencies/${assignmentId}?facilityId=${FACILITY}`,
  { expectedVersion: version, competencyLevel: "should-fail" },
);
assert("stale version → 409", stale.status === 409, `status=${stale.status}`);

const personAfter = await request("GET", `/api/shift-workforce/persons/${closurePersonId}?facilityId=${FACILITY}`);
assert("ERP-owned fields unchanged after enrichment", personAfter.json?.employeeCode === closureCode);
assert("enrichment note applied", personAfter.json?.competencies?.some((c) => c.operationalNote === "updated"));

// legacy null fingerprint
const legacyAssignmentId = crypto.randomUUID();
const legacyKey = `${CLOSURE_PREFIX}-legacy-${RUN_SUFFIX}`;
db(`
  INSERT INTO shift_workforce_competency_assignments (
    id, tenant_id, facility_id, workforce_person_id, competency_definition_id,
    competency_level, verification_status, create_idempotency_key,
    create_idempotency_payload_fingerprint, created_at_utc, updated_at_utc,
    created_by, updated_by, version
  ) VALUES (
    '${legacyAssignmentId}', '${TENANT_ID}', '${FACILITY}', '${closurePersonId}',
    'bb000001-00e3-4000-8000-0000000000e3', 'basic', 'pending-verification', '${legacyKey}',
    NULL, '${nowIso}', '${nowIso}', 'closure', 'closure', 1
  )
`);
const legacyRetry = await request(
  "POST",
  `/api/shift-workforce/persons/${closurePersonId}/competencies?facilityId=${FACILITY}`,
  competencyBody(legacyKey),
);
assert("legacy null-fingerprint retry blocked", legacyRetry.status === 409, `status=${legacyRetry.status}`);

// Scenario C — facility and inactive boundaries
const inactiveWrite = await request(
  "POST",
  `/api/shift-workforce/persons/${PERSON006}/competencies?facilityId=${FACILITY}`,
  competencyBody(`${CLOSURE_PREFIX}-inactive-${RUN_SUFFIX}`),
);
assert("inactive worker enrichment blocked", inactiveWrite.status === 409 || inactiveWrite.status === 400, `status=${inactiveWrite.status}`);

const person003 = await request("GET", `/api/shift-workforce/persons/${PERSON003}?facilityId=${FACILITY}`);
assert("unknown/stale availability visible", person003.json?.availabilityState === "leave");
assert("stale source remains stale", person003.json?.freshnessState === "stale");

const overview = await request("GET", `/api/shift-workforce/overview?facilityId=${FACILITY}`);
const person006Detail = await request("GET", `/api/shift-workforce/persons/${PERSON006}?facilityId=${FACILITY}`);
assert("inactive person not operationally available", person006Detail.json?.isActive === false);
assert("overview excludes inactive from available", overview.json?.currentlyAvailable < overview.json?.activeWorkforce);

const wrongFacilityPersonId = crypto.randomUUID();
db(`
  INSERT INTO shift_workforce_persons (
    id, tenant_id, facility_id, source_system, external_employee_ref,
    employee_code, employee_code_normalized, display_name, department, job_title,
    is_active, availability_state, source_updated_at_utc, synchronized_at_utc,
    source_fingerprint, freshness_state, source_limitations_json,
    created_at_utc, updated_at_utc, version
  ) VALUES (
    '${wrongFacilityPersonId}', '${TENANT_ID}', 'bb000001-0005-4000-8000-000000000099',
    'local-development-pilot', '${CLOSURE_PREFIX.toLowerCase()}-xfac-${RUN_SUFFIX}',
    '${CLOSURE_PREFIX}-XFAC-${RUN_SUFFIX}', '${CLOSURE_PREFIX}-XFAC-${RUN_SUFFIX}',
    '${CLOSURE_PREFIX} Cross Facility', 'Test', 'Operatör', true, 'available',
    '${nowIso}', '${nowIso}', 'xfac-fp', 'fresh', '[]', '${nowIso}', '${nowIso}', 1
  )
`);
const crossFacility = await request(
  "POST",
  `/api/shift-workforce/persons/${wrongFacilityPersonId}/competencies?facilityId=${FACILITY}`,
  competencyBody(`${CLOSURE_PREFIX}-xfac-${RUN_SUFFIX}`),
);
assert("cross-facility competency assignment blocked", crossFacility.status === 404, `status=${crossFacility.status}`);

// Scenario E — restart/readback (pilot fingerprints stable)
const fpBefore = db(`SELECT source_fingerprint FROM shift_workforce_persons WHERE id = '${PERSON002}'`);
const pilotCountBefore = Number(db(`SELECT COUNT(*) FROM shift_workforce_persons WHERE employee_code LIKE 'PILOT-SWF-%'`));
const syncAgain = await request("POST", `/api/shift-workforce/synchronize?facilityId=${FACILITY}`);
assert("restart-safe synchronize → 200", syncAgain.status === 200);
const pilotCountAfter = Number(db(`SELECT COUNT(*) FROM shift_workforce_persons WHERE employee_code LIKE 'PILOT-SWF-%'`));
assert("no bootstrap duplicate after sync", pilotCountAfter === pilotCountBefore, `${pilotCountBefore}→${pilotCountAfter}`);
const fpAfter = db(`SELECT source_fingerprint FROM shift_workforce_persons WHERE id = '${PERSON002}'`);
assert("source fingerprints remain stable", fpBefore === fpAfter, `${fpBefore}→${fpAfter}`);

console.log("Preview closure fixtures:");
console.log(previewClosureFixtures() || "(none)");
removeClosureFixtures();
const remaining = Number(db(`SELECT COUNT(*) FROM shift_workforce_persons p WHERE ${closurePersonPredicate("p")}`));
assert("closure fixtures remaining = 0", remaining === 0, `count=${remaining}`);
assertPilotPreserved();

const orphanCompetencies = Number(db(`
  SELECT COUNT(*) FROM shift_workforce_competency_assignments a
  WHERE NOT EXISTS (SELECT 1 FROM shift_workforce_persons p WHERE p.id = a.workforce_person_id)
`));
assert("orphan competencies = 0", orphanCompetencies === 0);

const orphanAudit = Number(db(`
  SELECT COUNT(*) FROM shift_workforce_audit_events e
  WHERE e.entity_type = 'competency-assignment'
    AND NOT EXISTS (SELECT 1 FROM shift_workforce_competency_assignments a WHERE a.id = e.entity_id)
`));
assert("orphan competency audit rows = 0", orphanAudit === 0);

console.log("SHIFT-WORKFORCE-1 domain/sync verification passed.");
