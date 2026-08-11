#!/usr/bin/env node
import { existsSync, readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), "../../..");

function read(rel) {
  const abs = resolve(ROOT, rel);
  if (!existsSync(abs)) throw new Error(`Missing ${rel}`);
  return readFileSync(abs, "utf8");
}

function assert(label, ok, detail = "") {
  if (!ok) {
    console.error(`✗ ${label}${detail ? `: ${detail}` : ""}`);
    process.exit(1);
  }
  console.log(`✓ ${label}`);
}

const view = read("apps/web/src/features/maintenance/components/maintenance-condition-view.tsx");
const labels = read("apps/web/src/features/maintenance/presentation/maintenance-predictive-risk-labels.ts");
const css = read("apps/web/src/features/maintenance/components/maintenance-workspace.css");

assert("risk section title", view.includes("Bakım Riski ve Öneriler"));
assert("primary page identity", view.includes("<h1>Kestirici Bakım</h1>"));
assert("risk KPI Yetersiz Veri", view.includes('label="Yetersiz Veri"'));
assert("risk KPI Düşük Risk", view.includes('label="Düşük Risk"'));
assert("risk KPI Yüksek Risk", view.includes('label="Yüksek Risk"'));
assert("risk KPI Kritik Risk", view.includes('label="Kritik Risk"'));
assert("recommendation UI", view.includes("Bakım Önerisi"));
assert("evidence UI", view.includes("Destekleyici Kanıtlar"));
assert("driver UI", view.includes("Ana Risk Sürücüleri"));
assert("history summary UI", view.includes("maintenanceHistorySummary"));
assert("safety copy", labels.includes("otomatik bakım talebi veya bakım emri oluşturmaz"));
assert("simulator honesty", labels.includes("gerçek makine doğrulaması değildir"));
assert("Turkish risk labels", labels.includes("Kritik Risk"));
assert("Turkish recommendation labels", labels.includes("Teknik İnceleme Planla"));
assert("responsive risk grid", css.includes(".maintenance-predictive-risk-kpis"));
assert("no failure probability wording", !view.toLowerCase().includes("arıza olasılığı"));
assert("no RUL wording", !view.toLowerCase().includes("remaining useful life"));
assert("no raw UUID render", !view.includes("{assessment.id}") && !view.includes("{profile?.machineId}"));

console.log("MAINT-PRED-2 premium UI verifier PASS");
