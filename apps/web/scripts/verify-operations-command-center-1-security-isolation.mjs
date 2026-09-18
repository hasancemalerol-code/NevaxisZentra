#!/usr/bin/env node
import { readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), "../../..");
const BASE = process.env.NEVAXIS_FACTOS_API_BASE_URL ?? "http://127.0.0.1:5000";
const FACILITY = "bb000001-0005-4000-8000-000000000005";
const MANAGER = { issuer: "crevafactos-dev", subject: "production-pilot-operator", tenant: "dev-pilot-tenant" };
const READONLY = { issuer: "crevafactos-dev", subject: "planning-pilot-reviewer", tenant: "dev-pilot-tenant" };

function fail(message) {
  console.error(`OPERATIONS-COMMAND-CENTER-1 security/isolation verification failed: ${message}`);
  process.exit(1);
}

function assert(label, ok, detail = "") {
  if (!ok) fail(`${label}${detail ? `: ${detail}` : ""}`);
  console.log(`✓ ${label}`);
}

function readRequired(relativePath) {
  return readFileSync(resolve(ROOT, relativePath), "utf8").replaceAll("\r\n", "\n");
}

function actorHeaders(actor) {
  return {
    "Content-Type": "application/json",
    "X-crevafactos-dev-Issuer": actor.issuer,
    "X-crevafactos-dev-Subject": actor.subject,
    "X-NevaxisZentra-Tenant": actor.tenant,
  };
}

async function liveRequest(method, path, body, headers = actorHeaders(MANAGER)) {
  const response = await fetch(`${BASE}${path}`, {
    method,
    headers,
    body: body ? JSON.stringify(body) : undefined,
  });
  const text = await response.text();
  let json = null;
  try { json = text ? JSON.parse(text) : null; } catch { json = { raw: text }; }
  return { status: response.status, json, text };
}

console.log("Running OPERATIONS-COMMAND-CENTER-1 security/isolation verifier.");

const queries = readRequired("apps/web/src/features/operations-command-center/server/operations-command-center-queries.ts");
const page = readRequired("apps/web/src/app/(app)/operasyon/yonetim-merkezi/page.tsx");

assert("composite read function", queries.includes("readOperationsCommandCenterWorkspace"));
assert("per-module permission evaluation", queries.includes("evaluateModuleAccess"));
assert("subscription entitlement check", queries.includes("isModuleEntitled"));
assert("unavailable permission state", queries.includes("unavailable-permission"));
assert("unavailable subscription state", queries.includes("unavailable-subscription"));
assert("no task draft writes", !queries.includes("task-draft"));
assert("no follow-up transition writes", !queries.includes("transitionFollowUp") && !queries.includes("createTaskDraft"));
assert("page uses any-granted gate", page.includes("denyUnlessAnyGranted"));
assert("bounded list page size", queries.includes("LIST_PAGE_SIZE = 100"));
assert("parallel bounded reads", queries.includes("Promise.all"));

const ready = await fetch(`${BASE}/health/ready`);
assert("API ready", ready.status === 200, `status=${ready.status}`);

const productionWorkspace = await liveRequest("GET", `/api/production/follow-up/workspace?facilityId=${FACILITY}`);
assert("production workspace authorized", productionWorkspace.status === 200);

const materialWorkspace = await liveRequest("GET", `/api/materials/availability-risks/workspace?facilityId=${FACILITY}`);
assert("material workspace authorized", materialWorkspace.status === 200);

const procurementWorkspace = await liveRequest("GET", `/api/procurement/delivery-risks/workspace?facilityId=${FACILITY}`);
assert("procurement workspace authorized", procurementWorkspace.status === 200);

const unauthProduction = await liveRequest(
  "GET",
  `/api/production/follow-up/workspace?facilityId=${FACILITY}`,
  null,
  {},
);
assert("unauthenticated production rejected", unauthProduction.status === 401);

const UNAUTH_FACILITY = "bb000001-0005-4000-8000-000000000099";

const wrongFacility = await liveRequest(
  "GET",
  `/api/production/follow-up/workspace?facilityId=${UNAUTH_FACILITY}`,
);
assert("wrong facility production rejected", wrongFacility.status === 403);

console.log("OPERATIONS-COMMAND-CENTER-1 security/isolation verification passed.");
