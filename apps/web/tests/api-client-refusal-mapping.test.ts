import assert from "node:assert/strict";
import test from "node:test";

import { getMoldWorkspaceSummary } from "../src/features/molds/server/mold-api-client";
import { getMaintenanceWorkspaceSummary } from "../src/features/maintenance/server/maintenance-api-client";
import {
  SESSION_REQUIRED_MESSAGE,
  SUBSCRIPTION_REQUIRED_MESSAGE,
} from "../src/lib/api/api-failure-classifier";

const context = {
  runtimeMode: "api" as const,
  apiBaseUrl: "http://127.0.0.1:5000",
  facilityId: "bb000001-0005-4000-8000-000000000005",
  requestHeaders: { Cookie: "NevaxisZentra.session=test" },
  correlationId: "correlation-test",
};

const realFetch = globalThis.fetch;
function respondWith(status: number, body: unknown) {
  globalThis.fetch = (async () =>
    new Response(body === undefined ? null : JSON.stringify(body), {
      status,
      headers: { "content-type": "application/json" },
    })) as typeof fetch;
}

type Failure = { status: string; code?: string; message?: string };
const cases: ReadonlyArray<Readonly<{ name: string; status: number; body: unknown; code: string; message?: string }>> = [
  { name: "403 licence refusal", status: 403, body: { failureCode: "saas-subscription.entitlement.module-not-entitled" }, code: "subscription-required", message: SUBSCRIPTION_REQUIRED_MESSAGE },
  { name: "403 subscription missing", status: 403, body: { failureCode: "saas-subscription.entitlement.subscription-not-found" }, code: "subscription-required", message: SUBSCRIPTION_REQUIRED_MESSAGE },
  { name: "403 real denial with a code", status: 403, body: { failureCode: "facility-access-denied" }, code: "permission-denied" },
  { name: "403 real denial without a body", status: 403, body: undefined, code: "permission-denied" },
  { name: "403 expired session", status: 403, body: { failureCode: "session-expired" }, code: "unauthenticated", message: SESSION_REQUIRED_MESSAGE },
  { name: "401", status: 401, body: {}, code: "unauthenticated" },
  { name: "503", status: 503, body: {}, code: "source-unavailable" },
];

for (const [label, call] of [
  ["molds (status-map client)", () => getMoldWorkspaceSummary(context)],
  ["maintenance (inline-403 client)", () => getMaintenanceWorkspaceSummary(context)],
] as const) {
  for (const item of cases) {
    test(`${label}: ${item.name} -> ${item.code}`, async () => {
      respondWith(item.status, item.body);
      try {
        const result = (await call()) as Failure;
        assert.equal(result.status, "failure");
        assert.equal(result.code, item.code);
        if (item.message) assert.equal(result.message, item.message);
        if (item.code !== "permission-denied") {
          assert.doesNotMatch(result.message ?? "", /Authorization denied|Yetki reddedildi/);
        }
      } finally {
        globalThis.fetch = realFetch;
      }
    });
  }
}
