#!/usr/bin/env node
import { readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), "../../..");

function fail(message) {
  console.error(`DASHBOARD-MARKET-RATES-1 web contract verification failed: ${message}`);
  process.exit(1);
}

function assert(label, ok, detail = "") {
  if (!ok) fail(`${label}${detail ? `: ${detail}` : ""}`);
  console.log(`✓ ${label}`);
}

function read(rel) {
  return readFileSync(resolve(ROOT, rel), "utf8").replaceAll("\r\n", "\n");
}

console.log("Running DASHBOARD-MARKET-RATES-1 web contract verifier.");

const dashboardPage = read("apps/web/src/app/(app)/dashboard/page.tsx");
const panel = read("apps/web/src/components/dashboard/piyasa-ozeti-panel.tsx");
const queries = read("apps/web/src/features/dashboard/server/market-rates-queries.ts");
const apiClient = read("apps/web/src/features/dashboard/server/market-rates-api-client.ts");
const endpoints = read("apps/api/MarketRates/MarketRatesEndpointRouteBuilderExtensions.cs");
const provider = read("packages/NevaxisFactOS.Persistence/MarketRates/IExchangeRateProvider.cs");
const notConfigured = read("apps/api/MarketRates/NotConfiguredExchangeRateProvider.cs");
const repository = read("packages/NevaxisFactOS.Persistence/MarketRates/MarketRateRepository.cs");

assert("dashboard panel mounted", dashboardPage.includes("PiyasaOzetiPanel"));
assert("compact context strip placement", dashboardPage.includes("command-center__titlebar-context-strip"));
assert("market rates beside live status", dashboardPage.includes("command-center__titlebar-status") && panel.includes("command-center__market-rates-card"));
assert("not relegated to footer area only", !dashboardPage.includes("command-center__market-strip"));
assert("not full-width market row", !dashboardPage.includes("command-center__titlebar-market-rates"));
assert("turkish panel title", panel.includes("Piyasa Özeti"));
assert("panel test id", panel.includes("dashboard-market-rates-panel"));
assert("compact unavailable badge", panel.includes("availabilityLabel"));
assert("compact quote rows", panel.includes("command-center__market-rates-card-quotes"));
assert("freshness badge support", panel.includes("freshnessLabel"));
assert("no provenance footer", !panel.includes("Kaynak:") && !panel.includes("Sağlayıcı zamanı") && !panel.includes("disclaimer"));
assert("no change percent copy", !panel.includes("Değişim verisi yok") && !panel.includes("changePercent"));
assert("server query", queries.includes("readMarketRatesSummary"));
assert("api path", apiClient.includes("/api/dashboard/market-rates"));
assert("provider abstraction", provider.includes("IExchangeRateProvider"));
assert("not configured provider", notConfigured.includes("ProviderNotConfigured"));
assert("GET summary endpoint", endpoints.includes('MapGet("/", GetSummaryAsync)'));
assert("POST refresh endpoint", endpoints.includes('MapPost("/refresh", RefreshAsync)'));
assert("no hardcoded rates", !repository.includes("34.") && !panel.includes("34."));
assert("safe unavailable presentations", read("apps/web/src/features/dashboard/domain/market-rates-presentations.ts").includes("Yapılandırma gerekli"));
assert("queries never hide panel on provider miss", queries.includes("buildSourceUnavailableSummary") || queries.includes("buildAccessRestrictedSummary"));

console.log("DASHBOARD-MARKET-RATES-1 web contract verification passed.");
