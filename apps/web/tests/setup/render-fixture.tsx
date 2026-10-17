// Visual-QA fixture renderer (not a test): server-renders data-rich states of shipping views into static HTML so they can be
// screenshotted against the production CSS bundle, because a fresh tenant only shows empty states.
// Usage: tsx --tsconfig tests/tsconfig.test.json --import ./tests/setup/register-next-mocks.mjs tests/setup/render-fixture.tsx <outDir>
import { mkdirSync, writeFileSync } from "node:fs";
import path from "node:path";
import { renderToStaticMarkup } from "react-dom/server";

import { MaintenancePredictiveRiskWorkspaceView } from "../../src/features/maintenance/components/maintenance-predictive-risk-workspace-view";
import type {
  MaintenancePredictiveRiskAssessment,
  MaintenancePredictiveRiskAssetRow,
  MaintenancePredictiveRiskWorkspacePayload,
} from "../../src/features/maintenance/domain/maintenance-predictive-risk";
import { StatePage, StatePageLink } from "../../src/components/ui/state-page";

const outDir = process.argv[2] ?? "fixture-out";
mkdirSync(outDir, { recursive: true });

const rows: MaintenancePredictiveRiskAssetRow[] = [
  { assetReference: "IM-07", machineCode: "IM-07", machineName: "Enjeksiyon Makinesi 07", riskBand: "critical", reviewPriority: "urgent", dataFreshness: "fresh", dataSufficiency: "high", assessedAtUtc: "2026-10-12T10:20:00Z", followUpStatus: "active" },
  { assetReference: "IM-03", machineCode: "IM-03", machineName: "Enjeksiyon Makinesi 03", riskBand: "high", reviewPriority: "planned", dataFreshness: "fresh", dataSufficiency: "medium", assessedAtUtc: "2026-10-12T10:20:00Z", followUpStatus: "task-draft-created" },
  { assetReference: "IM-11", machineCode: "IM-11", machineName: "Enjeksiyon Makinesi 11", riskBand: "high", reviewPriority: "planned", dataFreshness: "stale", dataSufficiency: "medium", assessedAtUtc: "2026-10-11T08:05:00Z", followUpStatus: "active" },
  { assetReference: "CN-02", machineCode: "CN-02", machineName: "Konveyör Hattı 02", riskBand: "watch", reviewPriority: "elevated", dataFreshness: "fresh", dataSufficiency: "high", assessedAtUtc: "2026-10-12T09:40:00Z", followUpStatus: "monitoring" },
  { assetReference: "RB-01", machineCode: "RB-01", machineName: "Robot Kolu 01", riskBand: "low", reviewPriority: "routine", dataFreshness: "fresh", dataSufficiency: "high", assessedAtUtc: "2026-10-12T09:40:00Z", followUpStatus: "human-reviewed" },
  { assetReference: "CH-04", machineCode: "CH-04", machineName: "Soğutucu 04", riskBand: "insufficient-data", reviewPriority: "routine", dataFreshness: "unknown", dataSufficiency: "low", assessedAtUtc: "2026-10-10T16:00:00Z", followUpStatus: "data-review-needed" },
];

const workspace: MaintenancePredictiveRiskWorkspacePayload = {
  ruleSetVersion: "MAINT-PRED-RISK-V1",
  automationDisclaimer: "Bu görev taslağı otomatik bakım emri, duruş veya makine müdahalesi başlatmaz.",
  kpis: { activeRiskCount: 3, taskLinkedCount: 1, humanReviewedCount: 1 },
  capabilities: { canRead: true, canManage: true, canCreateTask: true },
  overview: { monitoredAssetCount: 6, insufficientDataCount: 1, lowRiskCount: 1, watchCount: 1, highRiskCount: 2, criticalRiskCount: 1, lastAssessedAtUtc: new Date().toISOString() },
  assets: rows,
};

