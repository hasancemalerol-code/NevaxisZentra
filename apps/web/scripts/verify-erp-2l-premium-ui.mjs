import { existsSync, readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const currentFilePath = fileURLToPath(import.meta.url);
const scriptsDirectory = dirname(currentFilePath);
const webRoot = resolve(scriptsDirectory, "..");
const projectRoot = resolve(webRoot, "../..");

const WEB = process.env.CREVAFACTOS_WEB_BASE ?? "http://localhost:3001";

function fail(message) {
  console.error(`ERP-2L premium UI verification failed: ${message}`);
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

console.log("Running ERP-2L premium UI verifier.");

const ordersView = readRequired("apps/web/src/features/erp/components/erp-orders-view.tsx");
const orderDetail = readRequired("apps/web/src/features/erp/components/erp-order-detail-view.tsx");
const reconciliation = readRequired("apps/web/src/features/erp/components/erp-reconciliation-view.tsx");
const nav = readRequired("apps/web/src/features/erp/components/erp-workspace-nav.tsx");
const css = readRequired("apps/web/src/features/erp/components/erp-workspace.css");
const globals = readRequired("apps/web/src/app/globals.css");
const sidebar = readRequired("apps/web/src/lib/navigation/primary-navigation.ts");

requireText(ordersView, "ERP Siparişleri", "orders title");
requireText(ordersView, "ERP Sipariş No", "orders table column");
requireText(ordersView, "Hazırlık Durumu", "readiness column");
requireText(ordersView, "Siparişi Aç", "order action");
requireText(orderDetail, "Hazırlık Durumu", "order detail readiness");
requireText(orderDetail, "Engelleyici neden", "blocking reasons");
requireText(reconciliation, "Müşteri Eşleştirmeleri", "customer reconciliation section");
requireText(reconciliation, "Ürün Eşleştirmeleri", "product reconciliation section");
requireText(reconciliation, "Öneri — kullanıcı onayı gerekli", "suggestion label");
requireText(reconciliation, "getErpMappingStatusLabel", "mapping status labels");
const labels = readRequired("apps/web/src/features/erp/presentation/erp-labels.ts");
requireText(labels, "Eşleşmedi", "mapping status label");
requireText(nav, "/erp-entegrasyonlari/siparisler", "orders nav");
requireText(nav, "/erp-entegrasyonlari/eslestirme", "reconciliation nav");
requireText(css, ".erp-badge", "premium badge styling");

forbidText(ordersView, "<select", "no raw browser controls");
forbidText(ordersView, "<input", "no raw browser controls");
forbidText(globals, "erp-workspace", "globals.css unchanged");
forbidText(sidebar, "/erp-entegrasyonlari/siparisler", "sidebar unchanged");

console.log("Running ERP-2L premium UI live page checks.");

try {
  let r = await fetch(`${WEB}/erp-entegrasyonlari/siparisler`);
  if (r.status !== 200) fail(`orders page live expected 200 got ${r.status}`);
  const ordersHtml = await r.text();
  if (!ordersHtml.includes("ERP Siparişleri")) fail("orders page missing title");

  r = await fetch(`${WEB}/erp-entegrasyonlari/eslestirme`);
  if (r.status !== 200) fail(`reconciliation page live expected 200 got ${r.status}`);
  const reconHtml = await r.text();
  if (!reconHtml.includes("Eşleştirme")) fail("reconciliation page missing title");
} catch (error) {
  fail(error instanceof Error ? error.message : String(error));
}

console.log("ERP-2L premium UI verification passed.");
