import assert from "node:assert/strict";
import test from "node:test";

import {
  resolveProductionPlanRequestContext,
  serializeProductionPlanRequestContext,
} from "../../src/features/production-planning/server/production-plan-request-context";
import { resolveProductionPlanningServerScope } from "../../src/features/production-planning/server/production-plan-scope";

test("api mode without auth bridge fails as unauthenticated", () => {
  const config = {
    mode: "api" as const,
    apiBaseUrl: "https://api.example.test",
    facilityId:
      "11111111-1111-1111-1111-111111111111",
  };

  const scopeResult =
    resolveProductionPlanningServerScope(
      config,
    );

  assert.equal(
    scopeResult.status,
    "success",
  );

  if (scopeResult.status !== "success") {
    return;
  }

  const contextResult =
    resolveProductionPlanRequestContext(
      config,
      scopeResult.scope,
    );

  assert.equal(
    contextResult.status,
    "failure",
  );

  if (contextResult.status === "failure") {
    assert.equal(
      contextResult.code,
      "unauthenticated",
    );
  }
});

test("development-api mode uses server-side development headers", () => {
  const config = {
    mode: "development-api" as const,
    apiBaseUrl: "https://api.example.test",
    facilityId:
      "11111111-1111-1111-1111-111111111111",
    developmentAuth: {
      issuer: "dev-issuer",
      subject: "dev-subject",
      tenantKey: "tenant-key",
    },
  };

  const scopeResult =
    resolveProductionPlanningServerScope(
      config,
    );

  assert.equal(
    scopeResult.status,
    "success",
  );

  if (scopeResult.status !== "success") {
    return;
  }

  const contextResult =
    resolveProductionPlanRequestContext(
      config,
      scopeResult.scope,
    );

  assert.equal(
    contextResult.status,
    "success",
  );

  if (contextResult.status === "success") {
    assert.equal(
      contextResult.context.requestHeaders[
        "X-crevafactos-dev-Issuer"
      ],
      "dev-issuer",
    );
    assert.equal(
      contextResult.context.requestHeaders[
        "X-NevaxisFactOS-Tenant"
      ],
      "tenant-key",
    );

    const serialized =
      serializeProductionPlanRequestContext(
        contextResult.context,
      );

    assert.doesNotMatch(
      serialized,
      /dev-subject/,
    );
    assert.doesNotMatch(
      serialized,
      /dev-issuer/,
    );
  }
});

test("missing facility scope fails explicitly", () => {
  const config = {
    mode: "api" as const,
    apiBaseUrl: "https://api.example.test",
  };

  const scopeResult =
    resolveProductionPlanningServerScope(
      config,
    );

  assert.equal(
    scopeResult.status,
    "failure",
  );
});
