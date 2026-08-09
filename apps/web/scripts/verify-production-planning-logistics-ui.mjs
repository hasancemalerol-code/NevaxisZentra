import { existsSync, readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const currentFilePath = fileURLToPath(import.meta.url);
const scriptsDirectory = dirname(currentFilePath);
const webRoot = resolve(scriptsDirectory, "..");
const projectRoot = resolve(webRoot, "../..");

function fail(message) {
  console.error(
    `Production planning logistics UI verification failed: ${message}`,
  );
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

const listPage = readRequired("apps/web/src/app/(app)/planlama/page.tsx");
requireText(listPage, "ProductionPlanningLogisticsSection", "planlama page");
requireText(listPage, "readPlanningLogisticsSummaries", "planlama page");

const detailPage = readRequired(
  "apps/web/src/app/(app)/planlama/[planId]/page.tsx",
);
requireText(detailPage, "ProductionPlanLogisticsPanel", "plan detail page");

const panel = readRequired(
  "apps/web/src/features/production-planning/components/production-plan-logistics-panel.tsx",
);
requireText(panel, "Lojistik Kısıtları", "logistics panel");
requireText(panel, "İhracat / Sevkiyat", "logistics panel");
requireText(panel, "Export/Shipment", "logistics panel");
forbidText(panel, "PlannedPickupAtUtc", "logistics panel");
forbidText(panel, "type=\"date\"", "logistics panel");

const section = readRequired(
  "apps/web/src/features/production-planning/components/production-planning-logistics-section.tsx",
);
requireText(section, "Lojistik Kısıtları", "logistics section");

const card = readRequired(
  "apps/web/src/features/production-planning/components/production-plan-card.tsx",
);
requireText(card, "logisticsSummary", "production plan card");

console.log("Production planning logistics UI verification passed.");
