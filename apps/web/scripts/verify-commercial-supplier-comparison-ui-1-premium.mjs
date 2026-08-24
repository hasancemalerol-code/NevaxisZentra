#!/usr/bin/env node
import { readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), "../../..");

function fail(message) {
  console.error(`COMMERCIAL-SUPPLIER-COMPARISON-UI-1 premium UI verifier failed: ${message}`);
  process.exit(1);
}

function assert(label, ok, detail = "") {
  if (!ok) fail(`${label}${detail ? `: ${detail}` : ""}`);
  console.log(`✓ ${label}`);
}

function read(rel) {
  return readFileSync(resolve(ROOT, rel), "utf8").replaceAll("\r\n", "\n");
}

console.log("Running COMMERCIAL-SUPPLIER-COMPARISON-UI-1 premium UI verifier.");

const page = read("apps/web/src/app/(app)/ticari-zeka/tedarikci-karsilastirmasi/page.tsx");
const view = read("apps/web/src/features/commercial-intelligence/components/commercial-supplier-comparison-workspace-view.tsx");
const supplierRow = read("apps/web/src/features/commercial-intelligence/components/commercial-intelligence-supplier-row.tsx");
const presentation = read("apps/web/src/features/commercial-intelligence/components/commercial-intelligence-presentation.ts");
const labels = read("apps/web/src/features/commercial-intelligence/domain/commercial-intelligence-labels.ts");
const navigation = read("apps/web/src/lib/navigation/primary-navigation.ts");
const catchAll = read("apps/web/src/app/(app)/[...sections]/page.tsx");
const css = read("apps/web/src/features/commercial-intelligence/components/commercial-intelligence-workspace.css");

assert("dedicated supplier comparison page", page.includes('forbidUnlessGranted("commercial-intelligence.read")'));
assert("route excluded from catch-all", catchAll.includes('"/ticari-zeka/tedarikci-karsilastirmasi"'));
assert("navigation permission", navigation.includes('href: "/ticari-zeka/tedarikci-karsilastirmasi"') && navigation.includes('permissionKey: "commercial-intelligence.read"'));
assert("navigation label", navigation.includes('label: "Tedarikçi Karşılaştırması"'));
assert("executive eyebrow", view.includes("TİCARİ ZEKÂ"));
assert("executive title", view.includes("<h1>Tedarikçi Karşılaştırması</h1>"));
assert("six KPI grid", view.includes('data-testid="commercial-supplier-comparison-kpi-grid"'));
assert("three tabs", labels.includes("Genel Bakış") && labels.includes("Tedarikçi Sıralaması") && labels.includes("Karşılaştırma Geçmişi"));
assert("overview ranking panel", view.includes('data-testid="commercial-supplier-comparison-ranking-overview"'));
assert("spread panel", view.includes('data-testid="commercial-supplier-comparison-spread-panel"'));
assert("ranking workspace", view.includes('data-testid="commercial-supplier-comparison-ranking-workspace"'));
assert("history workspace", view.includes('data-testid="commercial-supplier-comparison-history-workspace"'));
assert("evaluation controls", view.includes('data-testid="commercial-supplier-comparison-evaluation-controls"'));
assert("detail ready testid", view.includes('data-testid="commercial-supplier-comparison-detail-ready"'));
assert("readonly note", view.includes('data-testid="commercial-supplier-comparison-readonly-note"'));
assert("provenance strip", view.includes('data-testid="commercial-supplier-comparison-provenance-strip"'));
assert("shared supplier row component", view.includes("CommercialIntelligenceSupplierRow"));
assert("60/40 overview row", css.includes("commint-panel-grid--supplier-comparison") && css.includes("grid-template-columns: minmax(0, 3fr) minmax(0, 2fr)"));
assert("spread meter styles", css.includes(".commint-spread-meter"));
assert("baseline badge", css.includes("commint-badge--baseline"));
assert("comparable suppliers KPI", presentation.includes('"Karşılaştırılabilir tedarikçiler"'));
assert("tied positions KPI", presentation.includes('"Eşit fiyat konumları"'));
assert("attention evidence KPI", presentation.includes('"Dikkat / kanıt sorunu"'));
assert("no recommendation wording", !view.toLowerCase().includes("önerilen tedarikçi") && !view.toLowerCase().includes("tercih edilen tedarikçi"));
assert("baseline wording", supplierRow.includes("Fiyat baz çizgisi"));

console.log("COMMERCIAL-SUPPLIER-COMPARISON-UI-1 premium UI verification passed.");