const assessment: MaintenancePredictiveRiskAssessment = {
  id: "assessment-1",
  assetReference: "IM-07",
  machineCode: "IM-07",
  machineName: "Enjeksiyon Makinesi 07",
  riskBand: "critical",
  reviewPriority: "urgent",
  ruleSetVersion: "MAINT-PRED-RISK-V1",
  dataFreshness: "fresh",
  dataSufficiency: "high",
  activeAnomalyCount: 3,
  maintenanceHistorySummary: "Son 30 günde 2 arıza kaydı.",
  explanation: "Aktif kritik anomali ve tekrarlayan alarm bağlamı birlikte yüksek risk gösteriyor; yetkili teknik inceleme önerilir.",
  assessedAtUtc: "2026-10-12T10:20:00Z",
  followUpStatus: "active",
  canCreateTaskDraft: true,
  riskDrivers: [
    { driverCode: "multiple-critical-anomalies", summary: "Son 24 saatte 3 kritik anomali tespit edildi." },
    { driverCode: "alarm-context", summary: "Sıcaklık alarmı iki vardiyadır tekrarlanıyor." },
    { driverCode: "maintenance-history", summary: "Son 30 günde 2 arıza kaydı mevcut." },
  ],
  evidence: [
    { evidenceType: "condition-anomaly", evidenceReference: "A-1042", summary: "Kovan sıcaklığı normal bandın %14 üzerinde." },
    { evidenceType: "edge-alarm", evidenceReference: "AL-883", summary: "Yağ basıncı alarmı, 3 kez tekrarladı." },
  ],
  recommendation: {
    category: "urgent-authorized-technical-review",
    reviewPriority: "urgent",
    explanation: "Yetkili bakım teknisyeni makineyi yerinde incelemeli; üretim kararı insan onayıyla verilir.",
    ruleSetVersion: "MAINT-PRED-RISK-V1",
    generatedAtUtc: "2026-10-12T10:20:00Z",
    status: "open",
  },
};

const page = (title: string, body: string) =>
  `<!doctype html><html lang="tr"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width, initial-scale=1"><title>${title}</title>${(process.env.FIXTURE_CSS ?? "").split(";").filter(Boolean).map((href) => `<link rel="stylesheet" href="${href}">`).join("")}</head><body><div class="app-shell" style="display:block"><main class="content-viewport" id="main-content">${body}</main></div></body></html>`;

writeFileSync(path.join(outDir, "maintenance-risk-data.html"), page("Risk", renderToStaticMarkup(
  <MaintenancePredictiveRiskWorkspaceView workspace={workspace} initialAssessment={assessment} initialAssetReference="IM-07" />,
)));
writeFileSync(path.join(outDir, "maintenance-risk-detail-error.html"), page("Risk hata", renderToStaticMarkup(
  <MaintenancePredictiveRiskWorkspaceView workspace={workspace} initialAssetReference="IM-03" assessmentError="Seçili varlık için risk değerlendirmesi şu anda alınamadı." />,
)));
writeFileSync(path.join(outDir, "state-forbidden.html"), page("403", renderToStaticMarkup(
  <StatePage kind="forbidden" code="403" title="Bu sayfaya erişim yetkiniz yok" description="Hesabınızın rolü bu alanı görüntülemeye izin vermiyor. Erişim gerekiyorsa yöneticinizle iletişime geçin." actions={<StatePageLink href="/" primary>Bugün ekranına dön</StatePageLink>} />,
)));
writeFileSync(path.join(outDir, "state-error.html"), page("Hata", renderToStaticMarkup(
  <StatePage kind="unavailable" title="Bu sayfa şu anda yüklenemedi" description="Gerekli verilere ulaşılamadı veya beklenmeyen bir sorun oluştu. Sayfayı yeniden deneyin; sorun sürerse destek ekibine aşağıdaki referansı iletin." reference="1234567890" actions={<><button type="button" className="button button-medium button-primary">Yeniden dene</button><StatePageLink href="/">Bugün ekranına dön</StatePageLink></>} />,
)));
console.log("rendered", outDir);
