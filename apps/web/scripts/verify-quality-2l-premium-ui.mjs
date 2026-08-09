import { existsSync, readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const currentFilePath = fileURLToPath(import.meta.url);
const scriptsDirectory = dirname(currentFilePath);
const webRoot = resolve(scriptsDirectory, "..");
const projectRoot = resolve(webRoot, "../..");

function fail(message) {
  console.error(`Quality 2L premium UI verification failed: ${message}`);
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

console.log("Running quality 2L premium UI verifier.");

const mainPage = readRequired("apps/web/src/app/(app)/kalite/page.tsx");
const ncrListPage = readRequired("apps/web/src/app/(app)/kalite/ncr/page.tsx");
const ncrDetailPage = readRequired("apps/web/src/app/(app)/kalite/ncr/[ncrId]/page.tsx");
const inspectionPage = readRequired("apps/web/src/app/(app)/kalite/kontroller/[inspectionId]/page.tsx");
const inspectionNcrLink = readRequired(
  "apps/web/src/features/quality-control/components/quality-inspection-ncr-link.tsx",
);
const ncrActions = readRequired("apps/web/src/features/quality-control/components/quality-ncr-actions.tsx");
const nav = readRequired("apps/web/src/lib/navigation/primary-navigation.ts");

requireText(mainPage, "production-execution-page", "kalite main page");
requireText(mainPage, "production-execution-kpis", "kalite main page");
requireText(mainPage, "Açık NCR", "kalite main page");
requireText(mainPage, "Aktif Kalite Blokeleri", "kalite main page");
requireText(mainPage, "Aksiyon Bekleyenler", "kalite main page");
requireText(mainPage, "/kalite/ncr", "kalite main page");

requireText(ncrListPage, "production-execution-page", "NCR list page");
requireText(ncrListPage, "Uygunsuzluk Raporları", "NCR list page");
requireText(ncrListPage, "production-execution-table", "NCR list page");

requireText(ncrDetailPage, "production-execution-page", "NCR detail page");
requireText(ncrDetailPage, "Kalite Blokesi", "NCR detail page");
requireText(ncrDetailPage, "QualityNcrActions", "NCR detail page");
requireText(ncrDetailPage, "Üretim bağlamı", "NCR detail page");

requireText(inspectionPage, "QualityInspectionNcrLink", "inspection page");
requireText(inspectionNcrLink, "NCR Oluştur", "quality-inspection-ncr-link");

requireText(ncrActions, "Hold Koy", "quality-ncr-actions");
requireText(ncrActions, "Hold Kaldır", "quality-ncr-actions");
requireText(ncrActions, "Disposition Belirle", "quality-ncr-actions");
requireText(ncrActions, "Düzeltici Aksiyon Ekle", "quality-ncr-actions");
requireText(ncrActions, "Çöz", "quality-ncr-actions");
requireText(ncrActions, "Kapat", "quality-ncr-actions");

requireText(nav, 'href: "/kalite"', "primary navigation");
forbidText(nav, 'href: "/kalite/ncr"', "primary navigation");
forbidText(mainPage, "globals.css", "kalite main page");

console.log("Quality 2L premium UI verification passed.");
