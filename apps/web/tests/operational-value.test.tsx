import assert from "node:assert/strict";
import test from "node:test";
import { renderToStaticMarkup } from "react-dom/server";

import { OperationalValueControlCenterView } from "../src/features/operational-value/components/operational-value-control-center-view";

const sampleControlCenter = {
  summary: {
    verifiedOutcomes: 14,
    goodOutputGainedUnits: 112,
    downtimeAvoidedMinutes: 95,
    lateOrdersProtected: 3,
    humanTouchesReduced: 28,
    intelligenceCost: 4.82,
    verifiedEconomicValue: 1840,
    economicConversionStatus: "CONVERTED",
    currency: "USD",
  },
  trend: [
    {
      periodLabel: "Hafta 37",
      periodStartUtc: "2026-09-14T00:00:00.000Z",
      verifiedOutcomes: 6,
      goodOutputGainedUnits: 48,
      downtimeAvoidedMinutes: 35,
      verifiedEconomicValue: 720,
      intelligenceCost: 2.1,
      costPerVerifiedOutcome: 0.35,
    },
    {
      periodLabel: "Hafta 38",
      periodStartUtc: "2026-09-21T00:00:00.000Z",
      verifiedOutcomes: 8,
      goodOutputGainedUnits: 64,
      downtimeAvoidedMinutes: 60,
      verifiedEconomicValue: 1120,
      intelligenceCost: 2.72,
      costPerVerifiedOutcome: 0.34,
    },
  ],
  breakdownByCategory: [
    {
      dimensionType: "CATEGORY",
      dimensionId: "GOOD_OUTPUT_GAIN",
      label: "İyi çıktı kazanımı",
      valueCategory: "GOOD_OUTPUT_GAIN",
      operationalValue: 112,
      unit: "adet",
      caseCount: 5,
      economicValue: 1120,
      attributionClass: "STRONGLY_ATTRIBUTED",
    },
    {
      dimensionType: "CATEGORY",
      dimensionId: "DOWNTIME_AVOIDED",
      label: "Önlenen duruş",
      valueCategory: "DOWNTIME_AVOIDED",
      operationalValue: 95,
      unit: "dk",
      caseCount: 4,
      attributionClass: "DIRECT",
    },
  ],
  breakdownByLine: [
    {
      dimensionType: "LINE",
      dimensionId: "line-mix-01",
      label: "MIX-LINE-01",
      valueCategory: "GOOD_OUTPUT_GAIN",
      operationalValue: 112,
      unit: "adet",
      caseCount: 5,
      economicValue: 1120,
      attributionClass: "STRONGLY_ATTRIBUTED",
    },
  ],
  breakdownByWorkflow: [
    {
      dimensionType: "WORKFLOW",
      dimensionId: "scenario-recovery",
      label: "Senaryo Kurtarma",
      valueCategory: "GOOD_OUTPUT_GAIN",
      operationalValue: 112,
      unit: "adet",
      caseCount: 3,
      economicValue: 1120,
      attributionClass: "STRONGLY_ATTRIBUTED",
    },
  ],
  featuredCase: {
    caseId: "case-golden-001",
    title: "Assembly bottleneck recovery",
    problemSummary: "MIX-LINE-01 throughput below target with Order 418 delivery risk",
    workflowType: "scenario-recovery",
    lineCode: "MIX-LINE-01",
    baselineValue: 612,
    baselineMethod: "BEFORE_INTERVENTION_WINDOW",
    baselineSource: "2 saatlik gözlem penceresi",
    actualValue: 668,
    deltaValue: 56,
    unit: "good/saat",
    valueCategory: "GOOD_OUTPUT_GAIN",
    status: "VERIFIED_OPERATIONAL",
    attributionClass: "STRONGLY_ATTRIBUTED",
    confidence: 0.91,
    economicValue: 1120,
    economicConversionStatus: "CONVERTED",
    intelligenceCost: 0.42,
    netValue: 1119.58,
    roiPercent: 266566.7,
    paybackStatus: "NOT_AVAILABLE",
    evidence: [
      {
        kind: "measurement",
        label: "Operasyon ölçümü",
        detail: "612 → 668 good/saat · 2 saat gözlem",
        occurredAtUtc: "2026-09-20T06:30:00.000Z",
      },
    ],
    verificationOutcome: "VERIFIED_SUCCESS",
    verifiedAtUtc: "2026-09-20T06:30:00.000Z",
  },
  recentCases: [],
  activeEconomicInputs: {
    inputVersionId: "econ-v1",
    versionCode: "2026-Q3",
    currency: "USD",
    effectiveFromUtc: "2026-07-01T00:00:00.000Z",
    isActive: true,
    unitContributionValue: 10,
    scrapUnitCost: 4.5,
    downtimeCostPerHour: 850,
    laborCostPerHour: 42,
    updatedAtUtc: "2026-09-01T00:00:00.000Z",
  },
  intelligenceCost: {
    totalIntelligenceCost: 4.82,
    costPerVerifiedOutcome: 0.34,
    modelCallCount: 38,
    deterministicRouteCount: 112,
    deterministicRouteRate: 0.62,
    frontierModelCallRate: 0.04,
    frontierCostPerVerifiedOutcome: 0.42,
    hasValidCounterfactual: false,
    message: "ULTRA H maliyet kaydı bağlandı. Sayaçfaktual tasarruf iddiası yok.",
  },
  reportPreview: {
    periodLabel: "Son 30 gün",
    periodStartUtc: "2026-08-22T00:00:00.000Z",
    periodEndUtc: "2026-09-21T00:00:00.000Z",
    verifiedOutcomes: 14,
    operationalImprovements: [
      "+112 iyi çıktı birimi",
      "95 dk duruş önlendi",
      "3 geciken sipariş korundu",
      "28 insan dokunuşu azaltıldı",
    ],
    verifiedEconomicValue: 1840,
    intelligenceCost: 4.82,
    netValue: 1835.18,
    roiPercent: 38074.3,
    paybackStatus: "NOT_AVAILABLE",
    methodologySummary:
      "Yalnızca doğrulanmış sonuçlar dahil edildi. Tahminler ve gözlem aşaması değerleri hariç tutuldu.",
    currency: "USD",
    economicConversionStatus: "CONVERTED",
  },
  canManageEconomicInputs: false,
  canExport: true,
  generatedAtUtc: "2026-09-21T06:00:00.000Z",
} as const;

