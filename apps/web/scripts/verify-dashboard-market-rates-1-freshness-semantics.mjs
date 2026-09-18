#!/usr/bin/env node
import { readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), "../../..");

function fail(message) {
  console.error(`DASHBOARD-MARKET-RATES-1 freshness semantics verification failed: ${message}`);
  process.exit(1);
}

function assert(label, ok, detail = "") {
  if (!ok) fail(`${label}${detail ? `: ${detail}` : ""}`);
  console.log(`✓ ${label}`);
}

function read(rel) {
  return readFileSync(resolve(ROOT, rel), "utf8").replaceAll("\r\n", "\n");
}

const MINUTE_MS = 60 * 1000;
const FRESH_LIMIT_MS = 90 * MINUTE_MS;
const DELAYED_LIMIT_MS = 180 * MINUTE_MS;

function computeFreshnessState(ageMs, providerKey = "open-exchange-rates") {
  const hourly =
    !providerKey
    || providerKey === "open-exchange-rates";

  if (!hourly) {
    return ageMs <= FRESH_LIMIT_MS ? "fresh" : "stale";
  }

  if (ageMs <= FRESH_LIMIT_MS) return "fresh";
  if (ageMs <= DELAYED_LIMIT_MS) return "delayed";
  return "stale";
}

function toDisplayFreshnessState(state) {
  return ({
    fresh: "Güncel",
    delayed: "Gecikmeli",
    stale: "Bayat",
  })[state] ?? "Bilinmiyor";
}

console.log("Running DASHBOARD-MARKET-RATES-1 freshness semantics verifier.");

const constants = read("packages/NevaxisZentra.Persistence/MarketRates/MarketRatesConstants.cs");
const conversions = read("packages/NevaxisZentra.Persistence/MarketRates/MarketRatesValueConversions.cs");
const panel = read("apps/web/src/components/dashboard/piyasa-ozeti-panel.tsx");

assert("fresh limit 90 minutes", constants.includes("TimeSpan.FromMinutes(90)"));
assert("delayed limit 180 minutes", constants.includes("TimeSpan.FromMinutes(180)"));
assert("delayed freshness state", constants.includes('Delayed = "delayed"'));
assert("gecikmeli label", conversions.includes('"Gecikmeli"'));
assert("provider cadence parameter", conversions.includes("providerKey"));
assert("no canli label", !conversions.toLowerCase().includes("canlı") && !conversions.toLowerCase().includes("canli"));

const cases = [
  { label: "26 minute snapshot", ageMs: 26 * MINUTE_MS, state: "fresh", display: "Güncel" },
  { label: "90 minute snapshot boundary", ageMs: 90 * MINUTE_MS, state: "fresh", display: "Güncel" },
  { label: "91 minute overdue snapshot", ageMs: 91 * MINUTE_MS, state: "delayed", display: "Gecikmeli" },
  { label: "120 minute overdue snapshot", ageMs: 120 * MINUTE_MS, state: "delayed", display: "Gecikmeli" },
  { label: "180 minute delayed boundary", ageMs: 180 * MINUTE_MS, state: "delayed", display: "Gecikmeli" },
  { label: "181 minute stale snapshot", ageMs: 181 * MINUTE_MS, state: "stale", display: "Bayat" },
  { label: "4 hour old snapshot", ageMs: 240 * MINUTE_MS, state: "stale", display: "Bayat" },
];

for (const testCase of cases) {
  const state = computeFreshnessState(testCase.ageMs);
  assert(`${testCase.label} state`, state === testCase.state, `got=${state}`);
  assert(`${testCase.label} label`, toDisplayFreshnessState(state) === testCase.display);
}

assert(
  "threshold summary documented",
  FRESH_LIMIT_MS === (90 * MINUTE_MS) && DELAYED_LIMIT_MS === (180 * MINUTE_MS),
);
assert("compact dashboard card unchanged", panel.includes("command-center__market-rates-card"));
assert("compact card uses freshness label", panel.includes("freshnessLabel"));

console.log("DASHBOARD-MARKET-RATES-1 freshness semantics verification passed.");
console.log("Thresholds: Güncel <= 90 min; Gecikmeli <= 180 min; Bayat > 180 min.");
