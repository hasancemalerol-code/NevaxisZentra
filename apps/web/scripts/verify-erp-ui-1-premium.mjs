import { existsSync, readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const currentFilePath = fileURLToPath(import.meta.url);
const scriptsDirectory = dirname(currentFilePath);
const webRoot = resolve(scriptsDirectory, "..");
const projectRoot = resolve(webRoot, "../..");

const WEB = process.env.CREVAFACTOS_WEB_BASE ?? "http://localhost:3001";
const BASE = process.env.CREVAFACTOS_API_BASE ?? "http://localhost:5000";
const FACILITY = "bb000001-0005-4000-8000-000000000005";
const CONNECTION = "bb000001-0090-4000-8000-000000000090";
const ISSUER = "crevafactos-dev";
const OPERATOR = "production-pilot-operator";
const TENANT = "dev-pilot-tenant";

function fail(message) {
  console.error(`ERP-UI-1 premium verification failed: ${message}`);
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

function headers() {
  return {
    Accept: "application/json",
    "Content-Type": "application/json",
    "X-CrevaFactOS-Dev-Issuer": ISSUER,
    "X-CrevaFactOS-Dev-Subject": OPERATOR,
    "X-CrevaFactOS-Tenant": TENANT,
  };
}

async function apiRequest(method, path) {
  const url = `${BASE}${path}${path.includes("?") ? "&" : "?"}facilityId=${FACILITY}`;
  const response = await fetch(url, { method, headers: headers() });
  const text = await response.text();
  let json = null;
  try {
    json = text ? JSON.parse(text) : null;
  } catch {
    json = null;
  }
  return { status: response.status, json, text };
}

console.log("Running ERP-UI-1 premium verifier.");

const primitives = readRequired("apps/web/src/features/erp/components/erp-ui-primitives.tsx");
const workspace = readRequired("apps/web/src/features/erp/components/erp-workspace-view.tsx");
const nav = readRequired("apps/web/src/features/erp/components/erp-workspace-nav.tsx");
const css = readRequired("apps/web/src/features/erp/components/erp-workspace.css");
const labels = readRequired("apps/web/src/features/erp/presentation/erp-labels.ts");
const globals = readRequired("apps/web/src/app/globals.css");
const sidebar = readRequired("apps/web/src/lib/navigation/primary-navigation.ts");

const ordersView = readRequired("apps/web/src/features/erp/components/erp-orders-view.tsx");
const orderDetail = readRequired("apps/web/src/features/erp/components/erp-order-detail-view.tsx");
const reconciliation = readRequired("apps/web/src/features/erp/components/erp-reconciliation-view.tsx");
const inventoryView = readRequired("apps/web/src/features/erp/components/erp-inventory-view.tsx");
const purchasingView = readRequired("apps/web/src/features/erp/components/erp-purchasing-view.tsx");
const purchaseDetail = readRequired("apps/web/src/features/erp/components/erp-purchase-order-detail-view.tsx");
const productionView = readRequired("apps/web/src/features/erp/components/erp-production-view.tsx");
const productionDetail = readRequired(
  "apps/web/src/features/erp/components/erp-production-order-detail-view.tsx",
);
const documentsView = readRequired("apps/web/src/features/erp/components/erp-documents-view.tsx");
const documentDetail = readRequired("apps/web/src/features/erp/components/erp-document-detail-view.tsx");
const writebacksView = readRequired("apps/web/src/features/erp/components/erp-writebacks-view.tsx");
const writebackDetail = readRequired("apps/web/src/features/erp/components/erp-writeback-detail-view.tsx");
const writebackActions = readRequired(
  "apps/web/src/features/erp/components/erp-writeback-approval-actions.tsx",
);

requireText(primitives, "ErpPageHeader", "ErpPageHeader primitive");
requireText(primitives, "ErpKpiCard", "ErpKpiCard primitive");
requireText(primitives, "ErpFreshnessBadge", "ErpFreshnessBadge primitive");
requireText(primitives, "ErpHealthIndicator", "ErpHealthIndicator primitive");
requireText(primitives, "ErpConnectionCard", "ErpConnectionCard primitive");
requireText(primitives, "getErpFacilityDisplayLabel", "facility display helper");

requireText(primitives, "ERP ENTEGRASYONLARI", "page eyebrow");
requireText(workspace, "Bağlantı Sağlığı", "connection health section");
requireText(workspace, "Son Senkronizasyonlar", "recent sync section");
requireText(workspace, "ERP Müşterileri", "customers section");
requireText(workspace, "ERP Ürünleri", "products section");
requireText(workspace, "getErpFacilityDisplayLabel", "no raw facility UUID");

requireText(nav, "erp-workspace-nav--compact", "compact nav architecture");
requireText(nav, "Bağlantı", "connection nav item");
requireText(nav, "Onaylı İşlemler", "writeback nav");
requireText(nav, "Entegrasyon Sağlığı", "health nav item");
forbidText(nav, "NAV_GROUPS", "no stacked category-block nav");

requireText(labels, "Güncel Değil", "freshness stale label");
requireText(css, ".erp-freshness", "freshness styling distinct from health");
requireText(css, ".erp-health", "health styling");
requireText(css, ".erp-table__actions", "dedicated action column");

forbidText(workspace, "Connection Health", "no English connection health heading");
forbidText(workspace, "Connections", "no English connections heading");
forbidText(workspace, "Recent Sync Runs", "no English sync heading");
forbidText(workspace, "facilityId.slice", "no raw facility UUID slice");
forbidText(workspace, "bb000001-", "no raw facility UUID in workspace");
forbidText(writebackDetail, "payloadHash", "no prominent payload hash");
forbidText(writebackDetail, "Onayla ve Uygula", "no combined approve execute");
forbidText(css, "width: 2400px", "no giant hardcoded table width");

requireText(productionView, "salt okunur", "production read-only notice");
requireText(inventoryView, "salt okunur", "inventory read-only notice");
requireText(purchasingView, "salt okunur", "purchasing read-only notice");
requireText(documentsView, "Yalnız Metadata", "document metadata-only state");
requireText(primitives, "gerçek ERP işlemi değildir", "simulator writeback disclaimer");
requireText(writebacksView, "ErpSimulatorNotice", "writebacks simulator notice usage");
requireText(writebackActions, "Onaya Gönder", "submit action");
requireText(writebackActions, "Onayla", "approve action");
requireText(writebackActions, "Reddet", "reject action");
requireText(writebackActions, "Uygula", "execute action");
forbidText(writebacksView, "Logo entegrasyonu aktif", "no false logo claim");
forbidText(globals, "erp-workspace", "globals.css untouched");
forbidText(sidebar, "Stok & Depolar", "main sidebar unchanged");

console.log("Running ERP-UI-1 premium live page checks.");

const routes = [
  "/erp-entegrasyonlari/baglanti",
  "/erp-entegrasyonlari/senkronizasyonlar",
  "/erp-entegrasyonlari/siparisler",
  "/erp-entegrasyonlari/eslestirme",
  "/erp-entegrasyonlari/stok",
  "/erp-entegrasyonlari/satin-alma",
  "/erp-entegrasyonlari/uretim",
  "/erp-entegrasyonlari/belgeler",
  "/erp-entegrasyonlari/onayli-islemler",
  "/erp-entegrasyonlari/saglik",
  "/erp-entegrasyonlari/hata-kuyrugu",
];

try {
  for (const route of routes) {
    const r = await fetch(`${WEB}${route}`);
    if (r.status !== 200) fail(`${route} live expected 200 got ${r.status}`);
    const html = await r.text();
    if (html.includes("bb000001-0005-4000-8000-000000000005")) {
      fail(`${route} exposes raw facility UUID`);
    }
    if (html.includes("Connection Health") || html.includes("Recent Sync Runs")) {
      fail(`${route} exposes English ERP headings`);
    }
  }

  console.log("ERP-UI-1 list route live checks passed.");

  let production = await apiRequest("GET", "/api/erp/production-orders");
  const productionOrder = production.json?.find?.((row) => row.externalId === "ERP-MO-001");
  if (productionOrder?.id) {
    const r = await fetch(`${WEB}/erp-entegrasyonlari/uretim/${productionOrder.id}`);
    if (r.status !== 200) fail(`production detail live expected 200 got ${r.status}`);
  }

  let documents = await apiRequest("GET", "/api/erp/documents");
  const document = documents.json?.find?.((row) => row.externalId === "ERP-DOC-001");
  if (document?.id) {
    const r = await fetch(`${WEB}/erp-entegrasyonlari/belgeler/${document.id}`);
    if (r.status !== 200) fail(`document detail live expected 200 got ${r.status}`);
  }

  let orders = await apiRequest("GET", "/api/erp/orders");
  if (Array.isArray(orders.json) && orders.json[0]?.id) {
    const r = await fetch(`${WEB}/erp-entegrasyonlari/siparisler/${orders.json[0].id}`);
    if (r.status !== 200) fail(`order detail live expected 200 got ${r.status}`);
  }

  let purchasing = await apiRequest("GET", "/api/erp/purchase-orders");
  if (Array.isArray(purchasing.json) && purchasing.json[0]?.id) {
    const r = await fetch(`${WEB}/erp-entegrasyonlari/satin-alma/${purchasing.json[0].id}`);
    if (r.status !== 200) fail(`purchase detail live expected 200 got ${r.status}`);
  }

  const writebacks = await apiRequest("GET", "/api/erp/writebacks");
  if (Array.isArray(writebacks.json) && writebacks.json[0]?.id) {
    const r = await fetch(`${WEB}/erp-entegrasyonlari/onayli-islemler/${writebacks.json[0].id}`);
    if (r.status !== 200) fail(`writeback detail live expected 200 got ${r.status}`);
  }

  console.log("ERP-UI-1 premium live checks passed.");
} catch (error) {
  fail(error instanceof Error ? error.message : String(error));
}

console.log("ERP-UI-1 premium verification passed.");

