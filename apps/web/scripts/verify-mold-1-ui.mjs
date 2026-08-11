import { readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const scriptsDirectory = dirname(fileURLToPath(import.meta.url));
const webRoot = resolve(scriptsDirectory, "..");
const projectRoot = resolve(webRoot, "../..");

function fail(message) {
  console.error(`MOLD-1 UI verification failed: ${message}`);
  process.exit(1);
}

function readRequired(relativePath) {
  return readFileSync(resolve(projectRoot, relativePath), "utf8").replaceAll("\r\n", "\n");
}

function requireText(source, expected, label) {
  if (!source.includes(expected)) fail(`${label} must contain "${expected}"`);
}

console.log("Running MOLD-1 UI verifier.");

const page = readRequired("apps/web/src/app/(app)/kaliplar/page.tsx");
const view = readRequired("apps/web/src/features/molds/components/mold-registry-view.tsx");
const nav = readRequired("apps/web/src/lib/navigation/primary-navigation.ts");
const visibility = readRequired("apps/web/src/lib/navigation/navigation-visibility.ts");
const ui = `${page}\n${view}`;

requireText(page, "Kalıplar", "page title");
requireText(ui, "Kalıp Oluştur", "create action");
requireText(ui, "Yaşam Döngüsü Geçmişi", "lifecycle history");
requireText(ui, "Sayaç Kaydet", "counter UI");
requireText(ui, "Durum Geçişi", "transition UI");
requireText(nav, 'iconKey: "molds"', "Mold sidebar icon key");
requireText(nav, 'permissionKey: "mold.read"', "sidebar permission key");
requireText(visibility, "resolveMoldNavigationVisibility", "permission-aware navigation");

console.log("MOLD-1 UI verification passed.");
