#!/usr/bin/env node
import { spawnSync } from "node:child_process";
import { readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), "../../..");
const BASE = process.env.NEVAXIS_FACTOS_API_BASE_URL ?? "http://127.0.0.1:5000";
const CATALOG_PATH = "/api/saas-commercial/catalog";

function fail(message) {
  console.error(`SAAS-COMMERCIAL-1 security/boundary verification failed: ${message}`);
  process.exit(1);
}

function assert(label, ok, detail = "") {
  if (!ok) fail(`${label}${detail ? `: ${detail}` : ""}`);
  console.log(`✓ ${label}`);
}

function readRequired(relativePath) {
  return readFileSync(resolve(ROOT, relativePath), "utf8").replaceAll("\r\n", "\n");
}

function forbidText(source, forbidden, label) {
  if (source.includes(forbidden)) fail(`${label} must not contain "${forbidden}"`);
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

async function liveRequest(method, path, body, headers = {}) {
  const response = await fetch(`${BASE}${path}`, {
    method,
    headers: { "Content-Type": "application/json", ...headers },
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

function assertNoSensitiveCommercialPayload(rawBody, label) {
  const forbiddenPatterns = [
    /card[_-]?number/i,
    /\bcvv\b/i,
    /\bpan\b/i,
    /provider[_-]?secret/i,
    /api[_-]?key/i,
    /sk_live/i,
    /stripe[_-]?secret/i,
    /merchant[_-]?id/i,
    /client[_-]?secret/i,
  ];
  for (const pattern of forbiddenPatterns) {
    assert(`${label} excludes ${pattern}`, !pattern.test(rawBody), rawBody.slice(0, 200));
  }
}

console.log("Running SAAS-COMMERCIAL-1 security/boundary verifier.");

const endpoints = readRequired("apps/api/SaasCommercial/SaasCommercialEndpointRouteBuilderExtensions.cs");
const helpers = readRequired("apps/api/SaasCommercial/SaasCommercialEndpointHelpers.cs");
const migration = readRequired(
  "packages/NevaxisZentra.Persistence.Migrations/Migrations/20260814051728_AddSaasCommercialPlanAndEntitlementFoundation.cs",
);
const permissions = readRequired("packages/NevaxisZentra.Persistence/SaasCommercial/SaasCommercialPermissionKeys.cs");
const bootstrap = readRequired("packages/NevaxisZentra.Persistence/Development/DevelopmentPilotBootstrapService.cs");

assert("endpoints require auth", endpoints.includes("RequireAuthorization"));
assert("read access helper", endpoints.includes("ResolveReadAccessAsync"));
assert("saas-commercial.read key", permissions.includes("saas-commercial.read"));
assert("catalog read route", endpoints.includes('MapGet("/catalog"'));
assert("audit append-only grant", migration.includes("GRANT SELECT, INSERT ON TABLE saas_commercial_audit_events"));
forbidText(migration, "GRANT DELETE ON TABLE saas_commercial_audit_events", "audit events migration");
forbidText(migration, "GRANT DELETE ON TABLE saas_commercial_plans", "catalog plans migration");
forbidText(endpoints, "MapPost", "saas-commercial endpoints");
forbidText(endpoints, "MapPut", "saas-commercial endpoints");
forbidText(endpoints, "MapDelete", "saas-commercial endpoints");

assert("bootstrap read seed", bootstrap.includes("EnsureSaasCommercialReadPermissionAssignmentAsync"));
assert("manager read grant", bootstrap.includes("EnsureSaasCommercialReadPermissionAssignmentAsync(\n            dbContext, tenant.Id, tenantRole.Id"));
assert("reviewer read grant", bootstrap.includes("EnsureSaasCommercialReadPermissionAssignmentAsync(\n            dbContext, tenant.Id, reviewerRole.Id"));
assert("operator read grant", bootstrap.includes("EnsureSaasCommercialReadPermissionAssignmentAsync(\n            dbContext, tenant.Id, operatorRole.Id"));
assert("helpers return 401 without identity", helpers.includes("TypedResults.Unauthorized()"));
assert("helpers return 403 without permission", helpers.includes("Authorization denied"));

const managerHeaders = {
  "X-crevafactos-dev-Issuer": "crevafactos-dev",
  "X-crevafactos-dev-Subject": "planning-pilot-user",
  "X-NevaxisZentra-Tenant": "dev-pilot-tenant",
};
const negativeHeaders = {
  "X-crevafactos-dev-Issuer": "crevafactos-dev",
  "X-crevafactos-dev-Subject": "quality-negative-test-user",
  "X-NevaxisZentra-Tenant": "dev-pilot-tenant",
};
const crossTenantHeaders = {
  "X-crevafactos-dev-Issuer": "crevafactos-dev",
  "X-crevafactos-dev-Subject": "planning-pilot-user",
  "X-NevaxisZentra-Tenant": "unknown-tenant-key",
};

const ready = await fetch(`${BASE}/health/ready`);
assert("API ready", ready.status === 200, `status=${ready.status}`);

const auditCountBefore = Number(db("SELECT COUNT(*) FROM saas_commercial_audit_events"));

assert("missing auth → 401", (await liveRequest("GET", CATALOG_PATH)).status === 401);

const authorizedCatalog = await liveRequest("GET", CATALOG_PATH, null, managerHeaders);
assert("authorized manager catalog GET → 200", authorizedCatalog.status === 200, `status=${authorizedCatalog.status}`);
assertNoSensitiveCommercialPayload(authorizedCatalog.raw, "authorized catalog response");

assert(
  "unauthorized catalog GET → 403",
  (await liveRequest("GET", CATALOG_PATH, null, negativeHeaders)).status === 403,
);
assert(
  "cross-tenant catalog GET → 403",
  (await liveRequest("GET", CATALOG_PATH, null, crossTenantHeaders)).status === 403,
);

const deniedDetail = await liveRequest(
  "GET",
  "/api/saas-commercial/catalog/dev-pilot-local-commercial",
  null,
  negativeHeaders,
);
assert("denied plan detail → 403", deniedDetail.status === 403, `status=${deniedDetail.status}`);

const auditCountAfter = Number(db("SELECT COUNT(*) FROM saas_commercial_audit_events"));
assert("denied requests created no audit rows", auditCountAfter === auditCountBefore, `${auditCountBefore}→${auditCountAfter}`);

const paymentMethods = await liveRequest(
  "GET",
  "/api/saas-commercial/catalog/dev-pilot-local-commercial/payment-methods?version=1",
  null,
  managerHeaders,
);
assert("authorized payment-methods GET → 200", paymentMethods.status === 200, `status=${paymentMethods.status}`);
assertNoSensitiveCommercialPayload(paymentMethods.raw, "payment-methods response");
const methodTypes = (paymentMethods.json?.paymentMethods ?? []).map((m) => m.methodType);
assert(
  "payment-methods expose capability tokens only",
  methodTypes.every((type) => ["card", "bank-transfer", "enterprise-invoice"].includes(type)),
  methodTypes.join(","),
);

console.log("SAAS-COMMERCIAL-1 security/boundary verification passed.");
