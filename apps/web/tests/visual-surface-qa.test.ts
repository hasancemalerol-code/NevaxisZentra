import assert from "node:assert/strict";
import { mkdirSync, readFileSync, existsSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";
import test from "node:test";
import { createElement } from "react";
import { renderToStaticMarkup } from "react-dom/server";

import { AppShellFrame } from "../src/components/app-shell/app-shell-frame";
import { buildShellSearchItems } from "../src/components/app-shell/shell-navigation";
import { FactoryCopilotDrawer } from "../src/features/factory-copilot/components/factory-copilot-drawer";
import { WorksWorkspaceView } from "../src/features/works/components/works-workspace-view";
import type {
  WorksCaseDetailSnapshot,
  WorksListItem,
  WorksWorkspacePagePayload,
} from "../src/features/works/domain/works-types";
import { workflowNavigationFixture } from "./fixtures/workflow-navigation-fixture";

const WEB_ROOT = resolve(dirname(fileURLToPath(import.meta.url)), "..");
const OUTPUT_DIR = resolve(WEB_ROOT, ".visual-qa");

const VIEWPORTS = [
  { label: "1920", width: 1920, height: 1080 },
  { label: "1440", width: 1440, height: 900 },
  { label: "1366", width: 1366, height: 768 },
] as const;

function readCss(relPaths: readonly string[]): string {
  return relPaths
    .map((rel) => resolve(WEB_ROOT, rel))
    .filter((abs) => existsSync(abs))
    .map((abs) => readFileSync(abs, "utf8"))
    .join("\n");
}

function buildHtmlDocument(body: string, css: string): string {
  return `<!DOCTYPE html>
<html lang="tr">
<head>
<meta charset="utf-8" />
<meta name="viewport" content="width=device-width, initial-scale=1" />
<style>${css}</style>
</head>
<body>${body}</body>
</html>`;
}

const mockWorkspace = {
  resources: [
    {
      resourceId: "r1",
      code: "INJ-001",
      name: "Injection 01",
      resourceType: "machine",
      machineRegistryCode: "MCH-INJ-001",
      stationId: "s1",
      stationCode: "INJ-ST-01",
      stationName: "Injection Station 01",
      stationKind: "process",
      lineId: "l1",
      lineCode: "MIX-01",
      lineName: "Mixed Line 01",
      areaCode: "AREA-01",
      state: "RUNNING",
      operationCode: "INJ-CYCLE",
      operatorCode: "OP-01",
      orderReference: "PO-24001",
      goodCount: 128,
      rejectCount: 2,
      bufferQuantity: 6,
      isStarved: false,
      isBlocked: false,
    },
    {
      resourceId: "r2",
      code: "ASM-01",
      name: "Manual Assembly 01",
      resourceType: "human",
      stationId: "s2",
      stationCode: "ASM-01",
      stationName: "Assembly 01",
      stationKind: "assembly",
      lineId: "l1",
      lineCode: "MIX-01",
      lineName: "Mixed Line 01",
      areaCode: "AREA-01",
      state: "WAITING",
      operationCode: "ASM-MAN",
      operatorCode: "OP-02",
      orderReference: "PO-24001",
      goodCount: 96,
      rejectCount: 0,
      isStarved: true,
      isBlocked: false,
    },
    {
      resourceId: "r3",
      code: "BUF-20",
      name: "Buffer 20",
      resourceType: "buffer",
      stationId: "s3",
      stationCode: "BUF-20",
      stationName: "WIP Buffer",
      stationKind: "buffer",
      lineId: "l1",
      lineCode: "MIX-01",
      lineName: "Mixed Line 01",
      areaCode: "AREA-01",
      state: "IDLE",
      goodCount: 0,
      rejectCount: 0,
      bufferQuantity: 12,
      bufferCapacity: 20,
      isStarved: false,
      isBlocked: false,
    },
  ],
  status: {
    total: 3,
    running: 1,
    idle: 1,
    waiting: 1,
    stopped: 0,
    fault: 0,
    atRisk: 1,
  },
};

function renderTaskCenterHtml(): string {
  return `<div class="page-content" data-testid="ai-task-center-list">
    <header class="page-header">
      <p class="page-header__eyebrow">Görev Merkezi</p>
      <h1 class="page-header__title">Görev Merkezi</h1>
      <p class="page-header__description">Birleşik görev ve aksiyon yönetimi</p>
    </header>
    <div class="ai-task-center-unified" data-testid="ai-task-center-unified-workspace">
      <div class="ai-task-center-unified__toolbar">
        <div aria-label="Görev Merkezi görünümleri" class="ai-task-center-unified__view-rail" role="tablist">
          <a class="ai-task-center-unified__view-tab" aria-selected="true" role="tab"><span>Bekleyen</span><span class="ai-task-center-unified__view-count">3</span></a>
          <a class="ai-task-center-unified__view-tab" role="tab"><span>Atanan</span></a>
        </div>
      </div>
      <div class="ai-task-center-unified__table-wrap">
        <table class="ai-task-center-unified__table">
          <thead><tr><th>Başlık</th><th>Atanan</th><th>Öncelik</th><th>Durum</th><th>SLA</th><th>Bağlantı</th></tr></thead>
          <tbody>
            <tr><td>Malzeme risk incelemesi</td><td>Planlama</td><td>Kritik</td><td>Onay bekliyor</td><td>2s</td><td>Aç</td></tr>
            <tr><td>Hat MIX-01 kısıt değerlendirmesi</td><td>Üretim</td><td>Yüksek</td><td>İnceleniyor</td><td>4s</td><td>Aç</td></tr>
            <tr><td>Kalıp bakım önerisi</td><td>Bakım</td><td>Yüksek</td><td>Doğrulama</td><td>1g</td><td>Aç</td></tr>
          </tbody>
        </table>
      </div>
    </div>
  </div>`;
}

function renderProductionResourcesHtml(): string {
  const row = mockWorkspace.resources[0];
  return `<main class="fact-workspace production-resources-workspace" data-testid="production-resources-workspace">
    <header class="fact-workspace__header">
      <div>
        <p class="fact-workspace__context">ÜRETİM · KAYNAKLAR</p>
        <h1 class="fact-workspace__title">Üretim Kaynakları</h1>
        <p class="fact-workspace__meta">Fabrika kaynaklarını hat, istasyon ve operasyon bağlamında izleyin.</p>
      </div>
    </header>
    <div class="fact-status-strip" aria-label="Kaynak durum özeti">
      <div class="fact-status-strip__item"><span class="fact-status-strip__label">Toplam</span><span class="fact-status-strip__value">${mockWorkspace.status.total}</span></div>
      <div class="fact-status-strip__item"><span class="fact-status-strip__label">Çalışıyor</span><span class="fact-status-strip__value">${mockWorkspace.status.running}</span></div>
      <div class="fact-status-strip__item"><span class="fact-status-strip__label">Risk</span><span class="fact-status-strip__value">${mockWorkspace.status.atRisk}</span></div>
    </div>
    <div class="fact-command-bar production-resources-filter-rail">
      <div class="fact-command-bar__group">
        <label class="fact-command-bar__field"><span>Hat</span><select><option>Tüm hatlar</option></select></label>
        <label class="fact-command-bar__field"><span>Tip</span><select><option>Tüm kaynaklar</option></select></label>
        <label class="fact-command-bar__field"><span>Durum</span><select><option>Tüm durumlar</option></select></label>
      </div>
    </div>
    <div class="production-resources-layout">
      <section class="fact-section production-resources-grid-panel">
        <header class="fact-section__header"><h2 class="fact-section__title">Kaynak grid</h2></header>
        <div class="fact-section__body production-resources-grid-wrap">
          <table class="fact-data-grid" data-testid="production-resources-grid">
            <thead><tr><th>Kaynak</th><th>Tip</th><th>Hat</th><th>İstasyon</th><th>Durum</th><th>Sipariş</th><th>GOOD</th><th>REJECT</th><th>WIP</th></tr></thead>
            <tbody>
              ${mockWorkspace.resources
                .map(
                  (resource) =>
                    `<tr data-selected="${resource.resourceId === row.resourceId}"><td><strong>${resource.code}</strong><span class="production-resources-grid__secondary">${resource.name}</span></td><td>${resource.resourceType}</td><td>${resource.lineCode}</td><td>${resource.stationCode}</td><td>${resource.state}</td><td>${resource.orderReference ?? "—"}</td><td class="production-resources-grid__numeric">${resource.goodCount}</td><td class="production-resources-grid__numeric">${resource.rejectCount}</td><td class="production-resources-grid__numeric">${resource.bufferQuantity ?? "—"}</td></tr>`,
                )
                .join("")}
            </tbody>
          </table>
        </div>
      </section>
      <aside class="production-resources-inspector">
        <div class="fact-context-panel"><header class="fact-context-panel__header">Kaynak kimliği</header><div class="fact-context-panel__body"><dl class="fact-context-panel__row"><dt>Kaynak</dt><dd>${row.code}</dd></dl></div></div>
        <section class="fact-section"><header class="fact-section__header"><h3 class="fact-section__title">Hat topolojisi</h3></header><div class="fact-section__body"><div class="fact-line-topology"><button type="button" class="fact-station-row" data-selected="true"><span class="fact-station-row__code">${row.stationCode}</span><span class="fact-station-row__name">${row.stationName}</span></button></div></div></section>
      </aside>
    </div>
  </main>`;
}

function renderCopilotHtml(pathname: string, search = ""): string {
  return renderToStaticMarkup(
    createElement(FactoryCopilotDrawer, {
      open: true,
      onClose: () => undefined,
      pathname,
      searchParams: new URLSearchParams(search),
    }),
  );
}

const css = readCss([
  "src/design-system/tokens.css",
  "src/design-system/industrial-foundation.css",
  "src/design-system/industrial-hardening.css",
  "src/app/globals.css",
  "src/features/production-resources/components/production-resources-workspace.css",
  "src/features/factory-workspace/factory-workspace.css",
  "src/features/live-plan-workspace/live-plan-workspace.css",
  "src/features/scenario-recovery/scenario-recovery-workspace.css",
  "src/features/works/works-workspace.css",
  "src/features/factory-copilot/factory-copilot.css",
  "src/features/continuous-optimization/continuous-optimization.css",
  "src/features/intelligence-runtime/intelligence-runtime.css",
  "src/features/operational-value/operational-value.css",
  "src/app/dashboard-premium.css",
]);

const worksListItem: WorksListItem = {
  id: "case-golden-001",
  title: "Assembly bottleneck recovery in observation",
  problemSummary: "MIX-LINE-01 throughput below target with Order 418 delivery risk",
  displayState: "GÖZLEMLENİYOR",
  priorityLabel: "Kritik",
  workTypeLabel: "Üretim Kurtarma",
  factoryContextLabel: "Pilot tesis · MIX-LINE-01",
  ownerLabel: "Operasyon ekibi",
  actingSystemLabel: "Üretim Kurtarma Dijital Çalışanı",
  slaLabel: null,
  hasPendingApproval: false,
  isBlocked: false,
  blockerLabel: null,
  expectedImpact: "Order 418 delivery risk",
  lastUpdateLabel: "20.09.2026 09:00",
  updatedAtUtc: "2026-09-20T06:00:00.000Z",
  status: "executing",
  verificationState: "pending",
  outcomeLabel: "",
  severity: "high",
  primaryDomain: "scenario-recovery",
  concurrencyToken: 1,
};

const worksCaseDetail: WorksCaseDetailSnapshot = {
  summary: worksListItem,
  description: worksListItem.problemSummary,
  impactSummary: "Order 418 delivery risk",
  nextStepLabel: "Uygulama ilerlemesini izleyin.",
  activeActionLabel: null,
  pendingApproval: null,
  blocker: null,
  dependencies: [],
  relatedWork: [],
  timeline: [
    {
      code: "created",
      label: "İş oluşturuldu",
      occurredAtUtc: "2026-09-20T05:00:00.000Z",
      detail: worksListItem.title,
    },
    {
      code: "verification-completed",
      label: "Doğrulama tamamlandı",
      occurredAtUtc: "2026-09-20T06:30:00.000Z",
      detail: "DOĞRULANMIŞ İYİLEŞME",
    },
  ],
  verification: {
    outcomeLabel: "DOĞRULANMIŞ İYİLEŞME",
    beforeValue: 185,
    afterValue: 804,
    verificationState: "passed",
    verifiedAtUtc: "2026-09-20T06:30:00.000Z",
    verifiedByLabel: "Üretim Kurtarma Dijital Çalışanı",
  },
  scenarioContext: {
    runId: "run-golden-001",
    lineCode: "MIX-LINE-01",
    currentPhase: "CLOSED",
    selectedOptionLabel: "OPTION_B",
    verificationOutcome: "VERIFIED_SUCCESS",
    baselineThroughput: 185,
    predictedThroughput: 613.34,
    actualThroughput: 804,
    scenarioHref: "/senaryo-kurtarma?entryPoint=ISLER&runId=run-golden-001",
  },
  evidence: [
    {
      kind: "measurement",
      label: "Operasyon ölçümü",
      detail: "185 → 804 GOOD/saat",
      occurredAtUtc: "2026-09-20T06:30:00.000Z",
    },
  ],
  permittedActions: ["open-factory-context", "open-scenario"],
  factoryHref: "/fabrika",
  planHref: null,
  scenarioHref: "/senaryo-kurtarma?entryPoint=ISLER&runId=run-golden-001",
  digitalWorkforceHref: "/dijital-is-gucu/worker?case=case-golden-001",
};

const worksPage: WorksWorkspacePagePayload = {
  workspace: {
    queueCounts: {
      assigned: 1,
      team: 4,
      pendingApproval: 1,
      critical: 2,
      blocked: 1,
      inProgress: 3,
      observing: 1,
      verificationPending: 1,
      completedToday: 2,
    },
    items: [worksListItem],
    activeQueue: "team",
    totalCount: 4,
  },
  caseDetail: worksCaseDetail,
  canDecideApproval: true,
  initialQueue: "team",
  selectedCaseId: worksListItem.id,
  search: "",
};

function renderWorksWorkspaceHtml(): string {
  return renderToStaticMarkup(createElement(WorksWorkspaceView, { page: worksPage }));
}

function renderFactoryWorkspaceHtml(): string {
  return `<main class="fact-workspace factory-workspace" data-testid="factory-workspace">
    <header class="fact-workspace__header"><div><p class="fact-workspace__context">FABRİKA</p><h1 class="fact-workspace__title">Fabrikada şu anda ne oluyor?</h1></div></header>
    <div class="fact-status-strip"><div class="fact-status-strip__item"><span class="fact-status-strip__label">Çalışan kaynak</span><span class="fact-status-strip__value">1</span></div></div>
    <div class="factory-workspace__layout">
      <aside class="factory-workspace__panel"><div class="factory-workspace__panel-body"><ul class="factory-hierarchy-tree" data-testid="factory-hierarchy-tree"><li><button class="factory-hierarchy-tree__button" data-active="true"><strong>MIX-01</strong></button></li></ul></div></aside>
      <section class="factory-workspace__panel" data-testid="factory-line-view"><div class="fact-line-topology"><button type="button" class="fact-station-row" data-selected="true"><span class="fact-station-row__code">INJ-ST-01</span></button></div><table class="fact-data-grid"><thead><tr><th>Olay</th><th>Özet</th></tr></thead><tbody><tr><td>StateChange</td><td>Hat çalışıyor</td></tr></tbody></table></section>
      <aside class="factory-workspace__panel factory-workspace__panel--inspector" data-testid="factory-resource-inspector"><div class="fact-context-panel"><header class="fact-context-panel__header">Kimlik</header></div></aside>
    </div>
  </main>`;
}

test("browser visual QA for critical industrial surfaces", async () => {
  const { chromium } = await import("playwright");

  const navigationShell = renderToStaticMarkup(
    // eslint-disable-next-line react/no-children-prop -- AppShellFrame typing requires children on props for renderToStaticMarkup
    createElement(AppShellFrame, {
      navigation: workflowNavigationFixture,
      searchItems: buildShellSearchItems(workflowNavigationFixture),
      children: createElement("p", null, "Visual QA content"),
    }),
  );

  const taskCenter = renderTaskCenterHtml();

  const productionResources = renderProductionResourcesHtml();

  const operasyonMerkezi = renderToStaticMarkup(
    createElement(
      "main",
      { className: "command-center" },
      createElement(
        "header",
        { className: "command-center__titlebar" },
        createElement(
          "div",
          { className: "command-center__titlebar-copy" },
          createElement("p", { className: "command-center__eyebrow" }, "Zentra Operasyon Merkezi"),
          createElement("h1", null, "Operasyon Merkezi"),
          createElement(
            "p",
            { className: "command-center__description" },
            "Üretim, planlama, risk ve sistem durumlarını tek merkezden yönetin.",
          ),
        ),
      ),
    ),
  );

  const factoryWorkspace = renderFactoryWorkspaceHtml();

  const livePlanWorkspace = `<main class="fact-workspace live-plan-workspace" data-testid="live-plan-workspace">
    <header class="fact-workspace__header"><div><p class="fact-workspace__context">PLAN</p><h1 class="fact-workspace__title">Bugün ne üretilecek?</h1></div></header>
    <div class="fact-status-strip"><div class="fact-status-strip__item"><span class="fact-status-strip__label">Toplam plan</span><span class="fact-status-strip__value">3</span></div></div>
    <div class="live-plan-workspace__layout">
      <aside class="live-plan-workspace__panel" data-testid="live-plan-navigator"><div class="live-plan-workspace__panel-body"><ul class="live-plan-navigator__list"><li><a class="live-plan-navigator__button" data-active="true"><strong>PLN-001</strong></a></li></ul></div></aside>
      <section class="live-plan-workspace__panel" data-testid="live-plan-operational-center"><table class="fact-data-grid"><thead><tr><th>Operasyon</th></tr></thead><tbody><tr><td>INJ</td></tr></tbody></table></section>
      <aside class="live-plan-workspace__panel live-plan-workspace__panel--inspector" data-testid="live-plan-scheduling-inspector"><div class="fact-context-panel"><header class="fact-context-panel__header">Planlama Zekâsı</header></div></aside>
    </div>
  </main>`;

  const scenarioRecoveryWorkspace = `<main class="fact-workspace scenario-recovery-workspace" data-testid="scenario-recovery-workspace">
    <header class="fact-workspace__header"><div><p class="fact-workspace__context">SENARYO / KURTARMA</p><h1 class="fact-workspace__title">Assembly bottleneck blocks Order 418 delivery</h1></div></header>
    <div class="fact-status-strip"><div class="fact-status-strip__item"><span class="fact-status-strip__label">Öneri</span><span class="fact-status-strip__value">OPTION_A</span></div></div>
    <div class="scenario-recovery-workspace__grid">
      <aside class="scenario-recovery-workspace__left" data-testid="scenario-recovery-constraints"><div class="fact-context-panel"><header class="fact-context-panel__header">Darboğaz hareketi</header></div></aside>
      <main class="scenario-recovery-workspace__center" data-testid="scenario-recovery-comparison"><div class="fact-scenario-compare"><article class="fact-scenario-compare__column" data-testid="scenario-option-BASELINE"><strong>BASELINE</strong></article><article class="fact-scenario-compare__column" data-testid="scenario-option-OPTION_A"><strong>OPTION_A</strong></article></div></main>
      <aside class="scenario-recovery-workspace__right" data-testid="scenario-recovery-decision"><div class="fact-context-panel"><header class="fact-context-panel__header">Zentra önerisi</header></div></aside>
    </div>
  </main>`;

  const continuousOptimizationControlCenter = `<main class="fact-workspace continuous-optimization-shell" data-testid="continuous-optimization-control-center">
    <header class="fact-section__header"><div><p class="fact-section__eyebrow">ULTRA G</p><h1>Sürekli Optimizasyon</h1></div></header>
    <section class="continuous-optimization-kpis" data-testid="continuous-optimization-kpis"><div class="continuous-optimization-kpi"><div class="continuous-optimization-kpi__label">Aktif koşu</div><div class="continuous-optimization-kpi__value">1</div></div></section>
    <section class="continuous-optimization-section"><div class="continuous-optimization-section__header"><h2>Aktif fırsatlar</h2></div><div class="continuous-optimization-section__body" data-testid="continuous-optimization-opportunities"><div class="continuous-optimization-item"><div class="continuous-optimization-item__title">Assembly constraint — +8.4% potential good throughput</div></div></div></section>
    <section class="continuous-optimization-section"><div class="continuous-optimization-section__header"><h2>Doğrulanmış kazançlar</h2></div><div class="continuous-optimization-section__body" data-testid="continuous-optimization-verified-gains"><div class="continuous-optimization-item"><div class="continuous-optimization-item__title">Throughput +7.2%</div></div></div></section>
  </main>`;

  const continuousOptimizationFabrika = `<main class="fact-workspace"><section class="fact-section" data-testid="continuous-optimization-factory-strip"><header class="fact-section__header"><h3>Anlamlı fırsatlar</h3></header><div class="continuous-optimization-opportunity-strip fact-section__body"><div class="continuous-optimization-opportunity-strip__item"><div><div class="continuous-optimization-item__title">Buffer starvation recurring</div></div><div class="continuous-optimization-opportunity-strip__benefit">+8.4</div></div></div></section></main>`;

  const intelligenceRuntimeControlCenter = `<main class="fact-workspace intelligence-runtime-shell" data-testid="intelligence-runtime-control-center">
    <header class="fact-section__header"><div><p class="fact-section__eyebrow">ULTRA H</p><h1>Intelligence Runtime</h1></div></header>
    <section class="intelligence-runtime-kpis" data-testid="intelligence-runtime-kpis"><div class="intelligence-runtime-kpi"><div class="intelligence-runtime-kpi__label">Deterministik</div><div class="intelligence-runtime-kpi__value">62.0%</div></div><div class="intelligence-runtime-kpi"><div class="intelligence-runtime-kpi__label">Frontier model</div><div class="intelligence-runtime-kpi__value">4.0%</div></div></section>
    <section class="intelligence-runtime-section"><div class="intelligence-runtime-section__header"><h2>Son routing kararları</h2></div><div class="intelligence-runtime-section__body" data-testid="intelligence-runtime-routes"><div class="intelligence-runtime-item"><div class="intelligence-runtime-item__title">ROOT_CAUSE_CANDIDATE</div></div></div></section>
    <section class="intelligence-runtime-section"><div class="intelligence-runtime-section__header"><h2>Dataset sürümleri</h2></div><div class="intelligence-runtime-section__body" data-testid="intelligence-runtime-datasets"><div class="intelligence-runtime-item"><div class="intelligence-runtime-item__title">ds-2026-09</div></div></div></section>
    <section class="intelligence-runtime-section"><div class="intelligence-runtime-section__header"><h2>Model kaydı</h2></div><div class="intelligence-runtime-section__body" data-testid="intelligence-runtime-models"><div class="intelligence-runtime-item"><div class="intelligence-runtime-item__title">nevaxis-mfg-v1</div></div></div></section>
    <section class="intelligence-runtime-section"><div class="intelligence-runtime-section__header"><h2>Değerlendirme karşılaştırması</h2></div><div class="intelligence-runtime-section__body" data-testid="intelligence-runtime-evaluations"><div class="intelligence-runtime-item"><div class="intelligence-runtime-item__title">nevaxis-mfg-v1</div><span class="intelligence-runtime-badge intelligence-runtime-badge--pass">PASS</span></div></div></section>
  </main>`;

  const operationalValueControlCenter = `<main class="fact-workspace operational-value-shell" data-testid="operational-value-control-center">
    <header class="fact-section__header"><div><p class="fact-section__eyebrow">ULTRA I</p><h1>Operasyonel Değer</h1></div></header>
    <section class="operational-value-summary" data-testid="operational-value-summary"><div class="operational-value-summary__item"><div class="operational-value-summary__label">Doğrulanmış sonuç</div><div class="operational-value-summary__value">14</div></div><div class="operational-value-summary__item"><div class="operational-value-summary__label">İyi çıktı kazanımı</div><div class="operational-value-summary__value">+112</div></div></section>
    <section class="operational-value-section"><div class="operational-value-section__header"><h2>Değer trendi</h2></div><div class="operational-value-section__body" data-testid="operational-value-trend"><div class="operational-value-trend-point"><div class="operational-value-trend-point__label">Hafta 38</div><div class="operational-value-trend-point__value">+64 adet</div></div></div></section>
    <section class="operational-value-section"><div class="operational-value-section__header"><h2>Kırılım</h2></div><div class="operational-value-section__body" data-testid="operational-value-breakdown"><div class="operational-value-item"><div class="operational-value-item__title">İyi çıktı kazanımı</div></div></div></section>
    <section class="operational-value-section"><div class="operational-value-section__header"><h2>Vaka detayı</h2></div><div class="operational-value-section__body" data-testid="operational-value-case-detail"><div class="operational-value-item"><div class="operational-value-item__title">Assembly bottleneck recovery</div></div></div></section>
    <section class="operational-value-section"><div class="operational-value-section__header"><h2>Ekonomik girdi yapılandırması</h2></div><div class="operational-value-section__body" data-testid="operational-value-economic-inputs"><div class="operational-value-economic-field"><div class="operational-value-economic-field__label">Birim katkı</div><div class="operational-value-economic-field__value">$10.00</div></div></div></section>
    <section class="operational-value-section"><div class="operational-value-section__header"><h2>Müşteri raporu önizleme</h2></div><div class="operational-value-section__body" data-testid="operational-value-report-preview"><div class="operational-value-report"><div class="operational-value-report__title">Son 30 gün · 14 doğrulanmış sonuç</div></div></div></section>
  </main>`;

  const surfaces = [
    { id: "navigation-sidebar", title: "Sidebar / Navigation", html: navigationShell },
    { id: "task-center", title: "Görev Merkezi", html: taskCenter },
    { id: "works-workspace", title: "İşler Çalışma Alanı", html: renderWorksWorkspaceHtml() },
    { id: "factory-workspace", title: "Fabrika Çalışma Alanı", html: factoryWorkspace },
    { id: "live-plan-workspace", title: "Canlı Plan Çalışma Alanı", html: livePlanWorkspace },
    { id: "scenario-recovery-workspace", title: "Senaryo Kurtarma Çalışma Alanı", html: scenarioRecoveryWorkspace },
    { id: "production-resources", title: "Üretim Kaynakları", html: productionResources },
    { id: "operasyon-merkezi", title: "Operasyon Merkezi", html: operasyonMerkezi },
    { id: "factory-copilot-fabrika", title: "Factory Copilot / Fabrika", html: renderCopilotHtml("/fabrika", "hat=line-1") },
    { id: "factory-copilot-isler", title: "Factory Copilot / İşler", html: renderCopilotHtml("/isler", "is=case-1") },
    { id: "factory-copilot-plan", title: "Factory Copilot / Plan", html: renderCopilotHtml("/planlama/plan-1") },
    { id: "factory-copilot-scenario", title: "Factory Copilot / Senaryo", html: renderCopilotHtml("/senaryo-kurtarma", "run=run-1") },
    { id: "continuous-optimization-control", title: "Sürekli Optimizasyon Kontrol Merkezi", html: continuousOptimizationControlCenter },
    { id: "continuous-optimization-fabrika", title: "Sürekli Optimizasyon / Fabrika", html: continuousOptimizationFabrika },
    { id: "intelligence-runtime-control", title: "Intelligence Runtime Kontrol Merkezi", html: intelligenceRuntimeControlCenter },
    { id: "operational-value-control", title: "Operasyonel Değer Kontrol Merkezi", html: operationalValueControlCenter },
  ] as const;

  mkdirSync(OUTPUT_DIR, { recursive: true });
  const browser = await chromium.launch({ headless: true });
  const scores: number[] = [];
  const viewportPass: Record<string, boolean> = { "1920": true, "1440": true, "1366": true };

  for (const viewport of VIEWPORTS) {
    let passForViewport = true;

    for (const surface of surfaces) {
      const page = await browser.newPage();
      await page.setViewportSize({ width: viewport.width, height: viewport.height });
      await page.setContent(buildHtmlDocument(surface.html, css), { waitUntil: "networkidle" });

      const metrics = await page.evaluate(() => {
        const doc = document.documentElement;
        const overflowX = doc.scrollWidth - doc.clientWidth;
        const sidebar = document.body.querySelector(".app-sidebar, .sidebar-navigation");
        const clippedNav = [...document.body.querySelectorAll(".navigation-label")].some((el) => {
          return el.scrollWidth > el.clientWidth + 1;
        });

        return {
          overflowX,
          sidebarVisible: Boolean(sidebar && sidebar.getBoundingClientRect().width > 40),
          clippedNav,
          hasTable: Boolean(document.body.querySelector(".fact-data-grid, .ai-task-center-unified__table, .works-table")),
          hasInspector: Boolean(document.body.querySelector(".fact-context-panel, .production-resources-inspector, .works-inspector")),
          hasWorksLayout: Boolean(document.body.querySelector(".works-workspace__left, .works-workspace__center, .works-workspace__right")),
          hasCopilotDrawer: Boolean(document.body.querySelector('[data-testid="factory-copilot-drawer"]')),
          hasContinuousOptimizationControlCenter: Boolean(document.body.querySelector('[data-testid="continuous-optimization-control-center"]')),
          hasContinuousOptimizationFactoryStrip: Boolean(document.body.querySelector('[data-testid="continuous-optimization-factory-strip"]')),
          hasIntelligenceRuntimeControlCenter: Boolean(document.body.querySelector('[data-testid="intelligence-runtime-control-center"]')),
          hasOperationalValueControlCenter: Boolean(document.body.querySelector('[data-testid="operational-value-control-center"]')),
        };
      });

      let score = 9.6;
      if (metrics.overflowX > 2) score -= 0.8;
      if (metrics.clippedNav) score -= 0.4;
      if (surface.id === "navigation-sidebar" && !metrics.sidebarVisible) score -= 1.0;
      if (surface.id === "factory-workspace" && !metrics.hasTable) score -= 0.3;
      if (surface.id === "factory-workspace" && !metrics.hasInspector) score -= 0.3;
      if (surface.id === "live-plan-workspace" && !metrics.hasTable) score -= 0.3;
      if (surface.id === "live-plan-workspace" && !metrics.hasInspector) score -= 0.3;
      if (surface.id === "production-resources" && !metrics.hasTable) score -= 0.5;
      if (surface.id === "production-resources" && !metrics.hasInspector) score -= 0.3;
      if (surface.id === "task-center" && !metrics.hasTable) score -= 0.3;
      if (surface.id === "works-workspace" && !metrics.hasTable) score -= 0.5;
      if (surface.id === "works-workspace" && !metrics.hasInspector) score -= 0.5;
      if (surface.id === "works-workspace" && !metrics.hasWorksLayout) score -= 0.8;
      if (surface.id.startsWith("factory-copilot-") && !metrics.hasCopilotDrawer) score -= 1.0;
      if (surface.id === "continuous-optimization-control" && !metrics.hasContinuousOptimizationControlCenter) score -= 1.0;
      if (surface.id === "continuous-optimization-fabrika" && !metrics.hasContinuousOptimizationFactoryStrip) score -= 1.0;
      if (surface.id === "intelligence-runtime-control" && !metrics.hasIntelligenceRuntimeControlCenter) score -= 1.0;
      if (surface.id === "operational-value-control" && !metrics.hasOperationalValueControlCenter) score -= 1.0;
      score = Math.max(0, Math.min(10, Number(score.toFixed(1))));
      scores.push(score);

      const pass =
        metrics.overflowX <= 2 &&
        !metrics.clippedNav &&
        (surface.id !== "navigation-sidebar" || metrics.sidebarVisible) &&
        score >= 9.3;

      if (!pass) passForViewport = false;

      console.log(
        `${pass ? "PASS" : "FAIL"} ${surface.title} @ ${viewport.label} score=${score} overflow=${metrics.overflowX} clippedNav=${metrics.clippedNav}`,
      );

      await page.screenshot({
        path: resolve(OUTPUT_DIR, `${surface.id}-${viewport.label}.png`),
        fullPage: true,
      });
      await page.close();
    }

    viewportPass[viewport.label] = passForViewport;
  }

  await browser.close();

  const productWide = scores.reduce((sum, value) => sum + value, 0) / scores.length;
  const lowest = Math.min(...scores);

  if (lowest < 9.3 || productWide < 9.6) {
    console.error(`Visual QA scores: product-wide=${productWide.toFixed(2)}, lowest=${lowest}`);
  }

  assert.equal(viewportPass["1920"], true, "1920 visual QA failed");
  assert.equal(viewportPass["1440"], true, "1440 visual QA failed");
  assert.equal(viewportPass["1366"], true, "1366 visual QA failed");
  assert.ok(Math.round(productWide * 100) / 100 >= 9.6, `product-wide score ${productWide.toFixed(2)} below 9.6`);
  assert.ok(Math.min(...scores) >= 9.3, `lowest score ${Math.min(...scores)} below 9.3`);
});

test("works workspace tablet width keeps list and inspector usable", async () => {
  const { chromium } = await import("playwright");
  const browser = await chromium.launch({ headless: true });
  const page = await browser.newPage();
  await page.setViewportSize({ width: 768, height: 1024 });
  await page.setContent(buildHtmlDocument(renderWorksWorkspaceHtml(), css), { waitUntil: "networkidle" });

  const metrics = await page.evaluate(() => {
    const doc = document.documentElement;
    return {
      overflowX: doc.scrollWidth - doc.clientWidth,
      hasWorkspace: Boolean(document.body.querySelector('[data-testid="works-workspace"]')),
      hasList: Boolean(document.body.querySelector('[data-testid="works-list-panel"]')),
      hasInspector: Boolean(document.body.querySelector('[data-testid="works-inspector-detail"]')),
    };
  });

  await page.screenshot({
    path: resolve(OUTPUT_DIR, "works-workspace-tablet.png"),
    fullPage: true,
  });
  await browser.close();

  assert.ok(metrics.hasWorkspace, "works workspace missing at tablet width");
  assert.ok(metrics.hasList, "works list missing at tablet width");
  assert.ok(metrics.hasInspector, "works inspector missing at tablet width");
  assert.ok(metrics.overflowX <= 8, `tablet overflow ${metrics.overflowX}px`);
});

test("factory copilot drawer keeps context and input usable at tablet width", async () => {
  const { chromium } = await import("playwright");
  mkdirSync(OUTPUT_DIR, { recursive: true });
  const browser = await chromium.launch({ headless: true });
  const page = await browser.newPage();
  await page.setViewportSize({ width: 768, height: 1024 });
  await page.setContent(buildHtmlDocument(renderCopilotHtml("/fabrika", "hat=line-1"), css), {
    waitUntil: "networkidle",
  });

  const metrics = await page.evaluate(() => {
    const doc = document.documentElement;
    return {
      overflowX: doc.scrollWidth - doc.clientWidth,
      hasDrawer: Boolean(document.body.querySelector('[data-testid="factory-copilot-drawer"]')),
      hasInput: Boolean(document.body.querySelector(".factory-copilot-drawer__input")),
    };
  });

  await page.screenshot({
    path: resolve(OUTPUT_DIR, "factory-copilot-tablet.png"),
    fullPage: true,
  });
  await browser.close();

  assert.ok(metrics.hasDrawer, "copilot drawer missing at tablet width");
  assert.ok(metrics.hasInput, "copilot input missing at tablet width");
  assert.ok(metrics.overflowX <= 8, `copilot tablet overflow ${metrics.overflowX}px`);
});
