import { existsSync, readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const currentFilePath = fileURLToPath(import.meta.url);
const scriptsDirectory = dirname(currentFilePath);
const webRoot = resolve(scriptsDirectory, "..");
const projectRoot = resolve(webRoot, "../..");

function fail(message) {
  console.error(`ERP-1L premium UI verification failed: ${message}`);
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

console.log("Running ERP-1L premium UI verifier.");

const view = readRequired("apps/web/src/features/erp/components/erp-workspace-view.tsx");
const primitives = readRequired("apps/web/src/features/erp/components/erp-ui-primitives.tsx");
const css = readRequired("apps/web/src/features/erp/components/erp-workspace.css");
const nav = readRequired("apps/web/src/lib/navigation/primary-navigation.ts");
const baglanti = readRequired("apps/web/src/app/(app)/erp-entegrasyonlari/baglanti/page.tsx");
const globals = readRequired("apps/web/src/app/globals.css");

requireText(view, "ERP Bağlantıları", "workspace title");
requireText(
  view,
  "ERP bağlantılarınızı, veri senkronizasyonunu ve kontrollü ERP işlemlerini",
  "workspace subtitle",
);
requireText(view, "Bağlantı Sağlığı", "health section");
requireText(view, "Son Senkronizasyonlar", "sync section");
requireText(view, "ERP Müşterileri", "customers section");
requireText(view, "ERP Ürünleri", "products section");
requireText(view, "ERP Bağlantıları", "KPI");
requireText(view, "Sağlıklı", "KPI");
requireText(view, "Dikkat Gerektiren", "KPI");
requireText(view, "Son Senkronizasyon", "KPI");
requireText(view, "ERP Müşterileri", "customer KPI");
requireText(view, "ERP Ürünleri", "product KPI");
requireText(primitives, "ERP Simulator — gerçek ERP bağlantısı değildir", "simulator labeling");
requireText(primitives, "getErpFacilityDisplayLabel", "facility display helper");

requireText(view, "ERP Müşteri Kodu", "customer table");
requireText(view, "Vergi No", "customer table");
requireText(view, "Barkod", "product table");
requireText(primitives, "Bağlantıyı Yönet", "connection primary action");
requireText(primitives, "Sağlığı Kontrol Et", "connection secondary action");
requireText(primitives, "Senkronizasyon Merkezi", "sync action routing");

requireText(css, ".erp-workspace-page", "erp css");
requireText(css, ".erp-kpi-grid", "erp css");
requireText(css, ".erp-connection-card", "connection card styling");
requireText(baglanti, "ErpWorkspacePage", "baglanti route");
requireText(nav, "/erp-entegrasyonlari/baglanti", "existing ERP nav reused");

forbidText(view, "Connection Health", "no English health heading");
forbidText(view, "Recent Sync Runs", "no English sync heading");
forbidText(view, "Canonical Customers", "no English customers heading");
forbidText(view, "facilityId.slice", "no raw facility UUID");
forbidText(view, "<select", "no raw browser controls");
forbidText(view, "<input", "no raw browser controls");
forbidText(globals, "erp-workspace", "globals.css must not change for ERP");

console.log("ERP-1L premium UI verification passed.");