test("operational value control center renders core sections", () => {
  const html = renderToStaticMarkup(
    <OperationalValueControlCenterView controlCenter={sampleControlCenter} />,
  );

  assert.match(html, /data-testid="operational-value-control-center"/);
  assert.match(html, /data-testid="operational-value-summary"/);
  assert.match(html, /data-testid="operational-value-trend"/);
  assert.match(html, /data-testid="operational-value-breakdown"/);
  assert.match(html, /data-testid="operational-value-case-detail"/);
  assert.match(html, /data-testid="operational-value-economic-inputs"/);
  assert.match(html, /data-testid="operational-value-report-preview"/);
  assert.match(html, /Operasyonel Değer/);
  assert.match(html, /ULTRA I/);
  assert.match(html, /Assembly bottleneck recovery/);
  assert.match(html, /GOOD_OUTPUT_GAIN/);
  assert.match(html, /MIX-LINE-01/);
  assert.match(html, /\$1\.840/);
  assert.match(html, /612/);
  assert.match(html, /668/);
  assert.match(html, /salt okunur/i);
});

test("operational value control center shows economic value when configured", () => {
  const html = renderToStaticMarkup(
    <OperationalValueControlCenterView controlCenter={sampleControlCenter} />,
  );

  assert.match(html, /Doğrulanmış ekonomik değer/);
  assert.match(html, /Intelligence Runtime/);
});
