import assert from "node:assert/strict";
import test from "node:test";

import { createProductionPlanRepository } from "../../src/features/production-planning/server/production-plan-repository-factory";

test("development-preview mode returns preview repository contract", () => {
  const repository =
    createProductionPlanRepository({
      runtimeConfig: {
        mode: "development-preview",
        previewTenantId:
          "tenant-NevaxisZentra",
        previewFacilityId:
          "facility-istanbul",
      },
      requestContext: null,
      scope: {
        tenantId: "tenant-NevaxisZentra",
        facilityId: "facility-istanbul",
        source:
          "development-preview-config",
      },
    });

  assert.equal(
    typeof repository.list,
    "function",
  );
  assert.equal(
    typeof repository.findById,
    "function",
  );
});

test("api mode requires request context for repository creation", () => {
  assert.throws(() => {
    createProductionPlanRepository({
      runtimeConfig: {
        mode: "api",
        apiBaseUrl:
          "https://api.example.test",
        facilityId:
          "11111111-1111-1111-1111-111111111111",
      },
      requestContext: null,
      scope: {
        tenantId: "",
        facilityId:
          "11111111-1111-1111-1111-111111111111",
        source: "server-config",
      },
    });
  });
});
