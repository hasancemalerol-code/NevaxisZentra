import assert from "node:assert/strict";
import test from "node:test";
import type { ReactElement } from "react";
import { renderToStaticMarkup } from "react-dom/server";

import { AppsOperationalOverviewSection } from "../src/features/apps-catalog/components/apps-operational-overview-section";

test("apps operational overview renders empty state without mock metrics", () => {
  const markup = renderToStaticMarkup(
    <AppsOperationalOverviewSection
      overview={{
        state: "empty",
        metrics: [],
        resourceDistribution: [],
        riskHighlights: [],
      }}
    /> as ReactElement,
  );

  assert.match(markup, /data-testid="apps-operational-overview-empty"/);
  assert.match(markup, /Veri Aktarım Merkezi/);
  assert.doesNotMatch(markup, /data-testid="apps-operational-overview-metrics"/);
});

test("apps operational overview renders real metrics and distribution", () => {
  const markup = renderToStaticMarkup(
    <AppsOperationalOverviewSection
      overview={{
        state: "ready",
        metrics: [{ label: "Aktif sipariş", value: "3" }],
        resourceDistribution: [
          { resourceType: "machine", label: "Makine", count: 2 },
          { resourceType: "human", label: "Montaj / İnsan İstasyonu", count: 1 },
        ],
        riskHighlights: ["Hat MIX-01 gecikme riski"],
      }}
    /> as ReactElement,
  );

  assert.match(markup, /data-testid="apps-operational-overview-metrics"/);
  assert.match(markup, /data-testid="apps-operational-overview-distribution"/);
  assert.match(markup, /Aktif sipariş/);
  assert.match(markup, /Hat MIX-01 gecikme riski/);
});
