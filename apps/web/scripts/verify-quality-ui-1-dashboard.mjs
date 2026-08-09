import { existsSync, readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const currentFilePath = fileURLToPath(import.meta.url);
const scriptsDirectory = dirname(currentFilePath);
const webRoot = resolve(scriptsDirectory, "..");
const projectRoot = resolve(webRoot, "../..");

function fail(message) {
  console.error(`Quality UI-1 dashboard verification failed: ${message}`);
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

console.log("Running quality UI-1 dashboard verifier.");

const mainPage = readRequired("apps/web/src/app/(app)/kalite/page.tsx");
const dashboardView = readRequired(
  "apps/web/src/features/quality-control/components/quality-dashboard-view.tsx",
);
const dashboardLabels = readRequired(
  "apps/web/src/features/quality-control/presentation/quality-dashboard-labels.ts",
);
const reader = readRequired("apps/web/src/features/quality-control/server/quality-control-reader.ts");
const nav = readRequired("apps/web/src/lib/navigation/primary-navigation.ts");
const globalsCss = readRequired("apps/web/src/app/globals.css");
const packageJson = readRequired("apps/web/package.json");

requireText(mainPage, "readQualityDashboard", "kalite main page");
requireText(mainPage, "QualityDashboardView", "kalite main page");
forbidText(mainPage, "production-execution-list", "kalite main page");
forbidText(dashboardView, "<ul className=\"production-execution-list\"", "quality dashboard view");

requireText(dashboardView, "Açık Kontroller", "quality dashboard view");
requireText(dashboardView, "İnceleme Bekleyenler", "quality dashboard view");
requireText(dashboardView, "Başarısız Kontroller", "quality dashboard view");
requireText(dashboardView, "Açık NCR", "quality dashboard view");
requireText(dashboardView, "Aktif Kalite Blokeleri", "quality dashboard view");
requireText(dashboardView, "Aksiyon Bekleyenler", "quality dashboard view");

requireText(dashboardView, "Aktif Kalite Kontrolleri", "quality dashboard view");
requireText(dashboardView, "Kritik Kalite Uyarıları", "quality dashboard view");
requireText(dashboardView, "Açık NCR", "quality dashboard view");
requireText(dashboardView, "Aktif Kalite Blokeleri", "quality dashboard view");
requireText(dashboardView, "Düzeltici Aksiyonlar", "quality dashboard view");
requireText(dashboardView, "Kontrol Planları", "quality dashboard view");
requireText(dashboardView, "Hızlı İşlemler", "quality dashboard view");
requireText(dashboardView, "/kalite/raporlama", "quality dashboard view");
requireText(dashboardView, "quality-inspection-row", "quality dashboard view");
requireText(dashboardView, "quality-dashboard-table", "quality dashboard view");
requireText(dashboardView, "Kalite Blokesi: Aktif", "quality dashboard view");

requireText(dashboardView, "QualityWorkspaceNav", "quality dashboard view");
requireText(dashboardView, "Kontrolü Aç", "quality dashboard view");
requireText(dashboardView, "getQualityReleaseStatusLabel", "quality dashboard view");

requireText(reader, "readQualityDashboard", "quality-control reader");
forbidText(reader, "employeeScore", "quality-control reader");
forbidText(dashboardLabels, "employeeScore", "quality dashboard labels");

requireText(nav, 'href: "/kalite"', "primary navigation");
forbidText(nav, 'href: "/kalite/raporlama"', "primary navigation");

forbidText(mainPage, "globals.css", "kalite main page");
forbidText(dashboardView, "globals.css", "quality dashboard view");

const migrationGlob = [
  "packages/CrevaFactOS.Persistence.Migrations/Migrations",
];
for (const dir of migrationGlob) {
  const path = resolve(projectRoot, dir);
  if (!existsSync(path)) {
    continue;
  }
}

forbidText(packageJson, '"chart.js"', "web package.json");
forbidText(packageJson, '"recharts"', "web package.json");

console.log("Quality UI-1 dashboard verification passed.");
