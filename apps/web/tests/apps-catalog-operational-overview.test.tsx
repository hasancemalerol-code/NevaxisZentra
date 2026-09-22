import assert from "node:assert/strict";
import test from "node:test";
import type { ReactElement } from "react";
import { renderToStaticMarkup } from "react-dom/server";

import { AppsFactoryOperationsSection } from "../src/features/apps-catalog/components/apps-factory-operations-section";
import { FactoryOperationsAnalyticsGrid } from "../src/features/factory-operations/components/factory-operations-analytics-grid";
import { mapDashboardShowcaseViewModel } from "../src/features/dashboard-showcase/server/dashboard-showcase-mapper";

const readyViewModel = mapDashboardShowcaseViewModel(
  {
    facilityDisplayName: "Creva Plast Enjeksiyon Tesisi",
    developmentPilotBanner: false,
    overview: {
      totalOrderCount: 4,
      activeOrderCount: 2,
      completedOrderCount: 2,
      pausedOrderCount: 0,
      readyOrderCount: 0,
      plannedQuantity: 50800,
        goodQuantity: 12345,
      scrapQuantity: 1000,
      remainingQuantity: 8120,
      completionRatePercent: 84,
      yieldPercent: 97.7,
      activeDowntimeCount: 1,
      totalDowntimeMinutes: 36,
      totalSetupMinutes: 0,
      manualCycleCount: 0,
      openExceptionCount: 1,
      unresolvedExceptionCount: 1,
      shiftCoverageCount: 2,
      operatorCoverageCount: 2,
      riskSignals: [],
      logisticsRisk: { riskLevel: "low", slackMinutes: 0, blocking: false, available: true },
    },
    orders: [
      {
        productionOrderId: "o1",
        referenceCode: "PO-1",
        title: "Order 1",
        status: "active",
        productReference: "SKU-A",
        sourceProductionPlanId: "p1",
        hasActiveException: false,
        hasActiveDowntime: false,
        planActual: {
          plannedStartAtUtc: "2026-09-22T08:00:00.000Z",
          plannedEndAtUtc: "2026-09-22T18:00:00.000Z",
          plannedQuantity: 22000,
          goodQuantity: 20000,
          scrapQuantity: 200,
          remainingQuantity: 2000,
          scheduleOverrun: false,
        },
      },
    ],
    shifts: Array.from({ length: 24 }, (_, hour) => ({
      shiftId: `s-${hour}`,
      shiftCode: `H${hour}`,
      shiftName: `Hour ${hour}`,
      activeWindowStartUtc: `2026-09-22T${String(hour).padStart(2, "0")}:00:00.000Z`,
      productionOrderCount: 1,
      goodQuantity: 1500 + hour * 20,
      scrapQuantity: 0,
      downtimeMinutes: 0,
      setupMinutes: 0,
      openExceptionCount: 0,
      assignedResourceCount: 2,
      assignedOperatorCount: 1,
      handoverState: "open",
    })),
    scrapRatePercent: 2.35,
    cycleDurationMs: 22800,
    latestOeePercent: 78.5,
    previousOeePercent: 76.2,
    latestAssessment: {
      id: "a1",
      machineAssetId: "m1",
      windowStartUtc: "2026-09-22T08:00:00.000Z",
      windowEndUtc: "2026-09-22T09:00:00.000Z",
      status: "calculated",
      statusLabel: "Hesaplandı",
      evaluatedAtUtc: "2026-09-22T10:00:00.000Z",
      oeePercent: 78.5,
      availabilityPercent: 82.1,
      performancePercent: 86.7,
      qualityPercent: 93.5,
    },
    qualityOverview: null,
    assessments: [],
  },
  "2026-09-22T10:00:00.000Z",
);

const emptyViewModel = mapDashboardShowcaseViewModel(
  {
    facilityDisplayName: "—",
    developmentPilotBanner: false,
    overview: null,
    orders: [],
    shifts: [],
    scrapRatePercent: null,
    cycleDurationMs: null,
    latestOeePercent: null,
    previousOeePercent: null,
    latestAssessment: null,
    qualityOverview: null,
    assessments: [],
  },
  "2026-09-22T10:00:00.000Z",
);

test("apps factory operations grid exposes nine canonical analytics surfaces", () => {
  const markup = renderToStaticMarkup(
    <FactoryOperationsAnalyticsGrid pendingLabel="Veri yok" viewModel={readyViewModel} /> as ReactElement,
  );

  assert.match(markup, /data-kpi-id="actual-production"/);
  assert.match(markup, /data-kpi-id="oee"/);
  assert.match(markup, /data-kpi-id="scrap-rate"/);
  assert.match(markup, /data-kpi-id="cycle-time"/);
  assert.match(markup, /data-kpi-id="quality-compliance"/);
  assert.match(markup, /data-kpi-id="target-achievement"/);
  assert.match(markup, /Saatlik Üretim Performansı/);
  assert.match(markup, /data-testid="apps-hourly-production-chart"/);
  assert.match(markup, /OEE Bileşenleri/);
  assert.match(markup, /data-testid="apps-oee-components-chart"/);
  assert.match(markup, /Üretim Dağılımı/);
  assert.match(markup, /data-testid="apps-production-distribution-chart"/);
});

test("apps factory operations section uses Veri yok for missing metrics", () => {
  const markup = renderToStaticMarkup(
    <AppsFactoryOperationsSection viewModel={emptyViewModel} /> as ReactElement,
  );

  assert.match(markup, /data-testid="apps-factory-operations-section"/);
  assert.match(markup, /Veri yok/);
});
