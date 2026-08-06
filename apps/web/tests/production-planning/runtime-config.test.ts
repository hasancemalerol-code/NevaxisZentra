import assert from "node:assert/strict";
import test from "node:test";

import { parseProductionPlanRuntimeConfig } from "../../src/features/production-planning/server/production-plan-runtime-config";

const ORIGINAL_ENV = {
  ...process.env,
};

test.afterEach(() => {
  for (const key of Object.keys(process.env)) {
    if (!(key in ORIGINAL_ENV)) {
      delete process.env[key];
    }
  }

  Object.assign(process.env, ORIGINAL_ENV);
});

function setProcessEnv(
  values: Record<string, string | undefined>,
): void {
  for (const [key, value] of Object.entries(values)) {
    if (value === undefined) {
      delete process.env[key];
      continue;
    }

    process.env[key] = value;
  }
}

test("production defaults to api mode", () => {
  setProcessEnv({
    NODE_ENV: "production",
    CREVAFACTOS_PRODUCTION_PLANNING_RUNTIME_MODE:
      undefined,
    CREVAFACTOS_API_BASE_URL:
      "https://api.example.test",
    CREVAFACTOS_FACILITY_ID:
      "11111111-1111-1111-1111-111111111111",
  });

  const result =
    parseProductionPlanRuntimeConfig();

  assert.equal(result.status, "success");

  if (result.status === "success") {
    assert.equal(result.config.mode, "api");
  }
});

test("production rejects development-preview mode", () => {
  setProcessEnv({
    NODE_ENV: "production",
    CREVAFACTOS_PRODUCTION_PLANNING_RUNTIME_MODE:
      "development-preview",
  });

  const result =
    parseProductionPlanRuntimeConfig();

  assert.equal(result.status, "failure");

  if (result.status === "failure") {
    assert.equal(
      result.code,
      "configuration-error",
    );
  }
});

test("development requires explicit runtime mode", () => {
  setProcessEnv({
    NODE_ENV: "development",
    CREVAFACTOS_PRODUCTION_PLANNING_RUNTIME_MODE:
      undefined,
  });

  const result =
    parseProductionPlanRuntimeConfig();

  assert.equal(result.status, "failure");
});

test("development-preview requires preview scope config", () => {
  setProcessEnv({
    NODE_ENV: "development",
    CREVAFACTOS_PRODUCTION_PLANNING_RUNTIME_MODE:
      "development-preview",
    CREVAFACTOS_DEV_PREVIEW_TENANT_ID:
      undefined,
    CREVAFACTOS_DEV_PREVIEW_FACILITY_ID:
      undefined,
  });

  const result =
    parseProductionPlanRuntimeConfig();

  assert.equal(result.status, "failure");
});

test("development-preview succeeds with explicit preview scope", () => {
  setProcessEnv({
    NODE_ENV: "development",
    CREVAFACTOS_PRODUCTION_PLANNING_RUNTIME_MODE:
      "development-preview",
    CREVAFACTOS_DEV_PREVIEW_TENANT_ID:
      "tenant-crevafactos",
    CREVAFACTOS_DEV_PREVIEW_FACILITY_ID:
      "facility-istanbul",
  });

  const result =
    parseProductionPlanRuntimeConfig();

  assert.equal(result.status, "success");

  if (result.status === "success") {
    assert.equal(
      result.config.mode,
      "development-preview",
    );
  }
});
