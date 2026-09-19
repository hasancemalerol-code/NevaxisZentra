import assert from "node:assert/strict";
import test from "node:test";
import { renderToStaticMarkup } from "react-dom/server";

import { ScenarioRecoveryWorkspaceView } from "../src/features/scenario-recovery/components/scenario-recovery-workspace-view";
import type { ScenarioRecoveryWorkspaceApi } from "../src/features/scenario-recovery/server/scenario-recovery-api-client";

const workspace = {
  runId: "run-1",
  lineId: "line-1",
  lineCode: "MIX-LINE-01",
  currentPhase: "RECOMMENDED",
  executionState: "SIMULATED",
  category: "PRODUCTION_RECOVERY",
  entryPoint: "FABRIKA",
  problem: {
    summary: "Assembly bottleneck blocks Order 418 delivery",
    orderReference: "ORD-418",
    lineCode: "MIX-LINE-01",
    constraintStationCode: "ST-30",
    baselineGoodThroughputPerHour: 612,
    targetGoodThroughputPerHour: 700,
    contributingFactors: ["assembly-bottleneck"],
  },
  objectives: [
    { objectiveType: "PROTECT_OTIF", label: "Protect OTIF", priority: 3, isHardConstraint: true },
  ],
  comparison: {
    metricCodes: ["GOOD_THROUGHPUT"],
    options: [
      {
        optionLabel: "BASELINE",
        title: "Current plan",
        category: "PRODUCTION_RECOVERY",
        feasibilityState: "FEASIBLE",
        feasibilityReasons: [],
        requiresApproval: false,
        isDominated: false,
        recommendationScore: 0,
        simulation: {
          predictedGoodThroughputPerHour: 612,
          baselineGoodThroughputPerHour: 612,
          deltaGoodThroughputPerHour: 0,
          predictedScrapRatePercent: 1,
          predictedWipAverage: 12,
          operationalRiskScore: 0.1,
          qualityImpactScore: 0.1,
          constraintBefore: { stationCode: "ST-30" },
          constraintAfter: { stationCode: "ST-30" },
          assumptions: ["baseline"],
        },
        metrics: [],
      },
      {
        optionLabel: "OPTION_A",
        title: "Add operator",
        category: "OPERATOR_REALLOCATION",
        feasibilityState: "FEASIBLE",
        feasibilityReasons: [],
        requiresApproval: false,
        isDominated: false,
        recommendationScore: 10,
        simulation: {
          predictedGoodThroughputPerHour: 674,
          baselineGoodThroughputPerHour: 612,
          deltaGoodThroughputPerHour: 62,
          predictedScrapRatePercent: 1,
          predictedWipAverage: 14,
          operationalRiskScore: 0.2,
          qualityImpactScore: 0.1,
          constraintBefore: { stationCode: "ST-30" },
          constraintAfter: { stationCode: "ST-40" },
          assumptions: ["operator-available"],
        },
        metrics: [],
      },
    ],
    tradeOffs: [
      {
        optionLabel: "OPTION_A",
        advantages: ["Higher throughput (+62/h)"],
        disadvantages: ["Higher WIP"],
        computedFacts: ["constraint-movement=ST-30->ST-40"],
      },
    ],
    recommendation: {
      recommendedOptionLabel: "OPTION_A",
      scoreComponents: ["throughput-delta=62.0"],
      guardrailNotes: ["hard:PROTECT_OTIF"],
      isPolicyDriven: true,
    },
    predictedConstraintBefore: "ST-30",
    predictedConstraintAfterRecommended: "ST-40",
  },
  hasPendingApproval: false,
  isVerifiedClosed: false,
  engineVersion: "scenario-recovery-orchestration/1.0.0",
  updatedAtUtc: "2026-09-19T10:00:00.000Z",
} as const;

test("ScenarioRecoveryWorkspaceView renders comparison and recommendation", () => {
  const html = renderToStaticMarkup(
    <ScenarioRecoveryWorkspaceView workspace={workspace as ScenarioRecoveryWorkspaceApi} history={[]} />,
  );

  assert.match(html, /data-testid="scenario-recovery-workspace"/);
  assert.match(html, /Assembly bottleneck blocks Order 418 delivery/);
  assert.match(html, /data-testid="scenario-option-BASELINE"/);
  assert.match(html, /data-testid="scenario-option-OPTION_A"/);
  assert.match(html, /Zentra önerisi/);
  assert.match(html, /ST-30 → ST-40/);
});
