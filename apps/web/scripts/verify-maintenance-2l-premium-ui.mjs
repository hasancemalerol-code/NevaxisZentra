import { existsSync, readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const currentFilePath = fileURLToPath(import.meta.url);
const scriptsDirectory = dirname(currentFilePath);
const webRoot = resolve(scriptsDirectory, "..");
const projectRoot = resolve(webRoot, "../..");

function fail(message) {
  console.error(`Maintenance 2L premium UI verification failed: ${message}`);
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

console.log("Running maintenance 2L premium UI verifier.");

const nav = readRequired("apps/web/src/features/maintenance/components/maintenance-workspace-nav.tsx");
const executionPanels = readRequired(
  "apps/web/src/features/maintenance/components/maintenance-work-order-execution-panels.tsx",
);
const planningView = readRequired(
  "apps/web/src/features/maintenance/components/maintenance-planning-view.tsx",
);
const planningPage = readRequired("apps/web/src/app/(app)/bakim/planlama/page.tsx");
const detailPage = readRequired("apps/web/src/app/(app)/bakim/emirler/[id]/page.tsx");
const assetPage = readRequired("apps/web/src/app/(app)/bakim/varliklar/[assetReference]/page.tsx");
const css = readRequired("apps/web/src/features/maintenance/components/maintenance-workspace.css");
const primaryNav = readRequired("apps/web/src/lib/navigation/primary-navigation.ts");

requireText(nav, "Genel Bakış", "workspace nav");
requireText(nav, "Bakım İşleri", "workspace nav");
requireText(nav, "Arıza Kayıtları", "workspace nav");
requireText(nav, "Periyodik Bakım", "workspace nav");
requireText(nav, "Bakım Planlama", "workspace nav");
requireText(nav, "/bakim/planlama", "workspace nav");

requireText(executionPanels, "maintenance-section", "execution panels");
requireText(executionPanels, "Not Ekle", "execution panels");
requireText(executionPanels, "Parça Kullanımı Ekle", "execution panels");
requireText(executionPanels, "Periyodik Bakım Kaynağı", "execution panels");

requireText(planningView, "Sonraki", "planning view");
requireText(planningView, "Bakım Emri", "planning view");
requireText(planningPage, "Bakım Planlama", "planning page");

requireText(detailPage, "MaintenanceWorkOrderNotesPanel", "detail page");
requireText(detailPage, "MaintenanceWorkOrderPartsPanel", "detail page");
requireText(assetPage, "Varlık Bakım Geçmişi", "asset history page");

requireText(executionPanels, "maintenance-section", "execution panels");
requireText(css, "maintenance-section", "workspace css");

forbidText(primaryNav, "Bakım Planlama", "primary navigation — sidebar unchanged");
forbidText(planningPage, "globals.css", "planning page");

console.log("Maintenance 2L premium UI verification passed.");
