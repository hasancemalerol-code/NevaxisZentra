import assert from "node:assert/strict";
import test from "node:test";

import {
  mapProductionPlanDetailApiDtoToDomain,
  mapProductionPlanSummaryApiDtoToDomain,
} from "../../src/features/production-planning/server/production-plan-api-mapper";

const summaryDto = {
  id: "33333333-3333-3333-3333-333333333333",
  referenceCode: "PLAN-001",
  title: "Test plan",
  facilityId:
    "11111111-1111-1111-1111-111111111111",
  status: "approved",
  origin: "manual",
  aiGenerated: false,
  confidence: "high",
  plannedStartAtUtc:
    "2026-08-06T05:00:00.000Z",
  plannedEndAtUtc:
    "2026-08-07T17:00:00.000Z",
  approvalStatus: "approved",
  transferStatus: "not-started",
  transferTargetSystem: "mes",
  updatedAtUtc:
    "2026-08-07T17:00:00.000Z",
  demandCount: 2,
  primaryRequiredQuantity: 1500,
  primaryUnit: "adet",
  primaryProductCode: "PP-100",
  primaryProductName: "Pilot part",
  highestDemandPriority: "high",
  riskCount: 1,
  criticalRiskCount: 0,
  blockingRiskCount: 1,
  hasBlockingRisk: true,
  hasMachineAssignment: true,
  hasMoldAssignment: true,
  hasMaterialAssignment: false,
  hasOperatorGroupAssignment: false,
  shiftName: "A vardiyası",
};

test("valid summary dto maps to domain without tenant leakage", () => {
  const result =
    mapProductionPlanSummaryApiDtoToDomain(
      summaryDto,
    );

  assert.equal(result.status, "success");

  if (result.status === "success") {
    assert.equal(
      result.value.id,
      summaryDto.id,
    );
    assert.equal(
      result.value.tenantId,
      "",
    );
    assert.equal(
      result.value.demands.length,
      0,
    );
    assert.equal(
      result.value.listSummary?.demandCount,
      2,
    );
    assert.equal(
      result.value.listSummary?.hasBlockingRisk,
      true,
    );
  }
});

test("invalid summary enum fails as invalid-response", () => {
  const result =
    mapProductionPlanSummaryApiDtoToDomain({
      ...summaryDto,
      status: "unknown-status",
    });

  assert.equal(result.status, "failure");

  if (result.status === "failure") {
    assert.equal(
      result.code,
      "invalid-response",
    );
  }
});

test("valid detail dto maps nested structures", () => {
  const result =
    mapProductionPlanDetailApiDtoToDomain({
      ...summaryDto,
      createdAtUtc:
        "2026-08-05T07:45:00.000Z",
      version: 1,
      createdBy: "planner",
      notes: "Notes",
      aiExplanation: "Explanation",
      demands: [
        {
          id: "demand-1",
          productId: "product-1",
          productName: "Product",
          requiredQuantity: 10,
          unit: "adet",
          priority: "normal",
        },
      ],
      risks: [
        {
          id: "risk-1",
          type: "delivery-risk",
          severity: "warning",
          title: "Risk",
          description: "Description",
          blocking: false,
        },
      ],
      resources: {
        machineName: "IM-07",
      },
      timeWindow: {
        plannedStartAtUtc:
          summaryDto.plannedStartAtUtc,
        plannedEndAtUtc:
          summaryDto.plannedEndAtUtc,
      },
      approval: {
        status: "approved",
      },
      transfer: {
        status: "not-started",
        targetSystem: "mes",
      },
      sourceReferences: [
        {
          sourceType: "manual-request",
          sourceId: "source-1",
        },
      ],
      history: [
        {
          id: "history-1",
          type: "created",
          title: "Created",
          description: "Created",
        },
      ],
    });

  assert.equal(result.status, "success");

  if (result.status === "success") {
    assert.equal(
      result.value.demands[0]?.productId,
      "product-1",
    );
    assert.equal(
      result.value.tenantId,
      "",
    );
  }
});

test("invalid detail date fails as invalid-response", () => {
  const result =
    mapProductionPlanDetailApiDtoToDomain({
      ...summaryDto,
      createdAtUtc: "not-a-date",
      version: 1,
      createdBy: "planner",
      demands: [],
      risks: [],
      resources: {},
      timeWindow: {},
      approval: {
        status: "approved",
      },
      transfer: {
        status: "not-started",
      },
      sourceReferences: [],
      history: [],
    });

  assert.equal(result.status, "failure");
});
