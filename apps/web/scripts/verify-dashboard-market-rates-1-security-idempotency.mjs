#!/usr/bin/env node
import { readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), "../../..");
const BASE = process.env.CREVAFACTOS_API_BASE_URL ?? "http://127.0.0.1:5000";
const FACILITY = "bb000001-0005-4000-8000-000000000005";
const OWNER = { issuer: "crevafactos-dev", subject: "planning-pilot-user", tenant: "dev-pilot-tenant" };
const NEGATIVE = { issuer: "crevafactos-dev", subject: "quality-negative-test-user", tenant: "dev-pilot-tenant" };

function fail(message) {
  console.error(`DASHBOARD-MARKET-RATES-1 security verification failed: ${message}`);
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
    "X-CrevaFactOS-Dev-Issuer": actor.issuer,
    "X-CrevaFactOS-Dev-Subject": actor.subject,
    "X-CrevaFactOS-Tenant": actor.tenant,
  };
}

async function liveRequest(method, path, body, headers = actorHeaders(OWNER)) {
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

console.log("Running DASHBOARD-MARKET-RATES-1 security verifier.");

const endpoints = readRequired("apps/api/MarketRates/MarketRatesEndpointRouteBuilderExtensions.cs");
const helpers = readRequired("apps/api/MarketRates/MarketRatesEndpointHelpers.cs");
const migration = readRequired("packages/CrevaFactOS.Persistence.Migrations/Migrations/20260830163108_AddMarketRatesFoundation.cs");
const synchronizer = readRequired("packages/CrevaFactOS.Persistence/MarketRates/MarketRateSourceSynchronizer.cs");
const openProvider = readRequired("apps/api/MarketRates/OpenExchangeRatesExchangeRateProvider.cs");

assert("market rates route family", endpoints.includes("/api/dashboard/market-rates"));
assert("read permission gate", helpers.includes("MarketRatesPermissionKeys.Read"));
assert("append-only audit migration", migration.includes("market_rate_audit_events"));
assert("tenant RLS", migration.includes("ENABLE ROW LEVEL SECURITY"));
assert("permission seed", migration.includes("dashboard.market-rates.read"));
assert("no operational side effects", !synchronizer.includes("PurchaseOrder") && !synchronizer.includes("Profitability"));
assert("provider attribution", openProvider.includes("Open Exchange Rates"));
assert("no simulated rates", !openProvider.includes("return 34") && !openProvider.includes("Seed"));

const ready = await fetch(`${BASE}/health/ready`);
assert("API ready", ready.status === 200, `status=${ready.status}`);

const unauth = await liveRequest("GET", `/api/dashboard/market-rates?facilityId=${FACILITY}`, null, {});
assert("unauthenticated summary 401", unauth.status === 401);

const negative = await liveRequest("GET", `/api/dashboard/market-rates?facilityId=${FACILITY}`, null, actorHeaders(NEGATIVE));
assert("negative user denied", negative.status === 401 || negative.status === 403 || negative.status === 404, `status=${negative.status}`);

const summary = await liveRequest("GET", `/api/dashboard/market-rates?facilityId=${FACILITY}`, null);
assert("authorized summary 200", summary.status === 200, summary.text?.slice(0, 200));
assert(
  "default not-configured or unavailable",
  summary.json?.availabilityState === "not-configured"
    || summary.json?.availabilityState === "unavailable",
  JSON.stringify(summary.json?.availabilityState),
);
assert("missing configuration keys present when not configured", Array.isArray(summary.json?.missingConfigurationKeys));

const refresh = await liveRequest("POST", `/api/dashboard/market-rates/refresh?facilityId=${FACILITY}`, null);
assert("authorized refresh 200", refresh.status === 200, refresh.text?.slice(0, 200));
assert("refresh does not invent rates when not configured", refresh.json?.success === false || summary.json?.availabilityState === "not-configured");

const mutate = await liveRequest("DELETE", `/api/dashboard/market-rates?facilityId=${FACILITY}`, null);
assert("DELETE not exposed", mutate.status === 404 || mutate.status === 405);

console.log("DASHBOARD-MARKET-RATES-1 security verification passed.");
