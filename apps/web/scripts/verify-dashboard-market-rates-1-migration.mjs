#!/usr/bin/env node
import { readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), "../../..");

function fail(message) {
  console.error(`DASHBOARD-MARKET-RATES-1 migration verification failed: ${message}`);
  process.exit(1);
}

function assert(label, ok, detail = "") {
  if (!ok) fail(`${label}${detail ? `: ${detail}` : ""}`);
  console.log(`✓ ${label}`);
}

function read(rel) {
  return readFileSync(resolve(ROOT, rel), "utf8").replaceAll("\r\n", "\n");
}

console.log("Running DASHBOARD-MARKET-RATES-1 migration verifier.");

const migration = read("packages/CrevaFactOS.Persistence.Migrations/Migrations/20260830163108_AddMarketRatesFoundation.cs");
const dbContext = read("packages/CrevaFactOS.Persistence/CrevaFactOSDbContext.cs");

assert("snapshot table", migration.includes("market_rate_snapshots"));
assert("quote lines table", migration.includes("market_rate_quote_lines"));
assert("audit table", migration.includes("market_rate_audit_events"));
assert("pair uniqueness", migration.includes("ux_market_rate_quote_lines_tenant_pair"));
assert("runtime grants", migration.includes("GRANT SELECT, INSERT, UPDATE, DELETE ON TABLE market_rate_snapshots"));
assert("dbcontext snapshots", dbContext.includes("MarketRateSnapshots"));
assert("dbcontext quotes", dbContext.includes("MarketRateQuoteLines"));
assert("dbcontext audit", dbContext.includes("MarketRateAuditEvents"));

console.log("DASHBOARD-MARKET-RATES-1 migration verification passed.");
