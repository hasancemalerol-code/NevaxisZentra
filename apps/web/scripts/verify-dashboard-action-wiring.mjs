#!/usr/bin/env node
import { readFileSync } from "node:fs";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";

const root = join(dirname(fileURLToPath(import.meta.url)), "..");

function fail(message) {
  console.error(`DASHBOARD action wiring verification failed: ${message}`);
  process.exit(1);
}

function assert(label, ok, detail = "") {
  if (!ok) fail(`${label}${detail ? `: ${detail}` : ""}`);
  console.log(`✓ ${label}`);
}

function read(relativePath) {
  return readFileSync(join(root, relativePath), "utf8");
}

const dashboardPage = read("src/app/(app)/dashboard/page.tsx");
const quickActions = read("src/components/dashboard/quick-actions-panel.tsx");
const aiSummary = read("src/components/dashboard/ai-task-center-summary.tsx");
const production = read("src/components/dashboard/production-orders-table.tsx");
const maintenance = read("src/components/dashboard/maintenance-status-panel.tsx");
const planning = read("src/components/dashboard/plan-confidence-panel.tsx");
const permissionHelper = read("src/components/dashboard/dashboard-permission.ts");
const sources = [
  dashboardPage,
  quickActions,
  aiSummary,
  production,
  maintenance,
  planning,
  permissionHelper,
];

const classifications = {
  "CTL-D-01 AI'ya Görev Ver": "DISABLED_WITH_REASON",
  "CTL-D-02 CrevaFact Teams": "WORKING_NAVIGATION",
  "CTL-D-03 AI Görevi": "WORKING_NAVIGATION",
  "CTL-D-04 Üretim Emri": "WORKING_NAVIGATION + PERMISSION_HIDDEN",
  "CTL-D-05 Plan Revizyonu": "WORKING_NAVIGATION + PERMISSION_HIDDEN",
  "CTL-D-06 Bakım Talebi": "WORKING_NAVIGATION + PERMISSION_HIDDEN",
  "CTL-D-07 Kalite Kaydı": "WORKING_NAVIGATION + PERMISSION_HIDDEN",
  "CTL-D-08 Malzeme Riski": "WORKING_NAVIGATION (honest placeholder)",
  "CTL-D-09 Termin Riski": "WORKING_NAVIGATION (honest placeholder)",
  "CTL-D-10 ERP Senkronizasyonu": "WORKING_NAVIGATION + PERMISSION_HIDDEN",
  "CTL-D-11 AI inspect": "DISABLED_WITH_REASON unless catalog id",
  "CTL-D-12 üretim footer": "WORKING_NAVIGATION or DISABLED_WITH_REASON",
  "CTL-D-13 bakım footer": "WORKING_NAVIGATION or DISABLED_WITH_REASON",
  "CTL-D-14 plan footer": "WORKING_NAVIGATION or DISABLED_WITH_REASON",
  "CTL-D-15 AI footer": "WORKING_NAVIGATION",
};

assert("every dashboard control has a classification", Object.keys(classifications).length === 15);
for (const [id, classification] of Object.entries(classifications)) {
  assert(`${id} classified`, Boolean(classification));
}

assert(
  "no empty dashboard handler",
  sources.every((source) => !/onClick=\{\s*\}/.test(source) && !/onClick=\{\(\)\s*=>\s*\{\s*\}\}/.test(source)),
);
assert(
  "no href-hash controls",
  sources.every((source) => !/href=["']#["']/.test(source)),
);
assert(
  "AI create does not target missing route",
  !dashboardPage.includes("/ai-gorevleri/yeni"),
);
assert(
  "AI create is honestly disabled",
  dashboardPage.includes("disabled")
    && dashboardPage.includes("AI görev oluşturma akışı henüz kullanıma hazır değil."),
);
assert(
  "Teams navigates to existing placeholder",
  dashboardPage.includes('href="/teams"') && dashboardPage.includes("command-center__teams-button"),
);
assert(
  "Teams is not a no-op button",
  !/<button[\s\S]{0,220}CrevaFact Teams/.test(dashboardPage),
);
assert(
  "production quick action uses canonical permission",
  /href:\s*"\/uretim"[\s\S]{0,80}permissionKey:\s*"production\.execution\.read"/.test(quickActions),
);
assert(
  "planning quick action uses canonical permission",
  /href:\s*"\/planlama"[\s\S]{0,80}permissionKey:\s*"production\.planning\.read"/.test(quickActions),
);
assert(
  "maintenance quick action uses canonical permission",
  /href:\s*"\/bakim"[\s\S]{0,80}permissionKey:\s*"maintenance\.read"/.test(quickActions),
);
assert(
  "quality quick action uses canonical permission",
  /href:\s*"\/kalite"[\s\S]{0,80}permissionKey:\s*"quality\.read"/.test(quickActions),
);
assert(
  "ERP quick action uses canonical permission",
  /href:\s*"\/erp-entegrasyonlari\/senkronizasyonlar"[\s\S]{0,80}permissionKey:\s*"erp\.read"/.test(quickActions),
);
assert(
  "AI Task Center quick action is inspect-only",
  /description:\s*"AI görevlerini incele"/.test(quickActions) && /href:\s*"\/"/.test(quickActions),
);
assert(
  "AI inspect resolves catalog ids only",
  aiSummary.includes("getAiTaskPreviewById")
    && aiSummary.includes("Bu görev için açılabilir bir kayıt bulunmuyor."),
);
assert(
  "AI footer navigates to Task Center",
  /<Link href="\/"[\s\S]{0,80}Tüm AI görevlerini görüntüle/.test(aiSummary),
);
assert(
  "production footer uses existing list route",
  production.includes('href="/uretim"')
    && production.includes("Üretim emirlerini görüntüleme yetkiniz yok."),
);
assert(
  "maintenance footer uses existing list route",
  maintenance.includes('href="/bakim"')
    && maintenance.includes("Bakım planını görüntüleme yetkiniz yok."),
);
assert(
  "planning footer uses existing list route",
  planning.includes('href="/planlama"')
    && planning.includes("Üretim planlarını görüntüleme yetkiniz yok."),
);
assert(
  "permission helper is deny-by-key",
  permissionHelper.includes("grantedPermissionKeys.includes(permissionKey)"),
);
assert(
  "no console-only action",
  sources.every((source) => !/console\.(log|info|debug|warn)\(/.test(source)),
);
assert(
  "no fake success copy",
  sources.every((source) => !/başarıyla oluşturul|görev oluşturuldu|işlem tamamlandı/i.test(source)),
);

console.log("DASHBOARD action wiring verification passed.");
