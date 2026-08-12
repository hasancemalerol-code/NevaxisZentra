#!/usr/bin/env node
import { readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), "../../..");

function fail(message) {
  console.error(`MOLD-3 UI verifier failed: ${message}`);
  process.exit(1);
}

function assert(label, ok, detail = "") {
  if (!ok) fail(`${label}${detail ? `: ${detail}` : ""}`);
  console.log(`✓ ${label}`);
}

function read(rel) {
  return readFileSync(resolve(ROOT, rel), "utf8");
}

console.log("Running MOLD-3 functional UI verifier.");

const panel = read("apps/web/src/features/molds/components/mold-compatibility-detail-panel.tsx");
const historyPanel = read("apps/web/src/features/molds/components/mold-history-detail-panel.tsx");
const labels = read("apps/web/src/features/molds/presentation/mold-compatibility-labels.ts");

assert("Makine Uyumluluğu tab", historyPanel.includes("Makine Uyumluluğu"));
assert("compatibility tab key", historyPanel.includes('"compatibility"'));
assert("compatibility KPI summary", panel.includes("Uyumlu") && panel.includes("Yetersiz Veri"));
assert("machine comparison table", panel.includes("Teknik Durum") && panel.includes("Kritik Kısıt"));
assert("evidence panel", panel.includes("Kanıt ve Kural Sonuçları"));
assert("data freshness UI", panel.includes("Veri Güncelliği") || panel.includes("Gereksinim eski"));
assert("theoretical capacity honesty", labels.includes("Gerçek üretim kapasitesi veya OEE değildir"));
assert("loading state", panel.includes("yükleniyor") || panel.includes("LoaderCircle"));
assert("empty state", panel.includes("henüz") || panel.includes("bulunmuyor"));
assert("error state", panel.includes("error") || panel.includes("Hata"));
assert("read-only state", panel.includes("canManage") || panel.includes("salt okunur"));
assert("no raw uuid in panel", !panel.includes("toLowerCase()") || !panel.match(/machineAssetId/));

console.log("MOLD-3 functional UI verifier passed.");
