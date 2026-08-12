import { readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const scriptsDirectory = dirname(fileURLToPath(import.meta.url));
const webRoot = resolve(scriptsDirectory, "..");
const projectRoot = resolve(webRoot, "../..");

function fail(message) {
  console.error(`MATERIAL-1 functional UI verification failed: ${message}`);
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

console.log("Running MATERIAL-1 functional UI verifier.");

const page = readRequired("apps/web/src/app/(app)/malzemeler/page.tsx");
const view = readRequired("apps/web/src/features/materials/components/material-registry-view.tsx");
const labels = readRequired("apps/web/src/features/materials/presentation/material-labels.ts");
const nav = readRequired("apps/web/src/lib/navigation/primary-navigation.ts");
const visibility = readRequired("apps/web/src/lib/navigation/navigation-visibility.ts");
const iconRegistry = readRequired("apps/web/src/lib/navigation/navigation-icon-registry.tsx");
const ui = `${page}\n${view}`;

requireText(page, "MALZEME YÖNETİMİ", "page eyebrow");
requireText(page, "Malzemeler", "page title");
requireText(ui, "Toplam Malzeme", "summary KPI");
requireText(ui, "Teknik Verisi Eksik", "missing technical KPI");
requireText(ui, "Genel Bakış", "overview section");
requireText(ui, "Teknik Özellikler", "technical section");
requireText(ui, "Kaynak ve ERP Referansı", "ERP reference section");
requireText(ui, "Durum Geçmişi", "lifecycle history section");
requireText(ui, "Yeni Malzeme", "create CTA");
requireText(ui, "Teknik Veriyi Güncelle", "technical update action");
requireText(ui, "Durum Değiştir", "lifecycle action");
requireText(labels, "Taslak", "Turkish lifecycle label");
requireText(labels, "Virgin Polimer", "Turkish category label");
requireText(nav, 'label: "Malzemeler"', "navigation label");
requireText(nav, 'href: "/malzemeler"', "navigation route");
requireText(nav, 'permissionKey: "material.read"', "navigation permission");
requireText(nav, 'iconKey: "package"', "navigation icon key");
requireText(visibility, "resolveMaterialNavigationVisibility", "permission-aware navigation");
requireText(iconRegistry, "package: Package", "client-side icon mapping");
forbidText(nav, "React.createElement", "server navigation must not embed React components");

console.log("MATERIAL-1 functional UI verification passed.");
