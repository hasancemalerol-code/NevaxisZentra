import assert from "node:assert/strict";
import test from "node:test";

import {
  buildDetailUrl,
  buildListUrl,
} from "../../src/features/production-planning/server/production-plan-api-client";
import {
  mapHttpStatusToProductionPlanApiFailure,
  parseProductionPlanApiProblemDetails,
} from "../../src/features/production-planning/server/production-plan-api-errors";

const requestContext = {
  runtimeMode: "development-api" as const,
  apiBaseUrl: "https://api.example.test/",
  facilityId:
    "11111111-1111-1111-1111-111111111111",
  authenticationMethod:
    "development-headers" as const,
  correlationId: "correlation-id",
  requestHeaders: {
    "X-CrevaFactOS-Dev-Issuer": "issuer",
    "X-CrevaFactOS-Dev-Subject": "subject",
    "X-CrevaFactOS-Tenant": "tenant",
  },
};

test("buildListUrl includes facilityId and normalized query params", () => {
  const url = buildListUrl(
    requestContext,
    {
      facilityId:
        "11111111-1111-1111-1111-111111111111",
      status: " approved ",
      search: " connector ",
      limit: 12.8,
    },
  );

  assert.match(
    url,
    /^https:\/\/api\.example\.test\/api\/production-planning\/plans\?/,
  );
  assert.match(url, /facilityId=11111111-1111-1111-1111-111111111111/);
  assert.match(url, /status=approved/);
  assert.match(url, /search=connector/);
  assert.match(url, /limit=12/);
});

test("buildDetailUrl includes encoded productionPlanId and facilityId", () => {
  const url = buildDetailUrl(
    requestContext,
    "22222222-2222-2222-2222-222222222222",
    "11111111-1111-1111-1111-111111111111",
  );

  assert.equal(
    url,
    "https://api.example.test/api/production-planning/plans/22222222-2222-2222-2222-222222222222?facilityId=11111111-1111-1111-1111-111111111111",
  );
});

test("HTTP status codes map to safe failure codes", () => {
  assert.equal(
    mapHttpStatusToProductionPlanApiFailure(
      401,
      null,
    ).code,
    "unauthenticated",
  );

  assert.equal(
    mapHttpStatusToProductionPlanApiFailure(
      403,
      parseProductionPlanApiProblemDetails({
        extensions: {
          failureCode: "permission-denied",
        },
      }),
    ).code,
    "permission-denied",
  );

  assert.equal(
    mapHttpStatusToProductionPlanApiFailure(
      404,
      null,
    ).code,
    "not-found",
  );

  assert.equal(
    mapHttpStatusToProductionPlanApiFailure(
      503,
      null,
    ).code,
    "source-unavailable",
  );
});

test("problem details parser ignores raw response body fields in failures", () => {
  const failure =
    mapHttpStatusToProductionPlanApiFailure(
      500,
      parseProductionPlanApiProblemDetails({
        title: "Database exploded with secret token",
        extensions: {
          failureCode: "unexpected-error",
        },
      }),
    );

  assert.equal(failure.code, "unexpected-error");
  assert.doesNotMatch(
    failure.message,
    /secret token/,
  );
});
