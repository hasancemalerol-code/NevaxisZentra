#!/usr/bin/env node
import { spawnSync } from "node:child_process";
import { readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const BASE = process.env.NEVAXIS_FACTOS_API_BASE_URL ?? "http://localhost:5000";
const FACILITY = "bb000001-0005-4000-8000-000000000005";
const UNAUTH_FACILITY = "bb000001-0005-4000-8000-000000000099";
const TENANT_ID = "bb000001-0001-4000-8000-000000000001";
const WRONG_TENANT_ID = "00000000-0000-0000-0000-000000000099";
const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), "../../..");

function fail(message) {
  console.error(`MOLD-1 request-context security verifier failed: ${message}`);
  process.exit(1);
}

function assert(label, ok, detail = "") {
  if (!ok) fail(`${label}${detail ? `: ${detail}` : ""}`);
  console.log(`✓ ${label}`);
}

function headers(subject, tenant = "dev-pilot-tenant") {
  return {
    "Content-Type": "application/json",
    "X-crevafactos-dev-Issuer": "crevafactos-dev",
    "X-crevafactos-dev-Subject": subject,
    "X-NevaxisFactOS-Tenant": tenant,
  };
}

function db(sql) {
  const result = spawnSync(
    "docker",
    ["exec", "NevaxisFactOS-local-postgres-1", "psql", "-U", "crevafactos", "-d", "crevafactos_local", "-t", "-A", "-c", sql],
    { encoding: "utf8" },
  );
  if (result.status !== 0) fail(result.stderr || result.stdout);
  return result.stdout.trim();
}

async function request(method, path, subject, body, tenant = "dev-pilot-tenant") {
  const response = await fetch(`${BASE}${path}`, {
    method,
    headers: headers(subject, tenant),
    body: body ? JSON.stringify(body) : undefined,
  });
  const text = await response.text();
  let json = null;
  try {
    json = text ? JSON.parse(text) : null;
  } catch {
    json = text;
  }
  return { status: response.status, json };
}

console.log("Running MOLD-1 request-context security live verifier.");

const requestContext = readFileSync(resolve(ROOT, "apps/web/src/features/molds/server/mold-request-context.ts"), "utf8");
assert("request context module present", requestContext.includes("resolveMoldRequestContext"));

const operator = "production-pilot-operator";
const negative = "quality-negative-test-user";

const authorizedList = await request("GET", `/api/molds?facilityId=${FACILITY}`, operator);
assert("authorized tenant list → 200", authorizedList.status === 200);

const crossTenantList = await request("GET", `/api/molds?facilityId=${FACILITY}`, operator, undefined, "foreign-tenant-key");
assert("cross-tenant list denied", crossTenantList.status === 403 || crossTenantList.status === 401, `got ${crossTenantList.status}`);

const moldId = authorizedList.json?.[0]?.id;
if (!moldId) fail("no mold available for detail isolation test");

const crossTenantDetail = await request("GET", `/api/molds/${moldId}?facilityId=${FACILITY}`, operator, undefined, "foreign-tenant-key");
assert("cross-tenant detail denied", crossTenantDetail.status === 403 || crossTenantDetail.status === 401);

const crossTenantMutation = await request("PUT", `/api/molds/${moldId}?facilityId=${FACILITY}`, operator, {
  expectedVersion: 1,
  name: "Cross Tenant Attempt",
  sourceType: "manual",
  ownershipType: "company-owned",
  locationType: "warehouse",
}, "foreign-tenant-key");
assert("cross-tenant mutation denied", crossTenantMutation.status === 403 || crossTenantMutation.status === 401);

const unauthorizedList = await request("GET", `/api/molds?facilityId=${FACILITY}`, negative);
assert("unauthorized actor list → 403", unauthorizedList.status === 403);

const migration = readFileSync(
  resolve(ROOT, "packages/NevaxisFactOS.Persistence.Migrations/Migrations/20260811200000_AddMoldRegistryLifecycleFoundation.cs"),
  "utf8",
);
assert("RLS in migration", migration.includes("ENABLE ROW LEVEL SECURITY") && migration.includes("FORCE ROW LEVEL SECURITY"));

for (const table of ["molds", "mold_lifecycle_events", "mold_counter_snapshots"]) {
  const rls = db(`SELECT relrowsecurity, relforcerowsecurity FROM pg_class WHERE relname = '${table}'`);
  assert(`${table} RLS enabled`, rls === "t|t" || rls.split("|").every((v) => v === "t"));
}

function runtimeTenantCount(tenantId, whereClause = "") {
  const output = db(
    `SET ROLE crevafactos_runtime; SELECT set_config('NevaxisFactOS.tenant_id', '${tenantId}', true); SELECT COUNT(*) FROM molds ${whereClause};`,
  );
  const lines = output.split("\n").map((line) => line.trim()).filter(Boolean);
  return Number(lines.at(-1));
}

const correctTenantRows = runtimeTenantCount(TENANT_ID);
assert("runtime role correct tenant sees rows", correctTenantRows > 0);

const wrongTenantRows = runtimeTenantCount(WRONG_TENANT_ID, `WHERE tenant_id = '${TENANT_ID}'`);
assert("runtime role wrong tenant sees 0 target rows", wrongTenantRows === 0);

console.log("MOLD-1 request-context security verification passed.");
