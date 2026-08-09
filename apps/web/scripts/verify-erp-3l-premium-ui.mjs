import { existsSync, readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const currentFilePath = fileURLToPath(import.meta.url);
const scriptsDirectory = dirname(currentFilePath);
const webRoot = resolve(scriptsDirectory, "..");
const projectRoot = resolve(webRoot, "../..");

const WEB = process.env.CREVAFACTOS_WEB_BASE ?? "http://localhost:3001";

function fail(message) {
  console.error(`ERP-3L premium UI verification failed: ${message}`);
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

console.log("Running ERP-3L premium UI verifier.");

const inventoryView = readRequired("apps/web/src/features/erp/components/erp-inventory-view.tsx");
const purchasingView = readRequired("apps/web/src/features/erp/components/erp-purchasing-view.tsx");
const purchaseOrderDetail = readRequired("apps/web/src/features/erp/components/erp-purchase-order-detail-view.tsx");
const orderDetail = readRequired("apps/web/src/features/erp/components/erp-order-detail-view.tsx");
const nav = readRequired("apps/web/src/features/erp/components/erp-workspace-nav.tsx");
const css = readRequired("apps/web/src/features/erp/components/erp-workspace.css");
const globals = readRequired("apps/web/src/app/globals.css");
const labels = readRequired("apps/web/src/features/erp/presentation/erp-labels.ts");
const stokPage = readRequired("apps/web/src/app/(app)/erp-entegrasyonlari/stok/page.tsx");
const satinAlmaPage = readRequired("apps/web/src/app/(app)/erp-entegrasyonlari/satin-alma/page.tsx");

requireText(inventoryView, "Stok & Depolar", "inventory title");
requireText(inventoryView, "Depolar", "warehouse KPI");
requireText(inventoryView, "Ürün Stok Kayıtları", "inventory KPI");
requireText(inventoryView, "Yetersiz Stok", "shortage KPI");
requireText(inventoryView, "Açık Satın Alma", "open PO KPI");
requireText(inventoryView, "Senkronizasyon Sağlığı", "sync reliability panel");
requireText(inventoryView, "salt okunur", "read-only inventory language");
requireText(purchasingView, "Satın Alma", "purchasing title");
requireText(purchasingView, "Satın Alma Siparişleri", "purchasing table title");
requireText(purchasingView, "salt okunur", "purchasing read-only language");
requireText(purchaseOrderDetail, "Satın Alma Özeti", "purchase order detail section");
requireText(orderDetail, "Malzeme Uygunluğu", "order material availability section");
requireText(orderDetail, "ERP stok projeksiyonuna dayalı bilgi", "material availability note");
requireText(nav, "/erp-entegrasyonlari/stok", "stok nav");
requireText(nav, "/erp-entegrasyonlari/satin-alma", "satin-alma nav");
requireText(labels, "getErpCoverageStatusLabel", "coverage status labels");
requireText(labels, "getErpShortageStatusLabel", "shortage status labels");
requireText(css, ".erp-badge", "premium badge styling");
requireText(stokPage, "ErpInventoryView", "stok page");
requireText(satinAlmaPage, "ErpPurchasingView", "satin-alma page");

forbidText(inventoryView, "type=\"submit\"", "no stock movement submit");
forbidText(inventoryView, "Stok Girişi", "no stock in button");
forbidText(inventoryView, "Stok Çıkışi", "no stock out button");
forbidText(purchasingView, ">Onayla<", "no PO approve button");
forbidText(purchasingView, ">Teslim Al<", "no PO receive button");
forbidText(purchaseOrderDetail, ">Onayla<", "no PO approve button");
forbidText(purchaseOrderDetail, ">Teslim Al<", "no PO receive button");
forbidText(inventoryView, "<select", "no raw browser controls");
forbidText(inventoryView, "<input", "no raw browser controls");
forbidText(globals, "erp-workspace", "globals.css unchanged");

console.log("Running ERP-3L premium UI live page checks.");

try {
  let r = await fetch(`${WEB}/erp-entegrasyonlari/stok`);
  if (r.status !== 200) fail(`stok page live expected 200 got ${r.status}`);
  const stokHtml = await r.text();
  if (!stokHtml.includes("Stok")) fail("stok page missing title");
  if (stokHtml.includes(">Stok Girişi<") || stokHtml.includes(">Stok Çıkışi<")) {
    fail("stok page must not expose movement actions");
  }

  r = await fetch(`${WEB}/erp-entegrasyonlari/satin-alma`);
  if (r.status !== 200) fail(`satin-alma page live expected 200 got ${r.status}`);
  const satinAlmaHtml = await r.text();
  if (!satinAlmaHtml.includes("Satın Alma")) fail("satin-alma page missing title");
  if (satinAlmaHtml.includes(">Teslim Al<") || satinAlmaHtml.includes(">Onayla<")) {
    fail("satin-alma page must not expose approve/receive actions");
  }
} catch (error) {
  fail(error instanceof Error ? error.message : String(error));
}

console.log("ERP-3L premium UI verification passed.");
