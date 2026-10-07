import assert from "node:assert/strict";
import test from "node:test";
import { renderToStaticMarkup } from "react-dom/server";

import { OperationalValueControlCenterView } from "../src/features/operational-value/components/operational-value-control-center-view";
import {
  mapCaseDetail,
  mapControlCenter,
  type RawOperationalValueCaseDetail,
  type RawOperationalValueControlCenter,
  type RawOperationalValueReportPreview,
} from "../src/features/operational-value/domain/operational-value-mapping";

// The packaged build crashed on /operational-value ("Cannot read properties of undefined (reading 'currency')")
// because the view model assumed fields the API does not return. These fixtures are the API wire shapes:
// the first two are byte-for-byte what the packaged API returns for a fresh tenant with no verified outcomes.
const emptyCenter: RawOperationalValueControlCenter = {
  summary: {
    verifiedOutcomes: 0,
    goodOutputGainUnits: 0,
    downtimeAvoidedMinutes: 0,
    lateOrdersProtected: 0,
    humanTouchesReduced: 0,
    intelligenceCost: 0,
    verifiedEconomicValue: null,
    costPerVerifiedOutcome: null,
  },
  trend: [],
  breakdown: [],
  recentValues: [],
  economicInputs: null,
  generatedAtUtc: "2026-10-05T12:43:48.8802844+00:00",
};
const emptyReport: RawOperationalValueReportPreview = {
  periodLabel: "Last 30 days",
  verifiedInterventions: 0,
  goodOutputGainUnits: 0,
  downtimeAvoidedMinutes: 0,
  lateOrdersProtected: 0,
  verifiedEconomicValue: null,
  currency: null,
  intelligenceCost: 0,
  netValue: null,
  roiPercent: null,
  methodologySummary: "Verified operational outcomes only.",
};
const noCaps = { canManageEconomicInputs: false, canExport: false };

test("an empty tenant (API wire shape) maps and renders without throwing", () => {
  const mapped = mapControlCenter(emptyCenter, emptyReport, undefined, noCaps);
  assert.equal(mapped.summary.economicConversionStatus, "NOT_CONFIGURED");
  assert.equal(mapped.reportPreview.economicConversionStatus, "NOT_CONFIGURED");
  assert.deepEqual(mapped.recentCases, []);
  const html = renderToStaticMarkup(<OperationalValueControlCenterView controlCenter={mapped} />);
  assert.match(html, /Operasyonel Değer/);
  assert.match(html, /Doğrulanmış değer vakası bulunamadı/);
  assert.match(html, /Ekonomik girdi yapılandırması bulunamadı/);
});

test("a populated tenant maps API fields without inventing unreported figures", () => {
  const center: RawOperationalValueControlCenter = {
    ...emptyCenter,
    summary: {
      ...emptyCenter.summary,
      verifiedOutcomes: 3,
      goodOutputGainUnits: 120,
      downtimeAvoidedMinutes: 45,
      intelligenceCost: 2.5,
      verifiedEconomicValue: 900,
      costPerVerifiedOutcome: 0.8,
    },
    trend: [
      {
        periodLabel: "2026-W40",
        verifiedOutcomeCount: 3,
        goodOutputGainUnits: 120,
        downtimeAvoidedMinutes: 45,
        verifiedEconomicValue: 900,
        intelligenceCost: 2.5,
        costPerVerifiedOutcome: 0.8,
      },
    ],
    breakdown: [
      {
        dimensionType: "category",
        dimensionKey: "GOOD_OUTPUT_GAIN",
        label: "GOOD_OUTPUT_GAIN",
        verifiedCount: 2,
        operationalDeltaTotal: 120,
        unit: "units",
        economicValueTotal: 900,
        intelligenceCostTotal: 2,
      },
      {
        dimensionType: "line",
        dimensionKey: "LINE-1",
        label: "LINE-1",
        verifiedCount: 2,
        operationalDeltaTotal: 120,
        unit: "mixed",
        economicValueTotal: null,
        intelligenceCostTotal: 2,
      },
    ],
    recentValues: [
      { valueRecordId: "11111111-1111-4111-8111-111111111111", valueCategory: "GOOD_OUTPUT_GAIN", workflowType: "scenario-recovery", lineCode: "LINE-1" },
    ],
    economicInputs: {
      configurationId: "22222222-2222-4222-8222-222222222222",
      versionCode: "v1",
      currency: "TRY",
      unitContributionValue: 7.5,
      scrapUnitCost: null,
      downtimeCostPerHour: 100,
      laborCostPerHour: null,
      changeoverCostPerMinute: null,
      lateDeliveryPenalty: null,
      effectiveFromUtc: "2026-10-01T00:00:00+00:00",
      updatedAtUtc: "2026-10-01T00:00:00+00:00",
    },
  };
  const detail: RawOperationalValueCaseDetail = {
    valueRecordId: "11111111-1111-4111-8111-111111111111",
    problemSummary: "Hat düşük verim",
    baseline: { value: 100, unit: "units", source: "line-history", window: "7d", method: "rolling-median" },
    actual: { value: 220, unit: "units", source: "mes" },
    verificationOutcome: "VERIFIED",
    deltaValue: 120,
    unit: "units",
    status: "VERIFIED_ECONOMIC",
    attributionClass: "DIRECT",
    confidenceScore: 0.9,
    economicConversionStatus: "CONVERTED",
    economicValue: 900,
    intelligenceCost: 2,
    netValue: 898,
    roiPercent: null,
    evidenceRefs: ["case:abc"],
    createdAtUtc: "2026-10-04T10:00:00+00:00",
  };
  const featured = mapCaseDetail(detail, { valueCategory: "GOOD_OUTPUT_GAIN", workflowType: "scenario-recovery", lineCode: "LINE-1" });
  const mapped = mapControlCenter(center, { ...emptyReport, verifiedInterventions: 3, goodOutputGainUnits: 120, verifiedEconomicValue: 900, currency: "TRY" }, featured, {
    canManageEconomicInputs: true,
    canExport: true,
  });

  assert.equal(mapped.summary.goodOutputGainedUnits, 120);
  assert.equal(mapped.summary.economicConversionStatus, "CONVERTED");
  assert.equal(mapped.summary.currency, "TRY");
  assert.equal(mapped.breakdownByCategory.length, 1);
  assert.equal(mapped.breakdownByLine.length, 1);
  assert.equal(mapped.breakdownByWorkflow.length, 0);
  assert.equal(mapped.intelligenceCost.deterministicRouteRate, undefined, "route statistics are not reported by this API");
  assert.equal(mapped.reportPreview.operationalImprovements[0], "120 adet iyi çıktı kazanımı");
  assert.equal(mapped.activeEconomicInputs?.unitContributionValue, 7.5);
  assert.equal(mapped.activeEconomicInputs?.scrapUnitCost, undefined);
  assert.equal(mapped.featuredCase?.baselineValue, 100);
  assert.equal(mapped.canExport, true);

  const html = renderToStaticMarkup(<OperationalValueControlCenterView controlCenter={mapped} />);
  assert.match(html, /LINE-1/);
  assert.match(html, /Hat düşük verim/);
  assert.doesNotMatch(html, /NaN|undefined/);
});
