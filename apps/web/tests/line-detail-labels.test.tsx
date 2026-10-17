import assert from "node:assert/strict";
import test from "node:test";
import { renderToStaticMarkup } from "react-dom/server";

import { MixedProductionLineDetailView, buildStationContextRows } from "../src/features/mixed-production/components/mixed-production-line-detail-view";
import {
  constraintTypeLabel,
  dataQualityLabel,
  lineHeaderFacts,
  lineStateLabel,
} from "../src/lib/presentation/throughput-labels";

test("line, data quality and constraint values are shown in Turkish", () => {
  assert.equal(lineStateLabel("STOPPED"), "Durdu");
  assert.equal(lineStateLabel("IDLE"), "Boşta");
  assert.equal(dataQualityLabel("PARTIAL"), "Kısmi");
  assert.equal(dataQualityLabel("MEASURED"), "Ölçülen");
  assert.equal(constraintTypeLabel("CYCLE_TIME"), "Çevrim süresi");
});

test("header facts drop placeholders and state an unassigned shift", () => {
  assert.equal(lineHeaderFacts({ code: "LINE-01" }), "LINE-01 · Vardiya: Atanmadı");
  assert.equal(
    lineHeaderFacts({ code: "LINE-01", currentProductCode: "P-1", currentOrderReference: "PO-9", currentShiftCode: "A" }),
    "LINE-01 · Ürün: P-1 · Emir: PO-9 · Vardiya: A",
  );
});

test("station context rows use Turkish labels, a resource type label and no empty rows", () => {
  const rows = buildStationContextRows({
    stationId: "s1",
    code: "ST-001",
    name: "Injection Station 01",
    stationKind: "machine",
    sequenceOrder: 1,
    state: "IDLE",
    resourceCode: "RES-001",
    resourceType: "machine",
    goodCount: 3,
    rejectCount: 0,
  });
  const byLabel = Object.fromEntries(rows.map((row) => [row.label, row.value]));
  assert.equal(byLabel["Durum"], "Boşta");
  assert.equal(byLabel["Kaynak"], "RES-001 (Makine)");
  assert.equal(byLabel["İyi"], 3);
  assert.equal(byLabel["Hurda"], 0);
  assert.ok(!("Operatör" in byLabel));
});

test("line detail page renders no raw English state and shows each KPI once", () => {
  const html = renderToStaticMarkup(
    <MixedProductionLineDetailView
      detail={{
        line: { lineId: "l1", code: "LINE-01", name: "Mixed Line 01", lineState: "STOPPED", goodOutputToday: 0, rejectOutputToday: 0 },
        stations: [
          { stationId: "s1", code: "ST-001", name: "Injection Station 01", stationKind: "machine", sequenceOrder: 1, state: "IDLE", goodCount: 0, rejectCount: 0 },
        ],
        timeline: [],
        playbackWindowLabel: "",
      }}
      playback={null}
    />,
  );
  assert.ok(html.includes("Durdu"));
  assert.ok(html.includes("Boşta"));
  assert.ok(!/>\s*(STOPPED|IDLE|PARTIAL|MEASURED)\s*</.test(html));
  assert.ok(!html.includes("GOOD"));
  assert.equal((html.match(/>Olay</g) ?? []).length, 1);
  assert.equal((html.match(/data-testid="fact-line-topology"/g) ?? []).length, 1);
});
