import assert from "node:assert/strict";
import test from "node:test";

import {
  applyOperationalRiskCoverageToKpis,
  summarizeOperationalRiskSourceCoverage,
} from "../src/features/operational-risk/components/operational-risk-presentation";
import type {
  OperationalRiskKpiCard,
  OperationalRiskSourceAdapterCoverage,
} from "../src/features/operational-risk/types/operational-risk-types";

const kpis: readonly OperationalRiskKpiCard[] = [
  { id: "open-cases", label: "Açık risk vakaları", value: "0", detail: "x", tone: "controlled", scopeLabel: "Açık vakalar", evidenceLabel: "Vaka kayıtları", stateBadge: "Kontrollü" },
  { id: "critical-high", label: "Kritik / yüksek öncelik", value: "0", detail: "x", tone: "controlled", scopeLabel: "Açık vakalar", evidenceLabel: "Vaka kayıtları", stateBadge: "Düşük" },
  { id: "new-detections", label: "Yeni tespitler", value: "0", detail: "x", tone: "neutral", scopeLabel: "Açık vakalar", evidenceLabel: "Vaka kayıtları", stateBadge: "Vaka yok" },
];

function source(module: string, state: OperationalRiskSourceAdapterCoverage["state"]): OperationalRiskSourceAdapterCoverage {
  return { module, label: module, state, detail: "", evidenceLabel: "" };
}

test("no usable source coverage yields neutral Veri yetersiz pills", () => {
  const coverage = [source("a", "unavailable"), source("b", "insufficient-data")];
  assert.equal(summarizeOperationalRiskSourceCoverage(coverage), "none");
  const result = applyOperationalRiskCoverageToKpis(kpis, coverage);
  for (const kpi of result.slice(0, 2)) {
    assert.equal(kpi.stateBadge, "Veri yetersiz");
    assert.equal(kpi.value, "—");
    assert.notEqual(kpi.tone, "controlled");
  }
  const text = JSON.stringify(result);
  assert.ok(!text.includes("Kontrollü"));
  assert.ok(!text.includes("\"Düşük\""));
  assert.equal(result[2], kpis[2]);
});

test("partial coverage says coverage is partial instead of controlled", () => {
  const coverage = [source("a", "active"), source("b", "unavailable")];
  const result = applyOperationalRiskCoverageToKpis(kpis, coverage);
  assert.equal(result[0].stateBadge, "Kısmi kapsam");
  assert.equal(result[0].tone, "neutral");
  assert.equal(result[0].value, "0");
});

test("full coverage keeps existing tones; non-zero counts are untouched", () => {
  const full = [source("a", "active"), source("b", "active")];
  assert.equal(applyOperationalRiskCoverageToKpis(kpis, full), kpis);
  const open: OperationalRiskKpiCard[] = [{ ...kpis[0], value: "3", tone: "attention", stateBadge: "Açık" }];
  const result = applyOperationalRiskCoverageToKpis(open, [source("a", "unavailable")]);
  assert.deepEqual(result[0], open[0]);
});
