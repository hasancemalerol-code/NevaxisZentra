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
const css = readRequired("apps/web/src/features/erp/components/erp-workspace.css");
const nav = readRequired("apps/web/src/lib/navigation/primary-navigation.ts");
const baglanti = readRequired("apps/web/src/app/(app)/erp-entegrasyonlari/baglanti/page.tsx");
const globals = readRequired("apps/web/src/app/globals.css");

requireText(view, "ERP Entegrasyonları", "workspace title");
requireText(
  view,
  "Bağlı ERP sistemlerinin bağlantı durumunu, veri senkronizasyonunu ve",
  "workspace subtitle",
);
requireText(view, "Connection Health", "health section");
requireText(view, "Connections", "connections section");
requireText(view, "Recent Sync Runs", "sync section");
requireText(view, "Canonical Customers", "customers section");
requireText(view, "Canonical Products", "products section");
requireText(view, "Bağlantılar", "KPI");
requireText(view, "Sağlıklı", "KPI");
requireText(view, "Sorunlu", "KPI");
requireText(view, "Son Senkronizasyon", "KPI");
requireText(view, "Müşteri Kayıtları", "KPI");
requireText(view, "Ürün Kayıtları", "KPI");
requireText(view, "ERP Simulator", "simulator labeling");
requireText(view, "Logo / Mikro / Netsis", "no misleading real vendor claim context");
requireText(view, "gerçek bağlantı değildir", "no misleading real vendor claim");

requireText(view, "ERP Kodu", "customer/product tables");
requireText(view, "Vergi No", "customer table");
requireText(view, "Barkod", "product table");
requireText(view, "Bağlantıyı Aç", "connection actions");
requireText(view, "Sağlığı Kontrol Et", "connection actions");
requireText(view, "Müşterileri Senkronize Et", "connection actions");
requireText(view, "Ürünleri Senkronize Et", "connection actions");

requireText(css, ".erp-workspace-page", "erp css");
requireText(css, ".erp-kpi-grid", "erp css");
requireText(baglanti, "ErpWorkspacePage", "baglanti route");
requireText(nav, "/erp-entegrasyonlari/baglanti", "existing ERP nav reused");

forbidText(view, "<select", "no raw browser controls");
forbidText(view, "<input", "no raw browser controls");
forbidText(globals, "erp-workspace", "globals.css must not change for ERP");

console.log("ERP-1L premium UI verification passed.");
