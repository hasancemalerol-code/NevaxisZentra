import { existsSync, readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const currentFilePath = fileURLToPath(import.meta.url);
const scriptsDirectory = dirname(currentFilePath);
const webRoot = resolve(scriptsDirectory, "..");
const projectRoot = resolve(webRoot, "../..");

function fail(message) {
  console.error(`Quality UI-2 subpages verification failed: ${message}`);
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

console.log("Running quality UI-2 subpages verifier.");

const shell = readRequired("apps/web/src/features/quality-control/components/quality-page-shell.tsx");
const backLink = readRequired("apps/web/src/features/quality-control/components/quality-back-link.tsx");
const ncrFilters = readRequired("apps/web/src/features/quality-control/components/quality-ncr-filters.tsx");
const workspaceCss = readRequired("apps/web/src/features/quality-control/components/quality-workspace.css");
const ncrListPage = readRequired("apps/web/src/app/(app)/kalite/ncr/page.tsx");
const ncrDetailPage = readRequired("apps/web/src/app/(app)/kalite/ncr/[ncrId]/page.tsx");
const reportingPage = readRequired("apps/web/src/app/(app)/kalite/raporlama/page.tsx");
const inspectionPage = readRequired("apps/web/src/app/(app)/kalite/kontroller/[inspectionId]/page.tsx");
const summaryPage = readRequired("apps/web/src/app/(app)/kalite/kontroller/[inspectionId]/ozet/page.tsx");
const inspectionCreatePage = readRequired("apps/web/src/app/(app)/kalite/kontroller/yeni/page.tsx");
const planCreatePage = readRequired("apps/web/src/app/(app)/kalite/planlar/yeni/page.tsx");
const inspectionForm = readRequired(
  "apps/web/src/features/quality-control/components/new-quality-inspection-form.tsx",
);
const planForm = readRequired("apps/web/src/features/quality-control/components/quality-plan-form.tsx");
const ncrActions = readRequired("apps/web/src/features/quality-control/components/quality-ncr-actions.tsx");
const inspectionActions = readRequired(
  "apps/web/src/features/quality-control/components/quality-inspection-actions.tsx",
);
const nav = readRequired("apps/web/src/lib/navigation/primary-navigation.ts");
const globalsCss = readRequired("apps/web/src/app/globals.css");
const packageJson = readRequired("apps/web/package.json");

requireText(shell, "QualityBackLink", "quality page shell");
requireText(shell, "QualityWorkspaceNav", "quality page shell");
requireText(backLink, "quality-back-link", "quality back link");

requireText(ncrFilters, "quality-filter-panel", "NCR filters");
requireText(ncrFilters, "quality-form-select", "NCR filters");
requireText(ncrFilters, "quality-form-input", "NCR filters");
requireText(ncrFilters, "Temizle", "NCR filters");

requireText(workspaceCss, ".quality-table", "quality workspace css");
requireText(workspaceCss, ".quality-form-select", "quality workspace css");
requireText(workspaceCss, ".quality-filter-panel", "quality workspace css");
requireText(workspaceCss, ".quality-empty-state", "quality workspace css");

requireText(ncrListPage, "QualityPageShell", "NCR list page");
requireText(ncrListPage, "QualityNcrFilters", "NCR list page");
requireText(ncrListPage, "quality-table", "NCR list page");
requireText(ncrListPage, 'nav="ncr"', "NCR list page");
forbidText(ncrListPage, "production-execution-list", "NCR list page");
forbidText(ncrListPage, '<select name="status"', "NCR list page");

requireText(ncrDetailPage, "NCR Özeti", "NCR detail page");
requireText(ncrDetailPage, "Bağlı Üretim", "NCR detail page");
requireText(ncrDetailPage, "Düzeltici Aksiyonlar", "NCR detail page");
requireText(ncrDetailPage, "Kalite Blokesi: Aktif", "NCR detail page");
forbidText(ncrDetailPage, "production-execution-list", "NCR detail page");

requireText(reportingPage, "QualityPageShell", "reporting page");
requireText(reportingPage, "quality-table", "reporting page");
requireText(reportingPage, "quality-reporting-kpis", "reporting page");
requireText(reportingPage, "salt okunur", "reporting page");

requireText(inspectionPage, "QualityPageShell", "inspection detail page");
requireText(inspectionPage, "quality-table", "inspection detail page");
requireText(inspectionPage, "Ölçüm / Kontrol Listesi", "inspection detail page");
requireText(inspectionPage, "QualityInspectionActions", "inspection detail page");

requireText(summaryPage, "Kalite Kontrol Özeti", "inspection summary page");
requireText(summaryPage, "Ölçüm Sonuçları", "inspection summary page");
requireText(summaryPage, "Release Durumu", "inspection summary page");
requireText(summaryPage, "quality-table", "inspection summary page");

requireText(inspectionCreatePage, "QualityPageShell", "inspection create page");
requireText(inspectionForm, "quality-form-select", "inspection create form");
requireText(inspectionForm, "Kontrolü Başlat", "inspection create form");

requireText(planCreatePage, "QualityPageShell", "plan create page");
requireText(planForm, "Kontrol Noktaları", "plan create form");
requireText(planForm, "quality-table", "plan create form");

requireText(ncrActions, "quality-form-select", "NCR actions");
requireText(ncrActions, "Kalite Blokesi Koy", "NCR actions");
requireText(inspectionActions, "quality-measurement-row", "inspection actions");
requireText(inspectionActions, "quality-form-input", "inspection actions");

forbidText(ncrActions, "employeeScore", "NCR actions");
forbidText(inspectionActions, "employeeScore", "inspection actions");
forbidText(inspectionForm, "OpenAI", "inspection create form");

requireText(nav, 'href: "/kalite"', "primary navigation");
forbidText(globalsCss, "quality-page", "globals.css");
forbidText(packageJson, '"chart.js"', "web package.json");
forbidText(packageJson, '"recharts"', "web package.json");

console.log("Quality UI-2 subpages verification passed.");
