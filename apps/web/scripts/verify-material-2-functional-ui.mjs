import { readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const scriptsDirectory = dirname(fileURLToPath(import.meta.url));
const webRoot = resolve(scriptsDirectory, "..");
const projectRoot = resolve(webRoot, "../..");

function fail(message) {
  console.error(`MATERIAL-2 functional UI verification failed: ${message}`);
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

console.log("Running MATERIAL-2 functional UI verifier.");

const view = readRequired("apps/web/src/features/materials/components/material-registry-view.tsx");
const lotPanel = readRequired("apps/web/src/features/materials/components/material-lot-panel.tsx");
const tracePanel = readRequired("apps/web/src/features/materials/components/material-lot-traceability-panel.tsx");
const labels = readRequired("apps/web/src/features/materials/presentation/material-labels.ts");
const nav = readRequired("apps/web/src/lib/navigation/primary-navigation.ts");
const ui = `${view}\n${lotPanel}\n${tracePanel}\n${labels}`;

requireText(view, '"lots"', "DetailTab lots");
requireText(view, '"traceability"', "DetailTab traceability");
requireText(ui, "Lotlar", "lots tab label");
requireText(ui, "İzlenebilirlik", "traceability tab label");
requireText(ui, "Toplam Lot", "total lot KPI");
requireText(ui, "Kullanılabilir", "available lot KPI");
requireText(ui, "Kalite Bekliyor", "quality hold KPI");
requireText(ui, "Bloke", "blocked lot KPI");
requireText(ui, "Süresi Yaklaşan", "expiry approaching KPI");
requireText(ui, "Lot Sicili", "lot registry title");
requireText(ui, "Tedarikçi/Üretici Lotu", "supplier lot column");
requireText(ui, "Gözlenen Miktar", "observed quantity column");
requireText(ui, "SKT/Yeniden Test", "expiry column");
requireText(ui, "Operasyonel gözlem miktarıdır; ERP stok bakiyesi değildir.", "quantity disclaimer");
requireText(ui, "Yeni Lot", "create lot action");
requireText(ui, "Kabul Gözlemi Kaydet", "receipt action");
requireText(ui, "Konum Değiştir", "transfer action");
requireText(ui, "Kalite Durumu Kaydet", "quality action");
requireText(ui, "Belge Referansı Ekle", "certificate action");
requireText(ui, "Genel Bakış", "lot overview section");
requireText(ui, "Kaynak ve Kabul", "lot source section");
requireText(ui, "Depo/Konum", "lot location section");
requireText(ui, "Kalite Durumu", "lot quality section");
requireText(ui, "Belgeler", "lot documents section");
requireText(ui, "Zaman Çizelgesi", "lot timeline section");
requireText(labels, "Kullanılabilir", "Turkish lot status label");
requireText(labels, "Kalite Bekliyor", "Turkish quality-hold label");
requireText(labels, "Koşullu Kabul", "Turkish conditionally accepted label");
requireText(labels, "Analiz Sertifikası (CoA)", "Turkish certificate label");
requireText(nav, 'label: "Malzemeler"', "existing navigation label");
requireText(nav, 'href: "/malzemeler"', "existing navigation route");
forbidText(nav, "Lotlar", "navigation must not add lot sidebar entry");
forbidText(nav, "/malzeme-lot", "navigation must not add lot route");

console.log("MATERIAL-2 functional UI verification passed.");
