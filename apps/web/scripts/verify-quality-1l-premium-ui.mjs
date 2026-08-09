import { existsSync, readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const currentFilePath = fileURLToPath(import.meta.url);
const scriptsDirectory = dirname(currentFilePath);
const webRoot = resolve(scriptsDirectory, "..");
const projectRoot = resolve(webRoot, "../..");

function fail(message) {
  console.error(`Quality 1L premium UI verification failed: ${message}`);
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

console.log("Running quality 1L premium UI verifier.");

const mainPage = readRequired("apps/web/src/app/(app)/kalite/page.tsx");
const dashboardView = readRequired(
  "apps/web/src/features/quality-control/components/quality-dashboard-view.tsx",
);
const mainUi = `${mainPage}\n${dashboardView}`;
const planPage = readRequired("apps/web/src/app/(app)/kalite/planlar/yeni/page.tsx");
const inspectionPage = readRequired("apps/web/src/app/(app)/kalite/kontroller/[inspectionId]/page.tsx");
const nav = readRequired("apps/web/src/lib/navigation/primary-navigation.ts");

requireText(mainUi, "production-execution-page", "kalite main page");
requireText(mainUi, "production-execution-kpis", "kalite main page");
requireText(mainUi, "İnceleme Bekleyenler", "kalite main page");
requireText(mainUi, "Başarısız Kontroller", "kalite main page");
requireText(mainUi, "Serbest Bırakılanlar", "kalite main page");
requireText(mainUi, "Kontrol Planları", "kalite main page");
requireText(mainUi, "Aktif Kontroller", "kalite main page");
requireText(mainUi, "Son Kontroller", "kalite main page");

requireText(planPage, "QualityPageShell", "plan page");
requireText(planPage, "Yeni Kontrol Planı", "plan page");
requireText(planPage, "QualityPlanForm", "plan page");

requireText(inspectionPage, "QualityPageShell", "inspection page");
requireText(inspectionPage, "Üretim Bağlamı", "inspection page");
requireText(inspectionPage, "salt okunur", "inspection page");
requireText(inspectionPage, "quality-table", "inspection page");

requireText(nav, 'href: "/kalite"', "primary navigation");

forbidText(mainUi, "globals.css", "kalite main page");

console.log("Quality 1L premium UI verification passed.");
