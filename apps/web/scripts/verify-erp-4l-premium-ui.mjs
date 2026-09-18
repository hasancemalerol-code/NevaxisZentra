import { existsSync, readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const currentFilePath = fileURLToPath(import.meta.url);
const scriptsDirectory = dirname(currentFilePath);
const webRoot = resolve(scriptsDirectory, "..");
const projectRoot = resolve(webRoot, "../..");

const WEB = process.env.NEVAXIS_FACTOS_WEB_BASE ?? "http://localhost:3001";
const BASE = process.env.NEVAXIS_FACTOS_API_BASE ?? "http://localhost:5000";
const FACILITY = "bb000001-0005-4000-8000-000000000005";
const CONNECTION = "bb000001-0090-4000-8000-000000000090";
const ISSUER = "crevafactos-dev";
const OPERATOR = "production-pilot-operator";
const TENANT = "dev-pilot-tenant";

function fail(message) {
  console.error(`ERP-4L premium UI verification failed: ${message}`);
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
    "X-NevaxisZentra-Tenant": TENANT,
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

console.log("Running ERP-4L premium UI verifier.");

const productionView = readRequired("apps/web/src/features/erp/components/erp-production-view.tsx");
const productionDetail = readRequired(
  "apps/web/src/features/erp/components/erp-production-order-detail-view.tsx",
);
const documentsView = readRequired("apps/web/src/features/erp/components/erp-documents-view.tsx");
const documentDetail = readRequired("apps/web/src/features/erp/components/erp-document-detail-view.tsx");
const writebacksView = readRequired("apps/web/src/features/erp/components/erp-writebacks-view.tsx");
const writebackDetail = readRequired("apps/web/src/features/erp/components/erp-writeback-detail-view.tsx");
const primitives = readRequired("apps/web/src/features/erp/components/erp-ui-primitives.tsx");
const nav = readRequired("apps/web/src/features/erp/components/erp-workspace-nav.tsx");
const css = readRequired("apps/web/src/features/erp/components/erp-workspace.css");
const globals = readRequired("apps/web/src/app/globals.css");
const uretimPage = readRequired("apps/web/src/app/(app)/erp-entegrasyonlari/uretim/page.tsx");
const belgelerPage = readRequired("apps/web/src/app/(app)/erp-entegrasyonlari/belgeler/page.tsx");
const onayliPage = readRequired("apps/web/src/app/(app)/erp-entegrasyonlari/onayli-islemler/page.tsx");
const uretimDetailPage = readRequired(
  "apps/web/src/app/(app)/erp-entegrasyonlari/uretim/[productionOrderId]/page.tsx",
);
const belgeDetailPage = readRequired(
  "apps/web/src/app/(app)/erp-entegrasyonlari/belgeler/[documentId]/page.tsx",
);
const onayliDetailPage = readRequired(
  "apps/web/src/app/(app)/erp-entegrasyonlari/onayli-islemler/[writebackId]/page.tsx",
);

requireText(productionView, "Üretim", "production title");
requireText(productionView, "Üretim Emirleri", "production table title");
requireText(productionView, "salt okunur", "read-only production language");
requireText(productionView, "ERP-4L yalnızca ERP Simulator", "production simulator note");
requireText(productionDetail, "salt okunur", "production detail read-only language");
requireText(productionDetail, "Üretim Emri Özeti", "production detail section");
requireText(documentsView, "Belgeler", "documents title");
requireText(documentsView, "salt okunur", "documents read-only language");
requireText(documentsView, "ERP-4L yalnızca ERP Simulator", "documents simulator note");
requireText(documentsView, "İçerik Mevcut", "content available KPI");
requireText(documentDetail, "Belge Özeti", "document detail section");
requireText(writebacksView, "Onaylı İşlemler", "writebacks title");
requireText(writebacksView, "ErpSimulatorNotice", "writebacks simulator notice");
requireText(primitives, "gerçek ERP işlemi değildir", "writeback simulator disclaimer text");
requireText(writebackDetail, "ErpSimulatorNotice", "writeback detail simulator notice");
requireText(nav, "/erp-entegrasyonlari/uretim", "uretim nav");
requireText(nav, "/erp-entegrasyonlari/belgeler", "belgeler nav");
requireText(nav, "/erp-entegrasyonlari/onayli-islemler", "onayli-islemler nav");
requireText(css, ".erp-simulator-note", "simulator note styling");
requireText(uretimPage, "ErpProductionView", "uretim page");
requireText(belgelerPage, "ErpDocumentsView", "belgeler page");
requireText(onayliPage, "ErpWritebacksView", "onayli-islemler page");
requireText(uretimDetailPage, "ErpProductionOrderDetailView", "uretim detail page");
requireText(belgeDetailPage, "ErpDocumentDetailView", "belge detail page");
requireText(onayliDetailPage, "ErpWritebackDetailView", "onayli detail page");

forbidText(productionView, "type=\"submit\"", "no production submit");
forbidText(productionView, "Üretim Başlat", "no production start action");
forbidText(productionDetail, "type=\"submit\"", "no production detail submit");
forbidText(documentsView, "type=\"submit\"", "no document submit");
forbidText(documentsView, "Belge İndir", "no document download action");
forbidText(globals, "erp-workspace", "globals.css unchanged");

console.log("Running ERP-4L premium UI live page checks.");

try {
  await apiRequest("POST", `/api/erp/connections/${CONNECTION}/sync/production`);
  await apiRequest("POST", `/api/erp/connections/${CONNECTION}/sync/documents`);

  let r = await fetch(`${WEB}/erp-entegrasyonlari/uretim`);
  if (r.status !== 200) fail(`uretim page live expected 200 got ${r.status}`);
  const uretimHtml = await r.text();
  if (!uretimHtml.includes("Üretim")) fail("uretim page missing title");
  if (!uretimHtml.includes("salt okunur") && !uretimHtml.includes("projeksiyon")) {
    fail("uretim page missing read-only production language");
  }
  if (!uretimHtml.includes("ERP Simulator")) fail("uretim page missing simulator note");

  r = await fetch(`${WEB}/erp-entegrasyonlari/belgeler`);
  if (r.status !== 200) fail(`belgeler page live expected 200 got ${r.status}`);
  const belgelerHtml = await r.text();
  if (!belgelerHtml.includes("Belgeler")) fail("belgeler page missing title");
  if (!belgelerHtml.includes("salt okunur")) fail("belgeler page missing read-only language");

  r = await fetch(`${WEB}/erp-entegrasyonlari/onayli-islemler`);
  if (r.status !== 200) fail(`onayli-islemler page live expected 200 got ${r.status}`);
  const onayliHtml = await r.text();
  if (!onayliHtml.includes("Onaylı İşlemler")) fail("onayli-islemler page missing title");
  if (!onayliHtml.includes("gerçek ERP işlemi değildir")) {
    fail("onayli-islemler page missing simulator warning");
  }

  let production = await apiRequest("GET", "/api/erp/production-orders");
  if (production.status !== 200 || !Array.isArray(production.json)) {
    fail("production list for detail route expected 200 array");
  }
  const productionOrder = production.json.find((row) => row.externalId === "ERP-MO-001");
  if (!productionOrder?.id) fail("ERP-MO-001 required for production detail route");

  let documents = await apiRequest("GET", "/api/erp/documents");
  if (documents.status !== 200 || !Array.isArray(documents.json)) {
    fail("documents list for detail route expected 200 array");
  }
  const document = documents.json.find((row) => row.externalId === "ERP-DOC-001");
  if (!document?.id) fail("ERP-DOC-001 required for document detail route");

  r = await fetch(`${WEB}/erp-entegrasyonlari/uretim/${productionOrder.id}`);
  if (r.status !== 200) fail(`uretim detail page expected 200 got ${r.status}`);
  const uretimDetailHtml = await r.text();
  if (!uretimDetailHtml.includes("salt okunur")) {
    fail("uretim detail page missing read-only language");
  }

  r = await fetch(`${WEB}/erp-entegrasyonlari/belgeler/${document.id}`);
  if (r.status !== 200) fail(`belge detail page expected 200 got ${r.status}`);
  const belgeDetailHtml = await r.text();
  if (!belgeDetailHtml.includes("Belge Özeti")) fail("belge detail page missing summary section");

  const writebacks = await apiRequest("GET", "/api/erp/writebacks");
  if (writebacks.status !== 200 || !Array.isArray(writebacks.json)) {
    fail("writebacks list for detail route expected 200 array");
  }
  if (writebacks.json.length > 0) {
    const writebackId = writebacks.json[0].id;
    r = await fetch(`${WEB}/erp-entegrasyonlari/onayli-islemler/${writebackId}`);
    if (r.status !== 200) fail(`onayli detail page expected 200 got ${r.status}`);
    const onayliDetailHtml = await r.text();
    if (!onayliDetailHtml.includes("gerçek ERP işlemi değildir")) {
      fail("onayli detail page missing simulator warning");
    }
  }

  console.log(
    `ERP-4L premium UI live checks passed: productionId=${productionOrder.id}, documentId=${document.id}`,
  );
} catch (error) {
  fail(error instanceof Error ? error.message : String(error));
}

console.log("ERP-4L premium UI verification passed.");
