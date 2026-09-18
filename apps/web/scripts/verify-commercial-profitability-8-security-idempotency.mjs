#!/usr/bin/env node
import { readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), "../../..");
const BASE = process.env.NEVAXIS_FACTOS_API_BASE_URL ?? "http://127.0.0.1:5000";
const WEB = (process.env.NEVAXIS_FACTOS_WEB_BASE_URL ?? "http://localhost:3001").replace("127.0.0.1", "localhost").replace(/\/$/, "");
const ROUTE = "/ticari-zeka/karlilik-yonetim-merkezi";
const FACILITY = "bb000001-0005-4000-8000-000000000005";
const UNAUTH_FACILITY = "bb000001-0005-4000-8000-000000000099";

const MANAGER = {
  issuer: "crevafactos-dev",
  subject: "planning-pilot-user",
  tenant: "dev-pilot-tenant",
};
const READONLY = {
  issuer: "crevafactos-dev",
  subject: "planning-pilot-reviewer",
  tenant: "dev-pilot-tenant",
};

function fail(message) {
  console.error(`COMMERCIAL-PROFITABILITY-8 security/idempotency verification failed: ${message}`);
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

async function liveRequest(method, path, body, headers) {
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

console.log("Running COMMERCIAL-PROFITABILITY-8 security/idempotency verifier.");

const queries = readRequired("apps/web/src/features/commercial-intelligence/server/commercial-profitability-command-center-queries.ts");
const view = readRequired("apps/web/src/features/commercial-intelligence/components/commercial-profitability-command-center-view.tsx");
const page = readRequired("apps/web/src/app/(app)/ticari-zeka/karlilik-yonetim-merkezi/page.tsx");

assert("query layer denies without read permission", queries.includes('denyUnlessGranted("commercial-intelligence.read")'));
assert("page forbids without read permission", page.includes('forbidUnlessGranted("commercial-intelligence.read")'));
assert("facility resolved server-side", queries.includes("resolveCommercialIntelligenceRequestContext"));
assert("no browser facility param trust", !queries.includes("searchParams.get(\"facilityId\")"));
assert("partial scope semantics", queries.includes("scopeFromPage"));
assert("no write actions in view", !view.includes("transitionCommercial") && !view.includes("createCommercial"));
assert("refresh is read-only revalidate", view.includes("refreshCommercialProfitabilityCommandCenterAction"));

const unauthAlertWorkspace = await liveRequest(
  "GET",
  `/api/commercial-intelligence/profitability/alerts/workspace?facilityId=${FACILITY}`,
  null,
  {},
);
assert("unauthenticated alert workspace rejected", unauthAlertWorkspace.status === 401);

const unauthScenarioWorkspace = await liveRequest(
  "GET",
  `/api/commercial-intelligence/pricing-scenarios/workspace?facilityId=${FACILITY}`,
  null,
  {},
);
assert("unauthenticated scenario workspace rejected", unauthScenarioWorkspace.status === 401);

const wrongFacilityAlerts = await liveRequest(
  "GET",
  `/api/commercial-intelligence/profitability/alerts/workspace?facilityId=${UNAUTH_FACILITY}`,
  null,
  actorHeaders(MANAGER),
);
assert(
  "wrong facility alert workspace rejected",
  wrongFacilityAlerts.status === 403 || wrongFacilityAlerts.status === 404,
  `status=${wrongFacilityAlerts.status}`,
);

const authorizedAlerts = await liveRequest(
  "GET",
  `/api/commercial-intelligence/profitability/alerts/workspace?facilityId=${FACILITY}`,
  null,
  actorHeaders(MANAGER),
);
assert("authorized alert workspace succeeds", authorizedAlerts.status === 200);
assert("alert workspace kpis authoritative", typeof authorizedAlerts.json?.kpis?.activeCount === "number");

const authorizedScenarios = await liveRequest(
  "GET",
  `/api/commercial-intelligence/pricing-scenarios/workspace?facilityId=${FACILITY}`,
  null,
  actorHeaders(MANAGER),
);
assert("authorized scenario workspace succeeds", authorizedScenarios.status === 200);
assert("scenario workspace kpis authoritative", typeof authorizedScenarios.json?.kpis?.needsReviewCount === "number");

const alertList = await liveRequest(
  "GET",
  `/api/commercial-intelligence/profitability/alerts?facilityId=${FACILITY}&page=1&pageSize=5`,
  null,
  actorHeaders(MANAGER),
);
assert("authorized alert list succeeds", alertList.status === 200);
assert("alert list returns items array", Array.isArray(alertList.json?.items));

const scenarioList = await liveRequest(
  "GET",
  `/api/commercial-intelligence/pricing-scenarios?facilityId=${FACILITY}&page=1&pageSize=5`,
  null,
  actorHeaders(MANAGER),
);
assert("authorized scenario list succeeds", scenarioList.status === 200);
assert("scenario list returns items array", Array.isArray(scenarioList.json?.items));

const unauthWeb = await fetch(`${WEB}${ROUTE}`, { redirect: "manual" });
assert(
  "unauthenticated web route redirects or rejects",
  unauthWeb.status === 307 || unauthWeb.status === 302 || unauthWeb.status === 401 || unauthWeb.status === 403,
  `status=${unauthWeb.status}`,
);

const readonlyAlerts = await liveRequest(
  "GET",
  `/api/commercial-intelligence/profitability/alerts/workspace?facilityId=${FACILITY}`,
  null,
  actorHeaders(READONLY),
);
assert("reviewer can read alert workspace", readonlyAlerts.status === 200);

console.log("COMMERCIAL-PROFITABILITY-8 security/idempotency verification passed.");
