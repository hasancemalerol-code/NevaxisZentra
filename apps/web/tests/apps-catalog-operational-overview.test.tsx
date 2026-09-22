import assert from "node:assert/strict";
import test from "node:test";
import type { ReactElement } from "react";
import { renderToStaticMarkup } from "react-dom/server";

import { AppsOperationalOverviewSection } from "../src/features/apps-catalog/components/apps-operational-overview-section";
import {
  AppsOperationalOutputTrendChart,
  AppsOperationalQuantityBarChart,
} from "../src/features/apps-catalog/components/apps-operational-analytics-charts";

test("apps operational overview renders empty analytics state", () => {
  const markup = renderToStaticMarkup(
    <AppsOperationalOverviewSection
      overview={{
        state: "empty",
        kpis: [],
        outputTrend: [],
        resourceTypeBars: [],
        resourceStateBars: [],
        scrapSummary: null,
        downtimeSummary: null,
        riskHighlights: [],
      }}
    /> as ReactElement,
  );

  assert.match(markup, /data-testid="apps-operational-overview-empty"/);
  assert.match(markup, /Henüz operasyonel veri yok/);
  assert.match(markup, /Fabrika yapısını yükle/);
  assert.match(markup, /Veri Aktarım Merkezi/);
  assert.doesNotMatch(markup, /data-testid="apps-operational-overview-metrics"/);
});

test("apps operational overview renders chart components with live projection shape", () => {
  const markup = renderToStaticMarkup(
    <AppsOperationalOverviewSection
      overview={{
        state: "ready",
        kpis: [{ label: "Aktif sipariş", value: "3" }],
        outputTrend: [
          { label: "08:00", goodQuantity: 120, targetQuantity: 100 },
          { label: "12:00", goodQuantity: 140, targetQuantity: 110 },
        ],
        resourceTypeBars: [
          { label: "Makine", count: 2 },
          { label: "Montaj / İnsan İstasyonu", count: 1 },
        ],
        resourceStateBars: [{ label: "Çalışıyor", count: 2 }],
        scrapSummary: { goodQuantity: 200, scrapQuantity: 4, scrapRatePercent: 2 },
        downtimeSummary: { activeCount: 1, totalMinutes: 12 },
        riskHighlights: ["Termin riski"],
      }}
    /> as ReactElement,
  );

  assert.match(markup, /data-testid="apps-operational-overview-metrics"/);
  assert.match(markup, /data-testid="apps-operational-analytics-charts"/);
  assert.match(markup, /data-testid="apps-output-trend-chart"/);
  assert.match(markup, /data-testid="apps-resource-type-chart"/);
  assert.match(markup, /data-chart-kind="output-trend"/);
  assert.match(markup, /Termin riski/);
});

test("apps output trend chart renders axes and series paths", () => {
  const markup = renderToStaticMarkup(
    <AppsOperationalOutputTrendChart
      points={[
        { label: "A", goodQuantity: 10, targetQuantity: 8 },
        { label: "B", goodQuantity: 14, targetQuantity: 12 },
      ]}
    /> as ReactElement,
  );

  assert.match(markup, /data-testid="apps-output-trend-chart"/);
  assert.match(markup, /apps-analytics-chart__line--actual/);
});

test("apps quantity bar chart shows numeric values", () => {
  const markup = renderToStaticMarkup(
    <AppsOperationalQuantityBarChart
      title="Kaynak tip dağılımı"
      bars={[{ label: "Makine", count: 3 }]}
      testId="apps-resource-type-chart"
    /> as ReactElement,
  );

  assert.match(markup, />\s*3\s*</);
  assert.match(markup, /Makine/);
});
