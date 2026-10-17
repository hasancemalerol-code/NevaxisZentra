import assert from "node:assert/strict";
import { test } from "node:test";

import {
  formatAdaptiveCountForPlan,
  formatAdaptivePercentForPlan,
  hasAdaptivePlanData,
} from "../src/features/adaptive-production-planning/domain/adaptive-planning-labels";
import { formatIotPlcDateTime } from "../src/features/iot-plc/domain/iot-plc-formatters";
import { isIotPlcKpiValueEmpty, resolveKpiToneClass } from "../src/features/iot-plc/components/iot-plc-presentation";

test("planning metrics of a missing plan are shown as an em dash, never as %100 or an invented risk count", () => {
  assert.equal(hasAdaptivePlanData({ activePlan: null, operations: [] }), false);
  assert.equal(formatAdaptivePercentForPlan(100, false), "—");
  assert.equal(formatAdaptiveCountForPlan(1, false), "—");
  assert.equal(hasAdaptivePlanData({ activePlan: { id: "p" }, operations: [] }), true);
  assert.equal(formatAdaptivePercentForPlan(87.4, true), "%87");
  assert.equal(formatAdaptiveCountForPlan(0, true), "0");
});

test("IoT timestamps are facility local time (UTC+3), not UTC", () => {
  assert.match(formatIotPlcDateTime("2026-10-15T09:51:00Z"), /12:51/);
  assert.match(formatIotPlcDateTime("2026-10-15T09:51:00"), /12:51/);
  assert.equal(formatIotPlcDateTime(null), "—");
});

test("zero-value IoT KPI tiles are neutral, never warning orange", () => {
  assert.equal(isIotPlcKpiValueEmpty("0"), true);
  assert.equal(isIotPlcKpiValueEmpty("—"), true);
  assert.equal(isIotPlcKpiValueEmpty("12"), false);
  assert.equal(resolveKpiToneClass("warning", "0"), "iot-plc-kpi-card--neutral");
  assert.equal(resolveKpiToneClass("warning", "3"), "iot-plc-kpi-card--warning");
});
