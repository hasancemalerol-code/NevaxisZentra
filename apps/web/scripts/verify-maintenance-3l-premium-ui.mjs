import { existsSync, readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const currentFilePath = fileURLToPath(import.meta.url);
const scriptsDirectory = dirname(currentFilePath);
const webRoot = resolve(scriptsDirectory, "..");
const projectRoot = resolve(webRoot, "../..");

const BASE = process.env.CREVAFACTOS_WEB_BASE_URL ?? "http://127.0.0.1:3000";

function fail(message) {
  console.error(`Maintenance 3L premium UI verification failed: ${message}`);
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

console.log("Running maintenance 3L premium UI verifier.");

const reportingPage = readRequired("apps/web/src/app/(app)/bakim/raporlama/page.tsx");
const reportingView = readRequired(
  "apps/web/src/features/maintenance/components/maintenance-reporting-view.tsx",
);
const nav = readRequired("apps/web/src/features/maintenance/components/maintenance-workspace-nav.tsx");
const assetPage = readRequired("apps/web/src/app/(app)/bakim/varliklar/[assetReference]/page.tsx");
const labels = readRequired("apps/web/src/features/maintenance/presentation/maintenance-labels.ts");

requireText(reportingPage, "Bakım Güvenilirlik ve Analiz", "reporting page title");
requireText(reportingView, "Tekrarlayan Arızalar", "repeat failures panel");
requireText(reportingView, "MTBF: Veri yetersiz", "MTBF unavailable UI");
requireText(reportingView, "Güvenilir çalışma süresi kaynağı henüz bağlı değil", "MTBF explanation");
requireText(reportingView, "Arıza Dağılımı", "failure breakdown panel");
requireText(reportingView, "Bakım Backlog Yaşlandırma", "backlog aging panel");
requireText(nav, 'href: "/bakim/raporlama"', "workspace nav reporting link");
requireText(nav, 'label: "Raporlama"', "workspace nav reporting label");
requireText(assetPage, "Güvenilirlik", "asset reliability section");
requireText(labels, "getMaintenanceConditionStatusLabel", "condition status labels");

forbidText(reportingView, "predictive maintenance", "reporting view");
forbidText(reportingView, "kalan ömür", "reporting view");
forbidText(reportingView, "recharts", "reporting view");
forbidText(reportingView, "chart.js", "reporting view");

console.log("Running maintenance 3L premium UI live checks.");

try {
  const bakimStatus = await fetch(`${BASE}/bakim`).then((response) => response.status);
  if (bakimStatus !== 200) {
    fail(`/bakim must return HTTP 200, got ${bakimStatus}.`);
  }

  const reportingStatus = await fetch(`${BASE}/bakim/raporlama`).then((response) => response.status);
  if (reportingStatus !== 200) {
    fail(`/bakim/raporlama must return HTTP 200, got ${reportingStatus}.`);
  }

  const assetStatus = await fetch(`${BASE}/bakim/varliklar/IM-120`).then((response) => response.status);
  if (assetStatus !== 200) {
    fail(`asset reliability page must return HTTP 200, got ${assetStatus}.`);
  }

  const reportingHtml = await fetch(`${BASE}/bakim/raporlama`).then((response) => response.text());
  if (!reportingHtml.includes("Bakım Güvenilirlik ve Analiz")) {
    fail("reporting page must render title.");
  }
  if (!reportingHtml.includes("Tekrarlayan Arızalar")) {
    fail("reporting page must render repeat failures section.");
  }
} catch (error) {
  fail(`live UI checks failed: ${error instanceof Error ? error.message : String(error)}`);
}

console.log("Maintenance 3L premium UI verifier passed.");
