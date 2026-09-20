import assert from "node:assert/strict";
import test from "node:test";
import { renderToStaticMarkup } from "react-dom/server";

import {
  ContinuousOptimizationControlCenterView,
  ContinuousOptimizationOpportunityStrip,
} from "../src/features/continuous-optimization/components/continuous-optimization-control-center-view";

const sampleControlCenter = {
  activeRuns: [],
  activeOpportunities: [
    {
      opportunityType: "THROUGHPUT",
      scopeEntityType: "line",
      scopeEntityId: "line-1",
      triggerType: "ThroughputDeviationChanged",
      title: "MIX-LINE-01 throughput opportunity",
      summary: "Potential +8.4 good units/hour via registered interventions.",
      baselineValue: 91.6,
      currentValue: 83.2,
      estimatedBenefit: 8.4,
      confidence: 0.9,
      riskClass: "MEDIUM",
      evidence: ["line:MIX-LINE-01", "constraint:ST-30"],
      requiredActions: ["Evaluate scenario options"],
      constraints: ["Policy"],
      expiresAtUtc: "2026-09-20T22:00:00.000Z",
    },
  ],
  awaitingApproval: [
    {
      runId: "run-1",
      currentPhase: "APPROVAL_REQUIRED",
      optimizationType: "THROUGHPUT",
      triggerType: "ThroughputDeviationChanged",
      lineCode: "MIX-LINE-01",
      updatedAtUtc: "2026-09-20T20:00:00.000Z",
    },
  ],
  observing: [],
  verifiedGains: [
    {
      runId: "run-2",
      caseId: "case-1",
      lineCode: "MIX-LINE-01",
      optimizationType: "THROUGHPUT",
      outcome: "VERIFIED_GAIN",
      verifiedImprovementPerHour: 7.2,
      summary: "Throughput +7.2%",
      verifiedAtUtc: "2026-09-20T19:00:00.000Z",
    },
  ],
  rejectedOrFailed: [],
  kpis: {
    opportunitiesDetected: 3,
    opportunitiesQualified: 2,
    opportunitiesSuppressed: 1,
    runsActive: 1,
    verifiedGains: 1,
    noGainOutcomes: 0,
    opportunityToActionRate: 0.67,
    verifiedGainRate: 0.5,
  },
  generatedAtUtc: "2026-09-20T20:30:00.000Z",
} as const;

test("continuous optimization control center renders core sections", () => {
  const html = renderToStaticMarkup(
    <ContinuousOptimizationControlCenterView controlCenter={sampleControlCenter} />,
  );

  assert.match(html, /data-testid="continuous-optimization-control-center"/);
  assert.match(html, /Sürekli Optimizasyon/);
  assert.match(html, /MIX-LINE-01 throughput opportunity/);
  assert.match(html, /Throughput \+7\.2%/);
});

test("continuous optimization factory strip stays compact", () => {
  const html = renderToStaticMarkup(
    <ContinuousOptimizationOpportunityStrip opportunities={sampleControlCenter.activeOpportunities} />,
  );

  assert.match(html, /data-testid="continuous-optimization-factory-strip"/);
  assert.match(html, /\+8\.4/);
});
