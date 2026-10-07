import assert from "node:assert/strict";
import test from "node:test";

import { mapFailureStatus } from "../src/features/ai-task-center/server/ai-task-api-client";

// Body captured from a real authenticated browser load of a first-account tenant without a subscription.
const subscriptionNotFoundBody = {
  failureCode: "saas-subscription.entitlement.subscription-not-found",
  explanationKey: "saas-subscription.explanation.subscription-not-found",
  subscriptionStatus: null,
  graceState: null,
};

test("a subscription entitlement 403 is not reported as a missing permission", () => {
  assert.equal(mapFailureStatus(403, subscriptionNotFoundBody), "subscription-required");
  assert.equal(
    mapFailureStatus(403, { failureCode: "saas-subscription.entitlement.subscription-inactive" }),
    "subscription-required",
  );
});

test("a genuine authorization 403 stays permission-denied", () => {
  assert.equal(mapFailureStatus(403, null), "permission-denied");
  assert.equal(mapFailureStatus(403, {}), "permission-denied");
  assert.equal(mapFailureStatus(403, { failureCode: "authorization.denied" }), "permission-denied");
});

test("other statuses keep their mapping", () => {
  assert.equal(mapFailureStatus(401, null), "unauthenticated");
  assert.equal(mapFailureStatus(404, null), "not-found");
  assert.equal(mapFailureStatus(500, null), "source-unavailable");
});
