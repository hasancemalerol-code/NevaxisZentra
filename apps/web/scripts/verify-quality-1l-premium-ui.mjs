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
const planPage = readRequired("apps/web/src/app/(app)/kalite/planlar/yeni/page.tsx");
const inspectionPage = readRequired("apps/web/src/app/(app)/kalite/kontroller/[inspectionId]/page.tsx");
const nav = readRequired("apps/web/src/lib/navigation/primary-navigation.ts");

requireText(mainPage, "production-execution-page", "kalite main page");
requireText(mainPage, "production-execution-kpis", "kalite main page");
requireText(mainPage, "İnceleme Bekleyenler", "kalite main page");
requireText(mainPage, "Başarısız Kontroller", "kalite main page");
requireText(mainPage, "Serbest Bırakılanlar", "kalite main page");
requireText(mainPage, "Kontrol Planları", "kalite main page");
requireText(mainPage, "Aktif Kontroller", "kalite main page");
requireText(mainPage, "Son Kontroller", "kalite main page");

requireText(planPage, "production-execution-page", "plan page");
requireText(planPage, "Yeni Kontrol Planı", "plan page");

requireText(inspectionPage, "production-execution-page", "inspection page");
requireText(inspectionPage, "Üretim bağlamı", "inspection page");
requireText(inspectionPage, "salt okunur", "inspection page");

requireText(nav, 'href: "/kalite"', "primary navigation");

forbidText(mainPage, "globals.css", "kalite main page");

console.log("Quality 1L premium UI verification passed.");
