#!/usr/bin/env node
import { readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), "../../..");

function fail(message) {
  console.error(`COMMERCIAL-SUPPLIER-COMPARISON-UI-1 data-honesty verifier failed: ${message}`);
  process.exit(1);
}

function assert(label, ok, detail = "") {
  if (!ok) fail(`${label}${detail ? `: ${detail}` : ""}`);
  console.log(`✓ ${label}`);
}

function read(rel) {
  return readFileSync(resolve(ROOT, rel), "utf8").replaceAll("\r\n", "\n");
}

function forbidText(source, forbidden, label) {
  if (source.toLowerCase().includes(forbidden.toLowerCase())) {
    fail(`${label} must not contain "${forbidden}"`);
  }
}

console.log("Running COMMERCIAL-SUPPLIER-COMPARISON-UI-1 data-honesty verifier.");

const presentation = read("apps/web/src/features/commercial-intelligence/components/commercial-intelligence-presentation.ts");
const view = read("apps/web/src/features/commercial-intelligence/components/commercial-supplier-comparison-workspace-view.tsx");
const supplierRow = read("apps/web/src/features/commercial-intelligence/components/commercial-intelligence-supplier-row.tsx");
const queries = read("apps/web/src/features/commercial-intelligence/server/commercial-supplier-comparison-queries.ts");

assert("supplier KPI builder", presentation.includes("buildCommercialSupplierComparisonKpiCards"));
assert("compatible selector reused", presentation.includes("selectLatestCompatibleSupplierComparison"));
assert("supplier filter helper", presentation.includes("filterCommercialIntelligenceSupplierComparisons"));
assert("spread reconciliation reused", presentation.includes("reconcileCommercialIntelligenceSupplierSpread"));
assert("single comparison denominators", presentation.includes("Seçili karşılaştırmanın dahil ettiği gözlem sayısı"));
assert("record scoped attention count", presentation.includes("Kayıtlı tedarikçi karşılaştırmaları"));
assert("tied count from isTied", presentation.includes("row.isTied"));
assert("queries use supplier comparison type filter", queries.includes('comparisonType: "supplier-price-position"'));
assert("queries reuse mapComparisonSummary pattern", queries.includes("mapCommercialIntelligenceSupplierResult"));
assert("no second API namespace", !queries.includes("/api/commercial-intelligence/supplier"));
forbidText(view, "garantili tasarruf", "supplier view");
forbidText(view, "otomatik tedarik", "supplier view");
forbidText(view, "en iyi tedarikçi", "supplier view");
forbidText(view, "tercih edilen tedarikçi", "supplier view");
assert("baseline label not recommendation", supplierRow.includes("Fiyat baz çizgisi"));

console.log("COMMERCIAL-SUPPLIER-COMPARISON-UI-1 data-honesty verification passed.");
