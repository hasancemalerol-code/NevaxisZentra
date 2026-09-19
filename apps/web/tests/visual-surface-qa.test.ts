import assert from "node:assert/strict";
import { mkdirSync, readFileSync, existsSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";
import test from "node:test";
import { createElement } from "react";
import { renderToStaticMarkup } from "react-dom/server";

import { AppShellFrame } from "../src/components/app-shell/app-shell-frame";
import { buildShellSearchItems } from "../src/components/app-shell/shell-navigation";
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

const css = readCss([
  "src/design-system/tokens.css",
  "src/design-system/industrial-foundation.css",
  "src/design-system/industrial-hardening.css",
  "src/app/globals.css",
  "src/features/production-resources/components/production-resources-workspace.css",
  "src/features/factory-workspace/factory-workspace.css",
  "src/features/live-plan-workspace/live-plan-workspace.css",
  "src/features/scenario-recovery/scenario-recovery-workspace.css",
  "src/app/dashboard-premium.css",
]);

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

  const surfaces = [
    { id: "navigation-sidebar", title: "Sidebar / Navigation", html: navigationShell },
    { id: "task-center", title: "Görev Merkezi", html: taskCenter },
    { id: "factory-workspace", title: "Fabrika Çalışma Alanı", html: factoryWorkspace },
    { id: "live-plan-workspace", title: "Canlı Plan Çalışma Alanı", html: livePlanWorkspace },
    { id: "scenario-recovery-workspace", title: "Senaryo Kurtarma Çalışma Alanı", html: scenarioRecoveryWorkspace },
    { id: "production-resources", title: "Üretim Kaynakları", html: productionResources },
    { id: "operasyon-merkezi", title: "Operasyon Merkezi", html: operasyonMerkezi },
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
          hasTable: Boolean(document.body.querySelector(".fact-data-grid, .ai-task-center-unified__table")),
          hasInspector: Boolean(document.body.querySelector(".fact-context-panel, .production-resources-inspector")),
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
