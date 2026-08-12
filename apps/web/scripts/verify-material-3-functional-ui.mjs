import { readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const scriptsDirectory = dirname(fileURLToPath(import.meta.url));
const projectRoot = resolve(scriptsDirectory, "../../..");

function fail(message) {
  console.error(`MATERIAL-3 functional UI verification failed: ${message}`);
  process.exit(1);
}

function readRequired(relativePath) {
  return readFileSync(resolve(projectRoot, relativePath), "utf8").replaceAll("\r\n", "\n");
}

function requireText(source, expected, label) {
  if (!source.includes(expected)) fail(`${label} must contain "${expected}"`);
}

function forbidText(source, forbidden, label) {
  if (source.includes(forbidden)) fail(`${label} must not contain "${forbidden}"`);
}

console.log("Running MATERIAL-3 functional UI verifier.");

const registryView = readRequired("apps/web/src/features/materials/components/material-registry-view.tsx");
const panel = readRequired("apps/web/src/features/materials/components/material-requirement-panel.tsx");
const labels = readRequired("apps/web/src/features/materials/presentation/material-labels.ts");
const css = readRequired("apps/web/src/features/materials/components/material-registry.css");

requireText(registryView, "İhtiyaç ve Tüketim", "registry tabs");
requireText(registryView, "Regrind Politikası", "registry tabs");
requireText(registryView, "MaterialRequirementPanel", "requirement panel import");
forbidText(registryView, "primary-navigation", "must not change sidebar");

requireText(panel, "Teorik İhtiyaç", "KPI summary");
requireText(panel, "Gözlenen Tüketim", "KPI summary");
requireText(panel, "Sapma", "KPI summary");
requireText(panel, "Virgin İhtiyacı", "KPI summary");
requireText(panel, "Regrind İhtiyacı", "KPI summary");
requireText(panel, "MATERIAL_REQUIREMENT_THEORETICAL_DISCLAIMER", "theoretical disclaimer");
requireText(panel, "MATERIAL_REQUIREMENT_CONSUMPTION_DISCLAIMER", "consumption disclaimer");
requireText(labels, "Teorik malzeme ihtiyacıdır", "theoretical disclaimer text");
requireText(labels, "Operasyonel tüketim gözlemidir", "consumption disclaimer text");
requireText(panel, "İhtiyaç Profili Oluştur", "manage action");
requireText(panel, "Tüketim Gözlemi Kaydet", "manage action");
requireText(panel, "Değerlendir", "evaluate action");
requireText(panel, "canManage", "manage gate");
requireText(panel, "insufficient-data", "insufficient data state");
requireText(panel, "—", "unavailable value placeholder");

requireText(labels, "production-order", "requirement type label");
requireText(labels, "consumption-observed", "observation type label");
requireText(labels, "compliant", "regrind status label");

requireText(css, "material-requirement", "requirement CSS");

console.log("MATERIAL-3 functional UI verification passed.");
