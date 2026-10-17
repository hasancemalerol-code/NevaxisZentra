import assert from "node:assert/strict";
import test from "node:test";
import { renderToStaticMarkup } from "react-dom/server";

import { IntelligenceRuntimeControlCenterView, formatRoutingRate } from "../src/features/intelligence-runtime/components/intelligence-runtime-control-center-view";

const sampleControlCenter = {
  metrics: {
    deterministicRouteRate: 0.62,
    analyticsRouteRate: 0.14,
    solverRouteRate: 0.08,
    retrievalRouteRate: 0.06,
    nevaxisModelCallRate: 0.05,
    frontierModelCallRate: 0.04,
    frontierFallbackRate: 0.01,
    frontierCostPerCase: 0.08,
    frontierCostPerVerifiedOutcome: 0.42,
    totalRequests: 1240,
    modelInvocations: 112,
    totalEstimatedCost: 18.64,
  },
  recentRoutes: [
    {
      routeId: "route-1",
      taskType: "ROOT_CAUSE_CANDIDATE",
      routeType: "DETERMINISTIC",
      routingReason: "deterministic-first-policy",
      whyModelCalledOrSkipped: "WHY NO MODEL: deterministic path sufficient.",
      modelInvoked: false,
      estimatedCost: 0,
      latencyMs: 42,
      createdAtUtc: "2026-09-20T20:00:00.000Z",
    },
    {
      routeId: "route-2",
      taskType: "FACTORY_COPILOT_SYNTHESIS",
      routeType: "FRONTIER_MODEL",
      routingReason: "frontier-required",
      whyModelCalledOrSkipped: "WHY MODEL: frontier required for FACTORY_COPILOT_SYNTHESIS.",
      modelInvoked: true,
      estimatedCost: 0.12,
      latencyMs: 890,
      createdAtUtc: "2026-09-20T20:05:00.000Z",
    },
  ],
  models: [
    {
      modelId: "model-1",
      modelCode: "nevaxis-mfg-v1",
      version: "1.0.0",
      baseModel: "gpt-4o-mini",
      status: "PRODUCTION",
      evaluationStatus: "PASS",
      safetyStatus: "CLEAR",
      taskCapabilities: ["ROOT_CAUSE_CANDIDATE", "RECOVERY_PLAN_DRAFT"],
      trainingDatasetVersion: "ds-2026-09",
      updatedAtUtc: "2026-09-20T18:00:00.000Z",
    },
    {
      modelId: "model-2",
      modelCode: "nevaxis-mfg-candidate",
      version: "1.1.0-rc1",
      baseModel: "gpt-4o-mini",
      status: "CANDIDATE",
      evaluationStatus: "PROMOTION_BLOCKED",
      safetyStatus: "BLOCKED",
      taskCapabilities: ["CASE_SIMILARITY"],
      updatedAtUtc: "2026-09-20T19:00:00.000Z",
    },
  ],
  datasetVersions: [
    {
      datasetVersionId: "ds-1",
      versionCode: "ds-2026-09",
      schemaVersion: "nevaxis-verified-dataset-v1",
      readinessState: "READY_FOR_TRAINING",
      includedCaseCount: 48,
      excludedCaseCount: 6,
      tenantPolicy: "TENANT_ONLY_LEARNING",
      createdAtUtc: "2026-09-20T12:00:00.000Z",
    },
  ],
  evaluations: [
    {
      evaluationRunId: "eval-1",
      modelId: "model-1",
      modelCode: "nevaxis-mfg-v1",
      evaluationMode: "candidate-vs-production",
      outcome: "PASS",
      structuredOutputValidityRate: 0.98,
      safetyPassRate: 0.97,
      hallucinatedEntityRate: 0.02,
      estimatedCost: 0.12,
      latencyMs: 180,
      createdAtUtc: "2026-09-20T17:00:00.000Z",
    },
    {
      evaluationRunId: "eval-2",
      modelId: "model-2",
      modelCode: "nevaxis-mfg-candidate",
      evaluationMode: "candidate-vs-production",
      outcome: "PROMOTION_BLOCKED",
      structuredOutputValidityRate: 0.88,
      safetyPassRate: 0.82,
      hallucinatedEntityRate: 0.11,
      estimatedCost: 0.14,
      latencyMs: 210,
      createdAtUtc: "2026-09-20T19:30:00.000Z",
    },
    {
      evaluationRunId: "eval-3",
      modelId: "model-3",
      modelCode: "nevaxis-mfg-retired",
      evaluationMode: "shadow",
      outcome: "ROLLBACK_TRIGGERED",
      structuredOutputValidityRate: 0.75,
      safetyPassRate: 0.7,
      hallucinatedEntityRate: 0.22,
      estimatedCost: 0.09,
      latencyMs: 160,
      createdAtUtc: "2026-09-19T22:00:00.000Z",
    },
  ],
  verifiedOutcomeCosts: [],
  generatedAtUtc: "2026-09-20T20:30:00.000Z",
} as const;

test("intelligence runtime control center renders core sections", () => {
  const html = renderToStaticMarkup(
    <IntelligenceRuntimeControlCenterView controlCenter={sampleControlCenter} />,
  );

  assert.match(html, /data-testid="intelligence-runtime-control-center"/);
  assert.match(html, /data-testid="intelligence-runtime-kpis"/);
  assert.match(html, /data-testid="intelligence-runtime-routes"/);
  assert.match(html, /data-testid="intelligence-runtime-datasets"/);
  assert.match(html, /data-testid="intelligence-runtime-models"/);
  assert.match(html, /data-testid="intelligence-runtime-evaluations"/);
  assert.match(html, /Zekâ Altyapısı Durumu/);
  assert.match(html, /Root cause candidate/);
  assert.match(html, /ds-2026-09/);
  assert.match(html, /nevaxis-mfg-v1/);
  assert.match(html, /Yayın engellendi/);
  assert.match(html, /Geri alma tetiklendi/);
  assert.match(html, /%62,0/);
  assert.doesNotMatch(html, /\$18\.64|USD|Frontier|Solver|Retrieval/);
});

test("routing ratios read as no data until a routing record exists", () => {
  assert.equal(formatRoutingRate(1, 0), "—");
  assert.notEqual(formatRoutingRate(0.62, 2), "—");
  const markup = renderToStaticMarkup(
    <IntelligenceRuntimeControlCenterView
      controlCenter={{ ...sampleControlCenter, recentRoutes: [] } as never}
    />,
  );
  assert.ok(!markup.includes("%100"));
});
