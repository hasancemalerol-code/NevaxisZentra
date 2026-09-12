import { existsSync, readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const currentFilePath = fileURLToPath(import.meta.url);
const scriptsDirectory = dirname(currentFilePath);
const webRoot = resolve(scriptsDirectory, "..");
const projectRoot = resolve(webRoot, "../..");

const WEB = process.env.NEVAXIS_FACTOS_WEB_BASE ?? "http://127.0.0.1:3001";

function fail(message) {
  console.error(`ERP-LOGO-1 premium UI verification failed: ${message}`);
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

console.log("Running ERP-LOGO-1 premium UI verifier.");

const labels = readRequired("apps/web/src/features/erp/presentation/erp-labels.ts");
const primitives = readRequired("apps/web/src/features/erp/components/erp-ui-primitives.tsx");
const workspace = readRequired("apps/web/src/features/erp/components/erp-workspace-view.tsx");
const workspaceCss = readRequired("apps/web/src/features/erp/components/erp-workspace.css");
const globals = readRequired("apps/web/src/app/globals.css");
const sidebar = readRequired("apps/web/src/lib/navigation/primary-navigation.ts");
const ui2 = readRequired("apps/web/scripts/verify-erp-ui-2-executive-density.mjs");

forbidText(labels, "Logo (hazır değil)", "adapter exists");
requireText(labels, "getErpLogoConnectionStateLabel", "Logo connection states");
requireText(labels, "Kurulum Bekliyor", "setup pending state");
requireText(labels, "Kimlik Bilgisi Gerekli", "credential required state");
requireText(labels, "Bağlantı Kurulamadı", "connection failed state");
requireText(labels, "Adapter Hazır", "adapter-ready distinction");
requireText(labels, "Gerçek Logo Bağlantısı Doğrulandı", "live-validated label available");
requireText(labels, "getErpLogoSetupGuidance", "second-PC guidance");
requireText(primitives, "Logo test transport — gerçek Logo doğrulaması değildir", "no false validation for test transport");
requireText(primitives, "Bağlantı Yöntemi", "transport presentation");
requireText(primitives, "Logo Ürünü", "product presentation");
requireText(workspace, "getErpLogoCapabilities", "Logo capability chips");
requireText(workspace, "getErpLogoSetupGuidance", "setup guidance on connection page");
requireText(workspaceCss, "erp-workspace-header--executive", "ERP-UI-2 hierarchy preserved");
forbidText(labels, "Logo entegrasyonu aktif", "no false live claim in labels");
forbidText(primitives, "credentialReference", "no raw credential in UI");
forbidText(globals, "erp-workspace", "globals.css unchanged");
forbidText(sidebar, "Stok & Depolar", "main sidebar unchanged");
requireText(ui2, "ERP-UI-2 executive density verifier", "ERP-UI-2 verifier still present");

console.log("Running ERP-LOGO-1 premium UI live check.");

try {
  const response = await fetch(`${WEB}/erp-entegrasyonlari/baglanti`);
  if (response.status !== 200) {
    fail(`ERP connection page expected 200 got ${response.status}`);
  }
  const html = await response.text();
  if (!html.includes("erp-workspace-page")) {
    fail("ERP workspace page shell missing on connection route");
  }
} catch (error) {
  fail(error instanceof Error ? error.message : String(error));
}

console.log("ERP-LOGO-1 premium UI verification passed.");
