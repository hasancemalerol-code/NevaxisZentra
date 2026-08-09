import { existsSync, readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const currentFilePath = fileURLToPath(import.meta.url);
const scriptsDirectory = dirname(currentFilePath);
const webRoot = resolve(scriptsDirectory, "..");
const projectRoot = resolve(webRoot, "../..");

function fail(message) {
  console.error(`Maintenance 1L premium UI verification failed: ${message}`);
  process.exit(1);
}

function readRequired(relativePath) {
  const absolutePath = resolve(projectRoot, relativePath);
  if (!existsSync(absolutePath)) {
    fail(`Required file is missing: ${relativePath}`);
  }
  return readFileSync(absolutePath, "utf8").replaceAll("\r\n", "\n");
}

function requireText(source, expected, label) {
  if (!source.includes(expected)) {
    fail(`${label} must contain "${expected}".`);
  }
}

function forbidText(source, forbidden, label) {
  if (source.includes(forbidden)) {
    fail(`${label} must not contain "${forbidden}".`);
  }
}

console.log("Running maintenance 1L premium UI verifier.");

const dashboardView = readRequired(
  "apps/web/src/features/maintenance/components/maintenance-dashboard-view.tsx",
);
const kpi = readRequired(
  "apps/web/src/features/maintenance/components/maintenance-dashboard-kpi.tsx",
);
const css = readRequired("apps/web/src/features/maintenance/components/maintenance-workspace.css");
const detailPage = readRequired("apps/web/src/app/(app)/bakim/emirler/[id]/page.tsx");
const nav = readRequired("apps/web/src/lib/navigation/primary-navigation.ts");
const kpiUi = `${dashboardView}\n${kpi}`;

requireText(dashboardView, "Bakım &amp; Arıza", "dashboard view");
requireText(dashboardView, "Makine, kalıp ve yardımcı ekipman", "dashboard view");
requireText(dashboardView, "Yeni Arıza Kaydı", "dashboard view");
requireText(dashboardView, "Yeni Bakım Emri", "dashboard view");
requireText(dashboardView, "Periyodik Bakım Planı", "dashboard view");
requireText(dashboardView, "Aktif Bakım İşleri", "dashboard view");
requireText(dashboardView, "Kritik Arıza ve Bakım Uyarıları", "dashboard view");
requireText(dashboardView, "Bakım Emrini Aç", "dashboard view");
requireText(dashboardView, "Bakım Emrine Dönüştür", "dashboard view");

requireText(kpiUi, "Açık Arızalar", "KPI");
requireText(kpiUi, "Açık Bakım Emirleri", "KPI");
requireText(kpiUi, "Kritik Bakımlar", "KPI");
requireText(kpiUi, "Devam Eden", "KPI");
requireText(kpiUi, "Bekleyen", "KPI");
requireText(kpiUi, "Geciken Periyodik Bakım", "KPI");

requireText(css, "maintenance-dashboard", "workspace css");
requireText(css, "#155eef", "workspace css");
requireText(detailPage, "Bakım Emri Özeti", "detail page");
requireText(detailPage, "Atanan Teknisyen", "detail page");
requireText(detailPage, "Aktivite", "detail page");

requireText(nav, '"/bakim"', "navigation");
forbidText(nav, "Bakım & Arıza", "navigation — sidebar label unchanged");

forbidText(dashboardView, "globals.css", "dashboard view");

console.log("Maintenance 1L premium UI verification passed.");
