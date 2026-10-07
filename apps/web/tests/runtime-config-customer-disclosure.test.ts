import assert from "node:assert/strict";
import test from "node:test";

import { parseProductionExecutionRuntimeConfig } from "../src/features/production-execution/server/production-order-runtime-config";
import { parseExportRuntimeConfig } from "../src/features/export/server/export-case-runtime-config";
import { resolveSaasIdentityRuntimeConfig } from "../src/features/saas-identity/server/saas-identity-runtime-config";
import { parseProductionPlanRuntimeConfig } from "../src/features/production-planning/server/production-plan-runtime-config";

const ORIGINAL_ENV = { ...process.env };
const originalConsoleError = console.error;
let loggedMessages: string[] = [];

test.beforeEach(() => {
  loggedMessages = [];
  console.error = (...args: unknown[]) => {
    loggedMessages.push(args.map(String).join(" "));
  };
});

test.afterEach(() => {
  console.error = originalConsoleError;
  for (const key of Object.keys(process.env)) {
    if (!(key in ORIGINAL_ENV)) {
      delete process.env[key];
    }
  }
  Object.assign(process.env, ORIGINAL_ENV);
});

function setEnv(values: Record<string, string | undefined>): void {
  for (const [key, value] of Object.entries(values)) {
    if (value === undefined) {
      delete process.env[key];
    } else {
      process.env[key] = value;
    }
  }
}

function assertCustomerSafe(message: string): void {
  assert.doesNotMatch(message, /NEVAXIS_[A-Z_]+/);
  assert.doesNotMatch(message, /RUNTIME_MODE|API_BASE_URL|FACILITY_ID/);
  assert.doesNotMatch(message, /\b[A-Z][A-Z0-9]*_[A-Z0-9_]+\b/);
}

test("production planning config failure does not leak env names to customers", () => {
  setEnv({
    NODE_ENV: "production",
    NEVAXIS_FACTOS_PRODUCTION_PLANNING_RUNTIME_MODE: undefined,
    NEVAXIS_FACTOS_API_BASE_URL: undefined,
    NEVAXIS_FACTOS_FACILITY_ID: undefined,
  });

  const result = parseProductionPlanRuntimeConfig();

  assert.equal(result.status, "failure");
  if (result.status === "failure") {
    assertCustomerSafe(result.message);
  }
  assert.ok(
    loggedMessages.some((line) => line.includes("NEVAXIS_FACTOS_")),
    "technical detail must still be logged server-side",
  );
});

test("production execution config failure does not leak env names to customers", () => {
  setEnv({
    NODE_ENV: "production",
    NEVAXIS_FACTOS_PRODUCTION_EXECUTION_RUNTIME_MODE: undefined,
    NEVAXIS_FACTOS_API_BASE_URL: undefined,
    NEVAXIS_FACTOS_FACILITY_ID: undefined,
  });

  const result = parseProductionExecutionRuntimeConfig();

  assert.equal(result.status, "failure");
  if (result.status === "failure") {
    assertCustomerSafe(result.message);
  }
  assert.ok(
    loggedMessages.some((line) => line.includes("NEVAXIS_FACTOS_")),
    "technical detail must still be logged server-side",
  );
});

test("invalid runtime mode failure is customer-safe in both configs", () => {
  setEnv({
    NODE_ENV: "development",
    NEVAXIS_FACTOS_PRODUCTION_PLANNING_RUNTIME_MODE: "not-a-mode",
    NEVAXIS_FACTOS_PRODUCTION_EXECUTION_RUNTIME_MODE: "not-a-mode",
  });

  for (const result of [
    parseProductionPlanRuntimeConfig(),
    parseProductionExecutionRuntimeConfig(),
  ]) {
    assert.equal(result.status, "failure");
    if (result.status === "failure") {
      assertCustomerSafe(result.message);
    }
  }
});

test("export and sign-in runtime config failures are customer-safe", () => {
  setEnv({
    NODE_ENV: "production",
    NEVAXIS_FACTOS_EXPORT_RUNTIME_MODE: undefined,
    NEVAXIS_FACTOS_API_BASE_URL: undefined,
    NEVAXIS_FACTOS_FACILITY_ID: undefined,
  });

  for (const result of [
    parseExportRuntimeConfig(),
    resolveSaasIdentityRuntimeConfig(),
  ]) {
    assert.equal(result.status, "failure");
    if (result.status === "failure") {
      assertCustomerSafe(result.message);
    }
  }
});
