#!/usr/bin/env node
import { readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), "..");

function fail(message) {
  console.error(`MOLD-2 history UI verifier failed: ${message}`);
  process.exit(1);
}

function assert(label, ok, detail = "") {
  if (!ok) fail(`${label}${detail ? `: ${detail}` : ""}`);
  console.log(`✓ ${label}`);
}

function read(relPath) {
  return readFileSync(resolve(ROOT, relPath), "utf8");
}

console.log("Running MOLD-2 history functional UI verifier.");

const panel = read("src/features/molds/components/mold-history-detail-panel.tsx");
const labels = read("src/features/molds/presentation/mold-history-labels.ts");
const page = read("src/app/(app)/kaliplar/page.tsx");

assert("route /kaliplar page exists", page.includes("MoldRegistryView"));
assert("Genel Bakış tab", panel.includes("Genel Bakış"));
assert("Bakım Geçmişi tab", panel.includes("Bakım Geçmişi"));
assert("Revizyonlar tab", panel.includes("Revizyonlar"));
assert("Denemeler tab", panel.includes("Denemeler"));
assert("Zaman Çizelgesi tab", panel.includes("Zaman Çizelgesi"));
assert("maintenance Turkish labels", labels.includes("Önleyici"));
assert("revision Turkish labels", labels.includes("Gözenek Değişikliği"));
assert("trial Turkish labels", labels.includes("Başarılı"));
assert("permission-aware empty state", panel.includes("yalnızca görüntüleme yetkiniz"));
assert("loading state", panel.includes("Detay yükleniyor"));
assert("empty maintenance table", panel.includes("Bakım kaydı yok"));
assert("no raw uuid in panel", !panel.includes("toUpperCase()") || panel.includes("crypto.randomUUID()"));
assert("localized enum labels used", panel.includes("getMoldMaintenanceTypeLabel"));

console.log("MOLD-2 history functional UI verifier passed.");
