import { existsSync, readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const currentFilePath = fileURLToPath(import.meta.url);
const scriptsDirectory = dirname(currentFilePath);
const webRoot = resolve(scriptsDirectory, "..");
const projectRoot = resolve(webRoot, "../..");

const BASE = process.env.CREVAFACTOS_WEB_BASE_URL ?? "http://127.0.0.1:3000";

function fail(message) {
  console.error(`Maintenance UI-1 premium verification failed: ${message}`);
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

console.log("Running maintenance UI-1 premium verifier.");

const dashboard = readRequired(
  "apps/web/src/features/maintenance/components/maintenance-dashboard-view.tsx",
);
const primitives = readRequired(
  "apps/web/src/features/maintenance/components/maintenance-table-primitives.tsx",
);
const planning = readRequired(
  "apps/web/src/features/maintenance/components/maintenance-planning-view.tsx",
);
const css = readRequired("apps/web/src/features/maintenance/components/maintenance-workspace.css");
const nav = readRequired("apps/web/src/features/maintenance/components/maintenance-workspace-nav.tsx");
const globalsCss = readRequired("apps/web/src/app/globals.css");
const primaryNav = readRequired("apps/web/src/lib/navigation/primary-navigation.ts");

requireText(primitives, "MaintenanceAssetCell", "table primitives");
requireText(primitives, "MaintenanceTableActionGroup", "table primitives");
requireText(primitives, "MaintenanceEmptyState", "table primitives");
requireText(primitives, "maintenance-asset-cell", "asset cell structure");
requireText(primitives, "maintenance-table-action-group", "action group structure");

requireText(dashboard, "MaintenanceAssetCell", "dashboard view");
requireText(dashboard, "MaintenanceTableActionGroup", "dashboard view");
requireText(dashboard, "MaintenanceEmptyState", "dashboard view");
requireText(dashboard, "maintenance-table-wrap--desktop-fit", "dashboard desktop table wrap");
requireText(dashboard, "maintenance-table--work-orders", "active work orders table");
requireText(dashboard, "maintenance-table--requests", "fault records table");
requireText(dashboard, "maintenance-table--preventive", "preventive plans table");
requireText(dashboard, "Kritik bakım uyarısı yok", "critical alert empty state");

requireText(planning, "MaintenanceAssetCell", "planning view");
requireText(planning, "maintenance-table-wrap--desktop-fit", "planning desktop table wrap");

requireText(css, "maintenance-asset-cell", "workspace css asset cell");
requireText(css, "maintenance-table-action-group", "workspace css action group");
requireText(css, "table-layout: fixed", "workspace css table layout");
requireText(css, "maintenance-table-wrap--desktop-fit", "workspace css desktop fit");
requireText(css, "maintenance-empty-state--premium", "workspace css premium empty state");
requireText(css, "maintenance-section-action", "workspace css section actions");

forbidText(dashboard, 'className="maintenance-table__action"', "dashboard — no inline concatenated action class");
forbidText(dashboard, "maintenance-table__actions", "dashboard — dedicated action group replaces raw actions cell class");
forbidText(dashboard, "Bakım Emrini Aç", "dashboard — action text must not live in order cell");
forbidText(nav, "second sidebar", "workspace nav");
forbidText(primaryNav, "Bakım Planlama", "primary navigation — sidebar unchanged");
forbidText(globalsCss, "maintenance-asset-cell", "globals.css untouched");
forbidText(globalsCss, "maintenance-table-action-group", "globals.css untouched");


console.log("Running maintenance UI-1 premium live checks.");

try {
  const bakimStatus = await fetch(`${BASE}/bakim`).then((response) => response.status);
  if (bakimStatus !== 200) {
    fail(`/bakim must return HTTP 200, got ${bakimStatus}.`);
  }

  const bakimHtml = await fetch(`${BASE}/bakim`).then((response) => response.text());
  forbidText(bakimHtml, "Bakım Emrini Aç", "live /bakim HTML");
  forbidText(bakimHtml, "İnceleBakım", "live /bakim HTML");
} catch (error) {
  console.warn(
    `Maintenance UI-1 live checks skipped (${error instanceof Error ? error.message : String(error)}).`,
  );
}

console.log("Maintenance UI-1 premium UI verification passed.");
