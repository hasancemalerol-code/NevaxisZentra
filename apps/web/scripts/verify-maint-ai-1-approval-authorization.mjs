#!/usr/bin/env node
import { existsSync, readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";
import { spawnSync } from "node:child_process";

const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), "../../..");
const BASE = process.env.CREVAFACTOS_API_BASE_URL ?? "http://localhost:5000";
const TENANT = process.env.CREVAFACTOS_TENANT ?? "dev-pilot-tenant";
const ISSUER = "crevafactos-dev";
const OPERATOR = process.env.CREVAFACTOS_SUBJECT ?? "production-pilot-operator";
const REVIEWER = "planning-pilot-reviewer";
const NEGATIVE = "quality-negative-test-user";
const OTHER_TENANT = "dev-other-tenant";
const FACILITY = process.env.CREVAFACTOS_FACILITY_ID ?? "bb000001-0005-4000-8000-000000000005";
const UNAUTH_FACILITY = "bb000001-0005-4000-8000-000000000099";
const ASSET = process.env.CREVAFACTOS_ASSET ?? "ASSET-CNC-001";

function read(rel) {
  const abs = resolve(ROOT, rel);
  if (!existsSync(abs)) throw new Error(`Missing ${rel}`);
  return readFileSync(abs, "utf8");
}

function fail(msg) {
  console.error(`MAINT-AI-1 approval authorization verifier failed: ${msg}`);
  process.exit(1);
}

function assert(label, ok, detail = "") {
  if (!ok) fail(`${label}${detail ? `: ${detail}` : ""}`);
  console.log(`✓ ${label}`);
}

function headers(subject = OPERATOR, extra = {}) {
  return {
    Accept: "application/json",
    "Content-Type": "application/json",
    "X-CrevaFactOS-Dev-Issuer": ISSUER,
    "X-CrevaFactOS-Dev-Subject": subject,
    "X-CrevaFactOS-Tenant": TENANT,
    ...extra,
  };
}

function pathFor(suffix) {
  return `/api/maintenance/assets/${ASSET}${suffix}?facilityId=${FACILITY}`;
}

async function statusFor(method, path, subject = OPERATOR, body, extra = {}) {
  const res = await fetch(`${BASE}${path}`, {
    method,
    headers: headers(subject, extra),
    body: body ? JSON.stringify(body) : undefined,
  });
  return res.status;
}

console.log("Running MAINT-AI-1 approval authorization static checks.");
const endpoints = read("apps/api/Maintenance/MaintenanceAiEndpointRouteBuilderExtensions.cs");

assert("generate uses Manage", endpoints.match(/GenerateDraftAsync[\s\S]*?MaintenancePermissionKeys\.Manage/));
assert("update uses Manage", endpoints.match(/UpdateDraftAsync[\s\S]*?MaintenancePermissionKeys\.Manage/));
assert("approve uses Manage", endpoints.match(/ApproveDraftAsync[\s\S]*?MaintenancePermissionKeys\.Manage/));
assert("reject uses Manage", endpoints.match(/RejectDraftAsync[\s\S]*?MaintenancePermissionKeys\.Manage/));
assert("read uses Read access", endpoints.match(/GetCurrentDraftAsync[\s\S]*?ResolveReadAccessAsync/));

console.log("Running MAINT-AI-1 approval authorization live checks.");
try {
  const ready = await fetch(`${BASE}/health/ready`);
  if (ready.status !== 200) fail(`/health/ready returned ${ready.status}`);

  const generatePath = pathFor("/ai-drafts/generate");
  const currentPath = pathFor("/ai-drafts/current");

  const readOnlyGet = await statusFor("GET", `/api/maintenance/assets/${ASSET}/ai-drafts?facilityId=${FACILITY}&limit=5`, REVIEWER);
  assert("read-only actor GET draft list → 200", readOnlyGet === 200, `got ${readOnlyGet}`);

  const readOnlyGenerate = await statusFor("POST", generatePath, REVIEWER);
  assert("read-only actor POST generate → 403", readOnlyGenerate === 403, `got ${readOnlyGenerate}`);

  const readOnlyApprove = await statusFor("POST", `/api/maintenance/ai-drafts/00000000-0000-4000-8000-000000000001/approve?facilityId=${FACILITY}`, REVIEWER, { expectedVersion: 1 });
  assert("read-only actor POST approve → 403", readOnlyApprove === 403, `got ${readOnlyApprove}`);

  const readOnlyReject = await statusFor("POST", `/api/maintenance/ai-drafts/00000000-0000-4000-8000-000000000001/reject?facilityId=${FACILITY}`, REVIEWER, { expectedVersion: 1, rejectionReason: "test" });
  assert("read-only actor POST reject → 403", readOnlyReject === 403, `got ${readOnlyReject}`);

  const readOnlyEdit = await statusFor("PUT", `/api/maintenance/ai-drafts/00000000-0000-4000-8000-000000000001?facilityId=${FACILITY}`, REVIEWER, { expectedVersion: 1, title: "x" });
  assert("read-only actor PUT edit → 403", readOnlyEdit === 403, `got ${readOnlyEdit}`);

  const noPerm = await statusFor("POST", generatePath, NEGATIVE);
  assert("actor without Maintenance permission POST generate → 403", noPerm === 403, `got ${noPerm}`);

  const missingAuth = await fetch(`${BASE}${generatePath}`, { method: "POST" }).then((r) => r.status);
  assert("missing authentication POST generate → 401", missingAuth === 401, `got ${missingAuth}`);

  const unauthFacility = await statusFor("POST", `/api/maintenance/assets/${ASSET}/ai-drafts/generate?facilityId=${UNAUTH_FACILITY}`, OPERATOR);
  assert("unauthorized facility POST generate → 403", unauthFacility === 403, `got ${unauthFacility}`);

  const crossTenant = await statusFor("POST", generatePath, OPERATOR, undefined, { "X-CrevaFactOS-Tenant": OTHER_TENANT });
  assert("cross-tenant POST generate → 403", crossTenant === 403, `got ${crossTenant}`);
} catch (error) {
  fail(error instanceof Error ? error.message : String(error));
}

console.log("MAINT-AI-1 approval authorization verifier PASS");
