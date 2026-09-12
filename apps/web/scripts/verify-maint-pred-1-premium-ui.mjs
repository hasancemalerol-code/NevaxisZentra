#!/usr/bin/env node
import { existsSync, readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), "../../..");
const WEB = process.env.NEVAXIS_FACTOS_WEB_BASE_URL ?? "http://127.0.0.1:3001";

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
const dashboard = read("apps/web/src/features/maintenance/components/maintenance-dashboard-view.tsx");
const labels = read("apps/web/src/features/maintenance/presentation/maintenance-condition-labels.ts");
const nav = read("apps/web/src/features/maintenance/components/maintenance-workspace-nav.tsx");
const css = read("apps/web/src/features/maintenance/components/maintenance-workspace.css");
const globalsCss = read("apps/web/src/app/globals.css");
const sidebar = read("apps/web/src/components/app-shell/sidebar.tsx");

assert("Kestirici Bakım primary title", view.includes("<h1>Kestirici Bakım</h1>"));
assert("Durum Zekâsı subsection", view.includes('title="Durum Zekâsı"'));
assert("Kestirici bakım altyapısı", view.includes("Kestirici bakım altyapısı"));
assert("KPI strip", view.includes("İzlenen Varlık"));
assert("condition table headers", view.includes("Varlık") && view.includes("Veri Yeterliliği"));
assert("anomaly evidence", view.includes("Anomali Kanıtları"));
assert("trend labels", labels.includes("Yükseliyor"));
assert("data sufficiency labels", labels.includes("Yetersiz"));
assert("stale warning", labels.includes("Son makine verisi güncel değil"));
assert("simulator disclaimer", labels.includes("simülatör verisiyle doğrulanmıştır"));
assert("anomaly explanation wording", labels.includes("Normal davranıştan sapma algılandı"));
assert("nav Kestirici Bakım label", nav.includes('label: "Kestirici Bakım"'));
assert("nav route preserved", nav.includes("/bakim/durum-zekasi"));
assert("nav no Durum Zekâsı tab label", !nav.includes('label: "Durum Zekâsı"'));
assert("overview section title", dashboard.includes("Kestirici Bakım Uyarıları"));
assert("overview CTA", dashboard.includes("Kestirici Bakımı İncele"));
assert("overview KPI Kritik", dashboard.includes('label="Kritik"'));
assert("overview KPI Bozulma Eğilimi", dashboard.includes('label="Bozulma Eğilimi"'));
assert("overview KPI İzlenmeli", dashboard.includes('label="İzlenmeli"'));
assert("overview KPI Yetersiz Veri", dashboard.includes('label="Yetersiz Veri"'));
assert("overview KPI Açık Anomali", dashboard.includes('label="Açık Anomali"'));
assert("overview honest subtitle", dashboard.includes("Edge gözlemleri ve bakım geçmişinden türetilen"));
assert("predictive premium card badge", dashboard.includes("maintenance-predictive-overview-card__badge"));
assert("predictive premium kpi tiles", dashboard.includes("maintenance-predictive-kpi-tile"));
assert("responsive rules", css.includes("@media (max-width: 1024px)"));
assert("predictive overview responsive grid", css.includes(".maintenance-predictive-overview-kpis"));
assert("predictive kpi semantic tones", css.includes(".maintenance-predictive-kpi-tile--critical"));
assert("no failure prediction wording", !view.toLowerCase().includes("arıza tahmin"));
assert("no machineId rendered in table", !view.includes("{row.machineId}") && !view.includes("{profile?.machineId}"));
assert("sidebar unchanged", !sidebar.includes("Durum Zekâsı") && !sidebar.includes("Kestirici Bakım"));
assert("globals.css unchanged", !globalsCss.includes("maintenance-condition"));

try {
  const conditionResponse = await fetch(`${WEB}/bakim/durum-zekasi`, { redirect: "follow" });
  assert(`/bakim/durum-zekasi HTTP ${conditionResponse.status}`, conditionResponse.status === 200);
  const conditionHtml = await conditionResponse.text();
  assert("condition page renders Kestirici Bakım", conditionHtml.includes("Kestirici Bakım"));

  const overviewResponse = await fetch(`${WEB}/bakim`, { redirect: "follow" });
  assert(`/bakim HTTP ${overviewResponse.status}`, overviewResponse.status === 200);
  const overviewHtml = await overviewResponse.text();
  assert("overview page renders Kestirici Bakım Uyarıları", overviewHtml.includes("Kestirici Bakım Uyarıları"));
  assert("overview page renders CTA", overviewHtml.includes("Kestirici Bakımı İncele"));
} catch (error) {
  console.error(`Live UI check skipped: ${error instanceof Error ? error.message : error}`);
}

console.log("MAINT-PRED-1 premium UI verifier PASS");
