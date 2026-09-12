import { existsSync, readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const currentFilePath = fileURLToPath(import.meta.url);
const scriptsDirectory = dirname(currentFilePath);
const webRoot = resolve(scriptsDirectory, "..");
const projectRoot = resolve(webRoot, "../..");

const WEB = process.env.NEVAXIS_FACTOS_WEB_BASE ?? "http://127.0.0.1:3001";
const BASE = process.env.NEVAXIS_FACTOS_API_BASE ?? "http://127.0.0.1:5000";
const FACILITY = "bb000001-0005-4000-8000-000000000005";
const ISSUER = "crevafactos-dev";
const OPERATOR = "production-pilot-operator";
const TENANT = "dev-pilot-tenant";

function fail(message) {
  console.error(`ERP-UI-2 executive density verification failed: ${message}`);
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
    "X-crevafactos-dev-Issuer": ISSUER,
    "X-crevafactos-dev-Subject": OPERATOR,
    "X-NevaxisFactOS-Tenant": TENANT,
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

console.log("Running ERP-UI-2 executive density verifier.");

const primitives = readRequired("apps/web/src/features/erp/components/erp-ui-primitives.tsx");
const workspace = readRequired("apps/web/src/features/erp/components/erp-workspace-view.tsx");
const nav = readRequired("apps/web/src/features/erp/components/erp-workspace-nav.tsx");
const css = readRequired("apps/web/src/features/erp/components/erp-workspace.css");
const labels = readRequired("apps/web/src/features/erp/presentation/erp-labels.ts");
const globals = readRequired("apps/web/src/app/globals.css");
const sidebar = readRequired("apps/web/src/lib/navigation/primary-navigation.ts");
const writebackActions = readRequired(
  "apps/web/src/features/erp/components/erp-writeback-approval-actions.tsx",
);
const writebackDetail = readRequired(
  "apps/web/src/features/erp/components/erp-writeback-detail-view.tsx",
);
const orderDetail = readRequired("apps/web/src/features/erp/components/erp-order-detail-view.tsx");

requireText(nav, "erp-workspace-nav--compact", "compact internal nav");
requireText(nav, "PRIMARY_NAV", "primary nav tier");
requireText(nav, "SECONDARY_NAV", "secondary nav tier");
requireText(nav, "MONITORING_NAV", "monitoring nav tier");
requireText(nav, "erp-workspace-nav__band", "single navigation band");
forbidText(nav, "NAV_GROUPS", "no stacked category-block nav");
forbidText(nav, "erp-workspace-nav__label", "no vertical category labels");

requireText(primitives, "ErpHeaderStatusContext", "executive header status context");
requireText(primitives, "erp-workspace-header--executive", "executive workspace header");
requireText(primitives, "erp-connection-card--compact", "compact connection card");
requireText(primitives, "erp-connection-card__top", "connection card top row");
requireText(primitives, "ERP Simulator", "simulator identity");
requireText(primitives, "Geliştirme / Test", "simulator secondary label");
forbidText(primitives, "Development ERP Simulator", "no duplicate simulator naming");

requireText(workspace, "ERP Entegrasyonları", "executive workspace title");
requireText(workspace, "ErpHeaderStatusContext", "header status on connection page");
requireText(workspace, "formatErpKpiTimestamp", "compact KPI timestamp");
requireText(workspace, "erp-workspace-page--dense", "dense workspace page");

requireText(css, ".erp-workspace-nav--compact", "compact nav styles");
requireText(css, ".erp-kpi__value-stack", "KPI timestamp stack");
requireText(css, ".erp-connection-card--compact", "compact connection card styles");
requireText(css, ".erp-header-status", "header status styles");
requireText(css, "min-height: 68px", "reduced KPI height");

requireText(labels, "formatErpKpiTimestamp", "KPI timestamp formatter");

forbidText(globals, "erp-workspace", "globals.css untouched");
forbidText(sidebar, "Stok & Depolar", "main sidebar unchanged");

requireText(writebackActions, "Onaya Gönder", "submit action");
requireText(writebackActions, "Onayla", "approve action");
requireText(writebackActions, "Uygula", "execute action");
forbidText(writebackActions, "Onayla ve Uygula", "no combined approve execute");
forbidText(writebackDetail, "payloadHash", "no prominent payload hash");
forbidText(orderDetail, "bb000001-", "no raw facility UUID");

forbidText(workspace, "Connection Health", "Turkish-first UI");
forbidText(workspace, "Recent Sync Runs", "Turkish-first UI");
requireText(css, ".erp-freshness", "freshness distinct styling");
requireText(css, ".erp-health", "health distinct styling");

console.log("Running ERP-UI-2 live route checks.");

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
    if (r.status !== 200) fail(`${route} expected HTTP 200 got ${r.status}`);
    const html = await r.text();
    if (html.includes("bb000001-0005-4000-8000-000000000005")) {
      fail(`${route} exposes raw facility UUID`);
    }
    if (html.includes("Connection Health") || html.includes("Recent Sync Runs")) {
      fail(`${route} exposes English ERP headings`);
    }
  }

  const connectionHtml = await fetch(`${WEB}/erp-entegrasyonlari/baglanti`).then((r) => r.text());
  requireText(connectionHtml, "ERP Entegrasyonları", "connection page executive title");
  requireText(connectionHtml, "erp-workspace-nav--compact", "compact nav rendered");
  forbidText(connectionHtml, "GENEL", "no stacked category label GENEL");
  forbidText(connectionHtml, "GELEN VERİ", "no stacked category label GELEN VERİ");

  let orders = await apiRequest("GET", "/api/erp/orders");
  if (Array.isArray(orders.json) && orders.json[0]?.id) {
    const detail = await fetch(`${WEB}/erp-entegrasyonlari/siparisler/${orders.json[0].id}`);
    if (detail.status !== 200) fail("order detail expected HTTP 200");
  }

  let purchasing = await apiRequest("GET", "/api/erp/purchase-orders");
  if (Array.isArray(purchasing.json) && purchasing.json[0]?.id) {
    const detail = await fetch(`${WEB}/erp-entegrasyonlari/satin-alma/${purchasing.json[0].id}`);
    if (detail.status !== 200) fail("purchase detail expected HTTP 200");
  }

  let production = await apiRequest("GET", "/api/erp/production-orders");
  const mo = production.json?.find?.((row) => row.externalId === "ERP-MO-001") ?? production.json?.[0];
  if (mo?.id) {
    const detail = await fetch(`${WEB}/erp-entegrasyonlari/uretim/${mo.id}`);
    if (detail.status !== 200) fail("production detail expected HTTP 200");
  }

  let documents = await apiRequest("GET", "/api/erp/documents");
  const doc = documents.json?.find?.((row) => row.externalId === "ERP-DOC-001") ?? documents.json?.[0];
  if (doc?.id) {
    const detail = await fetch(`${WEB}/erp-entegrasyonlari/belgeler/${doc.id}`);
    if (detail.status !== 200) fail("document detail expected HTTP 200");
  }

  const writebacks = await apiRequest("GET", "/api/erp/writebacks");
  if (Array.isArray(writebacks.json) && writebacks.json[0]?.id) {
    const detail = await fetch(`${WEB}/erp-entegrasyonlari/onayli-islemler/${writebacks.json[0].id}`);
    if (detail.status !== 200) fail("writeback detail expected HTTP 200");
  }
} catch (error) {
  fail(error instanceof Error ? error.message : String(error));
}

console.log("ERP-UI-2 executive density verification passed.");
