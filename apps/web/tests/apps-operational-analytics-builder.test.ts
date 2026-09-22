import assert from "node:assert/strict";
import test from "node:test";

import { buildAppsOperationalAnalytics } from "../src/features/apps-catalog/server/apps-operational-analytics-builder";

test("apps operational analytics returns empty model without production signal", () => {
  const model = buildAppsOperationalAnalytics({
    overview: null,
    shifts: [],
    resources: [],
    scrapRatePercent: null,
    scrapQuantity: null,
    goodQuantityFromScrap: null,
    downtimeActiveCount: null,
    downtimeTotalMinutes: null,
  });

  assert.equal(model.state, "empty");
  assert.equal(model.outputTrend.length, 0);
});

test("apps operational analytics builds trend and resource charts from live-shaped data", () => {
  const model = buildAppsOperationalAnalytics({
    overview: {
      totalOrderCount: 2,
      activeOrderCount: 1,
      completedOrderCount: 1,
      pausedOrderCount: 0,
      readyOrderCount: 0,
      plannedQuantity: 100,
      goodQuantity: 40,
      scrapQuantity: 2,
      remainingQuantity: 60,
      activeDowntimeCount: 1,
      totalDowntimeMinutes: 15,
      totalSetupMinutes: 0,
      manualCycleCount: 0,
      openExceptionCount: 1,
      unresolvedExceptionCount: 1,
      shiftCoverageCount: 1,
      operatorCoverageCount: 1,
      riskSignals: [{ code: "delay", label: "Termin riski", severity: "high" }],
      logisticsRisk: { riskLevel: "low", slackMinutes: 0, blocking: false, available: true },
    },
    shifts: [
      {
        shiftId: "s1",
        shiftCode: "A",
        shiftName: "Vardiya A",
        activeWindowStartUtc: "2026-09-22T08:00:00.000Z",
        productionOrderCount: 1,
        goodQuantity: 20,
        scrapQuantity: 1,
        downtimeMinutes: 5,
        setupMinutes: 0,
        openExceptionCount: 0,
        assignedResourceCount: 2,
        assignedOperatorCount: 1,
        handoverState: "open",
      },
      {
        shiftId: "s2",
        shiftCode: "B",
        shiftName: "Vardiya B",
        activeWindowStartUtc: "2026-09-22T12:00:00.000Z",
        productionOrderCount: 1,
        goodQuantity: 20,
        scrapQuantity: 1,
        downtimeMinutes: 0,
        setupMinutes: 0,
        openExceptionCount: 0,
        assignedResourceCount: 2,
        assignedOperatorCount: 1,
        handoverState: "open",
      },
    ],
    resources: [
      {
        resourceId: "r1",
        code: "M1",
        name: "Machine",
        resourceType: "machine",
        stationId: "st1",
        stationCode: "ST1",
        stationName: "Station",
        stationKind: "process",
        lineId: "l1",
        lineCode: "L1",
        lineName: "Line",
        areaCode: "A1",
        state: "running",
        goodCount: 10,
        rejectCount: 0,
        isStarved: false,
        isBlocked: false,
      },
      {
        resourceId: "r2",
        code: "H1",
        name: "Assembly",
        resourceType: "human",
        stationId: "st2",
        stationCode: "ST2",
        stationName: "Assembly",
        stationKind: "assembly",
        lineId: "l1",
        lineCode: "L1",
        lineName: "Line",
        areaCode: "A1",
        state: "idle",
        goodCount: 5,
        rejectCount: 0,
        isStarved: false,
        isBlocked: false,
      },
    ],
    scrapRatePercent: 4.8,
    scrapQuantity: 2,
    goodQuantityFromScrap: 40,
    downtimeActiveCount: 1,
    downtimeTotalMinutes: 15,
  });

  assert.equal(model.state, "ready");
  assert.ok(model.outputTrend.length >= 2);
  assert.ok(model.resourceTypeBars.some((bar) => bar.label.includes("Montaj")));
  assert.ok(model.resourceStateBars.length > 0);
  assert.ok(model.riskHighlights.includes("Termin riski"));
});
